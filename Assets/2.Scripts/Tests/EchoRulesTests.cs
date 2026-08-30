using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// ER 증감과 ECHO 진입 규칙 검증. 기존 ERManager 규칙을 옮긴 것이다.
/// </summary>
public class EchoRulesTests
{
    private const float Tolerance = 0.01f;

    private List<CombatEvent> _events;

    [SetUp]
    public void SetUp() => _events = new List<CombatEvent>();

    private static Unit MakeUnit(float erResist = 0f, CharacterJob job = CharacterJob.Vanguard)
        => new Unit("TestUnit", new UnitStats(maxHp: 100f, atk: 10f, def: 0f, erResist: erResist), job);

    // ── ER 증감 ──────────────────────────────────────────────────

    [Test]
    public void ER저항이_0이면_그대로_들어간다()
    {
        Unit unit = MakeUnit();

        EchoRules.ApplyER(unit, 30f, _events);

        Assert.AreEqual(30f, unit.CurrentER, Tolerance);
    }

    [Test]
    public void ER저항만큼_획득량이_줄어든다()
    {
        Unit unit = MakeUnit(erResist: 0.15f);   // Aegis

        EchoRules.ApplyER(unit, 100f, _events);

        // 100 * (1 - 0.15) = 85
        Assert.AreEqual(85f, unit.CurrentER, Tolerance);
    }

    [Test]
    public void ER은_100을_넘지_않는다()
    {
        Unit unit = MakeUnit();

        EchoRules.ApplyER(unit, 500f, _events);

        Assert.AreEqual(100f, unit.CurrentER, Tolerance);
    }

    [Test]
    public void ER은_0_아래로_내려가지_않는다()
    {
        Unit unit = MakeUnit();
        EchoRules.ApplyER(unit, 20f, _events);

        EchoRules.ReduceER(unit, 50f, _events);

        Assert.AreEqual(0f, unit.CurrentER, Tolerance);
    }

    [Test]
    public void ER이_변하면_ResourceChanged_이벤트가_나온다()
    {
        Unit unit = MakeUnit();

        EchoRules.ApplyER(unit, 30f, _events);

        Assert.AreEqual(1, _events.Count);
        var changed = _events[0] as ResourceChanged;
        Assert.IsNotNull(changed);
        Assert.AreEqual(ResourceKind.ER, changed.Kind);
        Assert.AreEqual(30f, changed.Delta, Tolerance);
    }

    [Test]
    public void 변화가_없으면_이벤트를_남기지_않는다()
    {
        Unit unit = MakeUnit();

        EchoRules.ReduceER(unit, 10f, _events);   // 이미 0이라 변화 없음

        Assert.AreEqual(0, _events.Count);
    }

    // ── ECHO 진입 ────────────────────────────────────────────────

    [Test]
    public void ER이_100이_되면_ECHO_상태가_된다()
    {
        Unit unit = MakeUnit();

        EchoRules.ApplyER(unit, 99f, _events);
        Assert.IsFalse(unit.IsEchoState);

        EchoRules.ApplyER(unit, 1f, _events);
        Assert.IsTrue(unit.IsEchoState);
    }

    [Test]
    public void ECHO_상태에서는_ER이_줄지_않는다()
    {
        Unit unit = MakeUnit();
        EchoRules.ApplyER(unit, 100f, _events);

        EchoRules.ReduceER(unit, 50f, _events);

        Assert.AreEqual(100f, unit.CurrentER, Tolerance);
        Assert.IsTrue(unit.IsEchoState);   // 자력 탈출 불가
    }

    [Test]
    public void 사용_비용은_ECHO_상태에서도_소비된다()
    {
        Unit unit = MakeUnit(job: CharacterJob.Resonance);
        EchoRules.ApplyER(unit, 100f, _events);

        EchoRules.SpendER(unit, 30f, _events);

        Assert.AreEqual(70f, unit.CurrentER, Tolerance);
    }

    [Test]
    public void 휴식_노드의_치료로_ECHO가_해제된다()
    {
        Unit unit = MakeUnit();
        EchoRules.ApplyER(unit, 100f, _events);

        EchoRules.CureEcho(unit, _events);

        Assert.AreEqual(0f, unit.CurrentER, Tolerance);
        Assert.IsFalse(unit.IsEchoState);
    }

    // ── 페널티 ───────────────────────────────────────────────────

    [Test]
    public void ECHO가_아니면_페널티가_없다()
    {
        Unit unit = MakeUnit();

        Assert.AreEqual(0f, EchoRules.GetEchoPenalty(unit), Tolerance);
    }

    [Test]
    public void 일반_직군의_페널티는_최대체력의_10퍼센트다()
    {
        Unit unit = MakeUnit(job: CharacterJob.Vanguard);
        EchoRules.ApplyER(unit, 100f, _events);

        Assert.AreEqual(10f, EchoRules.GetEchoPenalty(unit), Tolerance);
    }

    [Test]
    public void 레조넌스는_제어를_유지하는_대신_페널티가_두_배다()
    {
        Unit seiren = MakeUnit(job: CharacterJob.Resonance);
        EchoRules.ApplyER(seiren, 100f, _events);

        Assert.AreEqual(20f, EchoRules.GetEchoPenalty(seiren), Tolerance);
        Assert.IsFalse(EchoRules.IsOutOfControl(seiren));
    }

    [Test]
    public void 레조넌스가_아니면_ECHO에서_제어를_잃는다()
    {
        Unit aegis = MakeUnit(job: CharacterJob.Vanguard);
        EchoRules.ApplyER(aegis, 100f, _events);

        Assert.IsTrue(EchoRules.IsOutOfControl(aegis));
    }
}
