using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>버프 아이콘들이 공유하는 읽기 전용 설명창. 전투 값은 변경하지 않는다.</summary>
public sealed class BuffTooltipView : UiBase
{
    [SerializeField] private TMP_Text buffNameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text remainingTurnsText;
    [SerializeField] private Vector2 pointerOffset = new Vector2(18f, 18f);
    [SerializeField] private float edgePadding = 8f;
    private BuffIconView _ownerIcon;
    private BuffData _displayedDefinition;
    private int _displayedTurns = -1;
    private RectTransform _panelTransform;
    private RectTransform _parentTransform;
    private Canvas _parentCanvas;
    private CanvasGroup _visibilityGroup;

    public void ShowBuff(BuffIconView ownerIcon, BuffData definition, int remainingTurns, Vector2 screenPosition)
    {
        if (ownerIcon == null || definition == null || remainingTurns <= 0)
            return;
        bool contentChanged = _displayedDefinition != definition || _displayedTurns != remainingTurns;
        _ownerIcon = ownerIcon;
        if (_panelTransform == null)
        {
            _panelTransform = (RectTransform)transform;
            _parentTransform = (RectTransform)transform.parent;
            _parentCanvas = GetComponentInParent<Canvas>();
            _visibilityGroup = GetComponent<CanvasGroup>();
        }
        if (!IsShown || !gameObject.activeSelf)
            base.Show();
        // 설명창이 포인터를 가로채서 아이콘의 Enter/Exit가 반복되지 않게 한다.
        _visibilityGroup.interactable = false;
        _visibilityGroup.blocksRaycasts = false;
        if (contentChanged)
        {
            if (_displayedDefinition != definition)
            {
                buffNameText.text = definition.buffName;
                descriptionText.text = definition.description;
            }
            remainingTurnsText.SetText("남은 턴: {0}", remainingTurns);
            _displayedDefinition = definition;
            _displayedTurns = remainingTurns;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panelTransform);
        }
        transform.SetAsLastSibling();
        PositionNearPointer(screenPosition);
    }

    public void HideFor(BuffIconView ownerIcon)
    {
        // 먼저 나간 아이콘의 Exit가 새 아이콘의 설명을 닫지 않도록 소유자를 확인한다.
        if (_ownerIcon == ownerIcon)
            Hide();
    }

    public override void Hide(float duration = 0f)
    {
        ClearOwner();
        base.Hide(duration);
    }

    private void LateUpdate()
    {
        if (_ownerIcon == null || !_ownerIcon.isActiveAndEnabled)
        {
            Hide();
            return;
        }
        PositionNearPointer(Input.mousePosition);
    }

    private void PositionNearPointer(Vector2 screenPosition)
    {
        Camera uiCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _parentCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentTransform, screenPosition, uiCamera, out var pointer))
            return;
        Rect area = _parentTransform.rect;
        Vector2 size = Vector2.Scale(_panelTransform.rect.size, _panelTransform.localScale);
        float left = pointer.x + pointerOffset.x;
        float top = pointer.y - pointerOffset.y;
        if (left + size.x > area.xMax - edgePadding)
            left = pointer.x - pointerOffset.x - size.x;
        if (top - size.y < area.yMin + edgePadding)
            top = pointer.y + pointerOffset.y + size.y;
        left = Mathf.Clamp(left, area.xMin + edgePadding, Mathf.Max(area.xMin + edgePadding, area.xMax - edgePadding - size.x));
        top = Mathf.Clamp(top, Mathf.Min(area.yMax - edgePadding, area.yMin + edgePadding + size.y), area.yMax - edgePadding);
        Vector2 pivot = _panelTransform.pivot;
        _panelTransform.localPosition = new Vector3(left + size.x * pivot.x, top - size.y * (1f - pivot.y), 0f);
    }

    private void OnDisable() => ClearOwner();
    private void ClearOwner()
    {
        _ownerIcon = null;
        _displayedDefinition = null;
        _displayedTurns = -1;
    }
}
