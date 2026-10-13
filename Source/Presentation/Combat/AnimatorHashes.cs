using UnityEngine;

/// <summary>
/// Animator 파라미터의 해시를 캐싱한다.
/// 문자열을 수정할 때는 Animator Controller의 파라미터 이름도 함께 맞춰야 한다.
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
    public static readonly int IsGroggy    = Animator.StringToHash("IsGroggy");

    // 세이렌 전용
    public static readonly int E_IsEntry   = Animator.StringToHash("E_IsEntry");
    public static readonly int IsER        = Animator.StringToHash("IsER");
}
