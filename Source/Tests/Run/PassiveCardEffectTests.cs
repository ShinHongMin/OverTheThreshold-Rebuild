using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class PassiveCardEffectTests
{
    private static Unit Ally(string name = "Ally") => new Unit(name, new UnitStats(100, 20, 10));
    private static Unit Enemy() => new Unit("Enemy", new UnitStats(100, 20, 0), usesResources: false);
    private static void Give(Unit unit, PassiveEffectType type, float value, int count = 1)
    {
        var rules = new PassiveCardRules(); rules.RegisterCard("Card", type, value);
        for (int i = 0; i < count; i++) rules.ApplyToParty("Card", new[] { unit });
    }
    private static CommandResult Cast(CombatContext context, Unit actor, Unit target, params SkillEffect[] effects)
        => new UseSkillCommand(actor, target, new SkillSpec("Test", "", SkillType.Special, TargetType.Ally, 0, 0, 0, effects)).Execute(context);

    [Test]
    public void 주는_회복량은_시전자_보정으로_중첩되고_실제_회복량만_표시한다()
    {
        var caster = Ally(); var target = Ally("Target"); var context = new CombatContext(new[] { caster, target }, new[] { Enemy() });
        Give(caster, PassiveEffectType.IncreaseHeal_Percent, 10, 2);
        Give(target, PassiveEffectType.IncreaseHeal_Percent, 100);
        target.ApplyDamage(50);
        var result = Cast(context, caster, target, new HealEffect(1));
        Assert.AreEqual(74, target.CurrentHP, .0001f);
        Assert.AreEqual(24, result.Events.OfType<HealReceived>().Single().Amount, .0001f);
        result = Cast(context, caster, target, new HealEffect(10));
        Assert.AreEqual(26, result.Events.OfType<HealReceived>().Single().Amount, .0001f);
        target.ApplyDamage(1000); Cast(context, caster, target, new HealEffect(10));
        Assert.IsFalse(target.IsAlive);
    }

    [TestCase(false)] [TestCase(true)]
    public void 주는_실드는_시전자_보정을_사용하고_기준_스탯과_상한을_지킨다(bool hpBased)
    {
        var caster = Ally(); var target = Ally("Target"); var context = new CombatContext(new[] { caster, target }, new[] { Enemy() });
        Give(caster, PassiveEffectType.IncreaseShield_Percent, 10, 2);
        Give(target, PassiveEffectType.IncreaseShield_Percent, 100);
        var result = Cast(context, caster, target, new ShieldEffect(hpBased ? .2f : 2, 2, hpBased));
        Assert.AreEqual(24, target.CurrentShield, .0001f);
        Assert.AreEqual(24, result.Events.OfType<ShieldGranted>().Single().Amount, .0001f);
        result = Cast(context, caster, target, new ShieldEffect(100, 2));
        Assert.AreEqual(76, result.Events.OfType<ShieldGranted>().Single().Amount, .0001f);
        Assert.AreEqual(100, target.CurrentShield);
    }

    [Test]
    public void 시작_카드는_전투당_한번이며_자원은_파티_전체에_한번만_지급한다()
    {
        var rules = new PassiveCardRules();
        rules.RegisterCard("SP", PassiveEffectType.BattleStart_SP, 1);
        rules.RegisterCard("OP", PassiveEffectType.BattleStart_OP, 2);
        rules.RegisterCard("Heal", PassiveEffectType.BattleStart_Heal, 5);
        rules.RegisterCard("Shield", PassiveEffectType.BattleStart_Shield, 5);
        var party = new[] { Ally(), Ally("B"), Ally("Dead") };
        var run = new RunState(party, MapGenerator.Generate(new ScriptedRandom(0)), rules);
        foreach (var unit in party) { unit.ApplyDamage(50); Give(unit, PassiveEffectType.IncreaseMaxHP_Percent, 5, 2); Give(unit, PassiveEffectType.IncreaseHeal_Percent, 100); Give(unit, PassiveEffectType.IncreaseShield_Percent, 100); }
        party[2].ApplyDamage(1000);
        foreach (string id in new[] { "SP", "OP", "Heal", "Shield" }) { run.AddCard(id); run.AddCard(id); }
        Assert.AreEqual(50, party[0].CurrentHP); Assert.AreEqual(0, party[0].CurrentShield);
        run.AddStartingResources(1, 1); run.TryEnter(1);
        var context = run.BeginCombat(new[] { Enemy() }); var loop = new TurnLoop();
        Assert.AreEqual(7, context.Resources.CurrentSP); Assert.AreEqual(6, context.Resources.CurrentOP);
        var events = loop.BeginTurn(context);
        Assert.AreEqual(61, party[0].CurrentHP, .0001f); Assert.AreEqual(11, party[0].CurrentShield, .0001f);
        Assert.AreEqual(2, events.OfType<HealReceived>().Count()); Assert.AreEqual(2, events.OfType<ShieldGranted>().Count());
        loop.BeginTurn(context); Assert.AreEqual(61, party[0].CurrentHP, .0001f);
        loop.ExecuteTurn(context); Assert.AreEqual(1, party[0].ShieldDuration);
        loop.BeginTurn(context); loop.ExecuteTurn(context); Assert.AreEqual(0, party[0].CurrentShield);
        Assert.AreEqual(0, party[2].CurrentHP); Assert.AreEqual(0, party[2].CurrentShield);
        run.CompleteNode(); run.TryEnter(3); context = run.BeginCombat(new[] { Enemy() });
        new TurnLoop().BeginTurn(context); Assert.AreEqual(72, party[0].CurrentHP, .0001f);
    }

    [Test]
    public void 시작_카드는_진행중_전투에서_얻으면_다음_전투부터_발동한다()
    {
        var unit = Ally(); unit.ApplyDamage(50);
        var context = new CombatContext(new[] { unit }, new[] { Enemy() }); var loop = new TurnLoop(); loop.BeginTurn(context);
        Give(unit, PassiveEffectType.BattleStart_Heal, 5); loop.BeginTurn(context);
        Assert.AreEqual(50, unit.CurrentHP);
        new TurnLoop().BeginTurn(new CombatContext(new[] { unit }, new[] { Enemy() })); Assert.AreEqual(55, unit.CurrentHP);
    }

    [Test]
    public void 정밀_타격은_매_타격의_HP로_판정하며_실드만_맞으면_조건이_유지된다()
    {
        var actor = Ally(); var target = Enemy(); var context = new CombatContext(new[] { actor }, new[] { target });
        Give(actor, PassiveEffectType.Damage_FullHP, 20);
        target.AddShield(50, 2);
        Cast(context, actor, target, new DamageEffect(1)); Assert.AreEqual(26, target.CurrentShield, .0001f); Assert.AreEqual(100, target.CurrentHP);
        Cast(context, actor, target, new DamageEffect(1)); Assert.AreEqual(2, target.CurrentShield, .0001f);
        Cast(context, actor, target, new DamageEffect(1)); Assert.AreEqual(78, target.CurrentHP, .0001f);
        Cast(context, actor, target, new DamageEffect(1)); Assert.AreEqual(58, target.CurrentHP, .0001f);
    }

    [Test]
    public void 약점_포착은_디버프_표시를_사용하고_증폭은_합산하며_아군_오사는_제외한다()
    {
        var actor = Ally(); var target = Enemy(); var context = new CombatContext(new[] { actor }, new[] { target });
        Give(actor, PassiveEffectType.Damage_FullHP, 20); Give(actor, PassiveEffectType.Damage_Debuffed, 20, 2);
        target.Stats.AddBuff("Ordinary", BuffType.ATK_Percent, .1f, 2);
        Assert.IsFalse(target.Stats.HasDebuff);
        target.Stats.AddBuff("Mark", BuffType.Damage_Taken_Percent, .3f, 1, true);
        Cast(context, actor, target, new DamageEffect(1)); Assert.AreEqual(62, target.CurrentHP, .0001f);
        target.Stats.TickBuffDurations(); Assert.IsFalse(target.Stats.HasDebuff);
        Cast(context, actor, target, new DamageEffect(1)); Assert.AreEqual(42, target.CurrentHP, .0001f);
        var ally = Ally("Friend"); context = new CombatContext(new[] { actor, ally }, new[] { target });
        float expected = DamageCalculator.Calculate(actor, ally, 1);
        var damage = Cast(context, actor, ally, new DamageEffect(1)).Events.OfType<DamageDealt>().Single();
        Assert.AreEqual(expected, damage.Amount, .0001f);
    }

    [Test]
    public void 디버프_갱신은_분류도_갱신하고_보스_그로기처럼_음수_방어_효과를_판별한다()
    {
        var unit = Ally(); unit.Stats.AddBuff("Groggy", BuffType.DEF_Percent, -.5f, 2, true);
        Assert.IsTrue(unit.Stats.HasDebuff);
        unit.Stats.AddBuff("Groggy", BuffType.DEF_Percent, .5f, 2, false); Assert.IsFalse(unit.Stats.HasDebuff);
    }

    [Test]
    public void 침착한_대응은_외부_ER만_일반_저항_후_감소하고_100퍼센트에서_막는다()
    {
        var unit = new Unit("Ally", new UnitStats(100, 20, 10, .2f));
        Give(unit, PassiveEffectType.ReceivedER_Reduction, 10, 2);
        EchoRules.ApplyER(unit, 25, null); Assert.AreEqual(16, unit.CurrentER, .0001f);
        EchoRules.GainOwnER(unit, 25, null); Assert.AreEqual(41, unit.CurrentER, .0001f);
        Give(unit, PassiveEffectType.ReceivedER_Reduction, 10, 20);
        EchoRules.ApplyER(unit, 1000, null); Assert.AreEqual(41, unit.CurrentER, .0001f);
    }

    [TestCase(.249f, 50f, 0)] [TestCase(.25f, 100f, 4)]
    public void 불굴_판정은_진입_전에_하며_황혼은_실제_진입시에만_발동한다(float roll, float expectedER, int expectedOP)
    {
        var unit = Ally(); var resources = new PartyResources(); resources.TrySpendOP(resources.CurrentOP);
        Give(unit, PassiveEffectType.Echo_Resistance, 25); Give(unit, PassiveEffectType.Echo_OP, 4);
        unit.SetER(90); var events = new List<CombatEvent>(); var random = new CountingRandom(roll);
        EchoRules.ApplyER(unit, 20, events, random, resources);
        Assert.AreEqual(expectedER, unit.CurrentER); Assert.AreEqual(expectedOP, resources.CurrentOP); Assert.AreEqual(1, random.Draws);
        Assert.AreEqual(90, events.OfType<ResourceChanged>().First().Before); Assert.AreEqual(expectedER, events.OfType<ResourceChanged>().First().After);
        if (expectedER == 100)
        {
            EchoRules.ApplyER(unit, 50, events, random, resources); EchoRules.SpendER(unit, 50, events); EchoRules.ReduceER(unit, 50, events);
            Assert.AreEqual(100, unit.CurrentER); Assert.AreEqual(4, resources.CurrentOP); Assert.AreEqual(1, random.Draws);
            EchoRules.CureEcho(unit, events); EchoRules.GainOwnER(unit, 100, events, random, resources);
            Assert.AreEqual(6, resources.CurrentOP); Assert.AreEqual(2, random.Draws);
        }
    }

    [Test]
    public void 불굴_확률_중첩은_상한을_지키고_이미_ECHO인_상태를_해제하지_않는다()
    {
        var unit = Ally(); Give(unit, PassiveEffectType.Echo_Resistance, 25, 2); unit.SetER(90);
        EchoRules.GainOwnER(unit, 20, null, new ScriptedRandom(.49f)); Assert.AreEqual(50, unit.CurrentER);
        Give(unit, PassiveEffectType.Echo_Resistance, 25, 3); EchoRules.GainOwnER(unit, 100, null); Assert.AreEqual(50, unit.CurrentER);
        unit.SetER(100); EchoRules.GainOwnER(unit, 100, null); Assert.AreEqual(100, unit.CurrentER);
    }

    [Test]
    public void 황혼_중복은_공용_OP_상한을_지키며_맵_저항은_다음_전투_OP로_이월하지_않는다()
    {
        var unit = Ally(); Give(unit, PassiveEffectType.Echo_OP, 4, 2); var resources = new PartyResources();
        EchoRules.GainOwnER(unit, 100, null, null, resources); Assert.AreEqual(6, resources.CurrentOP);
        var rules = new PassiveCardRules(); rules.RegisterCard("Resist", PassiveEffectType.Echo_Resistance, 25); rules.RegisterCard("OP", PassiveEffectType.Echo_OP, 4);
        var run = new RunState(new[] { Ally(), Ally("B"), Ally("C") }, MapGenerator.Generate(new ScriptedRandom(.9f)), rules, new ScriptedRandom(0));
        run.AddCard("Resist"); run.AddCard("OP"); run.TryEnter(1);
        Assert.IsTrue(RunEventRules.TryApply(run, new[] { new RunEventEffect(EventEffectType.IncreaseER, 100) }));
        foreach (var member in run.Party) Assert.AreEqual(50, member.CurrentER);
        Assert.AreEqual(0, run.BonusStartOP);
        var fresh = Ally(); Assert.AreEqual(0, fresh.Stats.CardBonuses.EchoResistanceChance); Assert.AreEqual(0, fresh.Stats.CardBonuses.EchoOPGain);
    }

    [Test]
    public void 스킬_예약은_ER_카드를_발동하지_않고_실행에서_난수와_OP를_전달한다()
    {
        var actor = new Unit("Seiren", new UnitStats(100, 20, 10), CharacterJob.Resonance);
        var enemy = Enemy(); var random = new CountingRandom(.99f);
        var context = new CombatContext(new[] { actor, Ally("Other") }, new[] { enemy }, cardRandom: random);
        Give(actor, PassiveEffectType.Echo_Resistance, 25); Give(actor, PassiveEffectType.Echo_OP, 4); actor.SetER(90);
        var skill = new SkillSpec("OwnER", "", SkillType.Basic, TargetType.SingleEnemy, 0, 0, 0,
            new SkillEffect[] { new ERGainEffect(20, EffectTarget.Self) });
        var loop = new TurnLoop();
        Assert.IsTrue(loop.TryReserve(new UseSkillCommand(actor, enemy, skill), context, out var reserved));
        Assert.AreEqual(90, actor.CurrentER); Assert.AreEqual(2, context.Resources.CurrentOP); Assert.AreEqual(0, random.Draws);
        var result = loop.ExecuteTurn(context);
        Assert.AreEqual(100, actor.CurrentER); Assert.AreEqual(6, context.Resources.CurrentOP); Assert.AreEqual(1, random.Draws);
        Assert.AreEqual(1, result.Events.OfType<ResourceChanged>().Count(e => e.Kind == ResourceKind.OP));
    }

    [Test]
    public void 보스의_패시브_ER도_침착과_불굴과_황혼을_정상적으로_거친다()
    {
        var ally = Ally(); var boss = Enemy();
        Give(ally, PassiveEffectType.ReceivedER_Reduction, 10); Give(ally, PassiveEffectType.Echo_Resistance, 25); Give(ally, PassiveEffectType.Echo_OP, 4);
        var table = new SkillTable(new[] { TestSkills.Make("Basic", new DamageEffect(1)) });
        var brain = new AvatarBrain(new ScriptedRandom(0), table, new MonsterBrainConfig("Basic"), new AvatarBrainConfig("", "", "", 10));
        var context = new CombatContext(new[] { ally }, new[] { boss }, cardRandom: new ScriptedRandom(0));
        ally.SetER(90); brain.Decide(context, boss); Assert.AreEqual(99, ally.CurrentER, .0001f);
        brain.Decide(context, boss); Assert.AreEqual(50, ally.CurrentER); Assert.AreEqual(2, context.Resources.CurrentOP);
        var events = new List<CombatEvent>(); brain.AppendPendingEvents(events); events.Clear();
        context = new CombatContext(new[] { ally }, new[] { boss }, cardRandom: new ScriptedRandom(.99f));
        ally.SetER(95); brain.Decide(context, boss); brain.AppendPendingEvents(events);
        Assert.AreEqual(100, ally.CurrentER); Assert.AreEqual(6, context.Resources.CurrentOP);
        Assert.IsTrue(events.OfType<ResourceChanged>().Any(e => e.Kind == ResourceKind.OP));
    }

    [Test]
    public void 보스_실드_파괴_직후의_그로기는_다음_타격부터_약점_포착을_적용한다()
    {
        var actor = Ally(); var boss = Enemy(); boss.ApplyDamage(25);
        Give(actor, PassiveEffectType.Damage_Debuffed, 20);
        var context = new CombatContext(new[] { actor }, new[] { boss });
        var table = new SkillTable(new[] {
            TestSkills.Make("Basic", new DamageEffect(1)),
            TestSkills.Make("Charge", SkillType.Basic, TargetType.Self, new ShieldEffect(.1f, 4, true) { effectTarget = EffectTarget.Self }),
            TestSkills.Make("Groggy", SkillType.Basic, TargetType.Self, new BuffEffect("Groggy", BuffType.DEF_Percent, -.5f, 2, true) { effectTarget = EffectTarget.Self })
        });
        var brain = new AvatarBrain(new ScriptedRandom(0), table, new MonsterBrainConfig("Basic"), new AvatarBrainConfig("Charge", "Basic", "Groggy"));
        brain.Decide(context, boss).Execute(context);
        var first = Cast(context, actor, boss, new DamageEffect(1)).Events.OfType<DamageDealt>().Single();
        Assert.AreEqual(20, first.Amount, .0001f); brain.OnEvent(first);
        Assert.IsTrue(brain.IsGroggy); Assert.IsTrue(boss.Stats.HasDebuff);
        var next = Cast(context, actor, boss, new DamageEffect(1)).Events.OfType<DamageDealt>().Single();
        Assert.AreEqual(24, next.Amount, .0001f);
    }

    private sealed class CountingRandom : IRandom
    {
        private readonly float _value;
        public int Draws { get; private set; }
        public CountingRandom(float value) { _value = value; }
        public float Value01() { Draws++; return _value; }
        public int Range(int minInclusive, int maxExclusive) => minInclusive;
    }
}
