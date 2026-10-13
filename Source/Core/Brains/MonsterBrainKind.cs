/// <summary>
/// MonsterBrainFactory가 생성할 몬스터 AI의 종류.
/// </summary>
public enum MonsterBrainKind
{
    /// <summary>Brain 없음. 아군이거나 별도로 행동을 지정하는 경우.</summary>
    None = 0,

    LostScout = 1,
    WhisperingEcho = 2,
    CorruptedBulwark = 3,
    ThresholdMass = 4,
    VoidHunter = 5,
    Avatar = 6,
}
