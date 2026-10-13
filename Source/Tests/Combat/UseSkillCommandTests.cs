using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// 커맨드 실행 결과를 프레임 진행 없이 검증한다.
/// 계산과 연출을 분리했기 때문에 가능한 테스트다.
/// </summary>
public class UseSkillCommandTests
{
    private const float Tolerance = 0.01f;

    private Unit _aegis, _claire;
    private Unit _scout, _echo;
    private CombatContext _ctx;

    [SetUp]
    public void SetUp()
    {
        _aegis  = new Unit("Aegis",  new UnitStats(maxHp: 200f, atk: 50f, def: 20f));
        _claire = new Unit("Claire", new UnitStats(maxHp: 100f, atk: 15f, def: 15f));
        _scout  = new Unit("LostScout",      new UnitStats(maxHp: 80f, atk: 30f, def: 100f));
        _echo   = new Unit("WhisperingEcho", new UnitStats(maxHp: 30f, atk: 5f,  def: 100f));

        _ctx = new CombatContext(new[] { _aegis, _claire }, new[] { _scout, _echo });
    }

    private static List<CombatEvent> Results(CommandResult result)
        => TestSkills.WithoutCast(result.Events);

    // ── 시전 알림 ────────────────────────────────────────────────

    [Test]
    public void 실행하면_SkillCast가_가장_먼저_나온다()
    {
        CommandResult result = new UseSkillCommand(_aegis, _scout, TestSkills.Damage()).Execute(_ctx);

        var cast = result.Events[0] as SkillCast;

        Assert.IsNotNull(cast);
        Assert.AreSame(_aegis, cast.Caster);
        Assert.AreEqual(1, cast.Targets.Count);
    }

    // ── 피해 ─────────────────────────────────────────────────────

    [Test]
    public void 스킬을_사용하면_대상의_체력이_줄어든다()
    {
        new UseSkillCommand(_aegis, _scout, TestSkills.Damage()).Execute(_ctx);

        // 50 * (1 - 100/200) = 25
        Assert.AreEqual(55f, _scout.CurrentHP, Tolerance);
    }

    [Test]
    public void 실행_결과로_DamageDealt_이벤트가_나온다()
    {
        CommandResult result = new UseSkillCommand(_aegis, _scout, TestSkills.Damage()).Execute(_ctx);
        List<CombatEvent> events = Results(result);

        Assert.AreEqual(1, events.Count);

        var damage = events[0] as DamageDealt;
        Assert.IsNotNull(damage);
        Assert.AreSame(_scout, damage.Target);
        Assert.AreEqual(25f, damage.Amount, Tolerance);
    }

    [Test]
    public void 이벤트의_피해량은_실제로_깎인_양이다()
    {
        CommandResult result = new UseSkillCommand(_aegis, _scout, TestSkills.Damage(100f)).Execute(_ctx);

        var damage = (DamageDealt)Results(result)[0];
        Assert.AreEqual(80f, damage.Amount, Tolerance);   // 2500이 아니라 80
        Assert.AreEqual(0f, damage.HpAfter, Tolerance);
    }

    [Test]
    public void 대상이_쓰러지면_UnitDied가_뒤따른다()
    {
        CommandResult result = new UseSkillCommand(_aegis, _scout, TestSkills.Damage(100f)).Execute(_ctx);
        List<CombatEvent> events = Results(result);

        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOf<DamageDealt>(events[0]);
        Assert.IsInstanceOf<UnitDied>(events[1]);
    }

    [Test]
    public void 받는_피해_증가_디버프가_반영된다()
    {
        _scout.Stats.AddBuff("DEBUFF_VULN", BuffType.Damage_Taken_Percent, 0.5f, 2);

        CommandResult result = new UseSkillCommand(_aegis, _scout, TestSkills.Damage()).Execute(_ctx);

        // 25 * 1.5 = 37.5
        Assert.AreEqual(37.5f, ((DamageDealt)Results(result)[0]).Amount, Tolerance);
    }

    // ── 다중 대상 ────────────────────────────────────────────────

    [Test]
    public void 광역_스킬은_대상_수만큼_이벤트를_만든다()
    {
        var targets = new List<Unit> { _scout, _echo };

        CommandResult result = new UseSkillCommand(_aegis, targets, TestSkills.Damage()).Execute(_ctx);

        Assert.AreEqual(2, Results(result).Count);
    }

    [Test]
    public void 이미_죽은_대상은_건너뛴다()
    {
        _echo.ApplyDamage(999f);
        var targets = new List<Unit> { _scout, _echo };

        CommandResult result = new UseSkillCommand(_aegis, targets, TestSkills.Damage()).Execute(_ctx);
        List<CombatEvent> events = Results(result);

        Assert.AreEqual(1, events.Count);
        Assert.AreSame(_scout, ((DamageDealt)events[0]).Target);
    }

    // ── 효과 조합 ────────────────────────────────────────────────

    [TestCase(1)]
    [TestCase(2)]
    public void 광역_피해의_자기_ER_효과는_대상_수와_무관하게_한번이다(int targetCount)
    {
        SkillSpec skill = TestSkills.Make("AREA", SkillType.Basic, TargetType.AllEnemy,
            new DamageEffect(1f), new ERGainEffect(10f, EffectTarget.Self));
        var targets = new List<Unit> { _scout };
        if (targetCount == 2) targets.Add(_echo);

        List<CombatEvent> events = Results(new UseSkillCommand(_aegis, targets, skill).Execute(_ctx));

        Assert.AreEqual(55f, _scout.CurrentHP, Tolerance);
        Assert.AreEqual(targetCount == 2 ? 5f : 30f, _echo.CurrentHP, Tolerance);
        Assert.AreEqual(10f, _aegis.CurrentER, Tolerance);
        Assert.AreEqual(0f, _scout.CurrentER, Tolerance);
        Assert.AreEqual(0f, _echo.CurrentER, Tolerance);
        Assert.AreEqual(targetCount + 1, events.Count);
        for (int i = 0; i < targetCount; i++)
            Assert.IsInstanceOf<DamageDealt>(events[i]);
        Assert.IsInstanceOf<ResourceChanged>(events[targetCount]);
    }

    [Test]
    public void 아군_전체_버프와_자기_효과는_각자의_대상에게_한번씩_적용된다()
    {
        SkillSpec skill = TestSkills.Make("TEAM", SkillType.Special, TargetType.Team,
            new ERGainEffect(10f, EffectTarget.Self),
            new BuffEffect("TEAM_DEF", BuffType.DEF_Percent, 0.5f, 3),
            new ShieldEffect(1f, 2) { effectTarget = EffectTarget.Self });
        List<Unit> targets = TargetingRule.ResolveTargets(_ctx, _aegis, skill.TargetType, null);

        List<CombatEvent> events = Results(new UseSkillCommand(_aegis, targets, skill).Execute(_ctx));

        Assert.AreEqual(10f, _aegis.CurrentER, Tolerance);
        Assert.AreEqual(0f, _claire.CurrentER, Tolerance);
        Assert.AreEqual(30f, _aegis.Stats.Get(StatType.DEF), Tolerance);
        Assert.AreEqual(22.5f, _claire.Stats.Get(StatType.DEF), Tolerance);
        Assert.AreEqual(30f, _aegis.CurrentShield, Tolerance);
        Assert.AreEqual(0f, _claire.CurrentShield, Tolerance);
        Assert.IsFalse(_scout.Stats.HasBuff("TEAM_DEF"));
        Assert.IsFalse(_echo.Stats.HasBuff("TEAM_DEF"));
        Assert.AreEqual(4, events.Count);
        Assert.IsInstanceOf<ResourceChanged>(events[0]);
        Assert.AreSame(_aegis, ((BuffApplied)events[1]).Target);
        Assert.AreSame(_claire, ((BuffApplied)events[2]).Target);
        Assert.IsInstanceOf<ShieldGranted>(events[3]);
    }

    [Test]
    public void 자기_대상_스킬의_Target과_Self_효과도_각각_한번이다()
    {
        SkillSpec skill = TestSkills.Make("SELF", SkillType.Special, TargetType.Self,
            new ShieldEffect(1f, 2), new ERGainEffect(10f, EffectTarget.Self));
        List<Unit> targets = TargetingRule.ResolveTargets(_ctx, _aegis, skill.TargetType, null);

        List<CombatEvent> events = Results(new UseSkillCommand(_aegis, targets, skill).Execute(_ctx));

        Assert.AreEqual(20f, _aegis.CurrentShield, Tolerance);
        Assert.AreEqual(10f, _aegis.CurrentER, Tolerance);
        Assert.AreEqual(2, events.Count);
    }

    [Test]
    public void 버프_효과는_대상에게_버프를_걸고_이벤트를_남긴다()
    {
        SkillSpec skill = TestSkills.Make(
            new BuffEffect("Buff_Aegis_DEF_UP", BuffType.DEF_Percent, 0.5f, 3));

        CommandResult result = new UseSkillCommand(_aegis, _aegis, skill).Execute(_ctx);

        Assert.IsTrue(_aegis.Stats.HasBuff("Buff_Aegis_DEF_UP"));
        Assert.AreEqual(30f, _aegis.Stats.Get(StatType.DEF), Tolerance);   // 20 * 1.5

        var applied = Results(result)[0] as BuffApplied;
        Assert.IsNotNull(applied);
        Assert.AreEqual(3, applied.DurationTurns);
    }

    [Test]
    public void 효과를_여러_개_넣으면_순서대로_적용된다()
    {
        SkillSpec skill = TestSkills.Make(
            new DamageEffect(1f),
            new BuffEffect("DEBUFF_VULN", BuffType.Damage_Taken_Percent, 0.5f, 2));

        CommandResult result = new UseSkillCommand(_aegis, _scout, skill).Execute(_ctx);
        List<CombatEvent> events = Results(result);

        Assert.AreEqual(2, events.Count);
        Assert.IsInstanceOf<DamageDealt>(events[0]);
        Assert.IsInstanceOf<BuffApplied>(events[1]);
    }

    // ── 실행 가능 판정 ───────────────────────────────────────────

    [Test]
    public void 죽은_대상만_있으면_실행할_수_없다()
    {
        _scout.ApplyDamage(999f);

        var command = new UseSkillCommand(_aegis, _scout, TestSkills.Damage());

        Assert.IsFalse(command.CanExecute(_ctx));
    }

    [Test]
    public void 시전자가_죽었으면_실행할_수_없다()
    {
        _aegis.ApplyDamage(999f);

        var command = new UseSkillCommand(_aegis, _scout, TestSkills.Damage());

        Assert.IsFalse(command.CanExecute(_ctx));
    }

    [Test]
    public void 효과가_없으면_실행할_수_없다()
    {
        var command = new UseSkillCommand(_aegis, _scout, TestSkills.Make());

        Assert.IsFalse(command.CanExecute(_ctx));
    }

    // ── 자원 소비 ────────────────────────────────────────────────

    [TestCase(SkillType.Special, 99, 0, 0f)]
    [TestCase(SkillType.Overload, 0, 99, 0f)]
    [TestCase(SkillType.Special, 0, 0, 99f)]
    public void 자원이_부족하면_직접_실행해도_시전이나_효과가_없다(SkillType type, int sp, int op, float er)
    {
        var skill = TestSkills.WithCost(type, sp, op, er, new DamageEffect(1));
        var command = new UseSkillCommand(_aegis, _scout, skill);
        Assert.IsFalse(command.CanExecute(_ctx));
        Assert.AreEqual(0, command.Execute(_ctx).Events.Count);
        Assert.AreEqual(80f, _scout.CurrentHP);
        Assert.AreEqual(5, _ctx.Resources.CurrentSP);
        Assert.AreEqual(2, _ctx.Resources.CurrentOP);
    }

    [TestCase(44f, 0f, 75f, 59f)]
    [TestCase(45f, 0f, 50f, 75f)]
    [TestCase(50f, 0.5f, 75f, 57.5f)]
    [TestCase(60f, 0.5f, 50f, 75f)]
    public void 보스는_첫타격_ER반영후_60이상이면_ER을_포함한_추가타를_한번한다(float er, float resist, float hpAfter, float erAfter)
    {
        var target = new Unit("Target", new UnitStats(100, 10, 100, erResist: resist));
        target.SetER(er);
        var ctx = new CombatContext(new[] { target }, new[] { _aegis });
        var skill = TestSkills.Make(new DamageEffect(1), new ERGainEffect(15), new ERThresholdStrikeEffect());
        new UseSkillCommand(_aegis, target, skill).Execute(ctx);
        Assert.AreEqual(hpAfter, target.CurrentHP, Tolerance);
        Assert.AreEqual(erAfter, target.CurrentER, Tolerance);
    }

    [Test]
    public void 기본공격은_SP를_획득한다()
    {
        SkillSpec skill = TestSkills.WithCost(SkillType.Basic, spCost: 2, opCost: 0, erCost: 0f,
            new DamageEffect(1f));

        int before = _ctx.Resources.CurrentSP;
        new UseSkillCommand(_aegis, _scout, skill).Execute(_ctx);

        Assert.AreEqual(before + 2, _ctx.Resources.CurrentSP);
    }

    [Test]
    public void 특수기는_SP를_소모한다()
    {
        SkillSpec skill = TestSkills.WithCost(SkillType.Special, spCost: 3, opCost: 0, erCost: 0f,
            new DamageEffect(1f));

        int before = _ctx.Resources.CurrentSP;
        new UseSkillCommand(_aegis, _scout, skill).Execute(_ctx);

        Assert.AreEqual(before - 3, _ctx.Resources.CurrentSP);
    }

    [Test]
    public void 필살기는_OP를_소모한다()
    {
        SkillSpec skill = TestSkills.WithCost(SkillType.Overload, spCost: 0, opCost: 2, erCost: 0f,
            new DamageEffect(1f));

        int before = _ctx.Resources.CurrentOP;
        new UseSkillCommand(_aegis, _scout, skill).Execute(_ctx);

        Assert.AreEqual(before - 2, _ctx.Resources.CurrentOP);
    }
}
