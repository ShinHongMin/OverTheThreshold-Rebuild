using NUnit.Framework;

public class PassiveCardTests
{
    private static RunState MakeRun(float mapRoll = 0f)
    {
        var cards = new PassiveCardRules();
        cards.RegisterStatBonus("ATK", StatType.ATK, .1f);
        cards.RegisterStatBonus("DEF", StatType.DEF, .1f);
        cards.RegisterStatBonus("HP", StatType.MaxHP, .05f);
        return new RunState(new[] {
            new Unit("Aegis", new UnitStats(100, 20, 20)),
            new Unit("Claire", new UnitStats(100, 30, 10)),
            new Unit("Seiren", new UnitStats(100, 40, 15), CharacterJob.Resonance)
        }, MapGenerator.Generate(new ScriptedRandom(mapRoll)), cards);
    }

    [Test]
    public void 같은_카드는_합산하고_파티_전체의_기본값과_HP는_보존한다()
    {
        var run = MakeRun();
        run.Party[2].ApplyDamage(1000);
        run.AddCard("ATK"); run.AddCard("ATK"); run.AddCard("DEF");
        foreach (var member in run.Party)
        {
            Assert.AreEqual(member.Stats.GetBase(StatType.ATK) * 1.2f, member.Stats.Get(StatType.ATK), .0001f);
            Assert.AreEqual(member.Stats.GetBase(StatType.DEF) * 1.1f, member.Stats.Get(StatType.DEF), .0001f);
            Assert.AreEqual(100, member.MaxHP);
        }
        Assert.IsFalse(run.Party[2].IsAlive);
        Assert.AreEqual(3, run.Cards.Count);
    }

    [Test]
    public void 보상_재확정은_카드와_능력치를_중복_지급하지_않는다()
    {
        var run = MakeRun(); run.TryEnter(1);
        run.PrepareBattleReward(new[] { "ATK", "DEF" }, new ScriptedRandom(0f));
        Assert.IsTrue(run.TryClaimBattleReward("ATK"));
        Assert.IsFalse(run.TryClaimBattleReward("ATK"));
        Assert.IsFalse(run.TryClaimBattleReward("DEF"));
        Assert.AreEqual(1, run.Cards.Count);
        Assert.AreEqual(22, run.Party[0].Stats.Get(StatType.ATK), .0001f);
        Assert.AreEqual(20, run.Party[0].Stats.Get(StatType.DEF));
        Assert.IsTrue(run.IsCleared(1));
    }

    [Test]
    public void 다음_전투와_초기화_재호출에서_카드_보정은_유지되고_적에게_적용되지_않는다()
    {
        var run = MakeRun(); run.AddCard("ATK"); run.AddCard("DEF"); run.TryEnter(1);
        var enemy = new Unit("Enemy", new UnitStats(100, 20, 20), usesResources: false);
        for (int index = 0; index < 3; index++)
        {
            var context = run.BeginCombat(new[] { enemy });
            Assert.AreSame(run.Party[0], context.Party[0]);
            Assert.AreEqual(22, context.Party[0].Stats.Get(StatType.ATK), .0001f);
            Assert.AreEqual(22, context.Party[0].Stats.Get(StatType.DEF), .0001f);
            Assert.AreEqual(20, enemy.Stats.Get(StatType.ATK));
        }
        run.CompleteNode(); run.TryEnter(3); run.BeginCombat(new[] { enemy });
        Assert.AreEqual(2, run.Party[0].Stats.Modifiers.Count);
    }

    [Test]
    public void 영구_보너스와_전투_버프를_함께_계산하고_전투_종료는_버프만_제거한다()
    {
        var run = MakeRun(); var member = run.Party[0];
        member.Stats.AddModifier(new StatModifier(StatType.ATK, .2f, ModSource.MetaProgress));
        run.AddCard("ATK");
        member.Stats.AddBuff("SkillBuff", BuffType.ATK_Percent, .3f, 2);
        Assert.AreEqual(32, member.Stats.Get(StatType.ATK), .0001f);
        member.ResetCombatState();
        Assert.AreEqual(26, member.Stats.Get(StatType.ATK), .0001f);
        Assert.AreEqual(2, member.Stats.Modifiers.Count);
    }

    [Test]
    public void 공격력과_방어력_카드가_실제_피해_및_공격력_비례_회복에_반영된다()
    {
        var run = MakeRun(); var member = run.Party[0];
        var enemy = new Unit("Enemy", new UnitStats(100, 20, 0), usesResources: false);
        float incoming = DamageCalculator.Calculate(enemy, member, 1, 1);
        run.AddCard("ATK"); run.AddCard("DEF");
        var context = new CombatContext(run.Party, new[] { enemy });
        new UseSkillCommand(member, enemy, TestSkills.Damage(1)).Execute(context);
        Assert.AreEqual(78, enemy.CurrentHP, .0001f);
        Assert.Less(DamageCalculator.Calculate(enemy, member, 1, 1), incoming);
        member.ApplyDamage(50);
        var heal = new SkillSpec("Heal", "", SkillType.Special, TargetType.Ally, 0, 0, 0, new SkillEffect[] { new HealEffect(1) });
        new UseSkillCommand(member, member, heal).Execute(context);
        Assert.AreEqual(72, member.CurrentHP, .0001f);
    }

    [Test]
    public void 새_런은_이전_카드와_보정이_없다()
    {
        var previous = MakeRun(); previous.AddCard("ATK"); previous.AddCard("DEF"); previous.AddCard("HP"); previous.Fail();
        var fresh = MakeRun();
        Assert.AreEqual(0, fresh.Cards.Count);
        Assert.AreEqual(20, fresh.Party[0].Stats.Get(StatType.ATK));
        Assert.AreEqual(100, fresh.Party[0].MaxHP);
        Assert.AreEqual(100, fresh.Party[0].CurrentHP);
        Assert.AreEqual(0, fresh.Party[0].Stats.Modifiers.Count);
        Assert.AreNotSame(previous.Party[0], fresh.Party[0]);
    }

    [Test]
    public void 미연결_효과의_카드는_보유만_기록하고_능력치를_바꾸지_않는다()
    {
        var run = MakeRun(); run.AddCard("UNCONNECTED_CARD"); run.AddCard("");
        Assert.AreEqual(1, run.Cards.Count);
        Assert.AreEqual(0, run.Party[0].Stats.Modifiers.Count);
        Assert.AreEqual(100, run.Party[0].MaxHP);
    }

    [Test]
    public void 최대_HP_카드는_합산하며_만피와_부상과_사망_상태의_현재_HP를_유지한다()
    {
        var run = MakeRun();
        run.Party[1].ApplyDamage(40);
        run.Party[2].ApplyDamage(1000);
        for (int cardCount = 1; cardCount <= 2; cardCount++)
        {
            run.AddCard("HP");
            foreach (var member in run.Party)
            {
                Assert.AreEqual(100 + cardCount * 5, member.MaxHP, .0001f);
                Assert.AreEqual(100, member.Stats.GetBase(StatType.MaxHP));
                Assert.AreEqual(member.Stats.GetBase(StatType.ATK), member.Stats.Get(StatType.ATK));
            }
            Assert.AreEqual(100, run.Party[0].CurrentHP);
            Assert.AreEqual(60, run.Party[1].CurrentHP);
            Assert.AreEqual(0, run.Party[2].CurrentHP);
            Assert.IsFalse(run.Party[2].IsAlive);
        }
    }

    [Test]
    public void 늘어난_최대_HP까지_회복과_실드가_가능하고_다음_전투에서_HP를_보존한다()
    {
        var run = MakeRun(); var member = run.Party[0];
        run.AddCard("HP"); run.AddCard("HP"); run.TryEnter(1);
        var enemy = new Unit("Enemy", new UnitStats(100, 20, 20), usesResources: false);
        var context = run.BeginCombat(new[] { enemy });
        var heal = new SkillSpec("Heal", "", SkillType.Special, TargetType.Ally, 0, 0, 0, new SkillEffect[] { new HealEffect(10) });
        new UseSkillCommand(member, member, heal).Execute(context);
        Assert.AreEqual(110, member.CurrentHP, .0001f);
        member.AddShield(1000, 2);
        Assert.AreEqual(110, member.CurrentShield, .0001f);
        run.CompleteNode(); run.TryEnter(3); run.BeginCombat(new[] { enemy });
        Assert.AreEqual(110, member.MaxHP, .0001f);
        Assert.AreEqual(110, member.CurrentHP, .0001f);
        Assert.AreEqual(0, member.CurrentShield);
        Assert.AreEqual(2, member.Stats.Modifiers.Count);
        Assert.AreEqual(100, enemy.MaxHP);
    }

    [Test]
    public void 이벤트에서_획득한_HP_카드가_이후_비율_회복과_피해의_기준이_된다()
    {
        var run = MakeRun(.9f); var member = run.Party[0];
        member.ApplyDamage(50); run.Party[2].ApplyDamage(1000);
        Assert.IsTrue(run.TryEnter(1));
        Assert.IsTrue(RunEventRules.TryApply(run, new[] {
            new RunEventEffect(EventEffectType.GetRandomPassive, 0, new[] { "HP" }, 1)
        }, new ScriptedRandom(0f)));
        Assert.AreEqual(105, member.MaxHP, .0001f);
        Assert.AreEqual(50, member.CurrentHP);
        Assert.IsTrue(RunEventRules.TryApply(run, new[] { new RunEventEffect(EventEffectType.HealHP_Percent, 20) }));
        Assert.AreEqual(71, member.CurrentHP, .0001f);
        Assert.IsTrue(RunEventRules.TryApply(run, new[] { new RunEventEffect(EventEffectType.DamageHP_Percent, 10) }));
        Assert.AreEqual(60.5f, member.CurrentHP, .0001f);
        Assert.AreEqual(0, run.Party[2].CurrentHP);
    }

    [Test]
    public void 휴식_회복은_카드와_영구_보너스를_합한_최대_HP를_사용한다()
    {
        var run = MakeRun(); var member = run.Party[0];
        member.Stats.AddModifier(new StatModifier(StatType.MaxHP, .2f, ModSource.MetaProgress));
        run.AddCard("HP"); run.AddCard("HP");
        member.ApplyDamage(50); run.Party[2].ApplyDamage(1000);
        foreach (int index in new[] { 1, 4, 7, 10, 13, 16, 18 })
        { Assert.IsTrue(run.TryEnter(index)); run.CompleteNode(); }
        Assert.IsTrue(run.TryEnter(20));
        Assert.IsTrue(RestRules.TryApply(run, RestRules.Action.Heal, null));
        Assert.AreEqual(130, member.MaxHP, .0001f);
        Assert.AreEqual(76, member.CurrentHP, .0001f);
        Assert.IsFalse(RestRules.TryApply(run, RestRules.Action.Heal, null));
        Assert.AreEqual(76, member.CurrentHP, .0001f);
        Assert.AreEqual(0, run.Party[2].CurrentHP);
    }
}
