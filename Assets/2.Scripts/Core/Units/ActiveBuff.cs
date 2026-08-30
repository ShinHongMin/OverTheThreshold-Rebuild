/// <summary>
/// 유닛에게 현재 붙어 있는 버프 하나.
///
/// BuffData(ScriptableObject)를 직접 들지 않는 이유는 Core가 UnityEngine에
/// 의존하지 않기 위해서다. 대신 BuffId 문자열로 식별한다.
/// 이 문자열은 현재 BuffData 에셋의 파일 이름이며,
/// W4에서 BuffTableAsset으로 옮겨질 때 정식 필드가 된다.
///
/// 기존 ActiveBuff에 있던 startRound는 없다. 지속 감소를 각 유닛의 행동
/// 직전이 아니라 턴 종료 시 일괄로 처리하기로 하면서 예외 보정이 불필요해졌다.
/// </summary>
public sealed class ActiveBuff
{
    /// <summary>버프 식별자. 중복 갱신 판정과 특정 버프 조회에 쓰인다.</summary>
    public string BuffId { get; }

    public BuffType Type { get; }

    /// <summary>보정 수치. 0.2f 는 +20%.</summary>
    public float Value { get; private set; }

    /// <summary>남은 지속 턴 수.</summary>
    public int DurationTurns { get; private set; }

    public ActiveBuff(string buffId, BuffType type, float value, int durationTurns)
    {
        BuffId = buffId;
        Type = type;
        Value = value;
        DurationTurns = durationTurns;
    }

    /// <summary>같은 버프가 다시 걸렸을 때. 중첩되지 않고 값과 지속이 갱신된다.</summary>
    internal void Refresh(float value, int durationTurns)
    {
        Value = value;
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
