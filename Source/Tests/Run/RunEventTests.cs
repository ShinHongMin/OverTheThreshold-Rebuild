using System.Linq;
using NUnit.Framework;

public class RunEventTests
{
    private sealed class TargetRandom : IRandom
    {
        public int Calls;
        public int Target;
        public int Range(int minInclusive, int maxExclusive) { Calls++; Assert.Less(Target, maxExclusive - minInclusive); return minInclusive + Target; }
        public float Value01() { Assert.Fail("Target selection must use one integer draw."); return 0; }
    }
    private static RunState EventRun()
    {
        var types = Enumerable.Repeat(NodeType.Event, 22).ToArray();
        types[0] = NodeType.Start; types[3] = NodeType.Battle; types[20] = NodeType.Rest; types[21] = NodeType.Boss;
        var party = new[] { new Unit("A", new UnitStats(100, 20, 10, .25f)), new Unit("B", new UnitStats(100, 20, 10)), new Unit("C", new UnitStats(100, 20, 10), CharacterJob.Resonance) };
        var run = new RunState(party, MapGenerator.Restore(types), cardRandom: new SystemRandom(7));
        Assert.IsTrue(run.TryEnter(1)); return run;
    }
    private static RunEventEffect[] EncounterEffects() => new[] {
        new RunEventEffect(EventEffectType.IncreaseER, 50, target: RunEventTarget.RandomLivingAlly),
        new RunEventEffect(EventEffectType.BuffSingleRandom, 10, targetStat: StatType.ATK, target: RunEventTarget.RandomLivingAlly) };
    private static RunEventEffect[] MemoryEffects() => new[] {
        new RunEventEffect(EventEffectType.BuffAll, 10, targetStat: StatType.ER_GainMultiplier),
        new RunEventEffect(EventEffectType.BuffAll, 5, targetStat: StatType.ATK) };

    [Test]
    public void RandomEffectsShareOneLivingTargetAndSurviveCombatReset()
    {
        var run = EventRun(); run.Party[0].ApplyDamage(100); var random = new TargetRandom { Target = 1 };
        Assert.IsTrue(RunEventRules.TryApply(run, EncounterEffects(), null, random, out int index));
        Assert.AreEqual(2, index); Assert.AreEqual(1, random.Calls);
        Assert.AreEqual(50, run.Party[2].CurrentER); Assert.AreEqual(22, run.Party[2].Stats.Get(StatType.ATK), .001f);
        Assert.AreEqual(0, run.Party[1].CurrentER); Assert.AreEqual(20, run.Party[1].Stats.Get(StatType.ATK));
        Assert.AreEqual(0, run.Party[0].CurrentHP);
        run.CompleteNode(); Assert.IsTrue(run.TryEnter(3));
        run.BeginCombat(new[] { new Unit("Enemy", new UnitStats(100, 10, 0), usesResources: false) });
        run.Party[2].ResetCombatState(); Assert.AreEqual(22, run.Party[2].Stats.Get(StatType.ATK), .001f);
    }
    [Test]
    public void EchoTargetStillReceivesPermanentAttackWithoutLosingEcho()
    {
        var run = EventRun(); run.Party[2].SetER(100);
        Assert.IsTrue(RunEventRules.TryApply(run, EncounterEffects(), null, new TargetRandom { Target = 2 }, out _));
        Assert.AreEqual(100, run.Party[2].CurrentER); Assert.AreEqual(22, run.Party[2].Stats.Get(StatType.ATK), .001f);
        EchoRules.SpendER(run.Party[2], 30, null); Assert.AreEqual(100, run.Party[2].CurrentER);
    }
    [Test]
    public void MemoryIncreasesOwnAndReceivedErWithoutChangingResistanceCostsOrReduction()
    {
        var run = EventRun(); Assert.IsTrue(RunEventRules.TryApply(run, MemoryEffects())); var unit = run.Party[0];
        Assert.AreEqual(.25f, unit.Stats.Get(StatType.ER_Resist)); Assert.AreEqual(21, unit.Stats.Get(StatType.ATK), .001f);
        EchoRules.GainOwnER(unit, 10, null); Assert.AreEqual(11, unit.CurrentER, .001f);
        EchoRules.ApplyER(unit, 20, null); Assert.AreEqual(27.5f, unit.CurrentER, .001f);
        EchoRules.ReduceER(unit, 10, null); EchoRules.SpendER(unit, 5, null); Assert.AreEqual(12.5f, unit.CurrentER, .001f);
        EchoRules.GainOwnER(unit, 100, null); EchoRules.ReduceER(unit, 10, null); Assert.IsTrue(unit.IsEchoState);
    }
    [Test]
    public void RepeatedMemoryAddsPercentBonusesRatherThanCompounding()
    {
        var run = EventRun();
        Assert.IsTrue(RunEventRules.TryApply(run, MemoryEffects())); Assert.IsTrue(RunEventRules.TryApply(run, MemoryEffects()));
        Assert.AreEqual(1.2f, run.Party[0].Stats.Get(StatType.ER_GainMultiplier), .001f);
        Assert.AreEqual(22, run.Party[0].Stats.Get(StatType.ATK), .001f);
    }
    [Test]
    public void DestroyingMemoryReducesMaximumHpAndOnlyClampsExcessCurrentHp()
    {
        var run = EventRun(); run.Party[1].ApplyDamage(20); run.Party[2].ApplyDamage(100);
        run.Party[0].SetER(40); run.Party[1].SetER(100);
        var effects = new[] { new RunEventEffect(EventEffectType.BuffAll, -5, targetStat: StatType.MaxHP), new RunEventEffect(EventEffectType.DecreaseER, 10) };
        Assert.IsTrue(RunEventRules.TryApply(run, effects));
        foreach (var unit in run.Party) Assert.AreEqual(95, unit.MaxHP, .001f);
        Assert.AreEqual(95, run.Party[0].CurrentHP, .001f); Assert.AreEqual(80, run.Party[1].CurrentHP); Assert.AreEqual(0, run.Party[2].CurrentHP);
        Assert.AreEqual(30, run.Party[0].CurrentER); Assert.AreEqual(100, run.Party[1].CurrentER);
    }
    [Test]
    public void InvalidStatChangeRejectsWholeChoiceBeforeErOrRandomChanges()
    {
        var run = EventRun(); var random = new TargetRandom();
        var effects = new[] { new RunEventEffect(EventEffectType.IncreaseER, 50, target: RunEventTarget.RandomLivingAlly), new RunEventEffect(EventEffectType.BuffAll, -100, targetStat: StatType.MaxHP) };
        Assert.IsFalse(RunEventRules.TryApply(run, effects, null, random, out _)); Assert.AreEqual(0, random.Calls);
        Assert.AreEqual(0, run.Party[0].CurrentER); Assert.AreEqual(100, run.Party[0].MaxHP); Assert.AreEqual(0, run.Party[0].Stats.Modifiers.Count);
    }
    [Test]
    public void DecliningEncounterHasNoEffectsOrTargetDraw()
    {
        var run = EventRun(); var random = new TargetRandom();
        Assert.IsTrue(RunEventRules.TryApply(run, new RunEventEffect[0], null, random, out int index));
        Assert.AreEqual(-1, index); Assert.AreEqual(0, random.Calls); Assert.AreEqual(0, run.Party[0].Stats.Modifiers.Count);
    }
}
