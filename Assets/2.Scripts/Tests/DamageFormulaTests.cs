using NUnit.Framework;

/// <summary>
/// 데미지 공식 테스트. 씬을 실행하지 않고 EditMode에서 바로 돌아간다.
/// asmdef 설정이 제대로 됐는지 확인하는 용도도 겸한다.
/// </summary>
public class DamageFormulaTests
{
    private const float Tolerance = 0.01f;

    [Test]
    public void 방어력이_0이면_공격력에_배율만_곱해진다()
    {
        float damage = DamageFormula.Calculate(attackerATK: 50f, skillMultiplier: 1f, targetDEF: 0f);
        Assert.AreEqual(50f, damage, Tolerance);
    }

    [Test]
    public void 방어력이_100이면_피해가_절반이_된다()
    {
        // 1 - 100 / (100 + 100) = 0.5
        float damage = DamageFormula.Calculate(attackerATK: 50f, skillMultiplier: 1f, targetDEF: 100f);
        Assert.AreEqual(25f, damage, Tolerance);
    }

    [Test]
    public void 방어력이_아무리_높아도_최소_1의_피해는_들어간다()
    {
        float damage = DamageFormula.Calculate(attackerATK: 1f, skillMultiplier: 1f, targetDEF: 99999f);
        Assert.AreEqual(DamageFormula.MinimumDamage, damage, Tolerance);
    }

    [Test]
    public void 유닛_오버로드는_최종_스탯을_사용한다()
    {
        var aegis = new Unit("Aegis", new UnitStats(maxHp: 200f, atk: 50f, def: 20f));
        var scout = new Unit("LostScout", new UnitStats(maxHp: 80f, atk: 30f, def: 100f));

        float damage = DamageFormula.Calculate(aegis, scout, skillMultiplier: 1f);
        Assert.AreEqual(25f, damage, Tolerance);
    }

    [Test]
    public void 피해를_입으면_체력이_줄고_깎인_양을_반환한다()
    {
        var scout = new Unit("LostScout", new UnitStats(maxHp: 80f, atk: 30f, def: 0f));

        float applied = scout.ApplyDamage(30f);

        Assert.AreEqual(30f, applied, Tolerance);
        Assert.AreEqual(50f, scout.CurrentHP, Tolerance);
        Assert.IsTrue(scout.IsAlive);
    }

    [Test]
    public void 체력은_0_아래로_내려가지_않는다()
    {
        var scout = new Unit("LostScout", new UnitStats(maxHp: 80f, atk: 30f, def: 0f));

        float applied = scout.ApplyDamage(500f);

        Assert.AreEqual(80f, applied, Tolerance);   // 남은 체력만큼만 깎인다
        Assert.AreEqual(0f, scout.CurrentHP, Tolerance);
        Assert.IsFalse(scout.IsAlive);
    }
}
