/// <summary>지속 만료 전에 버프가 해제됐다. 화면은 이 시점에 아이콘과 상태를 갱신한다.</summary>
public sealed class BuffRemoved : CombatEvent
{
    public Unit Target { get; }
    public string BuffId { get; }

    public BuffRemoved(Unit target, string buffId)
    {
        Target = target;
        BuffId = buffId;
    }
}
