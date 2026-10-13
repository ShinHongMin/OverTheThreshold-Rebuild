using System.Collections.Generic;

/// <summary>
/// 임계 질량. 충전 버프가 있으면 강화기를 사용한다.
/// 충전하지 않은 상태에서는 확률에 따라 충전 또는 기본공격을 선택한다.
/// </summary>
public sealed class ThresholdMassBrain : MonsterBrain
{
    public ThresholdMassBrain(IRandom random, SkillTable skillTable, MonsterBrainConfig config)
        : base(random, skillTable, config) { }

    /// <summary>충전 중인가. 자기에게 걸린 버프로 판단한다.</summary>
    public bool IsCharging(Unit self)
        => self != null
        && !string.IsNullOrEmpty(Config.StateBuffId)
        && self.Stats.HasBuff(Config.StateBuffId);

    protected override SkillSpec SelectSkill(CombatContext ctx, Unit self)
    {
        if (IsCharging(self))
        {
            SkillSpec enhanced = FindSkill(Config.EnhancedSkillId);
            if (enhanced != null) return enhanced;
        }

        return PickSkillOrBasic();
    }
}
