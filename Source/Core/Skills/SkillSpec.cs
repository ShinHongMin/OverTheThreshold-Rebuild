using System.Collections.Generic;

/// <summary>
/// 전투 계산에 사용하는 스킬 정의.
/// SkillTableBuilder가 에셋의 SkillDefinition을 이 타입으로 변환한다.
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
    /// 자원이 부족하면 예약할 수 없다.
    /// </summary>
    public bool CanAfford(Unit actor, PartyResources resources)
    {
        if (actor == null) return false;
        if (!actor.UsesResources) return true;
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
