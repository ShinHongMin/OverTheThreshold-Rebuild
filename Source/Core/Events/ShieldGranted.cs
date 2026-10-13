/// <summary>
/// 실드 부여 결과. Amount는 최대 체력 상한을 적용한 실제 증가량이다.
/// </summary>
public sealed class ShieldGranted : CombatEvent
{
    public Unit Target { get; }

    /// <summary>실제로 늘어난 실드량. 화면에 표시할 값.</summary>
    public float Amount { get; }

    public float ShieldAfter { get; }
    public int DurationTurns { get; }

    public ShieldGranted(Unit target, float amount)
    {
        Target = target;
        Amount = amount;
        ShieldAfter = target.CurrentShield;
        DurationTurns = target.ShieldDuration;
    }

    public override string ToString()
        => $"ShieldGranted {{ Target={Target.Name}, Amount={Amount:0.##}, " +
           $"ShieldAfter={ShieldAfter:0.##}, {DurationTurns}턴 }}";
}
