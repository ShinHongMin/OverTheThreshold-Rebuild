/// <summary>
/// 유닛에게 버프가 걸렸다. 이미 같은 버프가 있었다면 갱신된 것이다.
///
/// 아이콘과 툴팁은 Presentation이 BuffId로 BuffData를 찾아 표시한다.
/// Core는 어떤 그림을 쓸지 모른다.
/// </summary>
public sealed class BuffApplied : CombatEvent
{
    public Unit Target { get; }
    public string BuffId { get; }
    public BuffType Type { get; }
    public float Value { get; }
    public int DurationTurns { get; }

    public BuffApplied(Unit target, string buffId, BuffType type, float value, int durationTurns)
    {
        Target = target;
        BuffId = buffId;
        Type = type;
        Value = value;
        DurationTurns = durationTurns;
    }

    public override string ToString()
        => $"BuffApplied {{ Target={Target.Name}, Buff={BuffId}, " +
           $"Value={Value * 100f:+0.#;-0.#}%, {DurationTurns}턴 }}";
}
