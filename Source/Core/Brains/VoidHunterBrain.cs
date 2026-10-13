using System.Collections.Generic;

/// <summary>
/// 공허 사냥꾼. 표식 대상을 우선 공격하고 강화기를 사용한다.
/// 표식 대상이 없으면 현재 HP가 가장 낮은 상대를 노린다.
/// </summary>
public sealed class VoidHunterBrain : MonsterBrain
{
    public VoidHunterBrain(IRandom random, SkillTable skillTable, MonsterBrainConfig config)
        : base(random, skillTable, config) { }

    protected override SkillSpec SelectSkill(CombatContext ctx, Unit self)
    {
        if (FindMarked(ctx, self) != null)
        {
            SkillSpec enhanced = FindSkill(Config.EnhancedSkillId);
            if (enhanced != null) return enhanced;
        }

        return PickSkillOrBasic();
    }

    protected override IReadOnlyList<Unit> SelectTargets(CombatContext ctx, Unit self, SkillSpec skill)
    {
        // 도발로 제한된 후보 안에서 표식 우선, 없으면 최저 현재 HP.
        Unit chosen = FindMarked(ctx, self) ?? FindLowestHP(ctx, self);

        return ResolveWith(ctx, self, skill, chosen);
    }

    private Unit FindMarked(CombatContext ctx, Unit self)
        => FindOpponentWithBuff(ctx, self, Config.StateBuffId);
}
