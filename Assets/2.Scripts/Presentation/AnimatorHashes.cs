using UnityEngine;

/// <summary>
/// 애니메이터 파라미터 해시 모음.
///
/// 문자열은 애니메이터 컨트롤러에 등록된 이름과 정확히 일치해야 하므로 변경 금지.
/// 호출 방식도 기존과 동일하게 유지한다.
///   SetTrigger : BasicAttack, Skill, OPSkill, Hit
///   SetBool    : IsEntry, Dead, E_IsEntry(세이렌), IsER(세이렌)
///   Idle       : 코드에서 직접 호출하지 않음 (전이 조건용)
/// </summary>
public static class AnimatorHashes
{
    public static readonly int IsEntry     = Animator.StringToHash("IsEntry");
    public static readonly int Idle        = Animator.StringToHash("Idle");
    public static readonly int BasicAttack = Animator.StringToHash("BasicAttack");
    public static readonly int Skill       = Animator.StringToHash("Skill");
    public static readonly int OPSkill     = Animator.StringToHash("OPSkill");
    public static readonly int Hit         = Animator.StringToHash("Hit");
    public static readonly int Dead        = Animator.StringToHash("Dead");

    // 세이렌 전용
    public static readonly int E_IsEntry   = Animator.StringToHash("E_IsEntry");
    public static readonly int IsER        = Animator.StringToHash("IsER");
}
