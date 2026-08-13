/// <summary>
/// 유닛이 피해를 입었다.
///
/// float인 이유: 정수 반올림은 표시 규칙이므로 FloatingText가 찍기 직전에 하고,
/// HP 바 채움 비율에는 반올림하지 않은 원본 값이 필요하다.
/// </summary>
public sealed class DamageDealt : CombatEvent
{
    public Unit Target { get; }
    public float Amount { get; }
    public float HpAfter { get; }

    public DamageDealt(Unit target, float amount, float hpAfter)
    {
        Target = target;
        Amount = amount;
        HpAfter = hpAfter;
    }

    public override string ToString()
        => $"DamageDealt {{ Target={Target.Name}, Amount={Amount:0.##}, HpAfter={HpAfter:0.##} }}";
}
