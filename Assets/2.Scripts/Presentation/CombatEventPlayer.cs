using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CombatEvent 목록을 화면 연출로 바꿔 재생한다.
///
/// 이 클래스는 전투 결과에 관여하지 않는다. 재생 속도를 바꿔도
/// 체력·실드·승패는 이미 Core에서 확정되어 있다.
/// </summary>
public class CombatEventPlayer : MonoBehaviour
{
    [Header("연출 타이밍 (초)")]
    [SerializeField] private float castDelay = 0.3f;
    [SerializeField] private float hitDelay = 0.4f;
    [SerializeField] private float deathDelay = 0.6f;
    [SerializeField] private float supportDelay = 0.3f;

    [Header("데미지 텍스트")]
    [SerializeField] private DamageTextView damageTextPrefab;
    [SerializeField] private Transform damageTextParent;   // Screen Space - Overlay 캔버스

    /// <summary>재생 배속. 1이면 원래 속도, 2면 두 배 빠르게.</summary>
    public float SpeedScale { get; set; } = 1f;

    private readonly Dictionary<Unit, UnitView> _views = new Dictionary<Unit, UnitView>();

    public void Register(Unit unit, UnitView view)
    {
        view.Bind(unit);
        _views[unit] = view;
    }

    public UnitView GetView(Unit unit)
        => _views.TryGetValue(unit, out UnitView view) ? view : null;

    /// <summary>이벤트 목록을 순서대로 재생한다.</summary>
    public IEnumerator Play(IReadOnlyList<CombatEvent> events)
    {
        for (int i = 0; i < events.Count; i++)
            yield return PlayOne(events[i]);
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
                yield return PlaySupport(heal.Target, heal.Amount);
                break;

            case ShieldGranted shield:
                yield return PlaySupport(shield.Target, shield.Amount);
                break;

            case BuffApplied buff:
                yield return PlaySupport(buff.Target, 0f);
                break;

            case UnitDied died:
                // 사망 연출은 DamageDealt 재생 중에 이미 시작된다.
                Debug.Log(died);
                break;

            default:
                yield break;
        }
    }

    /// <summary>
    /// 시전 동작. 스킬 종류에 따라 다른 애니메이션을 재생한다.
    ///
    /// SkillPresentation(선딜레이·이펙트·카메라)은 W5에서 연결한다.
    /// 지금은 고정 시간을 쓴다.
    /// </summary>
    private IEnumerator PlayCast(SkillCast cast)
    {
        UnitView view = GetView(cast.Caster);
        if (view == null) yield break;

        switch (cast.Type)
        {
            case SkillType.Special:  view.PlaySkill();       break;
            case SkillType.Overload: view.PlayOverload();    break;
            default:                 view.PlayBasicAttack(); break;
        }

        yield return Wait(castDelay);
    }

    private IEnumerator PlayDamage(DamageDealt damage)
    {
        UnitView view = GetView(damage.Target);
        if (view == null) yield break;

        view.PlayHit();
        SpawnText(view, damage.Amount);
        view.RefreshBars();

        if (!damage.Target.IsAlive)
            view.PlayDead();

        yield return Wait(damage.Target.IsAlive ? hitDelay : deathDelay);
    }

    /// <summary>회복·실드·버프처럼 피격 반응이 없는 이벤트의 공통 재생.</summary>
    private IEnumerator PlaySupport(Unit target, float amount)
    {
        UnitView view = GetView(target);
        if (view == null) yield break;

        if (amount > 0f) SpawnText(view, amount);
        view.RefreshBars();

        yield return Wait(supportDelay);
    }

    private void SpawnText(UnitView view, float amount)
    {
        if (damageTextPrefab == null || damageTextParent == null) return;

        DamageTextView text = Instantiate(damageTextPrefab, damageTextParent);
        text.Show(view.HitPos, amount);
    }

    /// <summary>배속을 반영한 대기.</summary>
    private IEnumerator Wait(float seconds)
    {
        if (SpeedScale <= 0f) yield break;
        yield return new WaitForSeconds(seconds / SpeedScale);
    }
}
