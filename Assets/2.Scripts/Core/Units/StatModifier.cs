/// <summary>
/// 스탯 보정 하나. 이 게임의 보정은 전부 가산 % 방식이며
/// 고정 수치를 더하는 보정은 존재하지 않는다.
///
/// 예: Percent = 0.3f 는 +30%.
/// </summary>
public readonly struct StatModifier
{
    public readonly StatType Stat;
    public readonly float Percent;
    public readonly ModSource Source;

    public StatModifier(StatType stat, float percent, ModSource source)
    {
        Stat = stat;
        Percent = percent;
        Source = source;
    }

    public override string ToString()
        => $"{Stat} {Percent * 100f:+0.#;-0.#}% ({Source})";
}
