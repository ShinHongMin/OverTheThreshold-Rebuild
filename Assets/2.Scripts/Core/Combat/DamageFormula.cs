/// <summary>
/// 데미지 계산. 기존 CombatCalculator에서 규칙만 떼어낸 것으로,
/// 부수효과가 없는 순수 함수라 씬을 띄우지 않고 테스트할 수 있다.
/// </summary>
public static class DamageFormula
{
    /// <summary>방어 감쇠 상수. 값이 클수록 방어력의 효율이 낮아진다.</summary>
    public const float DefenseConstant = 100f;

    /// <summary>어떤 경우에도 보장되는 최소 피해량.</summary>
    public const float MinimumDamage = 1f;

    /// <summary>
    /// 최종 = max(1, ATK * 배율 * (1 - DEF / (DEF + 100)) * 증폭)
    /// </summary>
    /// <param name="damageAmp">받는 피해 증가 디버프 등의 합. 보정이 없으면 1f.</param>
    public static float Calculate(float attackerATK, float skillMultiplier,
                                  float targetDEF, float damageAmp = 1f)
    {
        float baseDamage      = attackerATK * skillMultiplier;
        float defenseModifier = 1f - targetDEF / (targetDEF + DefenseConstant);
        float result          = baseDamage * defenseModifier * damageAmp;

        return result < MinimumDamage ? MinimumDamage : result;
    }

    /// <summary>유닛을 직접 넘기는 편의 오버로드.</summary>
    public static float Calculate(Unit attacker, Unit target,
                                  float skillMultiplier, float damageAmp = 1f)
        => Calculate(attacker.Stats.Get(StatType.ATK), skillMultiplier,
                     target.Stats.Get(StatType.DEF), damageAmp);
}
