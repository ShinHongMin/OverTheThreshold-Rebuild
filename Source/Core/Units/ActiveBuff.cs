/// <summary>
/// 유닛에 적용된 버프의 수치와 남은 턴 수. BuffId로 중복 여부를 판정한다.
/// </summary>
public sealed class ActiveBuff
{
    /// <summary>버프 식별자. 중복 갱신 판정과 특정 버프 조회에 쓰인다.</summary>
    public string BuffId { get; }

    public BuffType Type { get; }
    public bool IsDebuff { get; private set; }

    /// <summary>보정 수치. 0.2f 는 +20%.</summary>
    public float Value { get; private set; }

    /// <summary>남은 지속 턴 수.</summary>
    public int DurationTurns { get; private set; }

    public ActiveBuff(string buffId, BuffType type, float value, int durationTurns, bool isDebuff = false)
    {
        BuffId = buffId;
        Type = type;
        Value = value;
        IsDebuff = isDebuff;
        DurationTurns = durationTurns;
    }

    /// <summary>같은 버프가 다시 걸렸을 때. 중첩되지 않고 값과 지속이 갱신된다.</summary>
    internal void Refresh(float value, int durationTurns, bool isDebuff = false)
    {
        Value = value;
        IsDebuff = isDebuff;
        DurationTurns = durationTurns;
    }

    /// <summary>지속 턴을 1 줄인다. 만료되면 true를 반환한다.</summary>
    internal bool TickDuration()
    {
        DurationTurns--;
        return DurationTurns <= 0;
    }

    public override string ToString()
        => $"{BuffId}({Type} {Value * 100f:+0.#;-0.#}%, {DurationTurns}턴)";
}
