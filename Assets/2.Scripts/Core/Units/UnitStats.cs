using System.Collections.Generic;

/// <summary>
/// 유닛의 스탯. 기본값과 보정을 합쳐 최종값을 계산한다.
///
/// 기존 CharactersStat은 GetModifiedATK / GetModifiedDEF / GetModifiedER_Resist가
/// 각각 같은 순회를 복사해 갖고 있어서 스탯이 늘 때마다 함수가 늘어났다.
/// 여기서는 Get(StatType) 하나로 통합한다.
///
/// 보정의 출처는 둘이다.
///   StatModifier — 패시브 카드, 세이브 영구 보너스 (수명 없음)
///   ActiveBuff   — 전투 중 버프/디버프 (N턴 후 만료)
/// 계산 규칙은 같지만 수명과 제거 방식이 달라 따로 보관한다.
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
    }

    public IReadOnlyList<StatModifier> Modifiers => _modifiers;
    public IReadOnlyList<ActiveBuff> Buffs => _buffs;

    /// <summary>보정을 적용하지 않은 원본 값.</summary>
    public float GetBase(StatType stat)
        => _baseValues.TryGetValue(stat, out float value) ? value : 0f;

    /// <summary>
    /// 모든 보정이 적용된 최종 값.
    ///
    /// MaxHP / ATK / DEF : base × (1 + 보정합)
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
    /// (기존 CharactersStat.AddBuff 규칙)
    /// </summary>
    public void AddBuff(string buffId, BuffType type, float value, int durationTurns)
    {
        if (durationTurns <= 0) return;

        ActiveBuff existing = FindBuff(buffId);

        if (existing != null) existing.Refresh(value, durationTurns);
        else _buffs.Add(new ActiveBuff(buffId, type, value, durationTurns));
    }

    public ActiveBuff FindBuff(string buffId)
    {
        for (int i = 0; i < _buffs.Count; i++)
            if (_buffs[i].BuffId == buffId) return _buffs[i];

        return null;
    }

    public bool HasBuff(string buffId) => FindBuff(buffId) != null;

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
    /// 호출 시점은 턴 종료이며, W3의 TurnLoop이 담당한다.
    /// </summary>
    internal void TickBuffDurations()
    {
        for (int i = _buffs.Count - 1; i >= 0; i--)
            if (_buffs[i].TickDuration()) _buffs.RemoveAt(i);
    }
}
