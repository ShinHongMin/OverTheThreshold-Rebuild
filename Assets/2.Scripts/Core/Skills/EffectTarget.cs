/// <summary>
/// 효과 하나가 누구에게 적용되는가.
///
/// TargetType과 역할이 다르다.
///   TargetType   — 스킬을 누구에게 쓰는가 (UI에서 대상을 지정하는 기준)
///   EffectTarget — 그 스킬의 각 효과가 누구에게 적용되는가
///
/// 이 구분이 없으면 "적을 때리면서 자신에게 버프"를 표현할 수 없다.
/// 기존 세이렌의 스킬이 그런 형태였다(적에게 피해 + 자신의 ER 증가).
/// </summary>
public enum EffectTarget
{
    /// <summary>스킬이 지정한 대상.</summary>
    Target = 0,

    /// <summary>스킬을 쓴 유닛 자신.</summary>
    Self = 1,
}
