using System;
using System.Collections.Generic;
using System.Text;

/// <summary>한 노드의 이벤트·선택 결과를 보유한다. 맵 재표시/재로드로 추첨·효과를 반복하지 않는다.</summary>
public sealed class RunEventSession
{
    private readonly RunState _run;
    private readonly IRandom _random;
    private readonly IRandom _rewardRandom;
    private readonly Dictionary<string, PassiveCardData> _cardsById = new Dictionary<string, PassiveCardData>(StringComparer.Ordinal);
    private readonly RunEventEffect[][] _successEffects;
    private readonly RunEventEffect[][] _failureEffects;
    private readonly CombatEncounterData[] _successEncounters;
    private readonly CombatEncounterData[] _failureEncounters;
    public event Action ChoiceSelected;
    public EventData Data { get; }
    public int NodeIndex { get; }
    public int SelectedChoiceIndex { get; private set; } = -1;
    public bool IsResolved => SelectedChoiceIndex >= 0;
    public bool Succeeded
    {
        get; private set;
    }

    public string ResultText
    {
        get; private set;
    }

    public string EffectSummary
    {
        get; private set;
    }

    public CombatEncounterData BattleEncounter
    {
        get; private set;
    }

    private static readonly StatType[] SummaryStats = { StatType.MaxHP, StatType.ATK, StatType.DEF, StatType.ER_Resist, StatType.ER_GainMultiplier };

    public static bool CanPrepare(EventData data)
    {
        if (data == null || data.choices == null || data.choices.Count < 1 || data.choices.Count > 3)
            return false;
        foreach (var choice in data.choices)
        {
            if (choice == null || choice.successRate < 0 || choice.successRate > 100)
                return false;
            if (!Supports(choice.effects) || !Supports(choice.failureEffects))
                return false;
        }
        return true;
    }

    private static bool Supports(List<EffectInfo> effects)
    {
        if (effects == null)
            return true;
        bool hasBattle = false;
        foreach (var effect in effects)
        {
            if (effect.effectType == EventEffectType.StartBattle)
            {
                var encounter = effect.combatEncounter;
                if (effect.target != RunEventTarget.AllParty || hasBattle || effect.effectValue != 0f || encounter == null || !encounter.IsConfigured || encounter.Enemies.Count > 3 ||
                    (encounter.BattleType != NodeType.Battle && encounter.BattleType != NodeType.Elite && encounter.BattleType != NodeType.EliteHard))
                    return false;
                hasBattle = true;
                continue;
            }
            if (effect.effectType != EventEffectType.GetRandomPassive)
            {
                if (!RunEventRules.IsSupported(new RunEventEffect(effect.effectType, effect.effectValue, targetStat: effect.targetStat, target: effect.target)))
                    return false;
                continue;
            }
            // 맵 버튼의 진입 가능 검사에서 카드 ID 배열을 매번 만들지 않는다.
            if (effect.target != RunEventTarget.AllParty || float.IsNaN(effect.effectValue) || float.IsInfinity(effect.effectValue) || effect.effectValue < 0f ||
                effect.targetPassives == null || effect.cardCount < 1 || effect.cardCount > 2 || effect.cardCount > effect.targetPassives.Length)
                return false;
            for (int index = 0; index < effect.targetPassives.Length; index++)
            {
                var card = effect.targetPassives[index];
                if (card == null || string.IsNullOrWhiteSpace(card.cardId))
                    return false;
                for (int previous = 0; previous < index; previous++)
                    if (effect.targetPassives[previous].cardId == card.cardId)
                        return false;
            }
        }
        return true;
    }

    public RunEventSession(RunState run, EventData data, IRandom random, IRandom rewardRandom = null)
    {
        if (run == null || run.PendingNode < 0 || run.Map[run.PendingNode].Type != NodeType.Event || !CanPrepare(data))
            throw new ArgumentException("진입한 이벤트 노드와 지원되는 선택지 데이터가 필요합니다.");
        _run = run;
        _random = random ?? throw new ArgumentNullException(nameof(random));
        _rewardRandom = rewardRandom ?? _random;
        Data = data;
        NodeIndex = run.PendingNode;
        _successEffects = new RunEventEffect[data.choices.Count][];
        _failureEffects = new RunEventEffect[data.choices.Count][];
        _successEncounters = new CombatEncounterData[data.choices.Count];
        _failureEncounters = new CombatEncounterData[data.choices.Count];
        for (int index = 0; index < data.choices.Count; index++)
        {
            _successEffects[index] = Convert(data.choices[index].effects, out _successEncounters[index]);
            _failureEffects[index] = Convert(data.choices[index].failureEffects, out _failureEncounters[index]);
        }
    }

    private RunEventEffect[] Convert(List<EffectInfo> effects, out CombatEncounterData encounter)
    {
        encounter = null;
        if (effects == null || effects.Count == 0)
            return Array.Empty<RunEventEffect>();
        var result = new RunEventEffect[effects.Count];
        for (int index = 0; index < result.Length; index++)
        {
            var effect = effects[index];
            if (effect.effectType == EventEffectType.StartBattle)
            {
                // Core에는 수치 효과만 전달하고 씬 진입은 선택 확정 후 App에서 처리한다.
                encounter = effect.combatEncounter;
                result[index] = new RunEventEffect(EventEffectType.None, 0f);
                continue;
            }
            if (effect.effectType != EventEffectType.GetRandomPassive)
            {
                result[index] = new RunEventEffect(effect.effectType, effect.effectValue, targetStat: effect.targetStat, target: effect.target);
                continue;
            }
            var cardIds = new string[effect.targetPassives.Length];
            for (int cardIndex = 0; cardIndex < cardIds.Length; cardIndex++)
            {
                var card = effect.targetPassives[cardIndex];
                cardIds[cardIndex] = card.cardId;
                _cardsById[card.cardId] = card;
            }
            result[index] = new RunEventEffect(effect.effectType, effect.effectValue, Array.AsReadOnly(cardIds), effect.cardCount);
        }
        return result;
    }

    internal void RestoreChoice(int index, bool succeeded, string feedback)
    {
        if (IsResolved || index < 0 || index >= Data.choices.Count)
            throw new ArgumentException("복원할 이벤트 선택이 올바르지 않습니다.");
        SelectedChoiceIndex = index;
        Succeeded = succeeded;
        var choice = Data.choices[index];
        BattleEncounter = succeeded ? _successEncounters[index] : _failureEncounters[index];
        ResultText = succeeded ? choice.resultText : choice.failureText;
        EffectSummary = feedback;
    }

    public bool TrySelectChoice(int index)
    {
        if (IsResolved || _run.IsFinished || _run.PendingNode != NodeIndex || index < 0 || index >= Data.choices.Count)
            return false;
        var choice = Data.choices[index];
        bool succeeded = choice.successRate == 100 || (choice.successRate > 0 && _random.Value01() < choice.successRate / 100f);
        var beforeHP = new float[_run.Party.Count];
        var beforeER = new float[_run.Party.Count];
        var beforeStats = new float[_run.Party.Count, SummaryStats.Length];
        for (int unitIndex = 0; unitIndex < beforeHP.Length; unitIndex++)
        {
            var unit = _run.Party[unitIndex];
            beforeHP[unitIndex] = unit.CurrentHP;
            beforeER[unitIndex] = unit.CurrentER;
            for (int statIndex = 0; statIndex < SummaryStats.Length; statIndex++)
                beforeStats[unitIndex, statIndex] = unit.Stats.Get(SummaryStats[statIndex]);
        }
        int beforeSP = _run.BonusStartSP;
        int beforeOP = _run.BonusStartOP;
        int beforeCards = _run.Cards.Count;
        var selectedEffects = succeeded ? _successEffects[index] : _failureEffects[index];
        if (!RunEventRules.TryApply(_run, selectedEffects, _rewardRandom, _random, out int randomTargetIndex))
            return false;
        SelectedChoiceIndex = index;
        Succeeded = succeeded;
        BattleEncounter = succeeded ? _successEncounters[index] : _failureEncounters[index];
        ResultText = succeeded ? choice.resultText : choice.failureText;

        var summary = new StringBuilder();
        for (int unitIndex = 0; unitIndex < beforeHP.Length; unitIndex++)
        {
            var unit = _run.Party[unitIndex];
            bool changedHP = beforeHP[unitIndex] != unit.CurrentHP;
            bool changedStat = false;
            for (int statIndex = 0; statIndex < SummaryStats.Length; statIndex++)
                changedStat |= beforeStats[unitIndex, statIndex] != unit.Stats.Get(SummaryStats[statIndex]);
            bool affectsER = false;
            foreach (var effect in selectedEffects)
                affectsER |= (effect.Type == EventEffectType.IncreaseER || effect.Type == EventEffectType.DecreaseER) &&
                    (effect.Target == RunEventTarget.AllParty || unitIndex == randomTargetIndex);
            bool showER = beforeER[unitIndex] != unit.CurrentER || (affectsER && unit.IsAlive && unit.IsEchoState);
            if (!changedHP && !showER && !changedStat)
                continue;
            summary.Append(unit.Name).Append(": ");
            if (changedHP)
                summary.Append($"HP {beforeHP[unitIndex]:0.#} → {unit.CurrentHP:0.#}");
            if (changedHP && showER)
                summary.Append(" / ");
            if (showER)
            {
                summary.Append($"ER {beforeER[unitIndex]:0.#} → {unit.CurrentER:0.#}");
                if (unit.IsEchoState)
                    summary.Append(beforeER[unitIndex] < Unit.EchoThreshold ? " (ECHO 진입)" : " (ECHO 유지)");
            }
            bool hasValue = changedHP || showER;
            for (int statIndex = 0; statIndex < SummaryStats.Length; statIndex++)
            {
                var stat = SummaryStats[statIndex];
                float before = beforeStats[unitIndex, statIndex];
                float after = unit.Stats.Get(stat);
                if (before == after)
                    continue;
                if (hasValue)
                    summary.Append(" / ");
                summary.Append(StatName(stat)).Append(' ');
                bool percent = stat == StatType.ER_Resist || stat == StatType.ER_GainMultiplier;
                summary.Append(percent ? $"{before * 100f:0.#}% → {after * 100f:0.#}%" : $"{before:0.#} → {after:0.#}");
                hasValue = true;
            }
            summary.AppendLine();
        }
        if (beforeSP != _run.BonusStartSP)
            summary.AppendLine($"전투 시작 SP 보너스: +{beforeSP} → +{_run.BonusStartSP}");
        if (beforeOP != _run.BonusStartOP)
            summary.AppendLine($"전투 시작 OP 보너스: +{beforeOP} → +{_run.BonusStartOP}");
        if (beforeCards < _run.Cards.Count)
        {
            summary.Append("획득 카드: ");
            for (int cardIndex = beforeCards; cardIndex < _run.Cards.Count; cardIndex++)
            {
                if (cardIndex > beforeCards)
                    summary.Append(", ");
                summary.Append(_cardsById[_run.Cards[cardIndex]].cardName);
            }
        }
        EffectSummary = summary.ToString().TrimEnd();
        ChoiceSelected?.Invoke();
        return true;
    }

    private static string StatName(StatType stat)
    {
        switch (stat)
        {
            case StatType.MaxHP:
                return "최대 HP";
            case StatType.ATK:
                return "공격력";
            case StatType.DEF:
                return "방어력";
            case StatType.ER_Resist:
                return "ER 저항";
            case StatType.ER_GainMultiplier:
                return "ER 획득량";
            default:
                return stat.ToString();
        }
    }
}
