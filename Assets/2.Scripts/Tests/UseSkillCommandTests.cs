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
