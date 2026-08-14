using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 씬 조립. 데이터와 뷰를 짝지어 Unit을 만들고 전투를 진행시킨다.
///
/// 이 클래스에는 전투 규칙도 연출도 없다.
///   규칙 → Core (UseSkillCommand, DamageFormula)
///   연출 → CombatEventPlayer
///   조립 → 여기
///
/// 현재 턴 진행은 "파티가 순서대로 한 대씩 때린다"는 임시 규칙이다.
/// W3에서 TurnLoop이 들어오면 이 부분이 Core로 옮겨진다.
/// </summary>
public class CombatBootstrap : MonoBehaviour
{
    /// <summary>데이터와 화면 오브젝트를 한 쌍으로 묶는다.</summary>
    [System.Serializable]
    public class UnitSlot
    {
        public UnitData data;
        public UnitView view;
    }

    [Header("연결")]
    [SerializeField] private CombatEventPlayer eventPlayer;

    [Header("파티")]
    [SerializeField] private List<UnitSlot> party = new List<UnitSlot>();

    [Header("적")]
    [SerializeField] private List<UnitSlot> enemies = new List<UnitSlot>();

    [Header("진행")]
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float intervalBetweenActions = 0.5f;
    [SerializeField] private int maxTurns = 20;

    private CombatContext _context;
    private readonly List<Unit> _party = new List<Unit>();
    private readonly List<Unit> _enemies = new List<Unit>();
    private readonly Dictionary<Unit, float> _multipliers = new Dictionary<Unit, float>();

    private IEnumerator Start()
    {
        if (!BuildUnits()) yield break;

        yield return new WaitForSeconds(startDelay);
        yield return RunCombat();
    }

    private bool BuildUnits()
    {
        BuildSide(party, _party);
        BuildSide(enemies, _enemies);

        if (_party.Count == 0 || _enemies.Count == 0)
        {
            Debug.LogError("[조립 실패] 파티와 적을 최소 1명씩 등록해야 합니다.");
            return false;
        }

        _context = new CombatContext(_party, _enemies);
        return true;
    }

    private void BuildSide(List<UnitSlot> slots, List<Unit> target)
    {
        foreach (UnitSlot slot in slots)
        {
            if (slot.data == null || slot.view == null)
            {
                Debug.LogWarning("[건너뜀] data 또는 view가 비어 있는 칸이 있습니다.");
                continue;
            }

            Unit unit = UnitFactory.Create(slot.data);

            target.Add(unit);
            _multipliers[unit] = slot.data.basicAttackMultiplier;
            eventPlayer.Register(unit, slot.view);
        }
    }

    /// <summary>
    /// 임시 진행 규칙: 파티가 순서대로 첫 번째 생존 적을 때린다.
    /// 적의 반격과 턴 개념은 W3에서 TurnLoop이 담당한다.
    /// </summary>
    private IEnumerator RunCombat()
    {
        for (int turn = 1; turn <= maxTurns; turn++)
        {
            Debug.Log($"── 턴 {turn} ──");

            foreach (Unit actor in _party)
            {
                if (!actor.IsAlive) continue;

                Unit target = FindFirstAlive(_enemies);
                if (target == null)
                {
                    Debug.Log("[승리] 적을 모두 쓰러뜨렸습니다.");
                    yield break;
                }

                yield return Act(actor, target);
                yield return new WaitForSeconds(intervalBetweenActions);
            }
        }

        Debug.Log("[종료] 최대 턴에 도달했습니다.");
    }

    private IEnumerator Act(Unit actor, Unit target)
    {
        float multiplier = _multipliers.TryGetValue(actor, out float m) ? m : 1f;
        var command = new UseSkillCommand(actor, target, multiplier);

        // 예약 시점엔 살아 있었으나 순서가 밀려 죽은 경우 등을 걸러낸다.
        if (!command.CanExecute(_context))
        {
            Debug.Log($"[취소] {actor.Name} → 대상 소멸");
            yield break;
        }

        // 계산은 이 한 줄에서 끝난다. 아래는 전부 재생이다.
        CommandResult result = command.Execute(_context);

        eventPlayer.GetView(actor).PlayBasicAttack();
        yield return new WaitForSeconds(0.3f);

        yield return eventPlayer.Play(result.Events);
    }

    private static Unit FindFirstAlive(List<Unit> units)
    {
        foreach (Unit unit in units)
            if (unit.IsAlive) return unit;

        return null;
    }
}
