using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// 타게팅 규칙 검증.
/// 기존 CombatManager.CheckIfValidTarget과 컨트롤러의 대상 분기를 옮긴 것이다.
/// </summary>
public class TargetingRuleTests
{
    private Unit _aegis, _claire;
    private Unit _scout, _bulwark;
    private CombatContext _ctx;

    [SetUp]
    public void SetUp()
    {
        _aegis   = new Unit("Aegis",   new UnitStats(150f, 10f, 20f));
        _claire  = new Unit("Claire",  new UnitStats(100f, 15f, 15f));
        _scout   = new Unit("LostScout",        new UnitStats(40f, 10f, 5f));
        _bulwark = new Unit("CorruptedBulwark", new UnitStats(60f, 10f, 10f));

        _ctx = new CombatContext(new[] { _aegis, _claire }, new[] { _scout, _bulwark });
    }

    // ── 진영 구분 ────────────────────────────────────────────────

    [Test]
    public void 파티원의_적은_적군이고_아군은_파티다()
    {
        Assert.AreSame(_ctx.Enemies, _ctx.GetOpponentsOf(_aegis));
        Assert.AreSame(_ctx.Party, _ctx.GetAlliesOf(_aegis));
    }

    [Test]
    public void 적군_기준으로는_반대가_된다()
    {
        Assert.AreSame(_ctx.Party, _ctx.GetOpponentsOf(_scout));
        Assert.AreSame(_ctx.Enemies, _ctx.GetAlliesOf(_scout));
    }

    // ── 대상 해석 ────────────────────────────────────────────────

    [Test]
    public void 단일_적_대상은_지정한_하나만_반환한다()
    {
        List<Unit> targets = TargetingRule.ResolveTargets(
            _ctx, _aegis, TargetType.SingleEnemy, _scout);

        Assert.AreEqual(1, targets.Count);
        Assert.AreSame(_scout, targets[0]);
    }

    [Test]
    public void 자신_대상은_지정_없이_시전자를_반환한다()
    {
        List<Unit> targets = TargetingRule.ResolveTargets(
            _ctx, _aegis, TargetType.Self, null);

        Assert.AreEqual(1, targets.Count);
        Assert.AreSame(_aegis, targets[0]);
    }

    [Test]
    public void 아군_전체는_생존한_파티원_전원이다()
    {
        List<Unit> targets = TargetingRule.ResolveTargets(
            _ctx, _aegis, TargetType.Team, null);

        Assert.AreEqual(2, targets.Count);
    }

    [Test]
    public void 적_전체는_생존한_적_전원이다()
    {
        List<Unit> targets = TargetingRule.ResolveTargets(
            _ctx, _aegis, TargetType.AllEnemy, null);

        Assert.AreEqual(2, targets.Count);
    }

    [Test]
    public void 죽은_유닛은_전체_대상에서_제외된다()
    {
        _scout.ApplyDamage(999f);

        List<Unit> targets = TargetingRule.ResolveTargets(
            _ctx, _aegis, TargetType.AllEnemy, null);

        Assert.AreEqual(1, targets.Count);
        Assert.AreSame(_bulwark, targets[0]);
    }

    [Test]
    public void 죽은_유닛은_단일_대상으로도_해석되지_않는다()
    {
        _scout.ApplyDamage(999f);

        List<Unit> targets = TargetingRule.ResolveTargets(
            _ctx, _aegis, TargetType.SingleEnemy, _scout);

        Assert.AreEqual(0, targets.Count);
    }

    // ── 지정 가능 판정 ───────────────────────────────────────────

    [Test]
    public void 아군을_적_대상으로_지정할_수_없다()
    {
        Assert.IsFalse(TargetingRule.IsValidTarget(
            _ctx, _aegis, TargetType.SingleEnemy, _claire));
    }

    [Test]
    public void 죽은_아군은_아군_대상으로_지정할_수_없다()
    {
        _claire.ApplyDamage(999f);

        Assert.IsFalse(TargetingRule.IsValidTarget(
            _ctx, _aegis, TargetType.Ally, _claire));
    }

    [Test]
    public void 지정이_필요한_종류는_단일_대상뿐이다()
    {
        Assert.IsTrue(TargetingRule.NeedsManualTarget(TargetType.SingleEnemy));
        Assert.IsTrue(TargetingRule.NeedsManualTarget(TargetType.Ally));

        Assert.IsFalse(TargetingRule.NeedsManualTarget(TargetType.Self));
        Assert.IsFalse(TargetingRule.NeedsManualTarget(TargetType.Team));
        Assert.IsFalse(TargetingRule.NeedsManualTarget(TargetType.AllEnemy));
    }

    // ── 도발 ─────────────────────────────────────────────────────

    [Test]
    public void 도발중인_적이_있으면_다른_적은_지정할_수_없다()
    {
        _bulwark.Stats.AddBuff("Buff_CorruptedBulwark_Taunt", BuffType.Taunt, 0f, 2);

        Assert.IsFalse(TargetingRule.IsValidTarget(
            _ctx, _aegis, TargetType.SingleEnemy, _scout));

        Assert.IsTrue(TargetingRule.IsValidTarget(
            _ctx, _aegis, TargetType.SingleEnemy, _bulwark));
    }

    [Test]
    public void 도발한_적이_죽으면_다시_자유롭게_지정할_수_있다()
    {
        _bulwark.Stats.AddBuff("Buff_CorruptedBulwark_Taunt", BuffType.Taunt, 0f, 2);
        _bulwark.ApplyDamage(999f);

        Assert.IsTrue(TargetingRule.IsValidTarget(
            _ctx, _aegis, TargetType.SingleEnemy, _scout));
    }

    [Test]
    public void 도발중이면_선택_가능_목록에서도_제외된다()
    {
        _bulwark.Stats.AddBuff("Buff_CorruptedBulwark_Taunt", BuffType.Taunt, 0f, 2);

        List<Unit> selectable = TargetingRule.GetSelectableTargets(
            _ctx, _aegis, TargetType.SingleEnemy);

        Assert.AreEqual(1, selectable.Count);
        Assert.AreSame(_bulwark, selectable[0]);
    }
}
