/// <summary>
/// 유닛이 실드를 얻었다.
///
/// 기존에는 CharactersStat.AddShield가 CombatUI를 직접 호출해
/// 흰색 숫자를 띄웠다(FloatingTextType.Shield). 그 역할을 이 이벤트가 대신한다.
///
/// Amount는 실제로 늘어난 양이다. 최대 체력 상한에 걸려 잘린 몫은 포함하지 않는다.
/// DamageDealt가 "실제로 적용된 총량"을 담는 것과 같은 규칙이다.
/// (기존 코드는 상한과 무관하게 시도한 양을 그대로 표시했다)
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
