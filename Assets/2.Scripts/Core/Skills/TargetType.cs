/// <summary>
/// 스킬의 대상 분류.
///
/// 주의: 이 값들은 기존 SkillData 에셋 28개에 정수로 직렬화되어 있다.
/// 순서를 바꾸거나 중간에 값을 끼워 넣으면 기존 에셋이 다른 대상을 가리키게 된다.
///
/// AllEnemy는 기존 코드에서 AllEnmey로 오타가 나 있었다.
/// 정수값(4)을 그대로 두고 이름만 교정했으므로 에셋 데이터는 영향을 받지 않는다.
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
