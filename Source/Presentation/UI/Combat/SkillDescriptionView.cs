using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>기존 기어 애니메이션과 스킬 정보 조회만 담당한다.</summary>
public sealed class SkillDescriptionView : MonoBehaviour
{
    [SerializeField] private Animator panelAnimator;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text descriptionText;
    private static readonly int ClosedParameter = Animator.StringToHash("IsShow");
    private SkillDefinition _displayedSkill;
    public bool IsOpen { get; private set; }

    private void Awake()
    {
        // 원본 Controller의 IsShow=true는 Down(접힘) 상태로 연결되어 있다.
        panelAnimator.enabled = true;
        panelAnimator.SetBool(ClosedParameter, true);
        panelAnimator.Play("DescriptionButton_Down", 0, 1f);
        toggleButton.onClick.AddListener(Toggle);
        Clear();
    }
    public void Toggle()
    {
        IsOpen = !IsOpen;
        panelAnimator.SetBool(ClosedParameter, !IsOpen);
    }
    public void Clear()
    {
        _displayedSkill = null;
        if (skillNameText != null) skillNameText.text = "스킬 정보";
        if (descriptionText != null) descriptionText.text = "";
    }
    public void ShowSkill(SkillDefinition skill)
    {
        if (skill == null || skill == _displayedSkill) return;
        _displayedSkill = skill;
        string cost = skill.type == SkillType.Basic ? $"SP +{skill.spCost}" :
            skill.type == SkillType.Special ? $"SP {skill.spCost}" : $"OP {skill.opCost}";
        if (skill.erCost > 0) cost += $" · ER {skill.erCost:0}";
        if (skillNameText != null) skillNameText.text = skill.displayName;
        if (descriptionText != null)
            descriptionText.text = $"{skill.displayName} ({cost})\n{skill.description}";
    }
    private void OnDestroy() { if (toggleButton != null) toggleButton.onClick.RemoveListener(Toggle); }
}
