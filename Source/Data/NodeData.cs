using UnityEngine;

/// <summary>맵에 표시할 노드 종류와 아이콘. 실제 편성과 이벤트는 RunManager가 관리한다.</summary>
[CreateAssetMenu(fileName = "NewNode", menuName = "Node/Node Data")]
public class NodeData : ScriptableObject
{
    public NodeType nodeType;
    public string nodeName;
    public Sprite icon;
}
