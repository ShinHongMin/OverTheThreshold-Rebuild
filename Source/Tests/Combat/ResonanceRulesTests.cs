using System.Collections.Generic;
using NUnit.Framework;

public class ResonanceRulesTests
{
    [TestCase(0f, false)]
    [TestCase(49.99f, false)]
    [TestCase(50f, true)]
    [TestCase(99f, true)]
    [TestCase(100f, true)]
    public void CurrentER_SelectsEnhancedState(float er, bool expected)
    {
        var unit = new Unit("Seiren", new UnitStats(120, 20, 10), CharacterJob.Resonance);
        EchoRules.GainOwnER(unit, er, new List<CombatEvent>());
        Assert.AreEqual(expected, ResonanceRules.IsEnhanced(unit));
    }
    [Test]
    public void OtherJobsAndMonsters_DoNotUseResonance()
    {
        var medic = new Unit("Claire", new UnitStats(100, 15, 15), CharacterJob.Medic);
        EchoRules.GainOwnER(medic, 100, new List<CombatEvent>());
        Assert.IsFalse(ResonanceRules.IsEnhanced(medic));
        Assert.IsFalse(ResonanceRules.IsEnhanced(new Unit("enemy", new UnitStats(100, 10, 0), CharacterJob.Resonance, false)));
        Assert.IsFalse(ResonanceRules.IsEnhanced(null));
    }
}
