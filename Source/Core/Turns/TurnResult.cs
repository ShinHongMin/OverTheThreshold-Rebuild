using System.Collections.Generic;

/// <summary>
/// 한 턴에서 발생한 이벤트 목록과 전투 종료 여부.
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
