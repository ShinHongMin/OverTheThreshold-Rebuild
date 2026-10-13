using System.Collections.Generic;

/// <summary>
/// 스킬 ID로 SkillSpec을 조회한다.
/// </summary>
public sealed class SkillTable
{
    private readonly Dictionary<string, SkillSpec> _byId;

    public SkillTable(IEnumerable<SkillSpec> skills)
    {
        _byId = new Dictionary<string, SkillSpec>();

        if (skills == null) return;

        foreach (SkillSpec skill in skills)
        {
            if (skill == null || string.IsNullOrEmpty(skill.SkillId)) continue;
            _byId[skill.SkillId] = skill;
        }
    }

    public int Count => _byId.Count;

    public bool Contains(string skillId)
        => !string.IsNullOrEmpty(skillId) && _byId.ContainsKey(skillId);

    /// <summary>찾지 못하면 null을 반환한다.</summary>
    public SkillSpec Find(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;
        return _byId.TryGetValue(skillId, out SkillSpec spec) ? spec : null;
    }

    /// <summary>
    /// 반드시 있어야 하는 스킬을 가져온다. 없으면 예외를 던진다.
    /// </summary>
    public SkillSpec Get(string skillId)
    {
        SkillSpec spec = Find(skillId);

        if (spec == null)
            throw new KeyNotFoundException($"스킬 '{skillId}'을(를) 테이블에서 찾을 수 없습니다.");

        return spec;
    }

    public IEnumerable<SkillSpec> All => _byId.Values;
}
