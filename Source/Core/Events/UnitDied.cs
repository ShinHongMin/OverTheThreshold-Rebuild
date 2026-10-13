/// <summary>
/// 유닛이 쓰러졌다. 항상 원인이 된 이벤트(DamageDealt 등) 바로 뒤에 온다.
/// </summary>
public sealed class UnitDied : CombatEvent
{
    public Unit Unit { get; }

    public UnitDied(Unit unit)
    {
        Unit = unit;
    }

    public override string ToString() => $"UnitDied {{ Unit={Unit.Name} }}";
}
