/// <summary>
/// 스킬의 대상 분류.
/// 에셋에 정수로 저장되므로 기존 값은 유지하고 새 항목은 뒤에 추가한다.
/// </summary>
public enum TargetType
{
    /// <summary>적 하나. 지정 필요.</summary>
    SingleEnemy = 0,

    /// <summary>아군 하나. 지정 필요.</summary>
    Ally = 1,

    /// <summary>시전자 자신. 지정 없이 즉시 예약된다.</summary>
    Self = 2,

    /// <summary>아군 전체.</summary>
    Team = 3,

    /// <summary>적 전체.</summary>
    AllEnemy = 4,

    /// <summary>보스 전용. 자신을 그로기 상태로 만든다.</summary>
    Groggy = 5,
}
