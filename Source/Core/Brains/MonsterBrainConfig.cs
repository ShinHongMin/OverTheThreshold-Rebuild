using System.Collections.Generic;

/// <summary>
/// 몬스터 AI에 필요한 스킬 ID와 수치. UnitData에서 읽어 전달한다.
/// </summary>
public sealed class MonsterBrainConfig
{
    private static readonly string[] NoSkills = new string[0];

    public string BasicAttackId { get; }

    /// <summary>기본공격을 제외한 보유 스킬 ID.</summary>
    public IReadOnlyList<string> SkillIds { get; }

    /// <summary>보유 스킬을 쓸 확률. 0.5면 절반.</summary>
    public float SkillChance { get; }

    /// <summary>충전형이 쓰는 강화 스킬, 추적형이 표식 대상에게 쓰는 스킬.</summary>
    public string EnhancedSkillId { get; }

    /// <summary>충전 상태를 나타내는 버프(충전형), 또는 추적 대상 표식(추적형).</summary>
    public string StateBuffId { get; }

    public MonsterBrainConfig(
        string basicAttackId,
        IReadOnlyList<string> skillIds = null,
        float skillChance = 0.5f,
        string enhancedSkillId = null,
        string stateBuffId = null)
    {
        BasicAttackId = basicAttackId;
        SkillIds = skillIds ?? NoSkills;
        SkillChance = skillChance;
        EnhancedSkillId = enhancedSkillId;
        StateBuffId = stateBuffId;
    }
}
