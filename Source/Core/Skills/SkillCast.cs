using System.Collections.Generic;

/// <summary>
/// 스킬 사용 정보. 해당 스킬의 효과 이벤트보다 먼저 기록한다.
/// </summary>
public sealed class SkillCast : CombatEvent
{
    private static readonly Unit[] NoTargets = new Unit[0];

    public Unit Caster { get; }

    /// <summary>스킬 식별자. Presentation이 이것으로 연출 정보를 찾는다.</summary>
    public string SkillId { get; }

    public SkillType Type { get; }

    public IReadOnlyList<Unit> Targets { get; }

    public SkillCast(Unit caster, string skillId, SkillType type, IReadOnlyList<Unit> targets)
    {
        Caster = caster;
        SkillId = skillId;
        Type = type;
        Targets = targets ?? NoTargets;
    }

    public override string ToString()
        => $"SkillCast {{ {Caster.Name} → {SkillId}({Type}), 대상 {Targets.Count}명 }}";
}
