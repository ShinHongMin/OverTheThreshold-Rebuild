using NUnit.Framework;

/// <summary>
/// 난수 공급자 검증. 테스트에서 결과를 고정할 수 있는지가 핵심이다.
/// </summary>
public class RandomTests
{
    [Test]
    public void 지정한_값이_순서대로_나온다()
    {
        var rng = new ScriptedRandom(0.1f, 0.5f, 0.9f);

        Assert.AreEqual(0.1f, rng.Value01(), 0.001f);
        Assert.AreEqual(0.5f, rng.Value01(), 0.001f);
        Assert.AreEqual(0.9f, rng.Value01(), 0.001f);
    }

    [Test]
    public void 값을_다_쓰면_처음으로_돌아간다()
    {
        var rng = new ScriptedRandom(0.2f, 0.8f);

        rng.Value01();
        rng.Value01();

        Assert.AreEqual(0.2f, rng.Value01(), 0.001f);
    }

    [Test]
    public void Range는_지정한_구간_안에서_나온다()
    {
        var rng = new ScriptedRandom(0f, 0.5f, 0.99f);

        Assert.AreEqual(0, rng.Range(0, 4));
        Assert.AreEqual(2, rng.Range(0, 4));
        Assert.AreEqual(3, rng.Range(0, 4));
    }

    [Test]
    public void 같은_시드는_같은_결과를_낸다()
    {
        var first = new SystemRandom(seed: 12345);
        var second = new SystemRandom(seed: 12345);

        for (int i = 0; i < 10; i++)
            Assert.AreEqual(first.Value01(), second.Value01(), 0.0001f);
    }
}
