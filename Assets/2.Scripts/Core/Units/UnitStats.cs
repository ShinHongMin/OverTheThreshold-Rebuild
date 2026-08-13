using System.Collections.Generic;

/// <summary>
/// 유닛의 스탯. 현재는 기본값만 들고 있다.
/// 보정(패시브 카드 / 영구 보너스 / 버프) 합산은 W2에서 실제로 필요해질 때 추가한다.
/// </summary>
public sealed class UnitStats
{
    private readonly Dictionary<StatType, float> _baseValues = new Dictionary<StatType, float>();

    public UnitStats(float maxHp, float atk, float def, float erResist = 0f)
    {
        _baseValues[StatType.MaxHP]     = maxHp;
        _baseValues[StatType.ATK]       = atk;
        _baseValues[StatType.DEF]       = def;
        _baseValues[StatType.ER_Resist] = erResist;
    }

    public float Get(StatType stat)
        => _baseValues.TryGetValue(stat, out var value) ? value : 0f;
}
