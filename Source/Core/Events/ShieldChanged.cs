/// <summary>피해 이외의 이유로 실드가 제거되거나 만료된 시점의 표시값.</summary>
public sealed class ShieldChanged : CombatEvent
{
    public Unit Target { get; }
    public float ShieldAfter { get; }
    public ShieldChanged(Unit target)
    {
        Target = target;
        ShieldAfter = target.CurrentShield;
    }
}
