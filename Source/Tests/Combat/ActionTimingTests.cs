using System.Collections.Generic;
using NUnit.Framework;

public class ActionTimingTests
{
    [Test]
    public void Steps_KeepLaterDamageAndTurnEndPending()
    {
        var first = new Unit("first", new UnitStats(100, 10, 0));
        var second = new Unit("second", new UnitStats(100, 10, 0));
        var enemy = new Unit("enemy", new UnitStats(100, 10, 0), usesResources: false);
        var context = new CombatContext(new[] { first, second }, new[] { enemy });
        var loop = new TurnLoop();
        loop.BeginTurn(context);
        var skill = TestSkills.WithCost(SkillType.Basic, 1, 0, 0, new DamageEffect(1));
        Assert.IsTrue(loop.TryReserve(new UseSkillCommand(first, enemy, skill), context, out _));
        Assert.IsTrue(loop.TryReserve(new UseSkillCommand(second, enemy, skill), context, out _));
        using (var steps = loop.ExecuteSteps(context).GetEnumerator())
        {
            Assert.IsTrue(steps.MoveNext());
            Assert.AreEqual(100, enemy.CurrentHP, "approach has no damage");
            loop.ExecuteAction(context, steps.Current.Command);
            Assert.AreEqual(90, enemy.CurrentHP, "only first impact");
            Assert.AreEqual(1, loop.Round, "recovery does not end turn");
            Assert.IsTrue(steps.MoveNext());
            Assert.AreEqual(90, enemy.CurrentHP, "second approach has no damage");
            loop.ExecuteAction(context, steps.Current.Command);
            Assert.AreEqual(80, enemy.CurrentHP);
            Assert.IsTrue(steps.MoveNext());
            Assert.IsNull(steps.Current.Command);
            Assert.AreEqual(2, loop.Round);
            Assert.IsFalse(steps.MoveNext());
        }
    }

    [TestCase(60, 20)]
    [TestCase(100, 100)]
    public void ER_StaysUntilEnhancedImpact_AndEchoRemainsFixed(float initialER, float expectedER)
    {
        var actor = new Unit("Seiren", new UnitStats(120, 20, 0), CharacterJob.Resonance);
        var enemy = new Unit("enemy", new UnitStats(200, 10, 0), usesResources: false);
        var context = new CombatContext(new[] { actor }, new[] { enemy });
        var loop = new TurnLoop();
        var events = new List<CombatEvent>();
        EchoRules.GainOwnER(actor, initialER, events);
        var skill = TestSkills.WithCost(SkillType.Special, 0, 0, 40, new DamageEffect(3));
        var command = new UseSkillCommand(actor, enemy, skill);
        Assert.IsTrue(loop.TryReserve(command, context, out var reservation));
        Assert.AreEqual(initialER, actor.CurrentER);
        Assert.AreEqual(200, enemy.CurrentHP);
        var result = loop.ExecuteAction(context, command);
        Assert.AreEqual(140, enemy.CurrentHP);
        Assert.AreEqual(expectedER, actor.CurrentER);
        int damageIndex = -1, resourceIndex = -1;
        for (int i = 0; i < result.Count; i++)
        {
            if (result[i] is DamageDealt) damageIndex = i;
            if (result[i] is ResourceChanged) resourceIndex = i;
        }
        if (initialER < 100) Assert.Greater(resourceIndex, damageIndex);
        else Assert.AreEqual(-1, resourceIndex);
    }

    [Test]
    public void DeadTarget_IsSkippedBeforeNextAnimation()
    {
        var first = new Unit("first", new UnitStats(100, 100, 0));
        var second = new Unit("second", new UnitStats(100, 10, 0));
        var enemy = new Unit("enemy", new UnitStats(10, 10, 0), usesResources: false);
        var other = new Unit("other", new UnitStats(100, 10, 0), usesResources: false);
        var context = new CombatContext(new[] { first, second }, new[] { enemy, other });
        var loop = new TurnLoop();
        var skill = TestSkills.WithCost(SkillType.Basic, 1, 0, 0, new DamageEffect(1));
        loop.TryReserve(new UseSkillCommand(first, enemy, skill), context, out _);
        loop.TryReserve(new UseSkillCommand(second, enemy, skill), context, out _);
        using (var steps = loop.ExecuteSteps(context).GetEnumerator())
        {
            Assert.IsTrue(steps.MoveNext());
            loop.ExecuteAction(context, steps.Current.Command);
            Assert.IsTrue(steps.MoveNext());
            Assert.IsNull(steps.Current.Command, "dead target must not reach presentation");
            Assert.IsFalse(steps.MoveNext());
        }
    }
}
