using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CombatEvent 목록을 화면 연출로 바꿔 재생한다.
///
/// 이 클래스는 전투 결과에 관여하지 않는다. 재생을 통째로 건너뛰어도
/// 체력·버프·승패는 이미 Core에서 확정되어 있다.
/// 기존 TurnManager는 코루틴 중간에서 데미지를 적용했기 때문에
/// 연출을 건너뛰면 결과가 달라질 수 있었다.
///
/// 배속과 스킵이 여기 한 곳에만 있으면 되는 이유이기도 하다.
/// </summary>
public class CombatEventPlayer : MonoBehaviour
{
    [Header("연출 타이밍 (초)")]
    [SerializeField] private float hitDelay = 0.4f;
    [SerializeField] private float deathDelay = 0.6f;

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
            case DamageDealt damage:
                yield return PlayDamage(damage);
                break;

            default:
                Debug.Log($"[재생 미구현] {combatEvent}");
                break;
        }
    }

    private IEnumerator PlayDamage(DamageDealt damage)
    {
        UnitView view = GetView(damage.Target);
        if (view == null)
        {
            Debug.LogWarning($"[뷰 없음] {damage.Target}");
            yield break;
        }

        view.PlayHit();
        SpawnDamageText(view, damage.Amount);
        view.RefreshHpBar();

        if (!damage.Target.IsAlive)
            view.PlayDead();

        yield return Wait(damage.Target.IsAlive ? hitDelay : deathDelay);
    }

    private void SpawnDamageText(UnitView view, float amount)
    {
        if (damageTextPrefab == null || damageTextParent == null) return;

        DamageTextView text = Instantiate(damageTextPrefab, damageTextParent);
        text.Show(view.HitPos, amount);
    }

    /// <summary>배속을 반영한 대기. SpeedScale이 0 이하이면 대기하지 않는다.</summary>
    private IEnumerator Wait(float seconds)
    {
        if (SpeedScale <= 0f) yield break;
        yield return new WaitForSeconds(seconds / SpeedScale);
    }
}
