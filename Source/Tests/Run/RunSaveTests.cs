using System;
using System.Linq;
using NUnit.Framework;

public class RunSaveTests
{
    [TestCase(1)] [TestCase(2)]
    public void EarlierSaveVersionsStillLoad(int version)
    {
        var data = Save(Run()); data.version = version;
        Assert.AreEqual(1f, Load(data).Party[0].Stats.Get(StatType.ER_GainMultiplier));
    }
    [Test]
    public void EventModifiersRoundTripWithoutDuplicatingAndApplyToErGain()
    {
        var run = Run();
        run.Party[0].Stats.AddModifier(new StatModifier(StatType.ATK, .1f, ModSource.MetaProgress));
        run.Party[0].Stats.AddModifier(new StatModifier(StatType.ER_GainMultiplier, .1f, ModSource.MetaProgress));
        run.Party[0].Stats.AddModifier(new StatModifier(StatType.MaxHP, -.05f, ModSource.MetaProgress));
        run.Party[0].ClampHPToMaximum();
        var data = Save(run); Assert.AreEqual(3, data.version);
        for (int i = 0; i < 3; i++)
        {
            var loaded = Load(data); var unit = loaded.Party[0];
            Assert.AreEqual(3, unit.Stats.Modifiers.Count); Assert.AreEqual(95, unit.MaxHP, .001f); Assert.AreEqual(95, unit.CurrentHP, .001f);
            Assert.AreEqual(11, unit.Stats.Get(StatType.ATK), .001f);
            EchoRules.GainOwnER(unit, 10, null); Assert.AreEqual(11, unit.CurrentER, .001f);
            EchoRules.ReduceER(unit, 11, null); data = Save(loaded);
        }
    }
    static Unit[] Party() => new[] { new Unit("Aegis",new UnitStats(100,10,10)),new Unit("Claire",new UnitStats(100,10,10)),new Unit("Seiren",new UnitStats(100,10,10),CharacterJob.Resonance) };
    static PassiveCardRules Cards()
    {
        var cards=new PassiveCardRules();cards.RegisterStatBonus("HP",StatType.MaxHP,.2f);cards.RegisterStatBonus("ATK",StatType.ATK,.1f);
        cards.RegisterCard("SP",PassiveEffectType.BattleStart_SP,1);cards.RegisterCard("ECHO",PassiveEffectType.Echo_OP,1);return cards;
    }
    static RunState Run() => new RunState(Party(),MapGenerator.Generate(new SystemRandom(55)),Cards(),new SystemRandom(66));
    static RunSaveData Save(RunState run)
    {
        var data=RunSaveRules.Capture(run,new[]{"Aegis","Claire","Seiren"});
        data.mapRandom=new RandomState{seed=55,drawCount=19};data.combatRandom=new RandomState{seed=77,drawCount=8};
        data.rewardRandom=new RandomState{seed=88,drawCount=4};data.cardRandom=((SystemRandom)run.CardRandom).CaptureState();return data;
    }
    static RunState Load(RunSaveData data) => RunSaveRules.Restore(data,Party(),Cards(),SystemRandom.Restore(data.cardRandom));
    [TestCase(0)] [TestCase(-2147483648)] [TestCase(91882)]
    public void RandomSequenceMatchesExistingMixedCallsAndCanResume(int seed)
    {
        var original=new Random(seed);var random=new SystemRandom(seed);
        for(int i=0;i<200;i++) { Assert.AreEqual(original.Next(-12,9999),random.Range(-12,9999));Assert.AreEqual((float)original.NextDouble(),random.Value01()); }
        var restored=SystemRandom.Restore(random.CaptureState());
        for(int i=0;i<200;i++) { Assert.AreEqual(random.Range(0,71),restored.Range(0,71));Assert.AreEqual(random.Value01(),restored.Value01()); }
        Assert.AreEqual(800,restored.CaptureState().drawCount);
    }
    [Test]
    public void CompletedNodeRestoresHpEchoCardsModifiersAndResourcesExactlyOnce()
    {
        var run=Run();run.AddCard("HP");run.AddCard("HP");run.AddCard("ATK");run.AddCard("SP");run.AddCard("ECHO");
        run.AddStartingResources(1,2);run.Party[0].ApplyDamage(37);run.Party[1].ApplyDamage(1000);run.Party[2].SetER(100);
        run.Party[0].Stats.AddModifier(new StatModifier(StatType.DEF,.15f,ModSource.MetaProgress));
        run.Party[0].Stats.AddBuff("Temp",BuffType.ATK_Percent,.5f,3);run.Party[0].AddShield(10,2);
        Assert.IsTrue(run.TryEnter(3));run.CompleteNode();var data=Save(run);
        for(int repeat=0;repeat<4;repeat++)
        {
            var loaded=Load(data);Assert.AreEqual(3,loaded.CurrentNode);Assert.AreEqual(-1,loaded.PendingNode);Assert.IsTrue(loaded.IsCleared(3));
            Assert.AreEqual(63,loaded.Party[0].CurrentHP);Assert.AreEqual(0,loaded.Party[1].CurrentHP);Assert.IsTrue(loaded.Party[2].IsEchoState);
            Assert.AreEqual(140,loaded.Party[0].MaxHP,.001);Assert.AreEqual(11,loaded.Party[0].Stats.Get(StatType.ATK),.001);Assert.AreEqual(11.5,loaded.Party[0].Stats.Get(StatType.DEF),.001);
            Assert.AreEqual(0,loaded.Party[0].Stats.Buffs.Count);Assert.IsFalse(loaded.Party[0].HasShield);CollectionAssert.AreEqual(run.Cards,loaded.Cards);
            Assert.AreEqual(data.cardRandom.drawCount,((SystemRandom)loaded.CardRandom).CaptureState().drawCount);
            var context=loaded.CreateCombatContext(new[]{new Unit("Enemy",new UnitStats(20,2,0),usesResources:false)});
            Assert.AreEqual(7,context.Resources.CurrentSP);Assert.AreEqual(4,context.Resources.CurrentOP);
        }
        Assert.Throws<ArgumentException>(()=>RunSaveRules.Restore(data,run.Party,Cards(),new SystemRandom(0)));
    }
    [Test]
    public void EveryRandomStreamRestoresItsNextDraw()
    {
        var data=Save(Run());
        foreach(var state in new[]{data.mapRandom,data.combatRandom,data.rewardRandom,data.cardRandom})
        {var expected=new SystemRandom(state.seed);for(int i=0;i<state.drawCount;i++)expected.Value01();Assert.AreEqual(expected.Value01(),SystemRandom.Restore(state).Value01());}
    }
    [Test]
    public void PendingAndFinishedRunsCannotBeSaved()
    {
        var run=Run();Assert.IsTrue(run.TryEnter(1));Assert.Throws<InvalidOperationException>(()=>Save(run));run.CompleteNode();run.Fail();Assert.Throws<InvalidOperationException>(()=>Save(run));
    }
    [Test]
    public void MalformedVersionPathValuesIdsAndRandomAreRejected()
    {
        var data=Save(Run());data.version=0;Assert.Throws<ArgumentException>(()=>Load(data));
        data=Save(Run());data.currentNode=20;data.clearedNodes=new[]{0,20};Assert.Throws<ArgumentException>(()=>Load(data));
        data=Save(Run());data.party[0].currentHP=float.NaN;Assert.Throws<ArgumentException>(()=>Load(data));
        data=Save(Run());data.party[0].currentHP=10000;Assert.Throws<ArgumentException>(()=>Load(data));
        data=Save(Run());data.cards=new[]{"Unknown"};Assert.Throws<ArgumentException>(()=>Load(data));
        data=Save(Run());data.party[1].unitId=data.party[0].unitId;Assert.Throws<ArgumentException>(()=>Load(data));
        data=Save(Run());data.rewardRandom.drawCount=SystemRandom.MaximumSavedDrawCount+1;Assert.Throws<ArgumentException>(()=>Load(data));
        data=Save(Run());foreach(var unit in data.party)unit.currentHP=0;Assert.Throws<ArgumentException>(()=>Load(data));
    }
    [Test]
    public void RewardConfirmationAndRunEndNotifyAfterStateIsFinal()
    {
        var run=Run();int notifications=0;run.CheckpointReached+=()=>{notifications++;Assert.AreEqual(-1,run.PendingNode);if(!run.IsFinished)Assert.IsTrue(Save(run).cards.Contains("HP"));};
        Assert.IsTrue(run.TryEnter(1));run.PrepareBattleReward(new[]{"HP","SP"},new SystemRandom(1));Assert.AreEqual(0,notifications);
        Assert.IsTrue(run.TryClaimBattleReward("HP"));Assert.AreEqual(1,notifications);Assert.IsFalse(run.TryClaimBattleReward("HP"));
        run.Fail();run.Fail();Assert.AreEqual(2,notifications);
    }
    [Test]
    public void SelectedNodeSurvivesLoadAndCompletionUnlocksConnectedMerge()
    {
        var data = Save(Run());
        data.nodeTypes[1] = NodeType.Battle;
        data.hasNodeSelection = true;
        data.nodeSelection = new NodeSelectionSaveData { nodeIndex = 1 };
        for (int restart = 0; restart < 3; restart++)
        {
            var run = Load(data);
            Assert.AreEqual(1, run.SelectedNode);
            Assert.AreEqual(-1, run.PendingNode);
            Assert.IsTrue(run.CanEnter(1));
            Assert.IsFalse(run.TryEnter(2));
            Assert.IsFalse(run.TryEnter(3));
            data = Save(run);
        }
        var resumed = Load(data);
        Assert.IsTrue(resumed.TryEnter(1));
        resumed.CompleteNode();
        Assert.AreEqual(-1, resumed.SelectedNode);
        Assert.IsNull(Save(resumed).nodeSelection);
        Assert.IsTrue(resumed.CanEnter(3));
        Assert.IsTrue(resumed.CanEnter(4));
    }

    [TestCase(-1)] [TestCase(0)] [TestCase(4)] [TestCase(22)]
    public void InvalidSelectedNodeIsRejected(int nodeIndex)
    {
        var data = Save(Run());
        data.hasNodeSelection = true;
        data.nodeSelection = new NodeSelectionSaveData { nodeIndex = nodeIndex };
        Assert.Throws<ArgumentException>(() => Load(data));
    }

    [Test]
    public void VersionOneRemainsPlayableWithoutASelection()
    {
        var data = Save(Run()); data.version = 1;
        var run = Load(data);
        Assert.AreEqual(-1, run.SelectedNode);
        foreach (int node in new[] { 1, 2, 3 }) Assert.IsTrue(run.CanEnter(node));
        data.hasNodeSelection = true;
        data.nodeSelection = new NodeSelectionSaveData { nodeIndex = 1 };
        Assert.Throws<ArgumentException>(() => Load(data));
    }

    [Test]
    public void RestoringUsedRestDoesNotHealOrCureAgain()
    {
        var data = Save(Run()); data.nodeTypes[1] = NodeType.Rest;
        data.hasNodeSelection = true;
        data.nodeSelection = new NodeSelectionSaveData { nodeIndex = 1, choiceConfirmed = true, choiceIndex = 2 };
        data.party[0].currentHP = 60; data.party[1].currentER = 100;
        var run = Load(data); Assert.IsTrue(run.TryEnter(1));
        RestRules.RestoreSelection(run, RestRules.Action.CureEcho);
        Assert.AreEqual(60, run.Party[0].CurrentHP);
        Assert.IsTrue(run.Party[1].IsEchoState);
        foreach (RestRules.Action action in Enum.GetValues(typeof(RestRules.Action)))
            Assert.IsFalse(RestRules.TryApply(run, action, new SystemRandom(1)));
    }

}
