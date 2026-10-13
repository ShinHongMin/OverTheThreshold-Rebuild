using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

/// <summary>
/// 유닛의 애니메이션과 전투 상태 표시를 담당한다.
/// </summary>
public class UnitView : MonoBehaviour
{
    [Header("필수")]
    [UnityEngine.Serialization.FormerlySerializedAs("animator")]
    [SerializeField] private Animator unitAnimator;
    [UnityEngine.Serialization.FormerlySerializedAs("hitPos")]
    [SerializeField] private Transform hitEffectAnchor;
    [SerializeField] private Transform buffEffectAnchor;
    [SerializeField] private Transform projectileAnchor;
    [SerializeField] private GameObject turnIndicator;
    [SerializeField] private GameObject echoIcon;
    [SerializeField] private GameObject enhancedIcon;
    [SerializeField] private BuffListView buffList;
    public void ConfigureBuffPool(PrefabPoolManager pool)
    {
        buffList?.ConfigurePool(pool);
        RefreshBars();
    }

    public void ConfigureBuffTooltip(BuffTooltipView tooltip)
    {
        if (buffList != null)
            buffList.ConfigureTooltip(tooltip);
    }

    [Header("선택 — 몬스터만 사용")]
    [UnityEngine.Serialization.FormerlySerializedAs("hpBar")]
    [SerializeField] private Slider healthSlider;
    [UnityEngine.Serialization.FormerlySerializedAs("shieldBar")]
    [SerializeField] private Slider shieldSlider;
    [Tooltip("이 버프가 유지되는 동안 기존 IsGroggy 동작을 재생한다. 사용하지 않는 유닛은 비워 둔다.")]
    [SerializeField] private BuffData groggyBuff;
    private string _groggyBuffId;
    private bool HasGroggyBuff => Unit != null && Unit.IsAlive
        && !string.IsNullOrEmpty(_groggyBuffId) && Unit.Stats.HasBuff(_groggyBuffId);
    // Groggy 태그는 진입·대기·회복 상태에 지정한다. 회복이 끝나면 일반 Idle로 돌아간다.
    public bool IsGroggyRecoveryPlaying => Unit != null && Unit.IsAlive && !HasGroggyBuff
        && groggyBuff != null && unitAnimator != null && unitAnimator.isActiveAndEnabled
        && unitAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Groggy");
    private RuntimeAnimatorController _cachedAnimatorController;
    private AnimatorControllerParameter[] _cachedAnimatorParameters;
    private SortingGroup _unitSortingGroup;
    private SpriteRenderer _bodyRenderer;
    private bool _waitingForImpact;
    private bool _waitingForEntry;
    public bool HasImpactSignal
    {
        get; private set;
    }

    public bool IsEntryPlaying => _waitingForEntry && isActiveAndEnabled
        && unitAnimator != null && unitAnimator.isActiveAndEnabled && Unit != null && Unit.IsAlive;

    /// <summary>이 뷰가 표시하는 유닛. Bind로 연결된다.</summary>
    public Unit Unit
    {
        get; private set;
    }

    /// <summary>타격 이펙트가 생성될 위치. 지정되지 않았으면 자기 자신.</summary>
    public Transform HitEffectAnchor => hitEffectAnchor != null ? hitEffectAnchor : transform;
    public Transform BuffEffectAnchor => buffEffectAnchor != null ? buffEffectAnchor : transform;
    public Transform ProjectileAnchor => projectileAnchor != null ? projectileAnchor : HitEffectAnchor;
    public SortingGroup RenderOrder => _unitSortingGroup;
    public Bounds BodyBounds => _bodyRenderer != null ? _bodyRenderer.bounds : new Bounds(HitEffectAnchor.position, Vector3.one * 2f);

    public void BeginImpactWait()
    {
        HasImpactSignal = false;
        _waitingForImpact = true;
    }

    public void EndImpactWait() => _waitingForImpact = false;
    public void OnSkillImpact()
    {
        if (_waitingForImpact)
            HasImpactSignal = true;
    }

    private void Reset()
    {
        unitAnimator = GetComponentInChildren<Animator>();
    }

    public void Bind(Unit unit)
    {
        ResetEntry();
        // 이전 보스가 사망/그로기 상태였어도 새 전투의 유닛은 기본 자세에서 시작한다.
        if (groggyBuff != null && Unit != null && Unit != unit && unitAnimator != null)
            unitAnimator.Rebind();
        Unit = unit;
        _groggyBuffId = groggyBuff != null ? groggyBuff.name : null;
        SetTurnIndicator(false);
        if (GetComponent<UnitRenderOrder>() == null)
            gameObject.AddComponent<UnitRenderOrder>();
        _unitSortingGroup = GetComponent<SortingGroup>();
        _bodyRenderer = unitAnimator != null ? unitAnimator.GetComponent<SpriteRenderer>() : null;
        _waitingForImpact = false;
        HasImpactSignal = false;
        SetBoolSafe(AnimatorHashes.Dead, !unit.IsAlive);
        RefreshBars();
    }

    /// <summary>전투 첫 등장. 연출이 없는 적이나 사망한 유닛은 대기하지 않는다.</summary>
    public void PlayEntry()
    {
        ResetEntry();
        if (!isActiveAndEnabled || Unit == null || !Unit.IsAlive
            || unitAnimator == null || !unitAnimator.isActiveAndEnabled
            || !HasParameter(AnimatorHashes.IsEntry))
            return;

        // 슬롯의 뷰를 다음 전투에서 재사용해도 이전 행동/강화 Idle 상태에서 시작하지 않는다.
        unitAnimator.Rebind();
        SetBoolSafe(AnimatorHashes.IsER, ResonanceRules.IsEnhanced(Unit));
        _waitingForEntry = true;
        // 기존 세이렌 Animator는 강화 등장에서도 IsEntry=false를 퇴장 조건으로 쓴다.
        // 종료 이벤트가 두 값을 내릴 때까지 유지해 강화 클립이 중간에 끝나지 않게 한다.
        SetBoolSafe(AnimatorHashes.IsEntry, true);
        SetBoolSafe(AnimatorHashes.E_IsEntry, ResonanceRules.IsEnhanced(Unit));
    }

    public void ResetEntry()
    {
        _waitingForEntry = false;
        SetBoolSafe(AnimatorHashes.IsEntry, false);
        SetBoolSafe(AnimatorHashes.E_IsEntry, false);
    }

    public void RequestNormalEntry()
    {
        if (Unit != null && Unit.IsAlive && !ResonanceRules.IsEnhanced(Unit))
            SetBoolSafe(AnimatorHashes.IsEntry, true);
    }

    public void RequestEnhancedEntry()
    {
        if (Unit != null && Unit.IsAlive && ResonanceRules.IsEnhanced(Unit))
            SetBoolSafe(AnimatorHashes.E_IsEntry, true);
    }

    public void PlayBasicAttack() => SetTriggerSafe(AnimatorHashes.BasicAttack);
    public void PlaySkill() => SetTriggerSafe(AnimatorHashes.Skill);
    public void PlayOverload() => SetTriggerSafe(AnimatorHashes.OPSkill);
    public void PlayAnimationTrigger(string triggerName) => SetTriggerSafe(Animator.StringToHash(triggerName));

    /// <summary>피격 반응. 실드가 피해를 전부 막아도 재생한다.</summary>
    public void PlayHit()
    {
        // 그로기 중 쌓인 Hit Trigger가 회복 직후 뒤늦게 재생되지 않게 한다.
        if (!HasGroggyBuff)
            SetTriggerSafe(AnimatorHashes.Hit);
    }

    public void PlayDead()
    {
        SetTurnIndicator(false);
        SetBoolSafe(AnimatorHashes.IsGroggy, false);
        SetBoolSafe(AnimatorHashes.Dead, true);
    }

    public void SetTurnIndicator(bool visible)
    {
        if (turnIndicator != null)
            turnIndicator.SetActive(visible && Unit != null && Unit.IsAlive);
    }

    private void OnDisable()
    {
        SetTurnIndicator(false);
        EndImpactWait();
        ResetEntry();
    }

    /// <summary>
    /// 체력·실드 바, 버프와 강화/ECHO 표시를 현재 값으로 맞춘다.
    /// 상태 변경 이벤트를 표시할 때 호출한다.
    /// </summary>
    public void RefreshBars()
    {
        if (Unit == null)
            return;
        bool enhanced = ResonanceRules.IsEnhanced(Unit);
        SetBoolSafe(AnimatorHashes.IsER, enhanced);
        SetBoolSafe(AnimatorHashes.IsGroggy, HasGroggyBuff);
        if (echoIcon != null)
            echoIcon.SetActive(Unit.IsAlive && Unit.IsEchoState);
        if (enhancedIcon != null)
            enhancedIcon.SetActive(Unit.IsAlive && enhanced);
        buffList?.Refresh(Unit);

        if (healthSlider != null)
        {
            healthSlider.maxValue = Unit.MaxHP;
            healthSlider.value = Unit.CurrentHP;
        }

        if (shieldSlider != null)
        {
            // 실드 바도 최대 체력을 기준으로 채운다. 체력 바와 눈금이 맞아야
            // "실드가 체력 위에 덧씌워진" 것으로 읽힌다.
            shieldSlider.maxValue = Unit.MaxHP;
            shieldSlider.value = Unit.CurrentShield;
            shieldSlider.gameObject.SetActive(Unit.HasShield);
        }
    }

    // ── 애니메이터 안전 호출 ──────────────────────────────────────
    // 몬스터마다 애니메이터 구성이 달라 특정 파라미터가 없을 수 있다.
    // (예: LostScout에는 등장 연출이 없어 IsEntry가 존재하지 않는다)

    private bool HasParameter(int hash)
    {
        if (unitAnimator == null || unitAnimator.runtimeAnimatorController == null)
            return false;

        if (_cachedAnimatorController != unitAnimator.runtimeAnimatorController || _cachedAnimatorParameters == null)
        {
            _cachedAnimatorController = unitAnimator.runtimeAnimatorController;
            _cachedAnimatorParameters = unitAnimator.parameters;
        }
        AnimatorControllerParameter[] parameters = _cachedAnimatorParameters;
        for (int i = 0; i < parameters.Length; i++)
            if (parameters[i].nameHash == hash)
                return true;

        return false;
    }

    private void SetTriggerSafe(int hash)
    {
        if (HasParameter(hash))
            unitAnimator.SetTrigger(hash);
    }

    private void SetBoolSafe(int hash, bool value)
    {
        if (HasParameter(hash))
            unitAnimator.SetBool(hash, value);
    }
    /// <summary>등장 애니메이션이 끝났음을 클립 이벤트로부터 통지받는다.</summary>
    public void OnEntryFinished()
    {
        ResetEntry();
    }
}
