/// <summary>
/// 캐릭터 직군. ECHO 상태에서의 처리가 여기에 따라 갈린다.
///
/// 주의: 기존 프리팹과 에셋에 정수로 직렬화되어 있으므로 순서 변경 금지.
/// </summary>
public enum CharacterJob
{
    /// <summary>뱅가드. 전열 담당.</summary>
    Vanguard = 0,

    /// <summary>레조넌스. ER 50 이상에서 스킬이 교체되며, ECHO 상태에서도 제어를 유지한다.</summary>
    Resonance = 1,

    /// <summary>메딕. 회복 담당.</summary>
    Medic = 2,
}
