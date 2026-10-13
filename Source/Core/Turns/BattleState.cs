/// <summary>
/// 전투의 입력·실행 진행 단계.
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
