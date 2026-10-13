/// <summary>
/// 실드가 흡수한 피해와 실제 HP 감소량.
/// 표시할 총 피해량은 두 값을 합산한다.
/// </summary>
public readonly struct DamageBreakdown
{
    public static readonly DamageBreakdown None = new DamageBreakdown(0f, 0f);

    /// <summary>실드가 흡수한 양.</summary>
    public readonly float ShieldAbsorbed;

    /// <summary>체력에서 실제로 깎인 양.</summary>
    public readonly float HpDamage;

    /// <summary>화면에 표시할 총량. 남은 체력을 넘는 초과분은 포함하지 않는다.</summary>
    public float Total => ShieldAbsorbed + HpDamage;

    public DamageBreakdown(float shieldAbsorbed, float hpDamage)
    {
        ShieldAbsorbed = shieldAbsorbed;
        HpDamage = hpDamage;
    }
}
