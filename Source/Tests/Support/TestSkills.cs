using System.Collections.Generic;

/// <summary>
/// 테스트에서 쓰는 SkillSpec 생성 도우미.
///
/// 실제 게임에서는 SkillTable에서 꺼낸 스킬을 커맨드에 넘긴다.
/// 테스트도 같은 형태를 쓰기 위해 여기서 SkillSpec을 만든다.
/// </summary>
public static class TestSkills
{
    /// <summary>효과 목록만 지정한 기본공격 형태의 스킬.</summary>
    public static SkillSpec Make(params SkillEffect[] effects)
        => Make("SKILL_TEST", SkillType.Basic, TargetType.SingleEnemy, effects);

    public static SkillSpec Make(string id, params SkillEffect[] effects)
        => Make(id, SkillType.Basic, TargetType.SingleEnemy, effects);

    public static SkillSpec Make(string id, SkillType type, TargetType targetType, params SkillEffect[] effects)
        => new SkillSpec(
            skillId: id,
            displayName: id,
            type: type,
            targetType: targetType,
            spCost: 0,
            opCost: 0,
            erCost: 0f,
            effects: effects);

    /// <summary>비용이 있는 스킬.</summary>
    public static SkillSpec WithCost(SkillType type, int spCost, int opCost, float erCost, params SkillEffect[] effects)
        => new SkillSpec(
            skillId: "SKILL_COST",
            displayName: "비용 스킬",
            type: type,
            targetType: TargetType.SingleEnemy,
            spCost: spCost,
            opCost: opCost,
            erCost: erCost,
            effects: effects);

    /// <summary>피해 스킬. 가장 자주 쓰인다.</summary>
    public static SkillSpec Damage(float multiplier = 1f)
        => Make(new DamageEffect(multiplier));

    /// <summary>이벤트 목록에서 SkillCast를 제외한 것만 센다.</summary>
    public static List<CombatEvent> WithoutCast(IReadOnlyList<CombatEvent> events)
    {
        var result = new List<CombatEvent>();

        foreach (CombatEvent e in events)
            if (!(e is SkillCast)) result.Add(e);

        return result;
    }
}
