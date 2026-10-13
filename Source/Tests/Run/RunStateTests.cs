using NUnit.Framework;

public class RunStateTests
{
    private static RunState MakeRun()
        => new RunState(new[] {
            new Unit("Aegis", new UnitStats(100, 10, 10)),
            new Unit("Claire", new UnitStats(100, 10, 10)),
            new Unit("Seiren", new UnitStats(100, 10, 10), CharacterJob.Resonance)
        }, MapGenerator.Generate(new ScriptedRandom(0f)));

    [Test]
    public void 시작_휴식_보스와_22개_노드가_고정된다()
    {
        var run = MakeRun();
        Assert.AreEqual(22, run.Map.Count);
        Assert.AreEqual(NodeType.Start, run.Map[0].Type);
        Assert.AreEqual(NodeType.Rest, run.Map[20].Type);
        Assert.AreEqual(NodeType.Boss, run.Map[21].Type);
        Assert.IsTrue(run.IsCleared(0));
    }
    [Test]
    public void 연결되지_않은_노드나_처리중_다른_노드에_들어갈_수_없다()
    {
        var run = MakeRun();
        Assert.IsFalse(run.TryEnter(21));
        Assert.IsTrue(run.TryEnter(1));
        Assert.IsFalse(run.TryEnter(2));
        run.CompleteNode();
        Assert.IsTrue(run.IsCleared(1));
        Assert.IsTrue(run.CanEnter(3));
        Assert.IsFalse(run.CanEnter(0));
    }
    [Test]
    public void 전투_복귀시_HP_ER_파티순서는_유지하고_임시효과는_제거한다()
    {
        var run = MakeRun();
        var first = run.Party[0];
        run.TryEnter(1);
        var ctx = run.BeginCombat(new[] { new Unit("Enemy", new UnitStats(100, 10, 0), usesResources: false) });
        first.ApplyDamage(30);
        EchoRules.ApplyER(first, 100, null);
        first.AddShield(20, 4);
        first.Stats.AddBuff("Temporary", BuffType.ATK_Percent, 0.5f, 4);
        run.CompleteNode();
        Assert.AreEqual(70, first.CurrentHP);
        Assert.AreEqual(100, first.CurrentER);
        Assert.IsTrue(first.IsEchoState);
        Assert.IsFalse(first.HasShield);
        Assert.IsFalse(first.Stats.HasBuff("Temporary"));
        Assert.AreSame(first, ctx.Party[0]);
        Assert.AreSame(first, run.Party[0]);
    }
    [Test]
    public void 다음_전투는_이벤트_시작자원_보너스를_반영한다()
    {
        var run = MakeRun();
        run.AddStartingResources(1, 2);
        run.TryEnter(1);
        var ctx = run.BeginCombat(new[] { new Unit("Enemy", new UnitStats(100, 10, 0), usesResources: false) });
        Assert.AreEqual(6, ctx.Resources.CurrentSP);
        Assert.AreEqual(4, ctx.Resources.CurrentOP);
    }
    private static void EnterRest(RunState run)
    {
        foreach (int index in new[] { 3, 6, 9, 12, 15, 17 }) { Assert.IsTrue(run.TryEnter(index)); run.CompleteNode(); }
        Assert.IsTrue(run.TryEnter(20));
    }
    [Test]
    public void 휴식_정비는_ECHO를_해제하며_한_선택만_허용한다()
    {
        var run = MakeRun();
        EchoRules.ApplyER(run.Party[0], 100, null);
        EnterRest(run);
        Assert.IsTrue(RestRules.TryApply(run, RestRules.Action.Maintenance, null));
        Assert.AreEqual(70, run.Party[0].CurrentER);
        Assert.IsFalse(run.Party[0].IsEchoState);
        Assert.IsFalse(RestRules.TryApply(run, RestRules.Action.Heal, null));
    }
    [Test]
    public void 치료할_ECHO가_없으면_휴식_선택을_소비하지_않는다()
    {
        var run = MakeRun();
        EnterRest(run);
        Assert.IsFalse(RestRules.TryApply(run, RestRules.Action.CureEcho, new ScriptedRandom(0f)));
        Assert.IsFalse(run.RestUsed);
        EchoRules.ApplyER(run.Party[1], 100, null);
        Assert.IsTrue(RestRules.TryApply(run, RestRules.Action.CureEcho, new ScriptedRandom(0f)));
        Assert.AreEqual(0, run.Party[1].CurrentER);
    }
    [Test]
    public void 휴식은_사망자를_살리지_않는다()
    {
        var run = MakeRun();
        run.Party[0].ApplyDamage(1000);
        run.Party[1].ApplyDamage(50);
        EnterRest(run);
        RestRules.TryApply(run, RestRules.Action.Heal, null);
        Assert.AreEqual(0, run.Party[0].CurrentHP);
        Assert.AreEqual(70, run.Party[1].CurrentHP);
    }
    [Test]
    public void 보상은_같은_목록_위치를_두_번_뽑지_않는다()
    {
        var pool = new[] { "A", "B", "C" };
        var choices = RewardRules.Draw(pool, new ScriptedRandom(0f));
        Assert.AreEqual("A", choices[0]);
        Assert.AreEqual("B", choices[1]);
        CollectionAssert.AreEqual(new[] { "A", "B", "C" }, pool);
    }
}
