using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>노드 하나의 아이콘·현재 위치·진입 가능 표시. 맵 진행도는 변경하지 않는다.</summary>
public sealed class MapNodeView : MonoBehaviour, IPooledView
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject currentMarker;
    [SerializeField] private GameObject availableHighlight;
    [SerializeField] private Color lockedColor = new Color(.5f, .5f, .5f, 1f);
    [SerializeField] private Color clearedColor = new Color(.7f, .7f, .7f, 1f);

    private Action<int> _selectNode;
    public int NodeIndex { get; private set; } = -1;
    public NodeState State { get; private set; }

    private void Awake() => selectButton.onClick.AddListener(Select);

    public void Bind(int nodeIndex, Sprite icon, Action<int> selectNode)
    {
        NodeIndex = nodeIndex;
        _selectNode = selectNode;
        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
    }

    public void SetState(NodeState state, bool inputEnabled)
    {
        State = state;
        backgroundImage.color = state == NodeState.Locked ? lockedColor
            : state == NodeState.Cleared ? clearedColor : Color.white;
        currentMarker.SetActive(state == NodeState.Current);
        availableHighlight.SetActive(state == NodeState.Active);
        selectButton.interactable = state == NodeState.Active && inputEnabled && _selectNode != null;
    }

    private void Select()
    {
        if (isActiveAndEnabled && selectButton.interactable && State == NodeState.Active)
            _selectNode?.Invoke(NodeIndex);
    }

    public void OnRent() { }

    public void OnReturn()
    {
        _selectNode = null;
        NodeIndex = -1;
        iconImage.sprite = null;
        iconImage.enabled = false;
        SetState(NodeState.Locked, false);
    }

    private void OnDestroy()
    {
        if (selectButton != null) selectButton.onClick.RemoveListener(Select);
    }
}
