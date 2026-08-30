/// <summary>
/// 턴이 끝났다. 버프·실드 지속 감소가 이미 반영된 시점이다.
///
/// 만료된 버프를 개별 이벤트로 알리지 않는 이유는, 남은 턴이 1일 때
/// 아이콘을 깜빡여 미리 알리는 편이 유용하고, 만료 자체는 이 이벤트에
/// 맞춰 화면을 한 번 갱신하면 자연히 반영되기 때문이다.
/// </summary>
public sealed class TurnEnded : CombatEvent
{
    public int Round { get; }

    public TurnEnded(int round)
    {
        Round = round;
    }

    public override string ToString() => $"TurnEnded {{ Round={Round} }}";
}
