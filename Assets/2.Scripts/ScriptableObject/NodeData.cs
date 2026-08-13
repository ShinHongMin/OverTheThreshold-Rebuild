using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewNode", menuName = "Node/Node Data")]

public class NodeData : ScriptableObject
{
    [Header("기본 설정")]
    public NodeType nodeType;
    public string nodeName;
    public Sprite icon;

    [Header("등장할 몬스터 풀")]
    public List<EnemyEncountData> MonsterEncounters;

    [Header("이벤트일 경우")]
    public List<EventData> EventList;
}

