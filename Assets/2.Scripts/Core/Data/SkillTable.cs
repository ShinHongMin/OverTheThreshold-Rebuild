using System.Collections.Generic;

/// <summary>
/// 스킬 ID로 정의를 찾는 조회 테이블.
///
/// 기존에는 각 컨트롤러가 SkillData 에셋을 인스펙터 드래그로 참조했다.
/// 에셋을 옮기거나 이름을 바꾸면 참조가 끊기고, 끊겨도 실행 전까지 알 수 없었다.
/// ID 조회로 바꾸면 실패가 즉시 드러나고, 세이브에 스킬을 기록할 수 있게 된다.
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
    /// 오타나 이관 누락을 조용히 넘기지 않기 위한 경로다.
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
