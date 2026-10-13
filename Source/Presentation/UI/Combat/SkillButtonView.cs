using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>클릭 구독은 한 번만 연결한다. 캐릭터 교체 시 데이터만 다시 바인딩한다.</summary>
[RequireComponent(typeof(Button))]
public sealed class SkillButtonView : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image availabilityHighlight;
    [SerializeField] private Animator highlightAnimator;
    [SerializeField] private RuntimeAnimatorController normalHighlightAnimation;
    [SerializeField] private RuntimeAnimatorController enhancedHighlightAnimation;
    private Button _button;
    private SkillSpec _skill;
    private Action<SkillSpec> _selected;
    private Action<SkillSpec> _preview;
    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(Click);
    }
    public void Bind(SkillSpec skill, bool affordable, Action<SkillSpec> selected, Sprite skillIcon = null, bool enhanced = false, Action<SkillSpec> preview = null)
    {
        _skill = skill;
        _preview = preview;
        if (iconImage != null)
        {
            iconImage.sprite = skillIcon;
            iconImage.gameObject.SetActive(skill != null && skillIcon != null);
        }
        _selected = selected;
        if (_button == null) _button = GetComponent<Button>();
        _button.interactable = skill != null && affordable;
        if (availabilityHighlight != null)
        {
            var animation = enhanced ? enhancedHighlightAnimation : normalHighlightAnimation;
            if (highlightAnimator != null && highlightAnimator.runtimeAnimatorController != animation)
                highlightAnimator.runtimeAnimatorController = animation;
            availabilityHighlight.gameObject.SetActive(skill != null && affordable);
        }
        if (label != null) label.text = skill == null ? "—" : skill.DisplayName;
    }
    public void OnPointerEnter(PointerEventData eventData) { if (_skill != null) _preview?.Invoke(_skill); }
    public void OnSelect(BaseEventData eventData) { if (_skill != null) _preview?.Invoke(_skill); }
    private void OnDisable() { if (availabilityHighlight != null) availabilityHighlight.gameObject.SetActive(false); }
    private void Click() { if (_skill != null) _selected?.Invoke(_skill); }
    private void OnDestroy() { if (_button != null) _button.onClick.RemoveListener(Click); }
}
