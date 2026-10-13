using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// ER 관련 효과와 비용 검증.
///
/// 증가와 감소는 규칙이 다르다.
///   증가 — 대상의 ER 저항이 적용된다
///   감소 — 대상이 ECHO 상태이면 아무 일도 일어나지 않는다
/// 그래서 하나의 효과에 부호로 담지 않고 두 클래스로 나누었다.
/// 사용 비용(ERCost)도 ECHO 진입 후에는 ER 100을 유지한다.
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

    [TestCase(EffectTarget.Self)]
    [TestCase(EffectTarget.Target)]
    public void 레조넌스의_자기_ER획득은_저항버프와_무관하다(EffectTarget effectTarget)
    {
        _seiren.Stats.AddBuff("ER_RESIST", BuffType.ER_Resist_Percent, 0.5f, 3);
        var effect = new ERGainEffect(20f, effectTarget);
        effect.Apply(_ctx, _seiren, _seiren, _events);
        Assert.AreEqual(20f, _seiren.CurrentER, Tolerance);
        Assert.AreEqual(0.5f, _seiren.Stats.Get(StatType.ER_Resist), Tolerance);
    }

    [Test]
    public void 레조넌스도_적의_ER공격에는_저항버프가_적용된다()
    {
        _seiren.Stats.AddBuff("ER_RESIST", BuffType.ER_Resist_Percent, 0.5f, 3);
        new ERGainEffect(20f).Apply(_ctx, _scout, _seiren, _events);
        Assert.AreEqual(10f, _seiren.CurrentER, Tolerance);
    }

    [Test]
    public void 저항버프가_있어도_ER비용과_아군의_ER감소는_고정값이다()
    {
        _seiren.SetER(80f);
        _seiren.Stats.AddBuff("ER_RESIST", BuffType.ER_Resist_Percent, 0.5f, 3);
        var skill = TestSkills.WithCost(SkillType.Special, 0, 0, 30f, new DamageEffect(1f));
        new UseSkillCommand(_seiren, _scout, skill).Execute(_ctx);
        Assert.AreEqual(50f, _seiren.CurrentER, Tolerance);
        new ERReduceEffect(20f).Apply(_ctx, _claire, _seiren, _events);
        Assert.AreEqual(30f, _seiren.CurrentER, Tolerance);
    }

    [Test]
    public void 몬스터는_ER변경이나_ECHO페널티를_받지_않는다()
    {
        var monster = new Unit("Monster", new UnitStats(100f, 10f, 0f), usesResources: false);
        monster.SetER(100f);
        EchoRules.ApplyER(monster, 100f, _events);
        EchoRules.GainOwnER(monster, 100f, _events);
        EchoRules.SpendER(monster, 30f, _events);
        EchoRules.ReduceER(monster, 20f, _events);
        EchoRules.CureEcho(monster, _events);
        Assert.AreEqual(0f, monster.CurrentER);
        Assert.IsFalse(monster.IsEchoState);
        Assert.IsFalse(EchoRules.IsOutOfControl(monster));
        Assert.AreEqual(0f, EchoRules.GetEchoPenalty(monster));
        Assert.IsEmpty(_events);
    }

    [TestCase(SkillType.Basic)]
    [TestCase(SkillType.Special)]
    [TestCase(SkillType.Overload)]
    public void 몬스터는_자원없이_공격하고_파티자원은_유지한다(SkillType type)
    {
        var monster = new Unit("Monster", new UnitStats(100f, 10f, 0f), usesResources: false);
        var context = new CombatContext(new[] { _seiren }, new[] { monster });
        _seiren.Stats.AddBuff("ER_RESIST", BuffType.ER_Resist_Percent, 0.5f, 3);
        var skill = TestSkills.WithCost(type, 99, 99, 99f, new DamageEffect(1f), new ERGainEffect(20f));
        int sp = context.Resources.CurrentSP, op = context.Resources.CurrentOP;
        Assert.IsTrue(skill.CanAfford(monster, null));
        var command = new UseSkillCommand(monster, _seiren, skill);
        Assert.IsTrue(command.CanExecute(context));
        command.Execute(context);
        Assert.Less(_seiren.CurrentHP, 120f);
        Assert.AreEqual(10f, _seiren.CurrentER, Tolerance);
        Assert.AreEqual(0f, monster.CurrentER);
        Assert.AreEqual(sp, context.Resources.CurrentSP);
        Assert.AreEqual(op, context.Resources.CurrentOP);
    }

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
    public void 세이렌은_ECHO에서_ER_비용_스킬을_써도_ER100을_유지한다()
    {
        // 레조넌스는 ECHO에서도 제어를 유지하므로 스킬을 쓸 수 있어야 한다.
        // 사용은 가능하지만 비용으로 ECHO가 해제되지는 않는다.
        EchoRules.ApplyER(_seiren, 100f, _events);

        SkillSpec skill = TestSkills.WithCost(SkillType.Special, 0, 0, erCost: 30f, new DamageEffect(1f));
        Assert.IsTrue(skill.CanAfford(_seiren, _ctx.Resources));
        CommandResult result = new UseSkillCommand(_seiren, _scout, skill).Execute(_ctx);

        Assert.AreEqual(100f, _seiren.CurrentER, Tolerance);
        Assert.IsTrue(_seiren.IsEchoState);
        Assert.AreEqual(20f, _scout.CurrentHP, Tolerance);
        foreach (CombatEvent combatEvent in result.Events)
            Assert.IsFalse(combatEvent is ResourceChanged);
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
