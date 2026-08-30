using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// ER 관련 효과와 비용 검증.
///
/// 증가와 감소는 규칙이 다르다.
///   증가 — 대상의 ER 저항이 적용된다
///   감소 — 대상이 ECHO 상태이면 아무 일도 일어나지 않는다
/// 그래서 하나의 효과에 부호로 담지 않고 두 클래스로 나누었다.
/// 사용 비용(ERCost)은 또 달라서, ECHO 상태에서도 소비된다.
/// </summary>
public class ERSkillTests
{
    private const float Tolerance = 0.01f;

    private Unit _seiren, _claire;
    private Unit _scout;
    private CombatContext _ctx;
    private List<CombatEvent> _events;

    [SetUp]
    public void SetUp()
    {
        _seiren = new Unit("Seiren", new UnitStats(120f, 20f, 10f, erResist: 0f), CharacterJob.Resonance);
        _claire = new Unit("Claire", new UnitStats(100f, 15f, 15f, erResist: 0.5f), CharacterJob.Medic);
        _scout  = new Unit("LostScout", new UnitStats(40f, 10f, 0f));

        _ctx = new CombatContext(new[] { _seiren, _claire }, new[] { _scout });
        _events = new List<CombatEvent>();
    }

    // ── 증가 ─────────────────────────────────────────────────────

    [Test]
    public void 적을_때리면서_자신의_ER을_올릴_수_있다()
    {
        SkillSpec skill = TestSkills.Make(
            new DamageEffect(1f),
            new ERGainEffect(20f, EffectTarget.Self));

        new UseSkillCommand(_seiren, _scout, skill).Execute(_ctx);

        Assert.Less(_scout.CurrentHP, 40f);
        Assert.AreEqual(20f, _seiren.CurrentER, Tolerance);
        Assert.AreEqual(0f, _scout.CurrentER, Tolerance);
    }

    [Test]
    public void ER_증가는_대상의_저항을_받는다()
    {
        SkillSpec skill = TestSkills.Make(new ERGainEffect(40f, EffectTarget.Target));

        new UseSkillCommand(_seiren, _claire, skill).Execute(_ctx);

        // 클레어의 ER 저항 0.5 → 40 * 0.5 = 20
        Assert.AreEqual(20f, _claire.CurrentER, Tolerance);
    }

    // ── 감소 ─────────────────────────────────────────────────────

    [Test]
    public void 아군의_ER을_내릴_수_있다()
    {
        EchoRules.ApplyER(_seiren, 50f, _events);

        SkillSpec skill = TestSkills.Make(new ERReduceEffect(30f, EffectTarget.Target));
        new UseSkillCommand(_claire, _seiren, skill).Execute(_ctx);

        Assert.AreEqual(20f, _seiren.CurrentER, Tolerance);
    }

    [Test]
    public void ECHO_상태인_아군은_ER이_내려가지_않는다()
    {
        EchoRules.ApplyER(_seiren, 100f, _events);

        SkillSpec skill = TestSkills.Make(new ERReduceEffect(50f, EffectTarget.Target));
        new UseSkillCommand(_claire, _seiren, skill).Execute(_ctx);

        Assert.AreEqual(100f, _seiren.CurrentER, Tolerance);
        Assert.IsTrue(_seiren.IsEchoState);
    }

    // ── 비용 ─────────────────────────────────────────────────────

    [Test]
    public void ER이_부족하면_스킬을_쓸_수_없다()
    {
        SkillSpec skill = TestSkills.WithCost(SkillType.Special, 0, 0, erCost: 30f, new DamageEffect(1f));

        Assert.IsFalse(skill.CanAfford(_seiren, _ctx.Resources));
    }

    [Test]
    public void ER이_충분하면_스킬을_쓸_수_있다()
    {
        EchoRules.ApplyER(_seiren, 50f, _events);

        SkillSpec skill = TestSkills.WithCost(SkillType.Special, 0, 0, erCost: 30f, new DamageEffect(1f));

        Assert.IsTrue(skill.CanAfford(_seiren, _ctx.Resources));
    }

    [Test]
    public void 스킬을_쓰면_ER_비용이_차감된다()
    {
        EchoRules.ApplyER(_seiren, 50f, _events);

        SkillSpec skill = TestSkills.WithCost(SkillType.Special, 0, 0, erCost: 30f, new DamageEffect(1f));
        new UseSkillCommand(_seiren, _scout, skill).Execute(_ctx);

        Assert.AreEqual(20f, _seiren.CurrentER, Tolerance);
    }

    [Test]
    public void ECHO_상태에서도_ER_비용은_소비된다()
    {
        // 레조넌스는 ECHO에서도 제어를 유지하므로 스킬을 쓸 수 있어야 한다.
        // ReduceER과 달리 비용 소비는 차단되지 않는다.
        EchoRules.ApplyER(_seiren, 100f, _events);

        SkillSpec skill = TestSkills.WithCost(SkillType.Special, 0, 0, erCost: 30f, new DamageEffect(1f));
        new UseSkillCommand(_seiren, _scout, skill).Execute(_ctx);

        Assert.AreEqual(70f, _seiren.CurrentER, Tolerance);
        Assert.IsFalse(_seiren.IsEchoState);
    }

    [Test]
    public void ER_비용이_없는_스킬은_ER과_무관하다()
    {
        SkillSpec skill = TestSkills.WithCost(SkillType.Basic, 1, 0, erCost: 0f, new DamageEffect(1f));

        Assert.IsTrue(skill.CanAfford(_claire, _ctx.Resources));
    }

    [Test]
    public void SP가_부족하면_특수기를_쓸_수_없다()
    {
        SkillSpec skill = TestSkills.WithCost(SkillType.Special, 99, 0, erCost: 0f, new DamageEffect(1f));

        Assert.IsFalse(skill.CanAfford(_claire, _ctx.Resources));
    }

    [Test]
    public void OP가_부족하면_필살기를_쓸_수_없다()
    {
        SkillSpec skill = TestSkills.WithCost(SkillType.Overload, 0, 99, erCost: 0f, new DamageEffect(1f));

        Assert.IsFalse(skill.CanAfford(_claire, _ctx.Resources));
    }

    // ── 자신 대상 ────────────────────────────────────────────────

    [Test]
    public void 적을_때리면서_자신에게_실드를_걸_수_있다()
    {
        SkillSpec skill = TestSkills.Make(
            new DamageEffect(1f),
            new ShieldEffect(1f, 2) { effectTarget = EffectTarget.Self });

        new UseSkillCommand(_seiren, _scout, skill).Execute(_ctx);

        Assert.AreEqual(10f, _seiren.CurrentShield, Tolerance);   // 자신의 DEF 10 × 1
        Assert.AreEqual(0f, _scout.CurrentShield, Tolerance);
    }

    [Test]
    public void 효과별로_대상을_다르게_지정할_수_있다()
    {
        SkillSpec skill = TestSkills.Make(
            new DamageEffect(1f),
            new BuffEffect("BUFF_A", BuffType.ATK_Percent, 0.3f, 2)
                { effectTarget = EffectTarget.Self });

        new UseSkillCommand(_seiren, _scout, skill).Execute(_ctx);

        Assert.IsTrue(_seiren.Stats.HasBuff("BUFF_A"));
        Assert.IsFalse(_scout.Stats.HasBuff("BUFF_A"));
    }
}
