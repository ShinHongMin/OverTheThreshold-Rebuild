using System.Collections.Generic;

/// <summary>한 행동 또는 턴 경계의 결과. 행동은 효과 발동 시점까지 계산하지 않는다.</summary>
public sealed class TurnStep
{
    public ICombatCommand Command { get; }
    public IReadOnlyList<CombatEvent> Events { get; }
    public BattleResult Battle { get; }
    public TurnStep(ICombatCommand command, IReadOnlyList<CombatEvent> events, BattleResult battle)
    {
        Command = command;
        Events = events;
        Battle = battle;
    }
}
