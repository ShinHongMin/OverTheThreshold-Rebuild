using System;

/// <summary>씬·프리팹 참조 없이 JSON에 기록할 진행도와 노드 선택 상태.</summary>
[Serializable]
public sealed class RunSaveData
{
    public const int CurrentVersion = 3;
    public int version;
    public string savedAtUtc;
    public PartyMemberSaveData[] party;
    public NodeType[] nodeTypes;
    public int currentNode;
    public int[] clearedNodes;
    // Unity JSON은 비어 있는 객체도 생성하므로 선택 유무를 별도 값으로 구분한다.
    public bool hasNodeSelection;
    public NodeSelectionSaveData nodeSelection;
    public string[] cards;
    public int bonusStartSP;
    public int bonusStartOP;
    public RandomState mapRandom;
    public RandomState combatRandom;
    public RandomState rewardRandom;
    public RandomState cardRandom;
}

[Serializable]
public sealed class NodeSelectionSaveData
{
    public int nodeIndex;
    public string eventId;
    public bool choiceConfirmed;
    public int choiceIndex;
    public bool eventSucceeded;
    public string choiceFeedback;
}

[Serializable]
public sealed class PartyMemberSaveData
{
    public string unitId;
    public float currentHP;
    public float currentER;
    public PermanentStatSaveData[] permanentStats;
}

[Serializable]
public sealed class PermanentStatSaveData
{
    public StatType stat;
    public float percent;
}
