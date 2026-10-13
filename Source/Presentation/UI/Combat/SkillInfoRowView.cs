using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>정보창의 스킬 한 줄. 내용 길이에 맞춰 높이를 정하고 풀에서 재사용한다.</summary>
public sealed class SkillInfoRowView : MonoBehaviour, IPooledView
{
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private LayoutElement rowLayout;
    private string _displayedSkillName;
    private string _displayedSkillDescription;
    private float _measuredTextWidth;
    private bool _hasCachedContent;

    public void Show(SkillDefinition skill)
    {
        bool contentChanged = !_hasCachedContent
            || _displayedSkillName != skill.displayName
            || _displayedSkillDescription != skill.description;
        if (contentChanged)
        {
            _displayedSkillName = skill.displayName;
            _displayedSkillDescription = skill.description;
            descriptionText.text = $"<size=110%>{skill.displayName}</size>\n<color=#DDDDDD>{skill.description}</color>";
        }
        float textWidth = ((RectTransform)transform).rect.width
            - descriptionText.margin.x - descriptionText.margin.z
            - descriptionText.rectTransform.offsetMin.x + descriptionText.rectTransform.offsetMax.x;
        if (!contentChanged && Mathf.Approximately(_measuredTextWidth, textWidth)) return;

        rowLayout.preferredHeight = Mathf.Max(100f,
            descriptionText.GetPreferredValues(descriptionText.text, textWidth, 0f).y + 10f);
        _measuredTextWidth = textWidth;
        _hasCachedContent = true;
    }

    public void OnRent() { }
    // 반환 시 풀에서 비활성화한다. 마지막 내용만 보관하고, 다음 Show에서 변경 여부를 확인한다.
    public void OnReturn() { }
}
