using System.Collections.Generic;

/// <summary>
/// 스킬 하나의 정의. Core가 다루는 순수 C# 표현이다.
///
/// ScriptableObject를 Core에 들이지 않기 위해 존재한다.
/// 에디터에서 편집하는 쪽은 Data 계층의 SkillDefinition이고,
/// SkillTableBuilder가 그것을 이 타입으로 옮긴다.
/// </summary>
public sealed class SkillSpec
{
    private static readonly SkillEffect[] NoEffects = new SkillEffect[0];

    public string SkillId { get; }
    public string DisplayName { get; }
    public SkillType Type { get; }
    public TargetType TargetType { get; }

    /// <summary>SP 비용. 기본공격은 소모가 아니라 획득량으로 쓰인다.</summary>
    public int SPCost { get; }

    public int OPCost { get; }

    /// <summary>
    /// ER 비용. 세이렌처럼 ER을 자원으로 쓰는 스킬에만 0보다 크다.
    /// 사용 조건이자 소모량이며, 부족하면 스킬을 쓸 수 없다.
    /// </summary>
    public float ERCost { get; }

    public IReadOnlyList<SkillEffect> Effects { get; }

    public SkillSpec(
        string skillId,
        string displayName,
        SkillType type,
        TargetType targetType,
        int spCost,
        int opCost,
        float erCost,
        IReadOnlyList<SkillEffect> effects)
    {
        SkillId = skillId;
        DisplayName = displayName;
        Type = type;
        TargetType = targetType;
        SPCost = spCost;
        OPCost = opCost;
        ERCost = erCost;
        Effects = effects ?? NoEffects;
    }

    /// <summary>
    /// 이 유닛이 지금 이 스킬을 쓸 수 있는가.
    /// 자원이 부족하면 예약 자체가 성립하지 않는다. (기존 CombatManager 규칙)
    /// </summary>
    public bool CanAfford(Unit actor, PartyResources resources)
    {
        if (ERCost > 0f && (actor == null || actor.CurrentER < ERCost)) return false;

        switch (Type)
        {
            case SkillType.Special:
                return resources != null && resources.CanSpendSP(SPCost);

            case SkillType.Overload:
                return resources != null && resources.CanSpendOP(OPCost);

            default:
                return true;
        }
    }

    public override string ToString()
        => $"{SkillId}({Type}, {TargetType}, 효과 {Effects.Count}개)";
}
