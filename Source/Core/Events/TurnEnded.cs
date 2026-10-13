/// <summary>
/// 버프·실드의 지속 턴 감소를 마친 시점. 화면은 갱신된 상태를 읽어 표시한다.
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
