using System.Collections.Generic;

/// <summary>
/// 커맨드 실행으로 일어난 일들. Presentation은 이 목록을 순서대로 재생한다.
/// </summary>
public readonly struct CommandResult
{
    private static readonly CombatEvent[] NoEvents = new CombatEvent[0];

    public IReadOnlyList<CombatEvent> Events { get; }

    public CommandResult(IReadOnlyList<CombatEvent> events)
    {
        Events = events ?? NoEvents;
    }

    public static CommandResult Empty => new CommandResult(NoEvents);
}
