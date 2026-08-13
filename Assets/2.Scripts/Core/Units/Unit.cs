/// <summary>
/// 전투에 참여하는 유닛 하나. 아군/적군 구분은 CombatContext가 갖는다.
///
/// W1 범위이므로 ER / 실드 / 버프 목록은 아직 없다.
/// 기존 CharactersStat이 400줄이 된 원인이 "언젠가 쓸 것 같아서" 미리 넣은
/// 필드들이었으므로, 각 항목은 실제로 쓰이는 주차에 추가한다.
/// </summary>
public sealed class Unit
{
    public string Name { get; }
    public UnitStats Stats { get; }

    public float CurrentHP { get; private set; }

    public float MaxHP => Stats.Get(StatType.MaxHP);
    public bool IsAlive => CurrentHP > 0f;

    public Unit(string name, UnitStats stats)
    {
        Name = name;
        Stats = stats;
        CurrentHP = stats.Get(StatType.MaxHP);
    }

    /// <summary>
    /// 체력을 깎고 실제로 깎인 양을 반환한다.
    ///
    /// internal인 이유: 커맨드를 거치지 않고 체력을 바꾸는 경로를 막기 위해서다.
    /// 이렇게 해야 모든 피해가 반드시 CombatEvent를 남긴다.
    /// </summary>
    internal float ApplyDamage(float amount)
    {
        if (amount <= 0f) return 0f;

        float before = CurrentHP;
        CurrentHP = before - amount;
        if (CurrentHP < 0f) CurrentHP = 0f;

        return before - CurrentHP;
    }

    public override string ToString() => $"{Name} HP {CurrentHP:0.##}/{MaxHP:0.##}";
}
