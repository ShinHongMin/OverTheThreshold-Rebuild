using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 유닛 하나의 표시 담당. 아군·적군 공통으로 사용한다.
///
/// 기존에는 AegisController / MonsterController가 스탯·AI·연출을 모두 갖고 있었고
/// 캐릭터마다 클래스가 따로 있었다. 여기서는 표시만 담당하므로 한 클래스로 충분하다.
///
/// 이 클래스는 규칙을 갖지 않는다. Unit의 값을 읽어서 보여줄 뿐이며 상태를 바꾸지 않는다.
/// </summary>
public class UnitView : MonoBehaviour
{
    [Header("필수")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform hitPos;

    [Header("선택 — 몬스터만 사용")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private Slider shieldBar;

    /// <summary>이 뷰가 표시하는 유닛. Bind로 연결된다.</summary>
    public Unit Unit { get; private set; }

    /// <summary>타격 이펙트가 생성될 위치. 지정되지 않았으면 자기 자신.</summary>
    public Transform HitPos => hitPos != null ? hitPos : transform;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
    }

    public void Bind(Unit unit)
    {
        Unit = unit;
        RefreshBars();
    }

    public void PlayEntry()       => SetBoolSafe(AnimatorHashes.IsEntry, true);
    public void PlayBasicAttack() => SetTriggerSafe(AnimatorHashes.BasicAttack);
    public void PlaySkill()       => SetTriggerSafe(AnimatorHashes.Skill);
    public void PlayOverload()    => SetTriggerSafe(AnimatorHashes.OPSkill);

    /// <summary>피격 반응. 실드가 피해를 전부 막아도 재생한다.</summary>
    public void PlayHit()         => SetTriggerSafe(AnimatorHashes.Hit);

    public void PlayDead()        => SetBoolSafe(AnimatorHashes.Dead, true);

    /// <summary>
    /// 체력·실드 바를 현재 값으로 맞춘다. 바가 없으면 아무것도 하지 않는다.
    /// 기존 MonsterController는 Update()에서 매 프레임 갱신했으나
    /// 값이 바뀌는 시점에만 호출하면 충분하다.
    /// </summary>
    public void RefreshBars()
    {
        if (Unit == null) return;

        if (hpBar != null)
        {
            hpBar.maxValue = Unit.MaxHP;
            hpBar.value = Unit.CurrentHP;
        }

        if (shieldBar != null)
        {
            // 실드 바도 최대 체력을 기준으로 채운다. 체력 바와 눈금이 맞아야
            // "실드가 체력 위에 덧씌워진" 것으로 읽힌다.
            shieldBar.maxValue = Unit.MaxHP;
            shieldBar.value = Unit.CurrentShield;
            shieldBar.gameObject.SetActive(Unit.HasShield);
        }
    }

    // ── 애니메이터 안전 호출 ──────────────────────────────────────
    // 몬스터마다 애니메이터 구성이 달라 특정 파라미터가 없을 수 있다.
    // (예: LostScout에는 등장 연출이 없어 IsEntry가 존재하지 않는다)

    private bool HasParameter(int hash)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
            if (parameters[i].nameHash == hash) return true;

        return false;
    }

    private void SetTriggerSafe(int hash)
    {
        if (HasParameter(hash)) animator.SetTrigger(hash);
    }

    private void SetBoolSafe(int hash, bool value)
    {
        if (HasParameter(hash)) animator.SetBool(hash, value);
    }
    /// <summary>등장 애니메이션이 끝났음을 클립 이벤트로부터 통지받는다.</summary>
    public void OnEntryFinished() => SetBoolSafe(AnimatorHashes.IsEntry, false);
}
