using System.Collections.Generic;

/// <summary>
/// 여러 SkillTableAsset의 정의를 Core의 SkillTable로 변환한다.
/// 에셋 참조와 Unity 의존성은 이 계층에서 처리한다.
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
