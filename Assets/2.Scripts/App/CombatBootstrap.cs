using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 씬 조립. Core 객체를 만들고 화면의 UnitView와 짝지은 뒤 실행시킨다.
///
/// 이 클래스에는 전투 규칙이 없다. 계산은 전부 Core가 하고,
/// 여기서는 그 결과인 CombatEvent를 순서대로 재생할 뿐이다.
///
/// W1 Day5에서 재생 부분을 CombatEventPlayer로 분리한다.
/// 그때 Core는 한 줄도 바뀌지 않는다.
/// </summary>
public class CombatBootstrap : MonoBehaviour
{
    [Header("씬에 배치된 뷰")]
    [SerializeField] private UnitView aegisView;
    [SerializeField] private UnitView scoutView;

    [Header("데미지 텍스트")]
    [SerializeField] private DamageTextView damageTextPrefab;
    [SerializeField] private Transform damageTextParent;   // FloatingText가 붙을 Canvas

    [Header("Aegis 스탯")]
    [SerializeField] private float aegisMaxHP = 150f;
    [SerializeField] private float aegisATK = 10f;
    [SerializeField] private float aegisDEF = 20f;

    [Header("LostScout 스탯")]
    [SerializeField] private float scoutMaxHP = 40f;
    [SerializeField] private float scoutATK = 10f;
    [SerializeField] private float scoutDEF = 5f;

    [Header("기본공격")]
    [SerializeField] private float basicAttackMultiplier = 1f;

    [Header("연출 타이밍")]
    [SerializeField] private float preAttackDelay = 0.3f;
    [SerializeField] private float hitDelay = 0.4f;

    private readonly Dictionary<Unit, UnitView> _views = new Dictionary<Unit, UnitView>();

    private CombatContext _context;
    private Unit _aegis;
    private Unit _scout;

    private IEnumerator Start()
    {
        BuildUnits();

        aegisView.PlayEntry();
        scoutView.PlayEntry();
        yield return new WaitForSeconds(1f);

        yield return ExecuteOnce();

        Debug.Log($"[종료] {_scout}");
    }

    private void BuildUnits()
    {
        _aegis = new Unit("Aegis", new UnitStats(aegisMaxHP, aegisATK, aegisDEF));
        _scout = new Unit("LostScout", new UnitStats(scoutMaxHP, scoutATK, scoutDEF));

        _context = new CombatContext(new[] { _aegis }, new[] { _scout });

        Bind(_aegis, aegisView);
        Bind(_scout, scoutView);
    }

    private void Bind(Unit unit, UnitView view)
    {
        view.Bind(unit);
        _views[unit] = view;
    }

    /// <summary>Aegis가 LostScout를 기본공격으로 1회 타격한다.</summary>
    private IEnumerator ExecuteOnce()
    {
        var command = new UseSkillCommand(_aegis, _scout, basicAttackMultiplier);

        if (!command.CanExecute(_context))
        {
            Debug.LogWarning("[실행 불가] CanExecute가 false를 반환했습니다.");
            yield break;
        }

        // 계산은 여기서 이미 끝난다. 아래는 전부 재생일 뿐이다.
        CommandResult result = command.Execute(_context);

        _views[_aegis].PlayBasicAttack();
        yield return new WaitForSeconds(preAttackDelay);

        foreach (CombatEvent combatEvent in result.Events)
            yield return PlayEvent(combatEvent);
    }

    private IEnumerator PlayEvent(CombatEvent combatEvent)
    {
        switch (combatEvent)
        {
            case DamageDealt damage:
                yield return PlayDamage(damage);
                break;

            default:
                Debug.Log(combatEvent);
                break;
        }
    }

    private IEnumerator PlayDamage(DamageDealt damage)
    {
        UnitView view = _views[damage.Target];

        view.PlayHit();
        SpawnDamageText(view, damage.Amount);
        view.RefreshHpBar();

        Debug.Log(damage);

        if (!damage.Target.IsAlive)
            view.PlayDead();

        yield return new WaitForSeconds(hitDelay);
    }

    /// <summary>
    /// FloatingText 프리팹은 Canvas 자식으로 생성되어야 한다.
    /// 월드 좌표는 DamageTextView가 WorldToScreenPoint로 변환한다.
    /// </summary>
    private void SpawnDamageText(UnitView view, float amount)
    {
        if (damageTextPrefab == null || damageTextParent == null) return;

        DamageTextView text = Instantiate(damageTextPrefab, damageTextParent);
        text.Show(view.HitPos, amount);
    }
}
