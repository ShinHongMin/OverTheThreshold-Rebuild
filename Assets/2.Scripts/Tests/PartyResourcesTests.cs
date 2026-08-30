using NUnit.Framework;

/// <summary>
/// 파티 공용 자원 규칙 검증. 기존 ResourceManager 규칙을 그대로 옮긴 것이다.
/// </summary>
public class PartyResourcesTests
{
    [Test]
    public void 시작값은_SP5_OP2다()
    {
        var resources = new PartyResources();

        Assert.AreEqual(5, resources.CurrentSP);
        Assert.AreEqual(2, resources.CurrentOP);
    }

    [Test]
    public void 이벤트_보너스가_시작값에_더해진다()
    {
        var resources = new PartyResources(bonusStartSP: 2, bonusStartOP: 1);

        Assert.AreEqual(7, resources.CurrentSP);
        Assert.AreEqual(3, resources.CurrentOP);
    }

    [Test]
    public void 보너스를_받아도_상한을_넘지_않는다()
    {
        var resources = new PartyResources(bonusStartSP: 99, bonusStartOP: 99);

        Assert.AreEqual(PartyResources.DefaultMaxSP, resources.CurrentSP);
        Assert.AreEqual(PartyResources.DefaultMaxOP, resources.CurrentOP);
    }

    [Test]
    public void SP를_얻어도_상한_7을_넘지_않는다()
    {
        var resources = new PartyResources();

        resources.GainSP(10);

        Assert.AreEqual(7, resources.CurrentSP);
    }

    [Test]
    public void SP가_충분하면_차감되고_성공을_반환한다()
    {
        var resources = new PartyResources();   // SP 5

        bool success = resources.TrySpendSP(3);

        Assert.IsTrue(success);
        Assert.AreEqual(2, resources.CurrentSP);
    }

    [Test]
    public void SP가_부족하면_차감되지_않고_실패를_반환한다()
    {
        var resources = new PartyResources();   // SP 5

        bool success = resources.TrySpendSP(6);

        Assert.IsFalse(success);
        Assert.AreEqual(5, resources.CurrentSP);   // 그대로여야 한다
    }

    [Test]
    public void OP가_부족하면_차감되지_않고_실패를_반환한다()
    {
        var resources = new PartyResources();   // OP 2

        bool success = resources.TrySpendOP(3);

        Assert.IsFalse(success);
        Assert.AreEqual(2, resources.CurrentOP);
    }

    [Test]
    public void 턴_시작마다_OP가_1씩_회복된다()
    {
        var resources = new PartyResources();   // OP 2

        resources.GainOPOnTurnStart();
        resources.GainOPOnTurnStart();

        Assert.AreEqual(4, resources.CurrentOP);
    }

    [Test]
    public void OP_회복은_상한_6에서_멈춘다()
    {
        var resources = new PartyResources();

        for (int i = 0; i < 20; i++)
            resources.GainOPOnTurnStart();

        Assert.AreEqual(6, resources.CurrentOP);
    }

    [Test]
    public void 자원은_전투마다_독립적이다()
    {
        var first = new PartyResources();
        var second = new PartyResources();

        first.TrySpendSP(5);

        Assert.AreEqual(0, first.CurrentSP);
        Assert.AreEqual(5, second.CurrentSP);   // 싱글톤이 아니므로 간섭하지 않는다
    }

    [Test]
    public void CombatContext는_자원을_생략하면_기본값으로_만든다()
    {
        var unit = new Unit("Test", new UnitStats(100f, 10f, 0f));
        var ctx = new CombatContext(new[] { unit }, new[] { unit });

        Assert.IsNotNull(ctx.Resources);
        Assert.AreEqual(5, ctx.Resources.CurrentSP);
    }
}
