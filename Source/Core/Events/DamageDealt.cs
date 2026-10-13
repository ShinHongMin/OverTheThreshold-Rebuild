/// <summary>
/// 유닛이 피해를 입었다.
///
/// float인 이유: 정수 반올림은 표시 규칙이므로 화면에 찍기 직전에 하고,
/// 게이지 채움 비율에는 반올림하지 않은 원본 값이 필요하다.
/// </summary>
public sealed class DamageDealt : CombatEvent
{
    public Unit Target { get; }

    /// <summary>화면에 표시할 피해량. 실드가 막은 몫도 포함한다.</summary>
    public float Amount { get; }

    /// <summary>그중 실드가 흡수한 양.</summary>
    public float ShieldAbsorbed { get; }

    public float HpAfter { get; }
    public float ShieldAfter { get; }

    public DamageDealt(Unit target, DamageBreakdown breakdown)
    {
        Target = target;
        Amount = breakdown.Total;
        ShieldAbsorbed = breakdown.ShieldAbsorbed;
        HpAfter = target.CurrentHP;
        ShieldAfter = target.CurrentShield;
    }

    public override string ToString()
        => $"DamageDealt {{ Target={Target.Name}, Amount={Amount:0.##}, " +
           $"Shield={ShieldAbsorbed:0.##}, HpAfter={HpAfter:0.##}, ShieldAfter={ShieldAfter:0.##} }}";
}
