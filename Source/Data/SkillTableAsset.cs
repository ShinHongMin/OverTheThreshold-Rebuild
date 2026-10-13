using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 정의 목록을 관리하며, 에디터에서 ID 누락과 중복을 검사한다.
/// </summary>
[CreateAssetMenu(fileName = "SkillTable", menuName = "OTT/Skill Table")]
public class SkillTableAsset : ScriptableObject
{
    public List<SkillDefinition> skills = new List<SkillDefinition>();

    /// <summary>ID 중복이나 누락을 에디터에서 즉시 알린다.</summary>
    private void OnValidate()
    {
        var seen = new HashSet<string>();

        foreach (SkillDefinition skill in skills)
        {
            if (skill == null) continue;

            if (string.IsNullOrEmpty(skill.skillId))
            {
                Debug.LogWarning($"[{name}] skillId가 비어 있는 항목이 있습니다.", this);
                continue;
            }

            if (!seen.Add(skill.skillId))
                Debug.LogWarning($"[{name}] skillId 중복: {skill.skillId}", this);
        }
    }
}
