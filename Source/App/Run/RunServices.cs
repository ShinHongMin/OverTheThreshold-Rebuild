using System;
using System.Collections.Generic;

/// <summary>런 소유자가 한 번 조립한다. SO 변환과 난수 스트림을 전투마다 다시 만들지 않는다.</summary>
public sealed class RunServices
{
    public RunState Run { get; }
    public SkillTable Skills { get; }
    public IRandom MapRandom
    {
        get; private set;
    }

    public IRandom CombatRandom { get; }
    public IRandom RewardRandom { get; }
    public IReadOnlyList<UnitData> PartyData { get; }
    public RunEventSession CurrentEvent
    {
        get; private set;
    }

    public RunRestSession CurrentRest
    {
        get; private set;
    }

    private RunSaveData _nodeCheckpoint;
    public event Action NodeCheckpointChanged;

    public void BeginRest()
    {
        if (CurrentRest != null && CurrentRest.NodeIndex == Run.PendingNode)
            return;
        CurrentRest = new RunRestSession(Run, MapRandom);
        CurrentRest.ActionSelected += SaveNodeChoice;
    }

    public bool CompleteRest()
    {
        if (CurrentRest == null || Run.IsFinished || Run.PendingNode != CurrentRest.NodeIndex)
            return false;
        Run.CompleteNode();
        CurrentRest = null;
        return true;
    }

    public void BeginEvent(EventData data)
    {
        if (CurrentEvent != null && CurrentEvent.NodeIndex == Run.PendingNode)
            return;
        CurrentEvent = new RunEventSession(Run, data, MapRandom, RewardRandom);
        CurrentEvent.ChoiceSelected += SaveNodeChoice;
    }

    public bool CompleteEvent()
    {
        if (CurrentEvent == null || !CurrentEvent.IsResolved || CurrentEvent.BattleEncounter != null || Run.IsFinished || Run.PendingNode != CurrentEvent.NodeIndex)
            return false;
        Run.CompleteNode();
        CurrentEvent = null;
        return true;
    }

    public bool TryBeginEventBattle()
    {
        return CurrentEvent != null && CurrentEvent.IsResolved && CurrentEvent.BattleEncounter != null &&
            CurrentEvent.BattleEncounter.IsConfigured && Run.PendingNode == CurrentEvent.NodeIndex && Run.TryBeginEventBattle();
    }

    /// <summary>전투 보상 확정으로 완료된 이벤트는 맵 복귀 전에 정리한다.</summary>
    public void ClearCompletedEvent()
    {
        if (CurrentEvent != null && Run.IsCleared(CurrentEvent.NodeIndex))
            CurrentEvent = null;
    }

    public RunServices(IReadOnlyList<UnitData> orderedParty, IEnumerable<SkillTableAsset> tables, int seed, RewardTableAsset cardTable = null)
        : this(orderedParty, SkillTableBuilder.Build(tables), seed, cardTable) { }

    public RunServices(IReadOnlyList<UnitData> orderedParty, SkillTable skills, int seed, RewardTableAsset cardTable = null)
    {
        if (orderedParty == null || orderedParty.Count != 3)
            throw new ArgumentException("파티 3명을 순서대로 지정해야 합니다.");
        var data = new UnitData[3];
        var units = new Unit[3];
        for (int i = 0; i < 3; i++)
        {
            if (orderedParty[i] == null)
                throw new ArgumentException("파티 데이터가 비어 있습니다.");
            for (int j = 0; j < i; j++)
                if (orderedParty[i] == orderedParty[j])
                    throw new ArgumentException("같은 캐릭터를 중복 선택했습니다.");
            data[i] = orderedParty[i];
            units[i] = UnitFactory.Create(data[i], true);
        }
        PartyData = Array.AsReadOnly(data);
        MapRandom = new SystemRandom(seed);
        CombatRandom = new SystemRandom(unchecked(seed ^ (int)0x9E3779B9));
        RewardRandom = new SystemRandom(unchecked(seed ^ (int)0x85EBCA6B));
        Skills = skills ?? throw new ArgumentNullException(nameof(skills));
        Run = new RunState(units, MapGenerator.Generate(MapRandom), cardTable != null ? cardTable.BuildPassiveRules() : null, new SystemRandom(unchecked(seed ^ (int)0xC2B2AE35)));
    }

    public RunSaveData CreateSaveData()
    {
        if (Run.IsFinished)
            throw new InvalidOperationException("종료된 런은 저장할 수 없습니다.");
        // 전투 도중의 HP/ER 대신 진입 당시의 파티를 유지한다.
        if (Run.SelectedNode >= 0 && _nodeCheckpoint != null)
            return _nodeCheckpoint;
        var data = CaptureCheckpoint(false);
        RunSaveRules.Validate(data);
        return data;
    }

    private RunSaveData CaptureCheckpoint(bool captureNodeCheckpoint)
    {
        var ids = new string[PartyData.Count];
        for (int i = 0; i < ids.Length; i++)
            ids[i] = PartyData[i].unitId;
        var data = RunSaveRules.Capture(Run, ids, captureNodeCheckpoint);
        data.mapRandom = ((SystemRandom)MapRandom).CaptureState();
        data.combatRandom = ((SystemRandom)CombatRandom).CaptureState();
        data.rewardRandom = ((SystemRandom)RewardRandom).CaptureState();
        data.cardRandom = ((SystemRandom)Run.CardRandom).CaptureState();
        return data;
    }

    /// <summary>런 상태를 변경하기 전에 선택 노드와 진입 전 파티를 저장용으로 준비한다.</summary>
    public RunSaveData CreateNodeEntrySaveData(int nodeIndex, IReadOnlyList<EventData> events)
    {
        if (!Run.CanEnter(nodeIndex))
            throw new InvalidOperationException("이 노드에는 진입할 수 없습니다.");
        if (Run.SelectedNode >= 0 && _nodeCheckpoint != null)
            return _nodeCheckpoint;
        var data = CaptureCheckpoint(false);
        data.hasNodeSelection = true;
        data.nodeSelection = new NodeSelectionSaveData { nodeIndex = nodeIndex };
        if (Run.Map[nodeIndex].Type == NodeType.Event)
        {
            if (events == null || events.Count == 0)
                throw new ArgumentException("이벤트 목록이 필요합니다.");
            // 저장 실패 후 다시 클릭해도 추첨이 바뀌지 않도록 복제한 난수로 준비한다.
            var eventRandom = SystemRandom.Restore(data.mapRandom);
            int selected = events.Count == 1 ? 0 : eventRandom.Range(0, events.Count);
            var selectedEvent = events[selected];
            if (selectedEvent == null)
                throw new ArgumentException("이벤트 데이터가 없습니다.");
            data.nodeSelection.eventId = selectedEvent.eventId;
            ResolveSelectedEvent(data, events);
            data.mapRandom = eventRandom.CaptureState();
        }
        RunSaveRules.Validate(data);
        return data;
    }

    /// <summary>파일 저장에 성공한 후 진입한다. 복원된 선택 결과의 효과는 다시 적용하지 않는다.</summary>
    public bool BeginSelectedNode(RunSaveData data, IReadOnlyList<EventData> events)
    {
        RunSaveRules.Validate(data);
        if (!data.hasNodeSelection || data.currentNode != Run.CurrentNode)
            return false;
        var selection = data.nodeSelection;
        var eventData = ResolveSelectedEvent(data, events);
        if (!Run.TryEnter(selection.nodeIndex))
            return false;
        _nodeCheckpoint = data;
        MapRandom = SystemRandom.Restore(data.mapRandom);
        var type = Run.Map[selection.nodeIndex].Type;
        if (type == NodeType.Event)
        {
            BeginEvent(eventData);
            if (selection.choiceConfirmed)
                CurrentEvent.RestoreChoice(selection.choiceIndex, selection.eventSucceeded, selection.choiceFeedback);
        }
        else if (type == NodeType.Rest)
        {
            BeginRest();
            if (selection.choiceConfirmed)
                CurrentRest.RestoreChoice((RestRules.Action)selection.choiceIndex, selection.choiceFeedback);
        }
        return true;
    }

    private void SaveNodeChoice()
    {
        if (Run.IsFinished)
            return; // 파티 전멸은 종료 처리에서 저장 기록을 지운다.
        var data = CaptureCheckpoint(true);
        var selection = data.nodeSelection;
        if (Run.Map[Run.PendingNode].Type == NodeType.Event)
        {
            selection.eventId = CurrentEvent.Data.eventId;
            selection.choiceConfirmed = CurrentEvent.IsResolved;
            selection.choiceIndex = CurrentEvent.SelectedChoiceIndex;
            selection.eventSucceeded = CurrentEvent.Succeeded;
            selection.choiceFeedback = CurrentEvent.EffectSummary;
        }
        else
        {
            selection.choiceConfirmed = CurrentRest.SelectedAction.HasValue;
            selection.choiceIndex = (int)CurrentRest.SelectedAction.Value;
            selection.choiceFeedback = CurrentRest.Feedback;
        }
        RunSaveRules.Validate(data);
        _nodeCheckpoint = data;
        NodeCheckpointChanged?.Invoke();
    }

    private static EventData ResolveSelectedEvent(RunSaveData data, IReadOnlyList<EventData> events)
    {
        var selection = data.nodeSelection;
        if (!data.hasNodeSelection || data.nodeTypes[selection.nodeIndex] != NodeType.Event)
            return null;
        EventData result = null;
        if (events != null)
            foreach (var candidate in events)
                if (candidate != null && candidate.eventId == selection.eventId)
                {
                    if (result != null)
                        throw new ArgumentException("이벤트 ID가 중복됩니다.");
                    result = candidate;
                }
        if (string.IsNullOrWhiteSpace(selection.eventId) || !RunEventSession.CanPrepare(result) ||
            (selection.choiceConfirmed && selection.choiceIndex >= result.choices.Count))
            throw new ArgumentException("저장된 이벤트와 선택지를 복원할 수 없습니다.");
        return result;
    }

    public static RunServices Restore(RunSaveData data, IReadOnlyList<UnitData> availableCharacters,
        IEnumerable<SkillTableAsset> tables, RewardTableAsset cardTable, IReadOnlyList<EventData> events = null)
    {
        RunSaveRules.Validate(data);
        ResolveSelectedEvent(data, events);
        if (availableCharacters == null || cardTable == null)
            throw new ArgumentException("복원할 캐릭터·카드 데이터가 필요합니다.");
        var partyData = new UnitData[3];
        var units = new Unit[3];
        for (int i = 0; i < 3; i++)
        {
            foreach (var candidate in availableCharacters)
                if (candidate != null && candidate.unitId == data.party[i].unitId)
                {
                    if (partyData[i] != null)
                        throw new ArgumentException("캐릭터 데이터 ID가 중복됩니다.");
                    partyData[i] = candidate;
                }
            if (partyData[i] == null)
                throw new ArgumentException("현재 데이터에 없는 캐릭터가 저장되어 있습니다: " + data.party[i].unitId);
            units[i] = UnitFactory.Create(partyData[i], true);
        }
        var cards = cardTable.BuildPassiveRules();
        var run = RunSaveRules.Restore(data, units, cards, SystemRandom.Restore(data.cardRandom));
        var services = new RunServices(run, partyData, SkillTableBuilder.Build(tables),
            SystemRandom.Restore(data.mapRandom), SystemRandom.Restore(data.combatRandom), SystemRandom.Restore(data.rewardRandom));
        if (data.hasNodeSelection)
            services._nodeCheckpoint = data;
        return services;
    }

    private RunServices(RunState run, UnitData[] partyData, SkillTable skills, IRandom mapRandom, IRandom combatRandom, IRandom rewardRandom)
    {
        Run = run;
        PartyData = Array.AsReadOnly(partyData);
        Skills = skills;
        MapRandom = mapRandom;
        CombatRandom = combatRandom;
        RewardRandom = rewardRandom;
    }

    public void FinishBattle(BattleResult result)
    {
        if (result == BattleResult.Defeat)
            Run.Fail();
        else if (result == BattleResult.Victory)
        {
            // 보상 선택 전에도 전투 한정 버프·실드를 런에 남기지 않는다.
            foreach (var unit in Run.Party)
                unit.ResetCombatState();
            if (Run.PendingNode >= 0 && Run.Map[Run.PendingNode].Type == NodeType.Boss)
                Run.CompleteNode();
        }
    }
}
