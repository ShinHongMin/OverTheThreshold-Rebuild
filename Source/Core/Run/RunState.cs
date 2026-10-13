using System;
using System.Collections.Generic;

/// <summary>한 런의 파티·맵·카드·이벤트 보너스. 씬과 전투가 바뀌어도 소유한다.</summary>
public sealed class RunState
{
    private readonly Unit[] _party;
    private readonly bool[] _cleared;
    private readonly List<string> _cards = new List<string>();
    private readonly PassiveCardRules _passiveCardRules;
    public IRandom CardRandom { get; }
    private int _rewardNode = -1;
    public IReadOnlyList<string> RewardChoices { get; private set; } = Array.Empty<string>();
    public string ClaimedRewardId
    {
        get; private set;
    }

    public IReadOnlyList<Unit> Party => _party;
    public IReadOnlyList<MapGenerator.Node> Map { get; }
    public IReadOnlyList<string> Cards => _cards;
    public int CurrentNode
    {
        get; private set;
    }
    // 이어하기에서는 선택만 복원하고, 맵에서 해당 노드를 누르면 PendingNode로 진입한다.
    public int SelectedNode { get; private set; } = -1;
    public int PendingNode { get; private set; } = -1;
    public bool IsEventBattleActive
    {
        get; private set;
    }

    public int BonusStartSP
    {
        get; private set;
    }

    public int BonusStartOP
    {
        get; private set;
    }

    public bool RestUsed
    {
        get; internal set;
    }

    public bool IsFinished
    {
        get; private set;
    }

    public bool IsFailed
    {
        get; private set;
    }

    public event Action CheckpointReached;

    public RunState(IReadOnlyList<Unit> party, IReadOnlyList<MapGenerator.Node> map, PassiveCardRules passiveCardRules = null, IRandom cardRandom = null)
    {
        if (party == null || party.Count != 3)
            throw new ArgumentException("파티는 정확히 3명이어야 합니다.");
        if (map == null || map.Count != 22)
            throw new ArgumentException("맵은 22개 노드여야 합니다.");
        _party = new Unit[3];
        for (int i = 0; i < 3; i++)
        {
            if (party[i] == null || !party[i].UsesResources)
                throw new ArgumentException("파티 캐릭터가 필요합니다.");
            for (int j = 0; j < i; j++)
                if (party[j] == party[i])
                    throw new ArgumentException("같은 유닛의 중복 등록입니다.");
            _party[i] = party[i];
        }
        Map = map;
        _passiveCardRules = passiveCardRules;
        CardRandom = cardRandom ?? new SystemRandom(0);
        _cleared = new bool[map.Count];
        _cleared[0] = true;
    }

    public bool CanEnter(int index)
    {
        if (IsFinished || PendingNode >= 0 || index < 0 || index >= Map.Count || _cleared[index])
            return false;
        if (SelectedNode >= 0 && index != SelectedNode)
            return false;
        foreach (int next in Map[CurrentNode].Next)
            if (next == index)
                return true;
        return false;
    }

    public bool TryEnter(int index)
    {
        if (!CanEnter(index))
            return false;
        SelectedNode = index;
        PendingNode = index;
        RestUsed = false;
        RewardChoices = Array.Empty<string>();
        ClaimedRewardId = null;
        return true;
    }

    public bool IsCleared(int index) => index >= 0 && index < _cleared.Length && _cleared[index];
    /// <summary>이벤트 노드를 유지한 채 선택 결과의 전투를 한 번만 시작한다.</summary>
    public bool TryBeginEventBattle()
    {
        if (IsFinished || PendingNode < 0 || Map[PendingNode].Type != NodeType.Event || IsEventBattleActive)
            return false;
        IsEventBattleActive = true;
        return true;
    }

    public void CompleteNode()
    {
        if (PendingNode < 0)
            throw new InvalidOperationException("진행 중인 노드가 없습니다.");
        foreach (var unit in _party)
            unit.ResetCombatState();
        CurrentNode = PendingNode;
        _cleared[CurrentNode] = true;
        PendingNode = -1;
        SelectedNode = -1;
        IsEventBattleActive = false;
        IsFinished = Map[CurrentNode].Type == NodeType.Boss;
        CheckpointReached?.Invoke();
    }

    public void Fail()
    {
        if (IsFinished)
            return;
        IsFailed = true;
        IsFinished = true;
        CheckpointReached?.Invoke();
    }

    internal void RestoreSelection(int index)
    {
        if (!CanEnter(index))
            throw new ArgumentException("저장된 선택 노드에 진입할 수 없습니다.");
        SelectedNode = index;
    }

    internal void RestoreProgress(int currentNode, int[] clearedNodes)
    {
        if (PendingNode >= 0 || CurrentNode != 0 || IsFinished || clearedNodes == null || clearedNodes.Length == 0 ||
            clearedNodes.Length > Map.Count || clearedNodes[0] != 0 || currentNode >= 21 || currentNode < 0 ||
            clearedNodes[clearedNodes.Length - 1] != currentNode)
            throw new ArgumentException("저장된 맵 진행도가 올바르지 않습니다.");
        for (int i = 1; i < clearedNodes.Length; i++)
        {
            int previous = clearedNodes[i - 1], next = clearedNodes[i];
            bool connected = false;
            if (next <= previous || next >= 21)
                throw new ArgumentException("저장된 이동 경로가 올바르지 않습니다.");
            foreach (int allowed in Map[previous].Next)
                if (next == allowed)
                    connected = true;
            if (!connected)
                throw new ArgumentException("연결되지 않은 노드가 저장되어 있습니다.");
        }
        Array.Clear(_cleared, 0, _cleared.Length);
        foreach (int node in clearedNodes)
            _cleared[node] = true;
        CurrentNode = currentNode;
    }
    /// <summary>보상과 이벤트가 함께 사용하는 획득 경로. 능력치 보정은 새로 얻은 한 장에만 적용한다.</summary>
    public void AddCard(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;
        _passiveCardRules?.ApplyToParty(id, _party);
        _cards.Add(id);
    }
    /// <summary>동일 전투의 보상은 한 번만 추첨한다. UI를 다시 열어도 후보는 바뀌지 않는다.</summary>
    public void PrepareBattleReward(IReadOnlyList<string> cardPool, IRandom random)
    {
        if (IsFinished || RewardChoices.Count > 0)
            return;
        if (PendingNode >= 0)
        {
            var type = Map[PendingNode].Type;
            if (type != NodeType.Battle && type != NodeType.Elite && type != NodeType.EliteHard && !(type == NodeType.Event && IsEventBattleActive))
                throw new InvalidOperationException("일반 전투 보상을 지급할 노드가 아닙니다.");
        }
        if (cardPool == null || cardPool.Count < 2)
            throw new ArgumentException("서로 다른 보상 카드가 2장 이상 필요합니다.");
        var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in cardPool)
            if (string.IsNullOrWhiteSpace(id) || !uniqueIds.Add(id))
                throw new ArgumentException("보상 카드 ID가 비어 있거나 중복됩니다.");
        RewardChoices = Array.AsReadOnly(RewardRules.Draw(cardPool, random));
        _rewardNode = PendingNode;
    }

    /// <summary>후보 중 한 장만 기록한다. 노드 완료도 확정과 함께 처리하여 중복 지급을 막는다.</summary>
    public bool TryClaimBattleReward(string cardId)
    {
        if (IsFinished || ClaimedRewardId != null || PendingNode != _rewardNode)
            return false;
        bool offered = false;
        foreach (var id in RewardChoices)
            if (id == cardId)
            {
                offered = true;
                break;
            }
        if (!offered)
            return false;
        AddCard(cardId);
        ClaimedRewardId = cardId;
        if (PendingNode >= 0)
            CompleteNode();
        return true;
    }

    public void AddStartingResources(int sp, int op)
    {
        BonusStartSP += sp;
        BonusStartOP += op;
    }

    public CombatContext BeginCombat(IReadOnlyList<Unit> enemies)
    {
        if (PendingNode < 0 || IsFinished)
            throw new InvalidOperationException("전투 노드에 먼저 진입해야 합니다.");
        NodeType type = Map[PendingNode].Type;
        if (type != NodeType.Battle && type != NodeType.Elite && type != NodeType.EliteHard && type != NodeType.Boss && !(type == NodeType.Event && IsEventBattleActive))
            throw new InvalidOperationException("전투 노드가 아닙니다.");
        return CreateCombatContext(enemies);
    }
    /// <summary>씬 직접 실행도 같은 카드 효과와 자원 초기화 경로를 사용한다.</summary>
    public CombatContext CreateCombatContext(IReadOnlyList<Unit> enemies)
    {
        foreach (var unit in _party)
            unit.ResetCombatState();
        int cardSP = _passiveCardRules?.GetStartingResourceBonus(_cards, PassiveEffectType.BattleStart_SP) ?? 0;
        int cardOP = _passiveCardRules?.GetStartingResourceBonus(_cards, PassiveEffectType.BattleStart_OP) ?? 0;
        return new CombatContext(_party, enemies, new PartyResources(BonusStartSP + cardSP, BonusStartOP + cardOP), CardRandom);
    }
}
