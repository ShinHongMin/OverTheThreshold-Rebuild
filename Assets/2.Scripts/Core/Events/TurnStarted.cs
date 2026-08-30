/// <summary>턴이 시작되었다. 라운드 표시 갱신에 쓰인다.</summary>
public sealed class TurnStarted : CombatEvent
{
    public int Round { get; }

    public TurnStarted(int round)
    {
        Round = round;
    }

    public override string ToString() => $"TurnStarted {{ Round={Round} }}";
}
