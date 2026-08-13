using System.Collections.Generic;

/// <summary>
/// 전투 한 판의 상태를 담는 그릇. 커맨드는 이것만 받아서 실행된다.
///
/// W1 범위이므로 아직 참가자 목록만 있다.
/// 턴 번호 / BattleState / 파티 공용 자원(SP·OP) / 난수는
/// 각각 실제로 필요해지는 주차에 추가한다.
/// </summary>
public sealed class CombatContext
{
    public IReadOnlyList<Unit> Party { get; }
    public IReadOnlyList<Unit> Enemies { get; }

    public CombatContext(IReadOnlyList<Unit> party, IReadOnlyList<Unit> enemies)
    {
        Party = party;
        Enemies = enemies;
    }
}
