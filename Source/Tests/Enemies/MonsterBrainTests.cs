using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// 몬스터별 스킬·대상 선택 규칙을 검증한다.
/// </summary>
public class MonsterBrainTests
{
    private const float Tolerance = 0.01f;

    private const string ChargeBuff = "Buff_Charge";
    private const string MarkBuff = "Buff_VoidHunter_Mark";

    private Unit _aegis, _claire;
    private Unit _monster;
    private CombatContext _ctx;
    private SkillTable _table;

    [SetUp]
    public void SetUp()
    {
        _aegis   = new Unit("Aegis",  new UnitStats(150f, 10f, 0f));
        _claire  = new Unit("Claire", new UnitStats(100f, 15f, 0f));
        _monster = new Unit("Monster", new UnitStats(60f, 10f, 0f));

        _ctx = new CombatContext(new[] { _aegis, _claire }, new[] { _monster });

        _table = new SkillTable(new[]
        {
            TestSkills.Make("BASIC", new DamageEffect(1f)),
            TestSkills.Make("SPECIAL", new DamageEffect(2f)),
            TestSkills.Make("ENHANCED", new DamageEffect(3f)),
            TestSkills.Make("AOE", SkillType.Basic, TargetType.AllEnemy, new DamageEffect(1f)),
            TestSkills.Make("CHARGE", SkillType.Basic, TargetType.Self,
                new BuffEffect(ChargeBuff, BuffType.ATK_Percent, 0.5f, 2)
                    { effectTarget = EffectTarget.Self })
        });
    }

    private static MonsterBrainConfig Config(
        string basic = "BASIC", string[] skills = null, float chance = 0.5f,
        string enhanced = null, string stateBuff = null)
        => new MonsterBrainConfig(basic, skills, chance, enhanced, stateBuff);

    private static string SkillOf(ICombatCommand command)
        => ((UseSkillCommand)command).Skill.SkillId;

    // ── 일반 몬스터 ──────────────────────────────────────────────

    [Test]
    public void 확률에_걸리면_보유_스킬을_쓴다()
    {
        var brain = new LostScoutBrain(new ScriptedRandom(0.1f, 0f, 0f), _table,
            Config(skills: new[] { "SPECIAL" }));

        Assert.AreEqual("SPECIAL", SkillOf(brain.Decide(_ctx, _monster)));
    }

    [Test]
    public void 확률에_걸리지_않으면_기본공격을_쓴다()
    {
        var brain = new LostScoutBrain(new ScriptedRandom(0.9f, 0f), _table,
            Config(skills: new[] { "SPECIAL" }));

        Assert.AreEqual("BASIC", SkillOf(brain.Decide(_ctx, _monster)));
    }

    [Test]
    public void 보유_스킬이_없으면_항상_기본공격이다()
    {
        var brain = new WhisperingEchoBrain(new ScriptedRandom(0f, 0f), _table, Config());

        Assert.AreEqual("BASIC", SkillOf(brain.Decide(_ctx, _monster)));
    }

    [Test]
    public void 살아있는_상대_중_하나를_무작위로_고른다()
    {
        // 스킬 굴림 0.9(기본공격) → 대상 굴림 0.9(두 번째 후보 Claire)
        var brain = new LostScoutBrain(new ScriptedRandom(0.9f, 0.9f), _table, Config());

        brain.Decide(_ctx, _monster).Execute(_ctx);

        Assert.AreEqual(150f, _aegis.CurrentHP, Tolerance);
        Assert.Less(_claire.CurrentHP, 100f);
    }

    [Test]
    public void 죽은_상대는_고르지_않는다()
    {
        _aegis.ApplyDamage(999f);

        var brain = new LostScoutBrain(new ScriptedRandom(0.9f, 0f), _table, Config());
        brain.Decide(_ctx, _monster).Execute(_ctx);

        Assert.Less(_claire.CurrentHP, 100f);
    }

    [Test]
    public void 상대가_모두_죽으면_행동하지_않는다()
    {
        _aegis.ApplyDamage(999f);
        _claire.ApplyDamage(999f);

        var brain = new LostScoutBrain(new ScriptedRandom(0.9f, 0f), _table, Config());

        Assert.IsNull(brain.Decide(_ctx, _monster));
    }

    [Test]
    public void 광역_스킬은_대상_지정_없이_전원을_노린다()
    {
        var brain = new CorruptedBulwarkBrain(new ScriptedRandom(0.1f, 0f, 0f), _table,
            Config(skills: new[] { "AOE" }));

        brain.Decide(_ctx, _monster).Execute(_ctx);

        Assert.Less(_aegis.CurrentHP, 150f);
        Assert.Less(_claire.CurrentHP, 100f);
    }

    [Test]
    public void 죽은_몬스터는_행동하지_않는다()
    {
        _monster.ApplyDamage(999f);

        var brain = new LostScoutBrain(new ScriptedRandom(0f, 0f), _table, Config());

        Assert.IsNull(brain.Decide(_ctx, _monster));
    }

    // ── 충전형 (ThresholdMass) ───────────────────────────────────

    private ThresholdMassBrain MakeCharger(params float[] rolls)
        => new ThresholdMassBrain(new ScriptedRandom(rolls), _table,
            Config(skills: new[] { "CHARGE" }, enhanced: "ENHANCED", stateBuff: ChargeBuff));

    [Test]
    public void 충전하면_자기에게_버프가_걸린다()
    {
        ThresholdMassBrain brain = MakeCharger(0.1f, 0f, 0f);

        brain.Decide(_ctx, _monster).Execute(_ctx);

        Assert.IsTrue(brain.IsCharging(_monster));
    }

    [Test]
    public void 충전_중이면_확률과_무관하게_강화_스킬을_쓴다()
    {
        _monster.Stats.AddBuff(ChargeBuff, BuffType.ATK_Percent, 0.5f, 2);

        // 굴림이 0.9라 평소라면 기본공격이지만, 충전 중이라 무시된다
        ThresholdMassBrain brain = MakeCharger(0.9f, 0f);

        Assert.AreEqual("ENHANCED", SkillOf(brain.Decide(_ctx, _monster)));
    }

    [Test]
    public void 충전_버프가_만료되면_평소_행동으로_돌아온다()
    {
        _monster.Stats.AddBuff(ChargeBuff, BuffType.ATK_Percent, 0.5f, 1);
        _monster.Stats.TickBuffDurations();

        ThresholdMassBrain brain = MakeCharger(0.9f, 0f);

        Assert.IsFalse(brain.IsCharging(_monster));
        Assert.AreEqual("BASIC", SkillOf(brain.Decide(_ctx, _monster)));
    }

    // ── 추적형 (VoidHunter) ──────────────────────────────────────

    private VoidHunterBrain MakeStalker()
        => new VoidHunterBrain(new ScriptedRandom(0f), _table,
            Config(enhanced: "ENHANCED", stateBuff: MarkBuff));

    [Test]
    public void 표식이_없으면_체력이_가장_낮은_상대를_노린다()
    {
        _aegis.ApplyDamage(120f);   // 150 → 30. Claire의 100보다 낮다

        MakeStalker().Decide(_ctx, _monster).Execute(_ctx);

        Assert.Less(_aegis.CurrentHP, 30f);
        Assert.AreEqual(100f, _claire.CurrentHP, Tolerance);
    }

    [Test]
    public void 표식이_걸린_상대를_최우선으로_노린다()
    {
        _aegis.ApplyDamage(120f);   // 체력은 Aegis가 더 낮지만
        _claire.Stats.AddBuff(MarkBuff, BuffType.Damage_Taken_Percent, 0.2f, 2);

        MakeStalker().Decide(_ctx, _monster).Execute(_ctx);

        Assert.AreEqual(30f, _aegis.CurrentHP, Tolerance);
        Assert.Less(_claire.CurrentHP, 100f);
    }

    [Test]
    public void 표식_대상이_있으면_강화_스킬을_쓴다()
    {
        _claire.Stats.AddBuff(MarkBuff, BuffType.Damage_Taken_Percent, 0.2f, 2);

        Assert.AreEqual("ENHANCED", SkillOf(MakeStalker().Decide(_ctx, _monster)));
    }

    [Test]
    public void 표식이_없으면_기본공격을_쓴다()
    {
        Assert.AreEqual("BASIC", SkillOf(MakeStalker().Decide(_ctx, _monster)));
    }

    [Test]
    public void 표식_대상이_쓰러지면_다시_최저_체력을_노린다()
    {
        _claire.Stats.AddBuff(MarkBuff, BuffType.Damage_Taken_Percent, 0.2f, 2);
        _claire.ApplyDamage(999f);

        MakeStalker().Decide(_ctx, _monster).Execute(_ctx);

        Assert.Less(_aegis.CurrentHP, 150f);
    }

    // ── 턴 루프 연동 ─────────────────────────────────────────────

    [TestCase(0.1f, "SPECIAL")]
    [TestCase(0.9f, "BASIC")]
    public void VoidHunter는_표식이_없으면_확률로_특수기를_선택한다(float roll, string expected)
    {
        var brain = new VoidHunterBrain(new ScriptedRandom(roll), _table,
            Config(skills: new[] { "SPECIAL" }, enhanced: "ENHANCED", stateBuff: MarkBuff));
        Assert.AreEqual(expected, SkillOf(brain.Decide(_ctx, _monster)));
    }

    [Test]
    public void VoidHunter는_표식보다_도발을_우선한다()
    {
        _aegis.Stats.AddBuff("TAUNT", BuffType.Taunt, 0, 2);
        _claire.Stats.AddBuff(MarkBuff, BuffType.Damage_Taken_Percent, 0.2f, 2);
        var command = (UseSkillCommand)MakeStalker().Decide(_ctx, _monster);
        Assert.AreEqual("BASIC", command.Skill.SkillId);
        command.Execute(_ctx);
        Assert.Less(_aegis.CurrentHP, 150f);
        Assert.AreEqual(100f, _claire.CurrentHP);
    }

    [Test]
    public void 적은_예약뒤_아군이_건_도발을_자기차례에_반영한다()
    {
        var loop = new TurnLoop();
        loop.RegisterBrain(_monster, new LostScoutBrain(new ScriptedRandom(0), _table, Config()));
        loop.BeginTurn(_ctx);
        var taunt = TestSkills.Make(new BuffEffect("TAUNT", BuffType.Taunt, 0, 2));
        loop.Reserve(new UseSkillCommand(_claire, _claire, taunt));
        loop.ReserveBrainActions(_ctx, _ctx.Enemies);
        Assert.AreEqual(100f, _claire.CurrentHP);
        loop.ExecuteTurn(_ctx);
        Assert.AreEqual(150f, _aegis.CurrentHP);
        Assert.Less(_claire.CurrentHP, 100f);
    }

    [Test]
    public void 등록된_Brain이_턴마다_행동을_예약한다()
    {
        var loop = new TurnLoop();
        loop.RegisterBrain(_monster, new LostScoutBrain(
            new ScriptedRandom(0.9f, 0f, 0.9f, 0f), _table, Config()));

        loop.BeginTurn(_ctx);
        loop.ReserveBrainActions(_ctx, _ctx.Enemies);

        Assert.AreEqual(1, loop.ReservedCount);

        loop.ExecuteTurn(_ctx);

        Assert.IsTrue(_aegis.CurrentHP < 150f || _claire.CurrentHP < 100f);
    }

    [Test]
    public void Brain이_없는_유닛은_예약되지_않는다()
    {
        var loop = new TurnLoop();

        loop.BeginTurn(_ctx);
        loop.ReserveBrainActions(_ctx, _ctx.Enemies);

        Assert.AreEqual(0, loop.ReservedCount);
    }

    [Test]
    public void 관련된_Brain에게만_이벤트가_전달된다()
    {
        var spy = new SpyBrain();

        var loop = new TurnLoop();
        loop.RegisterBrain(_monster, spy);

        loop.BeginTurn(_ctx);
        loop.Reserve(new UseSkillCommand(_aegis, _monster, TestSkills.Damage()));
        loop.ExecuteTurn(_ctx);

        Assert.AreEqual(1, spy.Received.Count);
        Assert.IsInstanceOf<DamageDealt>(spy.Received[0]);
    }

    [Test]
    public void 남의_일은_전달되지_않는다()
    {
        var spy = new SpyBrain();

        var loop = new TurnLoop();
        loop.RegisterBrain(_monster, spy);

        loop.BeginTurn(_ctx);
        loop.Reserve(new UseSkillCommand(_monster, _aegis, TestSkills.Damage()));
        loop.ExecuteTurn(_ctx);

        Assert.AreEqual(0, spy.Received.Count);
    }

    /// <summary>통지 여부를 확인하기 위한 테스트용 Brain. 받은 것을 기록만 한다.</summary>
    private sealed class SpyBrain : IActionBrain
    {
        public List<CombatEvent> Received { get; } = new List<CombatEvent>();

        public ICombatCommand Decide(CombatContext ctx, Unit self) => null;

        public void OnEvent(CombatEvent combatEvent) => Received.Add(combatEvent);
    }
}
