using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스킬 정의를 한 에셋에 모아 관리한다.
///
/// 기존에는 SkillData 에셋이 28개로 흩어져 있어 밸런싱 시 파일을 하나씩 열어야 했고,
/// 참조는 인스펙터 드래그였다. 하나의 목록으로 모으면 전체를 나란히 비교할 수 있고,
/// 스프레드시트 임포터를 붙일 때도 시트 한 장과 그대로 대응된다.
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
