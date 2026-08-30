/// <summary>
/// 피해 하나가 실드와 체력에 어떻게 나뉘어 들어갔는지.
///
/// ApplyDamage가 두 값을 동시에 돌려줘야 하므로 존재한다.
/// 화면에 표시할 숫자(Total)와 게이지 갱신에 필요한 값을 함께 담는다.
/// 실드가 전부 막아도 Total은 줄어들지 않으므로 표시 숫자가 0이 되지 않는다.
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
