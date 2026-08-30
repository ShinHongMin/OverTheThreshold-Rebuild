using System.Collections.Generic;

/// <summary>
/// 전투 한 판의 상태를 담는 그릇. 커맨드는 이것만 받아서 실행된다.
///
/// 진영은 Unit이 갖지 않는다. "어느 편인가"는 유닛 자체의 성질이 아니라
/// 이 전투에서의 배치이므로 컨텍스트가 알려준다.
///
/// 턴 번호 / BattleState / 난수는 각각 실제로 필요해지는 주차에 추가한다.
/// </summary>
public sealed class CombatContext
{
    private static readonly Unit[] Empty = new Unit[0];

    public IReadOnlyList<Unit> Party { get; }
    public IReadOnlyList<Unit> Enemies { get; }

    /// <summary>SP·OP. 개인 자원이 아니라 파티 공용이므로 여기에 둔다.</summary>
    public PartyResources Resources { get; }

    public CombatContext(
        IReadOnlyList<Unit> party,
        IReadOnlyList<Unit> enemies,
        PartyResources resources = null)
    {
        Party = party;
        Enemies = enemies;
        Resources = resources ?? new PartyResources();
    }

    /// <summary>이 유닛이 파티 소속인가.</summary>
    public bool IsPartyMember(Unit unit) => Contains(Party, unit);

    /// <summary>이 유닛에게 적인 쪽.</summary>
    public IReadOnlyList<Unit> GetOpponentsOf(Unit unit)
    {
        if (IsPartyMember(unit)) return Enemies;
        if (Contains(Enemies, unit)) return Party;
        return Empty;
    }

    /// <summary>이 유닛에게 아군인 쪽. 자기 자신을 포함한다.</summary>
    public IReadOnlyList<Unit> GetAlliesOf(Unit unit)
    {
        if (IsPartyMember(unit)) return Party;
        if (Contains(Enemies, unit)) return Enemies;
        return Empty;
    }

    private static bool Contains(IReadOnlyList<Unit> source, Unit unit)
    {
        for (int i = 0; i < source.Count; i++)
            if (source[i] == unit) return true;

        return false;
    }
}
