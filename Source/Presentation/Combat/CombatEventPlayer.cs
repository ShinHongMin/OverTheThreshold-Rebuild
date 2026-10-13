using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CombatEvent 목록을 화면 연출로 바꿔 재생한다.
///
/// 이 클래스는 전투 결과에 관여하지 않는다. 재생 속도를 바꿔도
/// 체력·실드·승패는 Core가 계산하며 이 클래스는 실행 시점의 연출만 담당한다.
/// </summary>
public class CombatEventPlayer : MonoBehaviour
{
    [Header("연출 타이밍 (초)")]
    [SerializeField] private float castDelay = 0.3f;
    [SerializeField] private float hitDelay = 0.4f;
    [SerializeField] private float deathDelay = 0.6f;
    [SerializeField] private float supportDelay = 0.3f;
    [Tooltip("등장 종료 이벤트가 누락됐을 때의 최대 대기. 일시정지 시간은 포함하지 않는다.")]
    [SerializeField, Min(0.1f)] private float entryTimeout = 5f;
    [Tooltip("그로기 회복 전이가 누락됐을 때 진행을 복구할 최대 대기 시간")]
    [SerializeField, Min(0.1f)] private float stateTransitionTimeout = 5f;

    [Header("데미지 텍스트")]
    [SerializeField] private DamageTextView damageTextPrefab;
    [SerializeField] private Transform damageTextParent;   // Screen Space - Overlay 캔버스

    [Header("사운드")]
    [SerializeField] private SoundManager soundManager;

    [Header("카메라")]
    [SerializeField] private CameraManager cameraManager;

    [Header("스킬 VFX")]
    [SerializeField] private EffectManager effectManager;

    /// <summary>재생 배속. 1이면 원래 속도, 2면 두 배 빠르게.</summary>
    private float _speedScale = 1f;
    public float SpeedScale
    {
        get => _speedScale;
        set
        {
            _speedScale = Mathf.Max(0f, value);
            if (cameraManager != null)
                cameraManager.SpeedScale = _speedScale;
        }
    }

    private readonly Dictionary<Unit, UnitView> _views = new Dictionary<Unit, UnitView>();
    private PrefabPoolManager _pool;
    private readonly Dictionary<Unit, int> _numberRows = new Dictionary<Unit, int>();
    private readonly HashSet<Unit> _effectTargets = new HashSet<Unit>();
    private CombatHudView _combatHud;
    public void ConfigureHud(CombatHudView combatHud)
    {
        _combatHud = combatHud;
        _combatHud?.ConfigureBuffPool(_pool);
        foreach (var view in _views.Values)
            if (view != null)
                view.ConfigureBuffTooltip(combatHud != null ? combatHud.BuffTooltip : null);
    }

    private readonly Dictionary<string, SkillPresentation> _skillPresentations = new Dictionary<string, SkillPresentation>();
    private UnitView _actingView;
    private SkillPresentation _actingPresentation;
    private Vector3 _actionOrigin;
    private bool _playingActionEffects;
    public event System.Action<UseSkillCommand> ActionStarted;
    public event System.Action<UseSkillCommand> ActionFinished;

    public void ConfigureSkills(IReadOnlyList<SkillTableAsset> tables)
    {
        _skillPresentations.Clear();
        foreach (var table in tables)
            if (table != null)
                foreach (var skill in table.skills)
                    if (skill != null && skill.presentation != null)
                        _skillPresentations[skill.skillId] = skill.presentation;
        effectManager?.Prepare(_pool, tables);
    }

    private SkillPresentation FindPresentation(UseSkillCommand command)
        => command != null && _skillPresentations.TryGetValue(command.Skill.SkillId, out var presentation) ? presentation : null;

    /// <summary>접근·시전을 재생한다. 이 코루틴이 끝나는 시점에 App이 효과를 계산한다.</summary>
    public IEnumerator BeginAction(UseSkillCommand command)
    {
        cameraManager?.RestoreImmediately();
        _actingView = command == null ? null : GetView(command.Actor);
        ShowTurnIndicator(command?.Actor);
        ActionStarted?.Invoke(command);
        if (_actingView == null)
            yield break;
        _actionOrigin = _actingView.transform.position;
        var presentation = FindPresentation(command);
        _actingPresentation = presentation;
        bool useCameraSequence = presentation?.cameraSequence?.enabled == true;
        bool hasProjectile = presentation != null && effectManager != null && effectManager.IsProjectile(presentation.effectPrefab);
        bool attackCameraStarted = false;
        FocusCamera(command, presentation);
        yield return Wait(presentation != null ? presentation.preDelay : 0f);
        if (presentation != null && presentation.attackType == AttackType.Melee)
        {
            foreach (var target in command.Targets)
            {
                if (target == null || !target.IsAlive || target == command.Actor)
                    continue;
                var targetView = GetView(target);
                if (targetView == null)
                    continue;
                float direction = Mathf.Sign(_actionOrigin.x - targetView.transform.position.x);
                var destination = targetView.transform.position + Vector3.right * direction * 1.2f;
                // 근접 공격은 접근과 카메라 이동을 함께 시작해 타격 전에 구도를 잡는다.
                if (useCameraSequence)
                {
                    FocusCamera(command, presentation, attackPhase: true);
                    attackCameraStarted = true;
                }
                yield return MoveActor(destination, 0.25f);
                break;
            }
        }
        _actingView.BeginImpactWait();
        PlaySkillAnimation(_actingView, command.Skill.Type, presentation);
        PlaySkillSound(presentation, SkillSoundTiming.AnimationStart);
        // 발사체가 없으면 공격 동작 시작 때 전환해 실제 타격 전에 구도를 잡는다.
        // 근접은 이미 접근 때 전환했고, 발사체는 아래 발사 신호까지 시전 구도를 유지한다.
        if (useCameraSequence && !attackCameraStarted && !hasProjectile)
        {
            FocusCamera(command, presentation, attackPhase: true);
            attackCameraStarted = true;
        }
        if (presentation != null && presentation.impactTiming == SkillImpactTiming.AnimationEvent)
        {
            float elapsed = 0f;
            while (!_actingView.HasImpactSignal && elapsed < Mathf.Max(0.1f, presentation.impactEventTimeout))
            {
                // Animator.speed 배속 UI는 후속 범위다. 이벤트 대기는 실제 클립 시간에 맞춘다.
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (!_actingView.HasImpactSignal)
                Debug.LogWarning("타격/발사 이벤트 대기 시간 초과: " + command.Skill.SkillId, _actingView);
        }
        else
            yield return Wait(presentation != null ? presentation.animationTime : castDelay);
        _actingView.EndImpactWait();
        // 발사체는 발사 신호에서 전환한다. 먼저 시작한 공격 구도는 다시 움직이지 않는다.
        if (useCameraSequence && !attackCameraStarted)
            FocusCamera(command, presentation, attackPhase: true);
        if (hasProjectile)
        {
            for (int i = 0; i < command.Targets.Count; i++)
            {
                Unit target = command.Targets[i];
                if (target == null || !target.IsAlive)
                    continue;
                UnitView targetView = GetView(target);
                if (targetView != null)
                    effectManager.LaunchProjectile(presentation.effectPrefab, _actingView.ProjectileAnchor.position,
                        targetView.HitEffectAnchor, targetView.RenderOrder, presentation.projectileSpeed, SpeedScale);
            }
            yield return effectManager.WaitForProjectiles();
        }
    }

    public IEnumerator PlayActionEffects(IReadOnlyList<CombatEvent> events)
    {
        _numberRows.Clear();
        _effectTargets.Clear();
        PlaySkillSound(_actingPresentation, SkillSoundTiming.EffectApplied);
        if (cameraManager != null && _actingPresentation != null && _actingPresentation.useShake)
            for (int i = 0; i < events.Count; i++)
                if (events[i] is DamageDealt damage && damage.Amount > 0f)
                {
                    cameraManager.Shake(_actingPresentation.shakeIntensity, _actingPresentation.shakeDuration);
                    break;
                }
        // 현재 행동의 결과만 계산된 상태다. 이전/다음 행동의 값이 섞이지 않는다.
        RefreshUnitDisplays();
        _playingActionEffects = true;
        foreach (var combatEvent in events)
            if (!(combatEvent is SkillCast))
                yield return PlayOne(combatEvent);
        _playingActionEffects = false;
    }

    public IEnumerator EndAction(UseSkillCommand command)
    {
        var presentation = FindPresentation(command);
        float recovery = presentation == null ? hitDelay : Mathf.Max(presentation.postDelay,
            presentation.totalDuration - presentation.preDelay - presentation.animationTime);
        var cameraSequence = presentation?.cameraSequence;
        if (cameraManager != null && cameraSequence != null && cameraSequence.enabled)
        {
            float holdDuration = Mathf.Min(Mathf.Max(0f, cameraSequence.impactHoldDuration), Mathf.Max(0f, recovery));
            yield return Wait(holdDuration);
            recovery -= holdDuration;
            cameraManager.ReturnToDefault(cameraSequence.returnMoveDuration);
        }
        else
            cameraManager?.ReturnToDefault();
        yield return Wait(recovery);
        if (_actingView != null)
            yield return MoveActor(_actionOrigin, 0.25f);
        while (cameraManager != null && !cameraManager.IsAtRest)
            yield return null;
        while (effectManager != null && effectManager.HasActiveEffects)
            yield return null;
        _actingView = null;
        _actingPresentation = null;
        ShowTurnIndicator(null);
        ActionFinished?.Invoke(command);
    }

    private void FocusCamera(UseSkillCommand command, SkillPresentation presentation, bool attackPhase = false)
    {
        if (cameraManager == null || presentation == null)
            return;
        var sequence = presentation.cameraSequence;
        bool useSequence = sequence != null && sequence.enabled;
        SkillCameraFocus focus = useSequence ? (attackPhase ? sequence.attackFocus : sequence.castFocus) : presentation.cameraFocus;
        if (focus == SkillCameraFocus.None)
            return;
        cameraManager.SpeedScale = SpeedScale;
        bool frameUnitBodies = useSequence && sequence.keepUnitsInAttackFrame;
        Vector3 position = useSequence
            ? (frameUnitBodies ? _actingView.BodyBounds.center : _actingView.transform.position)
            : _actingView.HitEffectAnchor.position;
        Bounds frameBounds = frameUnitBodies ? _actingView.BodyBounds : default;
        float moveDuration = useSequence ? (attackPhase ? sequence.attackMoveDuration : sequence.castMoveDuration) : 0f;
        bool projectile = attackPhase && effectManager != null && effectManager.IsProjectile(presentation.effectPrefab);
        Vector3 targetCenter = Vector3.zero;
        int targetCount = 0;
        for (int i = 0; i < command.Targets.Count; i++)
        {
            Unit target = command.Targets[i];
            if (target == null || !target.IsAlive)
                continue;
            UnitView view = GetView(target);
            if (view == null)
                continue;
            targetCenter += useSequence
                ? (frameUnitBodies ? view.BodyBounds.center : view.transform.position)
                : view.HitEffectAnchor.position;
            targetCount++;
            if (frameUnitBodies)
                frameBounds.Encapsulate(view.BodyBounds);
            if (projectile)
                moveDuration = Mathf.Min(moveDuration, Vector3.Distance(_actingView.ProjectileAnchor.position, view.HitEffectAnchor.position)
                    / Mathf.Max(0.1f, presentation.projectileSpeed) * 0.75f);
        }
        if (focus == SkillCameraFocus.Targets && targetCount > 0)
            position = targetCenter / targetCount;
        else if (focus == SkillCameraFocus.CasterAndTargets && targetCount > 0)
            position = (position + targetCenter / targetCount) * 0.5f;
        if (useSequence)
        {
            if (attackPhase)
                position += (Vector3)sequence.attackFocusOffset;
            float zoomRatio = attackPhase ? sequence.attackZoomRatio : sequence.castZoomRatio;
            if (attackPhase && frameUnitBodies)
            {
                frameBounds.Expand(1f);
                cameraManager.MoveTo(position, zoomRatio, moveDuration, frameBounds);
            }
            else
                cameraManager.MoveTo(position, zoomRatio, moveDuration);
        }
        else
            cameraManager.FocusOn(position, presentation.cameraZoomAmount);
    }

    private void PlaySkillSound(SkillPresentation presentation, SkillSoundTiming timing)
    {
        if (presentation != null && presentation.soundTiming == timing && soundManager != null)
            soundManager.PlayEffect(presentation.skillSoundEffect, presentation.soundVolume);
    }

    private IEnumerator MoveActor(Vector3 destination, float duration)
    {
        Vector3 origin = _actingView.transform.position;
        if ((origin - destination).sqrMagnitude < 0.0001f)
            yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * Mathf.Max(0f, SpeedScale);
            _actingView.transform.position = Vector3.Lerp(origin, destination, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _actingView.transform.position = destination;
    }

    public void ConfigurePool(PrefabPoolManager pool)
    {
        _pool = pool;
        soundManager?.ConfigurePool(pool);
        if (damageTextPrefab != null)
            _pool.Prewarm(damageTextPrefab.gameObject, 12, 64);
    }

    public void Register(Unit unit, UnitView view)
    {
        view.Bind(unit);
        view.ConfigureBuffPool(_pool);
        view.ConfigureBuffTooltip(_combatHud != null ? _combatHud.BuffTooltip : null);
        _views[unit] = view;
    }

    public UnitView GetView(Unit unit)
        => _views.TryGetValue(unit, out UnitView view) ? view : null;

    /// <summary>효과 적용·턴 종료·전투 상태 초기화 후 유닛 표시와 HUD를 현재 값에 맞춘다.</summary>
    public void RefreshUnitDisplays()
    {
        foreach (var view in _views.Values)
            if (view != null)
                view.RefreshBars();
        if (_combatHud != null)
            _combatHud.Refresh();
    }

    /// <summary>등록된 유닛을 함께 등장시키고, 마지막 종료 신호까지 기다린다.</summary>
    public IEnumerator PlayEntry()
    {
        ShowTurnIndicator(null);
        foreach (var view in _views.Values)
            if (view != null)
                view.PlayEntry();

        float elapsed = 0f;
        while (isActiveAndEnabled)
        {
            bool waitingForEntry = false;
            foreach (var view in _views.Values)
            {
                if (view == null || !view.IsEntryPlaying)
                    continue;
                if (elapsed >= Mathf.Max(0.1f, entryTimeout))
                {
                    Debug.LogWarning($"[등장 연출] {view.name} 종료 신호 대기 초과. Animator와 AnimationEventRelay 연결을 확인하세요.", view);
                    view.ResetEntry();
                }
                else
                    waitingForEntry = true;
            }
            if (!waitingForEntry)
            {
                ResetEntry();
                yield break;
            }
            yield return null;
            // Animator와 같은 시간 기준. 아직 Animator.speed와 연동되지 않은 배속은 곱하지 않는다.
            elapsed += Time.deltaTime;
        }
    }

    private void ResetEntry()
    {
        foreach (var view in _views.Values)
            if (view != null)
                view.ResetEntry();
    }

    public void ShowTurnIndicator(Unit actor)
    {
        foreach (var pair in _views)
            pair.Value.SetTurnIndicator(pair.Key == actor);
    }

    public void ResetViews()
    {
        StopAllCoroutines();
        ResetEntry();
        _actingView?.EndImpactWait();
        effectManager?.ResetEffects();
        cameraManager?.RestoreImmediately();
        _playingActionEffects = false;
        _actingView = null;
        _actingPresentation = null;
        ShowTurnIndicator(null);
        _views.Clear();
        if (_pool != null)
            _pool.ReturnAll();
    }

    private void OnDisable()
    {
        ResetEntry();
        _actingView?.EndImpactWait();
        effectManager?.ResetEffects();
        cameraManager?.RestoreImmediately();
    }

    /// <summary>이벤트 목록을 순서대로 재생한다.</summary>
    public IEnumerator Play(IReadOnlyList<CombatEvent> events)
    {
        _numberRows.Clear();
        for (int i = 0; i < events.Count; i++)
        {
            yield return PlayOne(events[i]);
            _combatHud?.Refresh();
        }
    }

    private IEnumerator PlayOne(CombatEvent combatEvent)
    {
        switch (combatEvent)
        {
            case SkillCast cast:
                yield return PlayCast(cast);
                break;

            case DamageDealt damage:
                yield return PlayDamage(damage);
                break;

            case HealReceived heal:
                yield return PlaySupport(heal.Target, heal.Amount, CombatNumberKind.Healing);
                break;

            case ShieldGranted shield:
                yield return PlaySupport(shield.Target, shield.Amount, CombatNumberKind.ShieldGain);
                break;
            case ShieldChanged changed:
                GetView(changed.Target)?.RefreshBars();
                break;

            case BuffApplied buff:
                yield return PlaySupport(buff.Target, 0f);
                break;
            case BuffRemoved removed:
                GetView(removed.Target)?.RefreshBars();
                break;

            case UnitDied died:
                // 사망 연출은 DamageDealt 재생 중에 이미 시작된다.
                Debug.Log(died);
                break;

            //ER 변화 확인용
            case ResourceChanged res:
                if (res.Target != null)
                    GetView(res.Target)?.RefreshBars();
                break;
            case TurnEnded _:
                RefreshUnitDisplays();
                // 회복 동작을 마친 뒤 다음 입력/행동을 연다. 전투 수치나 턴 수는 바꾸지 않는다.
                foreach (var view in _views.Values)
                    if (view != null && view.IsGroggyRecoveryPlaying)
                        yield return WaitForGroggyRecovery(view);
                break;

            default:
                yield break;
        }
    }

    /// <summary>
    /// 시전 동작. 스킬 종류에 따라 다른 애니메이션을 재생한다.
    ///
    /// SkillCast 이벤트만 따로 재생하는 호환 경로다.
    /// 현재 전투 행동의 SkillPresentation·카메라·접근은 BeginAction에서 처리한다.
    /// </summary>
    private IEnumerator PlayCast(SkillCast cast)
    {
        UnitView view = GetView(cast.Caster);
        if (view == null)
            yield break;

        _skillPresentations.TryGetValue(cast.SkillId, out var presentation);
        PlaySkillAnimation(view, cast.Type, presentation);

        yield return Wait(castDelay);
    }

    private static void PlaySkillAnimation(UnitView view, SkillType type, SkillPresentation presentation)
    {
        if (presentation != null && presentation.skipSkillAnimation)
            return;
        if (presentation != null && !string.IsNullOrEmpty(presentation.animationTrigger))
        {
            view.PlayAnimationTrigger(presentation.animationTrigger);
            return;
        }
        switch (type)
        {
            case SkillType.Special:
                view.PlaySkill();
                break;
            case SkillType.Overload:
                view.PlayOverload();
                break;
            default:
                view.PlayBasicAttack();
                break;
        }
    }

    private IEnumerator WaitForGroggyRecovery(UnitView view)
    {
        float elapsed = 0f;
        while (view != null && view.IsGroggyRecoveryPlaying && elapsed < stateTransitionTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (view != null && view.IsGroggyRecoveryPlaying)
            Debug.LogWarning("그로기 회복 애니메이션 대기 시간 초과: " + view.name, view);
    }

    private IEnumerator PlayDamage(DamageDealt damage)
    {
        UnitView view = GetView(damage.Target);
        if (view == null)
            yield break;

        view.PlayHit();
        PlayTargetEffect(damage.Target, support: false);
        SpawnText(view, damage.Amount - damage.ShieldAbsorbed, CombatNumberKind.Damage);
        SpawnText(view, damage.ShieldAbsorbed, CombatNumberKind.ShieldLoss);
        view.RefreshBars();

        if (!damage.Target.IsAlive)
            view.PlayDead();

        yield return Wait(damage.Target.IsAlive ? hitDelay : deathDelay);
    }

    /// <summary>회복·실드·버프처럼 피격 반응이 없는 이벤트의 공통 재생.</summary>
    private IEnumerator PlaySupport(Unit target, float amount, CombatNumberKind kind = CombatNumberKind.Healing)
    {
        UnitView view = GetView(target);
        if (view == null)
            yield break;

        PlayTargetEffect(target, support: true);
        if (amount > 0f)
            SpawnText(view, amount, kind);
        view.RefreshBars();

        yield return Wait(supportDelay);
    }

    private void PlayTargetEffect(Unit target, bool support)
    {
        if (!_playingActionEffects || effectManager == null || _actingPresentation == null || !_effectTargets.Add(target))
            return;
        UnitView view = GetView(target);
        if (view == null)
            return;
        var prefab = _actingPresentation.hitEffectPrefab != null ? _actingPresentation.hitEffectPrefab : _actingPresentation.effectPrefab;
        bool useBuffAnchor = _actingPresentation.effectAnchor == SkillEffectAnchor.Buff
            || (_actingPresentation.effectAnchor == SkillEffectAnchor.EventDefault && support);
        var effectAnchor = useBuffAnchor ? view.BuffEffectAnchor : view.HitEffectAnchor;
        effectManager.PlayEffect(prefab, effectAnchor.position, view.RenderOrder, SpeedScale);
    }

    private void SpawnText(UnitView view, float amount, CombatNumberKind kind)
    {
        if (amount <= 0f || damageTextPrefab == null || damageTextParent == null)
            return;

        if (_pool == null)
            return;
        GameObject obj = _pool.Rent(damageTextPrefab.gameObject, damageTextParent);
        if (obj == null)
            return;
        DamageTextView text = obj.GetComponent<DamageTextView>();
        text.SetPool(_pool);
        _numberRows.TryGetValue(view.Unit, out int row);
        _numberRows[view.Unit] = row + 1;
        text.Show(view.HitEffectAnchor, amount, kind, row);
    }

    /// <summary>배속을 반영한 대기.</summary>
    private IEnumerator Wait(float seconds)
    {
        if (_playingActionEffects)
            yield break;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime * Mathf.Max(0f, SpeedScale);
            yield return null;
        }
    }
}
