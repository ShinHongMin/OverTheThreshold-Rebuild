/// <summary>
/// 유닛의 행동을 대신 결정한다. 적 AI와 ECHO 제어 불능 상태가 같은 인터페이스를 쓴다.
///
/// 기존에는 MonsterController가 abstract ActTurn()에서 대상 선택·스킬 선택·연출을
/// 한꺼번에 처리하고 코루틴을 직접 시작했다. 그래서 결정만 떼어내 검증할 수 없었고,
/// 언제 끝나는지 알 수 없어 호출한 쪽이 WaitForSeconds(2f)로 추측 대기를 해야 했다.
///
/// 여기서는 커맨드를 만들어 돌려주기만 한다. 실행은 TurnLoop이,
/// 연출은 CombatEventPlayer가 담당한다.
/// 만들어진 커맨드는 플레이어가 예약한 것과 같은 큐에 들어간다.
/// </summary>
public interface IActionBrain
{
    /// <summary>
    /// 이번 턴에 무엇을 할지 정한다.
    ///
    /// null 반환은 오류가 아니라 "이번 턴은 행동하지 않는다"는 정식 결과다.
    /// 보스가 그로기 상태이거나 충전을 기다리는 경우가 여기 해당한다.
    /// </summary>
    ICombatCommand Decide(CombatContext ctx, Unit self);

    /// <summary>
    /// 전투 중 일어난 일을 통지받는다. 자기 차례가 아닐 때 상태를 바꾸기 위한 창구다.
    ///
    /// 보스가 충전 중 실드를 잃으면 즉시 그로기에 빠지는 기믹이 이 경로를 쓴다.
    /// 상태를 갖지 않는 Brain은 아무것도 하지 않아도 된다.
    /// </summary>
    void OnEvent(CombatEvent combatEvent);
}
