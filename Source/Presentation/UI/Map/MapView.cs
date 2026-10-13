using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>기존 맵 위치에 노드를 표시한다. 노드 종류와 분기는 전달받은 Run에서 읽는다.</summary>
public sealed class MapView : UiBase
{
    [SerializeField] private MapNodeView nodePrefab;
    [Tooltip("노드 index 순서로 연결한 기존 MapPoint의 RectTransform")]
    [SerializeField] private RectTransform[] nodePositions = Array.Empty<RectTransform>();
    [Tooltip("맵에 표시할 노드 종류별 아이콘.")]
    [SerializeField] private NodeData[] nodeStyles = Array.Empty<NodeData>();

    private readonly Dictionary<NodeType, Sprite> _iconsByType = new Dictionary<NodeType, Sprite>();
    private readonly List<MapNodeView> _visibleNodes = new List<MapNodeView>();
    private PrefabPoolManager _pool;
    private RunState _run;
    private Action<int> _selectNode;
    private Func<int, bool> _canSelectNode;
    private bool _inputEnabled = true;

    public void Bind(RunState run, PrefabPoolManager pool, Action<int> selectNode = null, Func<int, bool> canSelectNode = null)
    {
        if (run == null || pool == null || nodePrefab == null || nodePositions.Length != run.Map.Count)
            throw new InvalidOperationException("런과 노드 프리팹/풀/배치 위치를 확인하세요.");
        foreach (var position in nodePositions)
            if (position == null) throw new InvalidOperationException("맵 노드 배치 위치가 비어 있습니다.");

        ReleaseNodes();
        _iconsByType.Clear();
        foreach (var style in nodeStyles)
        {
            if (style == null || style.icon == null || _iconsByType.ContainsKey(style.nodeType))
                throw new InvalidOperationException("맵 종류별 아이콘이 비어 있거나 중복되어 있습니다.");
            _iconsByType.Add(style.nodeType, style.icon);
        }
        foreach (var node in run.Map)
            if (!_iconsByType.ContainsKey(node.Type))
                throw new InvalidOperationException("맵 아이콘 연결이 없습니다: " + node.Type);

        _run = run;
        _pool = pool;
        _selectNode = selectNode;
        _canSelectNode = canSelectNode;
        _inputEnabled = true;
        if (_visibleNodes.Capacity < run.Map.Count) _visibleNodes.Capacity = run.Map.Count;
        _pool.Prewarm(nodePrefab.gameObject, run.Map.Count, run.Map.Count);
        if (IsShown) Refresh();
    }

    public override void Show(float duration = 0f)
    {
        if (_run == null) throw new InvalidOperationException("맵을 표시하기 전에 런을 연결하세요.");
        base.Show(duration);
        Refresh();
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        Refresh();
    }

    public void Refresh()
    {
        if (!IsShown || _run == null || !isActiveAndEnabled) return;
        if (_visibleNodes.Count == 0) CreateNodes();

        for (int index = 0; index < _visibleNodes.Count; index++)
        {
            NodeState state = index == _run.CurrentNode ? NodeState.Current
                : _run.IsCleared(index) ? NodeState.Cleared
                : _run.CanEnter(index) ? NodeState.Active : NodeState.Locked;
            _visibleNodes[index].SetState(state, _inputEnabled && (_canSelectNode == null || _canSelectNode(index)));
        }
    }

    private void CreateNodes()
    {
        try
        {
            for (int index = 0; index < _run.Map.Count; index++)
            {
                var rented = _pool.Rent(nodePrefab.gameObject, nodePositions[index]);
                if (rented == null) throw new InvalidOperationException("맵 노드 풀 용량이 부족합니다.");
                var nodeView = rented.GetComponent<MapNodeView>();
                _visibleNodes.Add(nodeView);
                nodeView.Bind(index, _iconsByType[_run.Map[index].Type], _selectNode);
            }
        }
        catch
        {
            ReleaseNodes();
            throw;
        }
    }

    public override void Hide(float duration = 0f)
    {
        // 즉시 숨김에서는 부모가 꺼지기 전에 보관소로 옮겨 재사용한다.
        if (duration <= 0f) ReleaseNodes();
        base.Hide(duration);
    }

    private void ReleaseNodes(bool moveToStorage = true)
    {
        if (_pool != null)
            foreach (var node in _visibleNodes)
                if (node != null) _pool.Return(node.gameObject, moveToStorage);
        _visibleNodes.Clear();
    }

    private void OnDisable() => ReleaseNodes(false);
}
