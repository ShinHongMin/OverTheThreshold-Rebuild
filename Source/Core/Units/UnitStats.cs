using System.Collections.Generic;

/// <summary>
/// 기본 스탯에 보정을 합산해 최종 수치를 계산한다.
/// 지속 턴이 없는 StatModifier와 턴 종료 시 만료되는 ActiveBuff를 따로 관리한다.
/// </summary>
public sealed class UnitStats
{
    private readonly Dictionary<StatType, float> _baseValues = new Dictionary<StatType, float>();
    private readonly List<StatModifier> _modifiers = new List<StatModifier>();
    private readonly List<ActiveBuff> _buffs = new List<ActiveBuff>();

    public UnitStats(float maxHp, float atk, float def, float erResist = 0f)
    {
        _baseValues[StatType.MaxHP]     = maxHp;
        _baseValues[StatType.ATK]       = atk;
        _baseValues[StatType.DEF]       = def;
        _baseValues[StatType.ER_Resist] = erResist;
        _baseValues[StatType.ER_GainMultiplier] = 1f;
    }

    public PassiveCardBonuses CardBonuses { get; } = new PassiveCardBonuses();

    public bool HasDebuff
    {
        get
        {
            for (int i = 0; i < _buffs.Count; i++) if (_buffs[i].IsDebuff) return true;
            return false;
        }
    }

    public IReadOnlyList<StatModifier> Modifiers => _modifiers;
    public IReadOnlyList<ActiveBuff> Buffs => _buffs;
    internal void ClearBuffs() => _buffs.Clear();

    /// <summary>보정을 적용하지 않은 원본 값.</summary>
    public float GetBase(StatType stat)
        => _baseValues.TryGetValue(stat, out float value) ? value : 0f;

    /// <summary>
    /// 모든 보정이 적용된 최종 값.
    ///
    /// MaxHP / ATK / DEF / ER_GainMultiplier : base × (1 + 보정합)
    /// ER_Resist         : base + 보정합 을 0~1 로 제한 (곱이 아니라 합이다)
    /// </summary>
    public float Get(StatType stat)
    {
        float baseValue = GetBase(stat);
        float sum = SumPercent(stat);

        if (stat == StatType.ER_Resist)
        {
            float total = baseValue + sum;
            if (total < 0f) return 0f;
            return total > 1f ? 1f : total;
        }

        return baseValue * (1f + sum);
    }

    private float SumPercent(StatType stat)
    {
        float sum = 0f;

        for (int i = 0; i < _modifiers.Count; i++)
            if (_modifiers[i].Stat == stat) sum += _modifiers[i].Percent;

        for (int i = 0; i < _buffs.Count; i++)
            if (BuffTypeToStat(_buffs[i].Type) == stat) sum += _buffs[i].Value;

        return sum;
    }

    /// <summary>
    /// 버프 종류가 어떤 스탯에 작용하는지. 스탯에 작용하지 않는 버프는 None을 반환한다.
    /// Damage_Taken_Percent는 데미지 계산에서, Taunt는 타게팅에서 따로 처리된다.
    /// </summary>
    public static StatType BuffTypeToStat(BuffType type)
    {
        switch (type)
        {
            case BuffType.ATK_Percent:       return StatType.ATK;
            case BuffType.DEF_Percent:       return StatType.DEF;
            case BuffType.ER_Resist_Percent: return StatType.ER_Resist;
            default:                         return StatType.None;
        }
    }

    // ── 보정 (패시브 카드 / 영구 보너스) ─────────────────────────

    public void AddModifier(StatModifier modifier) => _modifiers.Add(modifier);

    /// <summary>특정 출처의 보정을 모두 제거한다. 제거된 개수를 반환.</summary>
    public int RemoveModifiersFrom(ModSource source)
        => _modifiers.RemoveAll(m => m.Source == source);

    // ── 버프 ─────────────────────────────────────────────────────

    /// <summary>
    /// 버프를 건다. 같은 BuffId가 이미 있으면 중첩되지 않고 갱신된다.
    /// </summary>
    public void AddBuff(string buffId, BuffType type, float value, int durationTurns, bool isDebuff = false)
    {
        if (durationTurns <= 0) return;

        ActiveBuff existing = FindBuff(buffId);

        if (existing != null) existing.Refresh(value, durationTurns, isDebuff);
        else _buffs.Add(new ActiveBuff(buffId, type, value, durationTurns, isDebuff));
    }

    public ActiveBuff FindBuff(string buffId)
    {
        for (int i = 0; i < _buffs.Count; i++)
            if (_buffs[i].BuffId == buffId) return _buffs[i];

        return null;
    }

    public bool HasBuff(string buffId) => FindBuff(buffId) != null;

    /// <summary>지정한 버프만 제거한다. 상태 종료처럼 지속 만료보다 먼저 해제할 때 쓴다.</summary>
    internal bool RemoveBuff(string buffId)
    {
        for (int i = 0; i < _buffs.Count; i++)
            if (_buffs[i].BuffId == buffId)
            {
                _buffs.RemoveAt(i);
                return true;
            }
        return false;
    }

    public bool HasBuffType(BuffType type)
    {
        for (int i = 0; i < _buffs.Count; i++)
            if (_buffs[i].Type == type) return true;

        return false;
    }

    /// <summary>해당 종류의 버프 수치 합. 데미지 증폭 계산 등에 쓰인다.</summary>
    public float SumBuffValue(BuffType type)
    {
        float sum = 0f;

        for (int i = 0; i < _buffs.Count; i++)
            if (_buffs[i].Type == type) sum += _buffs[i].Value;

        return sum;
    }

    /// <summary>
    /// 모든 버프의 지속 턴을 1 줄이고 만료된 것을 제거한다.
    /// TurnLoop이 턴 종료 시 호출한다.
    /// </summary>
    internal void TickBuffDurations()
    {
        for (int i = _buffs.Count - 1; i >= 0; i--)
            if (_buffs[i].TickDuration()) _buffs.RemoveAt(i);
    }
}
