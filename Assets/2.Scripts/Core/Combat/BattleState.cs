/// <summary>
/// 전투 진행 단계. 기존 GameEnum.BattleState를 Core로 옮긴 것이다.
/// </summary>
public enum BattleState
{
    /// <summary>플레이어가 행동을 예약하는 중.</summary>
    PlayerTurn_Input = 0,

    /// <summary>예약된 행동을 실행하는 중.</summary>
    PlayerTurn_Action = 1,

    /// <summary>적이 행동하는 중.</summary>
    EnemyTurn = 2,
}
