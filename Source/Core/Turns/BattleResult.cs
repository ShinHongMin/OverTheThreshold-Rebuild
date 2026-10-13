/// <summary>
/// 전투 종료 판정.
/// </summary>
public enum BattleResult
{
    /// <summary>아직 끝나지 않았다.</summary>
    InProgress = 0,

    /// <summary>적을 모두 쓰러뜨렸다.</summary>
    Victory = 1,

    /// <summary>파티가 전멸했거나 전원 ECHO 상태가 되었다.</summary>
    Defeat = 2,
}
