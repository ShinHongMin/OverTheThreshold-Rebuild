using System;
using System.Collections.Generic;

/// <summary>노드 저장과 복원 규칙. 파일 입출력 및 UI에는 관여하지 않는다.</summary>
public static class RunSaveRules
{
    public static RunSaveData Capture(RunState run, IReadOnlyList<string> partyIds, bool captureNodeCheckpoint = false)
    {
        if (run == null || run.IsFinished || (run.PendingNode >= 0 && !captureNodeCheckpoint) || partyIds == null || partyIds.Count != 3)
            throw new InvalidOperationException("진행 중인 노드를 완료한 후 저장할 수 있습니다.");
        var data = new RunSaveData
        {
            version = RunSaveData.CurrentVersion,
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            party = new PartyMemberSaveData[3],
            nodeTypes = new NodeType[run.Map.Count],
            currentNode = run.CurrentNode,
            cards = new string[run.Cards.Count],
            bonusStartSP = run.BonusStartSP,
            bonusStartOP = run.BonusStartOP
        };
        data.hasNodeSelection = run.SelectedNode >= 0;
        if (data.hasNodeSelection)
            data.nodeSelection = new NodeSelectionSaveData { nodeIndex = run.SelectedNode };
        var cleared = new List<int>();
        for (int i = 0; i < run.Map.Count; i++)
        {
            data.nodeTypes[i] = run.Map[i].Type;
            if (run.IsCleared(i))
                cleared.Add(i);
        }
        data.clearedNodes = cleared.ToArray();
        for (int i = 0; i < run.Cards.Count; i++)
            data.cards[i] = run.Cards[i];
        for (int i = 0; i < 3; i++)
        {
            var unit = run.Party[i];
            var permanent = new List<PermanentStatSaveData>();
            foreach (var modifier in unit.Stats.Modifiers)
                if (modifier.Source == ModSource.MetaProgress)
                    permanent.Add(new PermanentStatSaveData { stat = modifier.Stat, percent = modifier.Percent });
            data.party[i] = new PartyMemberSaveData
            {
                unitId = partyIds[i],
                currentHP = unit.CurrentHP,
                currentER = unit.CurrentER,
                permanentStats = permanent.ToArray()
            };
        }
        return data;
    }

    public static void Validate(RunSaveData data)
    {
        if (data == null || data.version < 1 || data.version > RunSaveData.CurrentVersion || string.IsNullOrWhiteSpace(data.savedAtUtc) ||
            !DateTime.TryParse(data.savedAtUtc, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind, out _) || data.party == null || data.party.Length != 3 ||
            data.cards == null || data.cards.Length > 1000 || data.bonusStartSP < 0 || data.bonusStartSP > 1000 ||
            data.bonusStartOP < 0 || data.bonusStartOP > 1000)
            throw new ArgumentException("저장 기록의 형식이나 버전을 확인할 수 없습니다.");
        var map = MapGenerator.Restore(data.nodeTypes);
        ValidateProgress(data, map);
        for (int i = 0; i < 3; i++)
        {
            var member = data.party[i];
            if (member == null || string.IsNullOrWhiteSpace(member.unitId) || member.permanentStats == null || member.permanentStats.Length > 100 ||
                !Finite(member.currentHP) || member.currentHP < 0 || !Finite(member.currentER) || member.currentER < 0 || member.currentER > Unit.MaxER)
                throw new ArgumentException("저장된 캐릭터 상태가 올바르지 않습니다.");
            for (int j = 0; j < i; j++)
                if (data.party[j].unitId == member.unitId)
                    throw new ArgumentException("저장된 캐릭터 ID가 중복됩니다.");
            foreach (var modifier in member.permanentStats)
                if (modifier == null || modifier.stat < StatType.MaxHP || modifier.stat > StatType.ER_GainMultiplier || !Finite(modifier.percent))
                    throw new ArgumentException("저장된 영구 보정이 올바르지 않습니다.");
        }
        foreach (string id in data.cards)
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("저장된 카드 ID가 비어 있습니다.");
        ValidateRandom(data.mapRandom);
        ValidateRandom(data.combatRandom);
        ValidateRandom(data.rewardRandom);
        ValidateRandom(data.cardRandom);
    }

    private static void ValidateProgress(RunSaveData data, IReadOnlyList<MapGenerator.Node> map)
    {
        var cleared = data.clearedNodes;
        if (cleared == null || cleared.Length == 0 || cleared.Length > map.Count || cleared[0] != 0 ||
            data.currentNode < 0 || data.currentNode >= 21 || cleared[cleared.Length - 1] != data.currentNode)
            throw new ArgumentException("저장된 맵 진행도가 올바르지 않습니다.");
        for (int i = 1; i < cleared.Length; i++)
            if (cleared[i] <= cleared[i - 1] || cleared[i] >= 21 || !Connected(map, cleared[i - 1], cleared[i]))
                throw new ArgumentException("저장된 이동 경로가 올바르지 않습니다.");

        if (!data.hasNodeSelection)
            return; // 이전 버전은 완료된 노드부터 계속한다.
        var selection = data.nodeSelection;
        if (selection == null || data.version == 1 || selection.nodeIndex <= data.currentNode || selection.nodeIndex >= map.Count ||
            !Connected(map, data.currentNode, selection.nodeIndex))
            throw new ArgumentException("저장된 선택 노드가 올바르지 않습니다.");
        var type = map[selection.nodeIndex].Type;
        if ((type == NodeType.Event && string.IsNullOrWhiteSpace(selection.eventId)) ||
            (type != NodeType.Event && !string.IsNullOrEmpty(selection.eventId)) ||
            (selection.choiceFeedback != null && selection.choiceFeedback.Length > 10000) ||
            (selection.choiceConfirmed && (selection.choiceIndex < 0 || selection.choiceIndex > 2 ||
                (type != NodeType.Event && type != NodeType.Rest))))
            throw new ArgumentException("저장된 노드 선택 결과가 올바르지 않습니다.");
    }

    private static bool Connected(IReadOnlyList<MapGenerator.Node> map, int from, int to)
    {
        foreach (int next in map[from].Next)
            if (next == to)
                return true;
        return false;
    }

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static void ValidateRandom(RandomState state)
    {
        if (state == null || state.drawCount < 0 || state.drawCount > SystemRandom.MaximumSavedDrawCount)
            throw new ArgumentException("저장된 난수 정보가 올바르지 않습니다.");
    }

    public static RunState Restore(RunSaveData data, IReadOnlyList<Unit> freshParty, PassiveCardRules cards, IRandom cardRandom)
    {
        Validate(data);
        if (freshParty == null || freshParty.Count != 3)
            throw new ArgumentException("새 파티 3명이 필요합니다.");
        foreach (var unit in freshParty)
            if (unit == null || unit.Stats.Modifiers.Count != 0 || unit.CurrentHP != unit.MaxHP || unit.CurrentER != 0 || unit.HasShield)
                throw new ArgumentException("카드나 상태가 적용되지 않은 새 유닛으로 복원해야 합니다.");
        var run = new RunState(freshParty, MapGenerator.Restore(data.nodeTypes), cards, cardRandom);
        run.RestoreProgress(data.currentNode, data.clearedNodes);
        if (data.hasNodeSelection)
            run.RestoreSelection(data.nodeSelection.nodeIndex);
        foreach (string id in data.cards)
            if (cards == null || !cards.Contains(id))
                throw new ArgumentException("현재 데이터에 없는 카드가 저장되어 있습니다: " + id);
        foreach (string id in data.cards)
            run.AddCard(id);
        bool hasLivingMember = false;
        for (int i = 0; i < 3; i++)
        {
            var member = data.party[i];
            var unit = run.Party[i];
            foreach (var modifier in member.permanentStats)
                unit.Stats.AddModifier(new StatModifier(modifier.stat, modifier.percent, ModSource.MetaProgress));
            if (!Finite(unit.MaxHP) || unit.MaxHP <= 0)
                throw new ArgumentException("복원된 최대 HP가 올바르지 않습니다.");
            float erGainMultiplier = unit.Stats.Get(StatType.ER_GainMultiplier);
            if (!Finite(erGainMultiplier) || erGainMultiplier < 0)
                throw new ArgumentException("복원된 ER 획득 배율이 올바르지 않습니다.");
            unit.RestorePersistentState(member.currentHP, member.currentER);
            hasLivingMember |= unit.IsAlive;
        }
        if (!hasLivingMember)
            throw new ArgumentException("종료된 파티의 기록은 이어갈 수 없습니다.");
        run.AddStartingResources(data.bonusStartSP, data.bonusStartOP);
        return run;
    }
}
