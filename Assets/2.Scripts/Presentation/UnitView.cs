using UnityEngine;
using UnityEngine.UI;

public class UnitView : MonoBehaviour
{
    [Header("필수")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform hitPos;

    [Header("선택 — 몬스터만 사용")]
    [SerializeField] private Slider hpBar;

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
        RefreshHpBar();
    }

    public void PlayEntry()       => SetBoolSafe(AnimatorHashes.IsEntry, true);
    public void PlayBasicAttack() => SetTriggerSafe(AnimatorHashes.BasicAttack);
    public void PlaySkill()       => SetTriggerSafe(AnimatorHashes.Skill);
    public void PlayOverload()    => SetTriggerSafe(AnimatorHashes.OPSkill);

    /// <summary>피격 반응. 실드가 피해를 전부 막아도 재생한다.</summary>
    public void PlayHit()         => SetTriggerSafe(AnimatorHashes.Hit);

    public void PlayDead()        => SetBoolSafe(AnimatorHashes.Dead, true);

    /// <summary>
    /// HP 바를 현재 값으로 맞춘다. hpBar가 없으면 아무것도 하지 않는다.
    /// 기존 MonsterController는 Update()에서 매 프레임 갱신했으나
    /// 값이 바뀌는 시점에만 호출하면 충분하다.
    /// </summary>
    public void RefreshHpBar()
    {
        if (hpBar == null || Unit == null) return;

        hpBar.maxValue = Unit.MaxHP;
        hpBar.value = Unit.CurrentHP;
    }

    // ── 애니메이터 안전 호출 ──────────────────────────────────────
    // 몬스터마다 애니메이터 구성이 달라 특정 파라미터가 없을 수 있다.
    // (예: LostScout에는 등장 연출이 없어 IsEntry가 존재하지 않는다)
    // 없는 파라미터를 호출하면 경고가 쏟아지므로 미리 확인한다.
    public void OnEntryFinished() => SetBoolSafe(AnimatorHashes.IsEntry, false);

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
}
