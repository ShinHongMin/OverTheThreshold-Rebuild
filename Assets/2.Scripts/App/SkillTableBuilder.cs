using System.Collections.Generic;

/// <summary>
/// SkillTableAsset(ScriptableObject)들을 Core의 SkillTable 하나로 합친다.
///
/// UnitFactory와 같은 역할이다. SO 의존이 여기서 끊기고
/// Core는 순수 C# 타입만 받는다.
///
/// 테이블 에셋이 소유자별로 나뉘어 있는 이유는 편집 편의 때문이며,
/// Core 입장에서는 스킬 ID 하나의 평평한 목록이면 충분하다.
/// </summary>
public static class SkillTableBuilder
{
    public static SkillTable Build(params SkillTableAsset[] assets)
        => Build((IEnumerable<SkillTableAsset>)assets);

    public static SkillTable Build(IEnumerable<SkillTableAsset> assets)
    {
        var specs = new List<SkillSpec>();

        if (assets == null) return new SkillTable(specs);

        foreach (SkillTableAsset asset in assets)
        {
            if (asset == null) continue;

            foreach (SkillDefinition definition in asset.skills)
            {
                SkillSpec spec = Convert(definition);
                if (spec != null) specs.Add(spec);
            }
        }

        return new SkillTable(specs);
    }

    public static SkillSpec Convert(SkillDefinition definition)
    {
        if (definition == null || string.IsNullOrEmpty(definition.skillId)) return null;

        // 효과 인스턴스는 이미 순수 C#이므로 그대로 넘긴다.
        var effects = new List<SkillEffect>();

        foreach (SkillEffect effect in definition.effects)
            if (effect != null) effects.Add(effect);

        return new SkillSpec(
            definition.skillId,
            definition.displayName,
            definition.type,
            definition.targetType,
            definition.spCost,
            definition.opCost,
            definition.erCost,
            effects);
    }
}
