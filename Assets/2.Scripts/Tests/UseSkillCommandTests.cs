using NUnit.Framework;

/// <summary>
/// 커맨드 실행 결과를 프레임 진행 없이 검증한다.
/// 계산과 연출을 분리했기 때문에 가능한 테스트다.
/// </summary>
public class UseSkillCommandTests
{
    private const float Tolerance = 0.01f;

    private static CombatContext MakeContext(out Unit aegis, out Unit scout)
    {
        aegis = new Unit("Aegis", new UnitStats(maxHp: 200f, atk: 50f, def: 20f));
        scout = new Unit("LostScout", new UnitStats(maxHp: 80f, atk: 30f, def: 100f));
        return new CombatContext(new[] { aegis }, new[] { scout });
    }

    [Test]
    public void 스킬을_사용하면_대상의_체력이_줄어든다()
    {
        var ctx = MakeContext(out var aegis, out var scout);

        new UseSkillCommand(aegis, scout, 1f).Execute(ctx);

        // 50 * (1 - 100/200) = 25
        Assert.AreEqual(55f, scout.CurrentHP, Tolerance);
    }

    [Test]
    public void 실행_결과로_DamageDealt_이벤트가_하나_나온다()
    {
        var ctx = MakeContext(out var aegis, out var scout);

        CommandResult result = new UseSkillCommand(aegis, scout, 1f).Execute(ctx);

        Assert.AreEqual(1, result.Events.Count);

        var damageEvent = result.Events[0] as DamageDealt;
        Assert.IsNotNull(damageEvent);
        Assert.AreSame(scout, damageEvent.Target);
        Assert.AreEqual(25f, damageEvent.Amount, Tolerance);
        Assert.AreEqual(scout.CurrentHP, damageEvent.HpAfter, Tolerance);
    }

    [Test]
    public void 이벤트의_피해량은_실제로_깎인_양이다()
    {
        var ctx = MakeContext(out var aegis, out var scout);

        // 체력 80인 대상에게 배율 100배로 과다 피해를 준다
        CommandResult result = new UseSkillCommand(aegis, scout, 100f).Execute(ctx);

        var damageEvent = (DamageDealt)result.Events[0];
        Assert.AreEqual(80f, damageEvent.Amount, Tolerance);   // 2500이 아니라 80
        Assert.AreEqual(0f, damageEvent.HpAfter, Tolerance);
    }

    [Test]
    public void 죽은_대상에게는_실행할_수_없다()
    {
        var ctx = MakeContext(out var aegis, out var scout);
        new UseSkillCommand(aegis, scout, 100f).Execute(ctx);   // 먼저 쓰러뜨린다

        var second = new UseSkillCommand(aegis, scout, 1f);

        Assert.IsFalse(scout.IsAlive);
        Assert.IsFalse(second.CanExecute(ctx));
    }

    [Test]
    public void 배율이_커지면_피해도_비례해서_커진다()
    {
        var ctx1 = MakeContext(out var a1, out var s1);
        var ctx2 = MakeContext(out var a2, out var s2);

        var r1 = new UseSkillCommand(a1, s1, 1f).Execute(ctx1);
        var r2 = new UseSkillCommand(a2, s2, 2f).Execute(ctx2);

        float d1 = ((DamageDealt)r1.Events[0]).Amount;
        float d2 = ((DamageDealt)r2.Events[0]).Amount;

        Assert.AreEqual(d1 * 2f, d2, Tolerance);
    }
}
