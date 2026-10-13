using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>클릭은 행동 입력으로, 호버는 읽기 전용 상태창으로 전달한다.</summary>
public sealed class UnitTargetView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private GameObject targetIcon;
    private CombatInputView _combatInput;
    private CombatHudView _combatHud;
    private Unit _unit;
    private bool _isHovered;
    private bool _subscribed;
    private void Awake() => SetTargetIcon(false);
    public void Bind(Unit unit, CombatInputView combatInput, CombatHudView combatHud = null)
    {
        Unsubscribe();
        _combatHud?.ClearHoveredUnit(_unit);
        _isHovered = false;
        _unit = unit;
        _combatInput = combatInput;
        _combatHud = combatHud;
        if (isActiveAndEnabled)
            Subscribe();
        RefreshTargetIcon();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            _combatInput?.SelectWorldTarget(_unit);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        _combatHud?.SetHoveredUnit(_unit);
        RefreshTargetIcon();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovered = false;
        _combatHud?.ClearHoveredUnit(_unit);
        SetTargetIcon(false);
    }

    private void RefreshTargetIcon()
        => SetTargetIcon(_isHovered && _combatInput != null && _combatInput.CanSelectWorldTarget(_unit));
    private void SetTargetIcon(bool visible)
    {
        if (targetIcon != null && targetIcon.activeSelf != visible)
            targetIcon.SetActive(visible);
    }
    // 호버 중 생사·비용이 바뀌어도 표시와 클릭에 같은 판정을 사용한다.
    private void LateUpdate()
    {
        if (_isHovered)
            RefreshTargetIcon();
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshTargetIcon();
    }

    private void Subscribe()
    {
        if (_subscribed || _combatInput == null)
            return;
        _combatInput.TargetSelectionChanged += RefreshTargetIcon;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (_subscribed && _combatInput != null)
            _combatInput.TargetSelectionChanged -= RefreshTargetIcon;
        _subscribed = false;
    }

    private void OnDisable()
    {
        Unsubscribe();
        _isHovered = false;
        SetTargetIcon(false);
        _combatHud?.ClearHoveredUnit(_unit);
    }
}
