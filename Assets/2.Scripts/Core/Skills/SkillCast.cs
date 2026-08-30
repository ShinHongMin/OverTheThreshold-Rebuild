using System.Collections.Generic;

/// <summary>
/// 스킬이 발동되었다. 효과가 적용되기 전에 먼저 나온다.
///
/// 재생기가 "누가 어떤 스킬을 썼는가"를 알아야 시전 모션을 재생하고
/// SkillPresentation(연출 정보)을 조회할 수 있다.
/// 이 이벤트가 없으면 이벤트 목록에 결과만 남아, 공격 동작 없이
/// 피격 반응부터 재생되는 문제가 생긴다.
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
