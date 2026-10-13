using System;
using System.Collections.Generic;

public readonly struct RunEventEffect
{
    public EventEffectType Type { get; }
    public float Value { get; }
    public IReadOnlyList<string> CardPool { get; }
    public int CardCount { get; }
    public StatType TargetStat { get; }
    public RunEventTarget Target { get; }
    public RunEventEffect(EventEffectType type, float value, IReadOnlyList<string> cardPool = null, int cardCount = 0,
        StatType targetStat = StatType.None, RunEventTarget target = RunEventTarget.AllParty)
    {
        Type = type;
        Value = value;
        CardPool = cardPool;
        CardCount = cardCount;
        TargetStat = targetStat;
        Target = target;
    }
}

/// <summary>현재 연결된 맵 이벤트의 효과. UI와 ScriptableObject를 참조하지 않는다.</summary>
public static class RunEventRules
{
    public static bool IsSupported(RunEventEffect effect)
    {
        if (!Finite(effect.Value) || (effect.Target != RunEventTarget.AllParty && effect.Target != RunEventTarget.RandomLivingAlly))
            return false;
        if (IsPermanentStat(effect))
            return effect.TargetStat >= StatType.MaxHP && effect.TargetStat <= StatType.ER_GainMultiplier &&
                effect.Target == (effect.Type == EventEffectType.BuffSingleRandom ? RunEventTarget.RandomLivingAlly : RunEventTarget.AllParty);
        if (effect.Value < 0f)
            return false;
        switch (effect.Type)
        {
            case EventEffectType.IncreaseER:
            case EventEffectType.DecreaseER:
                return true;
            case EventEffectType.HealHP_Percent:
            case EventEffectType.DamageHP_Percent:
                return effect.Value <= 100f;
            case EventEffectType.None:
                return effect.Target == RunEventTarget.AllParty;
            case EventEffectType.GetSP:
                return effect.Target == RunEventTarget.AllParty && effect.Value <= PartyResources.DefaultMaxSP && effect.Value == Math.Floor(effect.Value);
            case EventEffectType.GetOP:
                return effect.Target == RunEventTarget.AllParty && effect.Value <= PartyResources.DefaultMaxOP && effect.Value == Math.Floor(effect.Value);
            case EventEffectType.GetRandomPassive:
                if (effect.Target != RunEventTarget.AllParty || effect.CardPool == null || effect.CardCount < 1 || effect.CardCount > 2 || effect.CardCount > effect.CardPool.Count)
                    return false;
                for (int index = 0; index < effect.CardPool.Count; index++)
                {
                    if (string.IsNullOrWhiteSpace(effect.CardPool[index]))
                        return false;
                    for (int previous = 0; previous < index; previous++)
                        if (effect.CardPool[previous] == effect.CardPool[index])
                            return false;
                }
                return true;
            default:
                return false;
        }
    }

    public static bool TryApply(RunState run, IReadOnlyList<RunEventEffect> effects, IRandom rewardRandom = null)
        => TryApply(run, effects, rewardRandom, null, out _);

    public static bool TryApply(RunState run, IReadOnlyList<RunEventEffect> effects, IRandom rewardRandom,
        IRandom eventRandom, out int randomTargetIndex)
    {
        randomTargetIndex = -1;
        if (run == null || run.IsFinished || run.PendingNode < 0 || run.Map[run.PendingNode].Type != NodeType.Event || effects == null)
            return false;
        bool needsRandomTarget = false;
        foreach (var effect in effects)
        {
            if (!IsSupported(effect) || (effect.Type == EventEffectType.GetRandomPassive && rewardRandom == null))
                return false;
            needsRandomTarget |= effect.Target == RunEventTarget.RandomLivingAlly;
        }
        int livingCount = 0;
        foreach (var unit in run.Party)
            if (unit.IsAlive)
                livingCount++;
        if (livingCount == 0 || (needsRandomTarget && eventRandom == null) || !CanApplyPermanentStats(run, effects))
            return false;
        if (needsRandomTarget)
        {
            // 같은 선택지 안의 ER 증가와 버프가 서로 다른 아군에게 갈 수 없도록 한 번만 뽑는다.
            int livingIndex = eventRandom.Range(0, livingCount);
            for (int index = 0; index < run.Party.Count; index++)
                if (run.Party[index].IsAlive && livingIndex-- == 0)
                {
                    randomTargetIndex = index;
                    break;
                }
        }
        foreach (var effect in effects)
        {
            if (effect.Type == EventEffectType.GetSP)
            {
                run.AddStartingResources((int)effect.Value, 0);
                continue;
            }
            if (effect.Type == EventEffectType.GetOP)
            {
                run.AddStartingResources(0, (int)effect.Value);
                continue;
            }
            if (effect.Type == EventEffectType.GetRandomPassive)
            {
                if (effect.CardCount == 1)
                    run.AddCard(effect.CardPool[rewardRandom.Range(0, effect.CardPool.Count)]);
                else
                    foreach (string cardId in RewardRules.Draw(effect.CardPool, rewardRandom))
                        run.AddCard(cardId);
                continue;
            }
            for (int index = 0; index < run.Party.Count; index++)
            {
                if (effect.Target == RunEventTarget.RandomLivingAlly && index != randomTargetIndex)
                    continue;
                var unit = run.Party[index];
                switch (effect.Type)
                {
                    case EventEffectType.IncreaseER:
                        // 맵의 ER 증가는 저항을 무시하고 획득 배율만 반영한다.
                        EchoRules.GainOwnER(unit, effect.Value, null, run.CardRandom);
                        break;
                    case EventEffectType.DecreaseER:
                        if (unit.IsAlive)
                            EchoRules.ReduceER(unit, effect.Value, null);
                        break;
                    case EventEffectType.DamageHP_Percent:
                        if (unit.IsAlive)
                            unit.ApplyDamage(unit.MaxHP * effect.Value / 100f);
                        break;
                    case EventEffectType.HealHP_Percent:
                        unit.Heal(unit.MaxHP * effect.Value / 100f);
                        break;
                    case EventEffectType.BuffSingleRandom:
                    case EventEffectType.BuffAll:
                        unit.Stats.AddModifier(new StatModifier(effect.TargetStat, effect.Value / 100f, ModSource.MetaProgress));
                        unit.ClampHPToMaximum();
                        break;
                }
            }
        }
        bool anyAlive = false;
        foreach (var unit in run.Party)
            anyAlive |= unit.IsAlive;
        if (!anyAlive)
            run.Fail();
        return true;
    }

    private static bool IsPermanentStat(RunEventEffect effect)
        => effect.Type == EventEffectType.BuffSingleRandom || effect.Type == EventEffectType.BuffAll;
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool CanApplyPermanentStats(RunState run, IReadOnlyList<RunEventEffect> effects)
    {
        // 무작위 추첨과 상태 변경 전에 가능한 대상의 최종값을 검사한다.
        foreach (var unit in run.Party)
            for (var stat = StatType.MaxHP; stat <= StatType.ER_GainMultiplier; stat++)
            {
                float percent = 0f;
                foreach (var effect in effects)
                    if (IsPermanentStat(effect) && effect.TargetStat == stat && (effect.Target == RunEventTarget.AllParty || unit.IsAlive))
                        percent += effect.Value / 100f;
                float next = unit.Stats.Get(stat) + (stat == StatType.ER_Resist ? percent : unit.Stats.GetBase(stat) * percent);
                if (!Finite(next) || (stat == StatType.MaxHP && next <= 0f) ||
                    (stat != StatType.ER_Resist && stat != StatType.MaxHP && next < 0f))
                    return false;
            }
        return true;
    }
}
