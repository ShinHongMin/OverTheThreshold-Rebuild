/// <summary>
/// 자원 수치가 변했다. ER, SP, OP가 여기에 해당한다.
///
/// 기존에는 CharactersStat의 currentER setter가 UI를 직접 호출해서
/// 규칙 계층이 화면을 알고 있었다(기획서 문제 2). 그 역할을 이 이벤트가 대신한다.
/// </summary>
public sealed class ResourceChanged : CombatEvent
{
    /// <summary>ER처럼 유닛에 속한 자원이면 해당 유닛, 파티 공용이면 null.</summary>
    public Unit Target { get; }

    public ResourceKind Kind { get; }
    public float Before { get; }
    public float After { get; }

    /// <summary>변화량. 음수면 감소.</summary>
    public float Delta => After - Before;

    public ResourceChanged(Unit target, ResourceKind kind, float before, float after)
    {
        Target = target;
        Kind = kind;
        Before = before;
        After = after;
    }

    public override string ToString()
    {
        string who = Target != null ? Target.Name : "파티";
        return $"ResourceChanged {{ {who} {Kind} {Before:0.#} → {After:0.#} }}";
    }
}

/// <summary>변한 자원의 종류.</summary>
public enum ResourceKind
{
    ER = 0,
    SP = 1,
    OP = 2,
}
