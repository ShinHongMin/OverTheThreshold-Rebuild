/// <summary>
/// 개별 효과의 적용 대상. 스킬의 선택 대상(TargetType)과 구분한다.
/// 적에게 피해를 주면서 시전자에게 ER을 부여하는 등의 조합에 사용한다.
/// </summary>
public enum EffectTarget
{
    /// <summary>스킬이 지정한 대상.</summary>
    Target = 0,

    /// <summary>스킬을 쓴 유닛 자신.</summary>
    Self = 1,
}
