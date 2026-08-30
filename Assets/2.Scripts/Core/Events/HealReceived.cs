/// <summary>
/// 유닛이 체력을 회복했다.
///
/// Amount는 실제로 회복된 양이다. 최대 체력 상한에 걸려 잘린 몫은 포함하지 않는다.
/// DamageDealt·ShieldGranted와 같은 규칙이다.
/// </summary>
public sealed class HealReceived : CombatEvent
{
    public Unit Target { get; }

    /// <summary>실제로 회복된 양. 화면에 표시할 값.</summary>
    public float Amount { get; }

    public float HpAfter { get; }

    public HealReceived(Unit target, float amount)
    {
        Target = target;
        Amount = amount;
        HpAfter = target.CurrentHP;
    }

    public override string ToString()
        => $"HealReceived {{ Target={Target.Name}, Amount={Amount:0.##}, HpAfter={HpAfter:0.##} }}";
}
