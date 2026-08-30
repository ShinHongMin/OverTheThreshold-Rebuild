using System.Collections.Generic;

/// <summary>
/// 한 턴을 실행한 결과.
///
/// 그 턴의 스냅샷이므로 나중에 읽어도 값이 변하지 않는다.
/// TurnLoop이 마지막 결과를 들고 있는 방식이었다면 다음 턴에 덮여서,
/// 연출을 재생하는 동안 결과를 다시 읽을 때 어긋날 수 있다.
/// </summary>
public readonly struct TurnResult
{
    private static readonly CombatEvent[] NoEvents = new CombatEvent[0];

    public IReadOnlyList<CombatEvent> Events { get; }
    public BattleResult Battle { get; }

    /// <summary>이 턴에 전투가 끝났는가.</summary>
    public bool IsBattleOver => Battle != BattleResult.InProgress;

    public TurnResult(IReadOnlyList<CombatEvent> events, BattleResult battle)
    {
        Events = events ?? NoEvents;
        Battle = battle;
    }
}
