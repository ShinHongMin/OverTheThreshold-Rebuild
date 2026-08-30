using NUnit.Framework;

/// <summary>
/// 회복 규칙 검증. 기존 CharactersStat.TakeHeal / CombatCalculator.CalculateHeal을 옮긴 것이다.
/// </summary>
public class HealTests
{
    private const float Tolerance = 0.01f;

    private Unit _claire, _aegis;
    private CombatContext _ctx;

    [SetUp]
    public void SetUp()
    {
        _claire = new Unit("Claire", new UnitStats(maxHp: 100f, atk: 20f, def: 15f), CharacterJob.Medic);
        _aegis  = new Unit("Aegis",  new UnitStats(maxHp: 150f, atk: 10f, def: 20f));

        _ctx = new CombatContext(new[] { _claire, _aegis },
                                 new[] { new Unit("Enemy", new UnitStats(50f, 10f, 0f)) });
    }

    private static SkillSpec Heal(float multiplier)
        => TestSkills.Make(new HealEffect(multiplier));

    [Test]
    public void 회복량은_시전자의_공격력에_배율을_곱한_값이다()
    {
        _aegis.ApplyDamage(100f);   // 150 → 50

        new UseSkillCommand(_claire, _aegis, Heal(2f)).Execute(_ctx);

        // Claire의 ATK 20 × 2 = 40
        Assert.AreEqual(90f, _aegis.CurrentHP, Tolerance);
    }

    [Test]
    public void 회복은_최대_체력을_넘지_않는다()
    {
        _aegis.ApplyDamage(10f);   // 150 → 140

        new UseSkillCommand(_claire, _aegis, Heal(10f)).Execute(_ctx);

        Assert.AreEqual(150f, _aegis.CurrentHP, Tolerance);
    }

    [Test]
    public void 상한에_걸리면_실제로_회복된_만큼만_이벤트에_담긴다()
    {
        _aegis.ApplyDamage(10f);   // 남은 회복 여지는 10

        CommandResult result = new UseSkillCommand(_claire, _aegis, Heal(10f)).Execute(_ctx);

        var healed = TestSkills.WithoutCast(result.Events)[0] as HealReceived;
        Assert.IsNotNull(healed);
        Assert.AreEqual(10f, healed.Amount, Tolerance);   // 200이 아니라 10
    }

    [Test]
    public void 최대_체력_증가_보정이_회복_상한에_반영된다()
    {
        _aegis.Stats.AddModifier(new StatModifier(StatType.MaxHP, 0.2f, ModSource.PassiveCard));
        // 최대 체력 150 → 180
        _aegis.ApplyDamage(100f);

        new UseSkillCommand(_claire, _aegis, Heal(10f)).Execute(_ctx);

        Assert.AreEqual(180f, _aegis.CurrentHP, Tolerance);
    }

    [Test]
    public void 사망한_대상은_회복되지_않는다()
    {
        _aegis.ApplyDamage(999f);

        CommandResult result = new UseSkillCommand(_claire, _aegis, Heal(2f)).Execute(_ctx);

        Assert.AreEqual(0f, _aegis.CurrentHP, Tolerance);
        Assert.AreEqual(0, TestSkills.WithoutCast(result.Events).Count);
    }

    [Test]
    public void 체력이_가득이면_이벤트를_남기지_않는다()
    {
        CommandResult result = new UseSkillCommand(_claire, _aegis, Heal(2f)).Execute(_ctx);

        Assert.AreEqual(0, TestSkills.WithoutCast(result.Events).Count);
    }

    [Test]
    public void 아군_전체_회복이_가능하다()
    {
        _claire.ApplyDamage(50f);
        _aegis.ApplyDamage(50f);

        var targets = TargetingRule.ResolveTargets(_ctx, _claire, TargetType.Team, null);
        CommandResult result = new UseSkillCommand(_claire, targets, Heal(1f)).Execute(_ctx);

        Assert.AreEqual(2, TestSkills.WithoutCast(result.Events).Count);
        Assert.AreEqual(70f, _claire.CurrentHP, Tolerance);
        Assert.AreEqual(120f, _aegis.CurrentHP, Tolerance);
    }
}
