/// <summary>
/// 적 AI와 ECHO 제어 불능 유닛의 행동을 결정한다.
/// 실행할 커맨드를 반환하며, 실행과 연출은 호출자가 담당한다.
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
