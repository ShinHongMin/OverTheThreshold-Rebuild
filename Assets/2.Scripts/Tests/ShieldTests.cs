using NUnit.Framework;

/// <summary>
/// 실드 규칙 검증. 기존 CharactersStat.AddShield / TakeDamage 규칙을 옮긴 것이다.
/// 지속 턴 감소는 TurnLoop이 담당하므로 여기서 검증하지 않는다.
/// </summary>
public class ShieldTests
{
    private const float Tolerance = 0.01f;

    private static Unit MakeUnit(float maxHp = 100f, float def = 0f)
        => new Unit("TestUnit", new UnitStats(maxHp: maxHp, atk: 10f, def: def));

    // ── 흡수 ─────────────────────────────────────────────────────

    [Test]
    public void 실드가_피해를_먼저_흡수하고_체력은_줄지_않는다()
    {
        Unit unit = MakeUnit();
        unit.AddShield(50f, duration: 2);

        DamageBreakdown result = unit.ApplyDamage(30f);

        Assert.AreEqual(30f, result.ShieldAbsorbed, Tolerance);
        Assert.AreEqual(0f, result.HpDamage, Tolerance);
        Assert.AreEqual(20f, unit.CurrentShield, Tolerance);
        Assert.AreEqual(100f, unit.CurrentHP, Tolerance);
    }

    [Test]
    public void 실드를_넘는_피해는_남은_만큼_체력에서_깎인다()
    {
        Unit unit = MakeUnit();
        unit.AddShield(20f, duration: 2);

        DamageBreakdown result = unit.ApplyDamage(50f);

        Assert.AreEqual(20f, result.ShieldAbsorbed, Tolerance);
        Assert.AreEqual(30f, result.HpDamage, Tolerance);
        Assert.AreEqual(0f, unit.CurrentShield, Tolerance);
        Assert.AreEqual(70f, unit.CurrentHP, Tolerance);
    }

    [Test]
    public void 실드가_전부_막아도_표시할_피해량은_줄지_않는다()
    {
        Unit unit = MakeUnit();
        unit.AddShield(200f, duration: 2);

        DamageBreakdown result = unit.ApplyDamage(80f);

        Assert.AreEqual(80f, result.Total, Tolerance);   // 화면에 0이 아니라 80
    }

    [Test]
    public void 실드가_깨지면_지속턴도_함께_사라진다()
    {
        Unit unit = MakeUnit();
        unit.AddShield(20f, duration: 5);

        unit.ApplyDamage(100f);

        Assert.AreEqual(0f, unit.CurrentShield, Tolerance);
        Assert.AreEqual(0, unit.ShieldDuration);
    }

    // ── 부여 ─────────────────────────────────────────────────────

    [Test]
    public void 실드는_누적되지만_최대_체력을_넘지_않는다()
    {
        Unit unit = MakeUnit(maxHp: 100f);

        unit.AddShield(70f, duration: 2);
        unit.AddShield(70f, duration: 2);

        Assert.AreEqual(100f, unit.CurrentShield, Tolerance);
    }

    [Test]
    public void 상한에_걸리면_실제로_늘어난_만큼만_반환한다()
    {
        Unit unit = MakeUnit(maxHp: 100f);
        unit.AddShield(70f, duration: 2);

        float applied = unit.AddShield(70f, duration: 2);

        Assert.AreEqual(30f, applied, Tolerance);   // 70이 아니라 30
    }

    [Test]
    public void 실드_지속턴은_더_긴_쪽으로_갱신된다()
    {
        Unit unit = MakeUnit();

        unit.AddShield(10f, duration: 3);
        unit.AddShield(10f, duration: 1);   // 짧은 쪽은 무시된다

        Assert.AreEqual(3, unit.ShieldDuration);
    }

    // ── 효과 / 이벤트 ────────────────────────────────────────────

    [Test]
    public void 실드_효과는_방어력을_기준으로_계산된다()
    {
        Unit caster = MakeUnit(def: 20f);
        Unit target = MakeUnit(def: 20f);
        var ctx = new CombatContext(new[] { caster, target }, new[] { MakeUnit() });

        SkillSpec skill = TestSkills.Make(new ShieldEffect(multiplier: 2f, durationTurns: 3));
        CommandResult result = new UseSkillCommand(caster, target, skill).Execute(ctx);

        Assert.AreEqual(40f, target.CurrentShield, Tolerance);   // 20 * 2
        Assert.AreEqual(3, target.ShieldDuration);

        var granted = TestSkills.WithoutCast(result.Events)[0] as ShieldGranted;
        Assert.IsNotNull(granted);
        Assert.AreEqual(40f, granted.Amount, Tolerance);
    }

    [Test]
    public void 실드_효과는_최대_체력_기준으로도_계산할_수_있다()
    {
        Unit caster = MakeUnit(maxHp: 100f, def: 20f);
        var ctx = new CombatContext(new[] { caster }, new[] { MakeUnit() });

        SkillSpec skill = TestSkills.Make(
            new ShieldEffect(multiplier: 0.3f, durationTurns: 2, useMaxHpAsBase: true));

        new UseSkillCommand(caster, caster, skill).Execute(ctx);

        Assert.AreEqual(30f, caster.CurrentShield, Tolerance);   // 100 * 0.3
    }

    [Test]
    public void 커맨드_실행_결과에_실드_흡수량이_담긴다()
    {
        Unit attacker = new Unit("Attacker", new UnitStats(maxHp: 100f, atk: 50f, def: 0f));
        Unit target = MakeUnit();
        target.AddShield(30f, duration: 2);

        var ctx = new CombatContext(new[] { attacker }, new[] { target });
        CommandResult result = new UseSkillCommand(attacker, target, TestSkills.Damage()).Execute(ctx);

        var damage = (DamageDealt)TestSkills.WithoutCast(result.Events)[0];

        Assert.AreEqual(50f, damage.Amount, Tolerance);           // 표시값은 전체
        Assert.AreEqual(30f, damage.ShieldAbsorbed, Tolerance);
        Assert.AreEqual(80f, damage.HpAfter, Tolerance);          // 100 - 20
        Assert.AreEqual(0f, damage.ShieldAfter, Tolerance);
    }
}
