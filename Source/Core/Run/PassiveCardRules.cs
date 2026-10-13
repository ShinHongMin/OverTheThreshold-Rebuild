using System;
using System.Collections.Generic;

/// <summary>카드 ID로 데이터를 조회하고, 효과 종류에 따라 런에 적용한다.</summary>
public sealed class PassiveCardRules
{
    private readonly struct CardEffect
    {
        public readonly PassiveEffectType Type;
        public readonly float Value;
        public CardEffect(PassiveEffectType type, float value)
        {
            Type = type;
            Value = value;
        }
    }

    private readonly Dictionary<string, CardEffect> _cardEffects = new Dictionary<string, CardEffect>(StringComparer.Ordinal);

    public void RegisterStatBonus(string cardId, StatType stat, float percent)
    {
        PassiveEffectType type;
        switch (stat)
        {
            case StatType.ATK:
                type = PassiveEffectType.IncreaseATK_Percent;
                break;
            case StatType.DEF:
                type = PassiveEffectType.IncreaseDEF_Percent;
                break;
            case StatType.MaxHP:
                type = PassiveEffectType.IncreaseMaxHP_Percent;
                break;
            case StatType.ER_Resist:
                type = PassiveEffectType.IncreaseER_Resist;
                break;
            default:
                throw new ArgumentException("지원되지 않는 카드 능력치입니다.");
        }
        RegisterCard(cardId, type, percent * 100f);
    }

    public void RegisterCard(string cardId, PassiveEffectType type, float value)
    {
        if (string.IsNullOrWhiteSpace(cardId))
            throw new ArgumentException("카드 ID가 필요합니다.");
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
            throw new ArgumentOutOfRangeException(nameof(value));
        if (type <= PassiveEffectType.None || type > PassiveEffectType.Echo_OP || type == PassiveEffectType.Speical)
            throw new ArgumentException("카드의 구체적인 효과 종류를 지정하세요: " + cardId);
        if (type == PassiveEffectType.BattleStart_SP || type == PassiveEffectType.BattleStart_OP || type == PassiveEffectType.Echo_OP)
        {
            int cap = type == PassiveEffectType.BattleStart_SP ? PartyResources.DefaultMaxSP : PartyResources.DefaultMaxOP;
            if (value != Math.Floor(value) || value > cap)
                throw new ArgumentException("SP/OP 효과는 자원 상한 이내의 정수여야 합니다.");
        }
        if (_cardEffects.ContainsKey(cardId))
            throw new ArgumentException("중복된 카드 정의: " + cardId);
        _cardEffects.Add(cardId, new CardEffect(type, value));
    }

    public bool Contains(string cardId) => cardId != null && _cardEffects.ContainsKey(cardId);

    public void ApplyToParty(string cardId, IReadOnlyList<Unit> party)
    {
        if (!_cardEffects.TryGetValue(cardId, out var effect))
            return;
        StatType stat = StatType.None;
        switch (effect.Type)
        {
            case PassiveEffectType.IncreaseATK_Percent:
                stat = StatType.ATK;
                break;
            case PassiveEffectType.IncreaseDEF_Percent:
                stat = StatType.DEF;
                break;
            case PassiveEffectType.IncreaseMaxHP_Percent:
                stat = StatType.MaxHP;
                break;
            case PassiveEffectType.IncreaseER_Resist:
                stat = StatType.ER_Resist;
                break;
        }
        foreach (var member in party)
        {
            // 최대 HP가 늘어도 현재 HP는 유지한다. 회복이나 부활은 별도 효과다.
            if (stat != StatType.None)
                member.Stats.AddModifier(new StatModifier(stat, effect.Value / 100f, ModSource.PassiveCard));
            else
                member.Stats.CardBonuses.Add(effect.Type, effect.Value);
        }
    }
    /// <summary>시작 SP/OP는 파티 인원 수와 무관하게 보유한 카드 장수만 합산한다.</summary>
    public int GetStartingResourceBonus(IReadOnlyList<string> cards, PassiveEffectType type)
    {
        int cap = type == PassiveEffectType.BattleStart_SP ? PartyResources.DefaultMaxSP : PartyResources.DefaultMaxOP;
        int total = 0;
        foreach (var cardId in cards)
            if (_cardEffects.TryGetValue(cardId, out var effect) && effect.Type == type)
                total = Math.Min(cap, total + (int)effect.Value);
        return total;
    }
}
