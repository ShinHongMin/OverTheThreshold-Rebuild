using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>버프 하나의 이미지와 남은 턴을 표시한다. 전투 수치는 변경하지 않는다.</summary>
public sealed class BuffIconView : MonoBehaviour, IPooledView, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text remainingTurnsText;
    [SerializeField] private CanvasGroup visibilityGroup;
    private bool _expiresNextTurn;
    private Sprite _displayedIcon;
    private int _remainingTurns = -1;
    private BuffTooltipView _tooltip;
    private BuffData _definition;
    private bool _isHovered;

    public void ConfigureTooltip(BuffTooltipView tooltip)
    {
        HideTooltip();
        _tooltip = tooltip;
    }

    public void Show(ActiveBuff buff, BuffData definition)
    {
        _definition = definition;
        Sprite icon = definition.icon;
        if (_displayedIcon != icon)
        {
            _displayedIcon = icon;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
        if (_isHovered && _tooltip != null)
            _tooltip.ShowBuff(this, definition, buff.DurationTurns, Input.mousePosition);
        if (_remainingTurns == buff.DurationTurns) return;
        _remainingTurns = buff.DurationTurns;
        remainingTurnsText.SetText("{0}", _remainingTurns);
        _expiresNextTurn = buff.DurationTurns == 1;
        if (!_expiresNextTurn) visibilityGroup.alpha = 1f;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        if (_tooltip != null) _tooltip.ShowBuff(this, _definition, _remainingTurns, eventData.position);
    }
    public void OnPointerExit(PointerEventData eventData) => HideTooltip();
    // 버프 조회 클릭을 부모 유닛의 공격 대상 선택으로 전달하지 않는다.
    public void OnPointerClick(PointerEventData eventData) { }
    private void OnDisable() => HideTooltip();
    private void HideTooltip()
    {
        _isHovered = false;
        if (_tooltip != null) _tooltip.HideFor(this);
    }

    private void Update()
    {
        if (_expiresNextTurn)
            visibilityGroup.alpha = Mathf.Lerp(0.35f, 1f, (Mathf.Sin(Time.unscaledTime * 6f) + 1f) * 0.5f);
    }

    public void OnRent()
    {
        _isHovered = false;
        _definition = null;
        _expiresNextTurn = false;
        _displayedIcon = null;
        _remainingTurns = -1;
        visibilityGroup.alpha = 1f;
    }
    public void OnReturn()
    {
        HideTooltip();
        _tooltip = null;
        _definition = null;
        _expiresNextTurn = false;
        _displayedIcon = null;
        _remainingTurns = -1;
        visibilityGroup.alpha = 1f;
        iconImage.sprite = null;
        remainingTurnsText.text = "";
    }
}
