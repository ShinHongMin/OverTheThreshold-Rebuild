using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 씬 조립. 데이터와 뷰를 짝지어 Unit을 만들고 TurnLoop을 돌린다.
///
/// 이 클래스에는 전투 규칙도 연출도 없다.
///   규칙 → Core (TurnLoop, UseSkillCommand, SkillEffect)
///   연출 → CombatEventPlayer
///   조립 → 여기
///
/// 행동 선택은 임시다.
///   파티 → W5에서 플레이어 입력(SkillButtonView)으로 대체
///   적   → W4에서 몬스터별 IActionBrain으로 대체
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

    [Header("스킬 테이블")]
    [Tooltip("유닛별로 나뉜 테이블을 모두 등록한다. 조회는 하나로 합쳐서 이루어진다")]
    [SerializeField] private List<SkillTableAsset> skillTables = new List<SkillTableAsset>();

    [Header("파티")]
    [SerializeField] private List<UnitSlot> party = new List<UnitSlot>();

    [Header("적")]
    [SerializeField] private List<UnitSlot> enemies = new List<UnitSlot>();

    [Header("진행")]
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float turnInterval = 0.6f;
    [SerializeField] private int maxTurns = 30;

    [Header("난수")]
    [Tooltip("0이면 매번 다른 결과, 그 외에는 같은 시드로 고정된다.")]
    [SerializeField] private int randomSeed = 0;

    private CombatContext _context;
    private TurnLoop _turnLoop;
    private IRandom _random;
    private SkillTable _skillTable;

    private readonly List<Unit> _party = new List<Unit>();
    private readonly List<Unit> _enemies = new List<Unit>();

    /// <summary>유닛별 기본공격 스킬 ID. ECHO 강제 행동에도 쓰인다.</summary>
    private readonly Dictionary<Unit, string> _basicAttackIds = new Dictionary<Unit, string>();

    private IEnumerator Start()
    {
        if (!Build()) yield break;

        yield return new WaitForSeconds(startDelay);
        yield return RunBattle();
    }

    private bool Build()
    {
        _random = randomSeed == 0 ? new SystemRandom() : new SystemRandom(randomSeed);
        _skillTable = SkillTableBuilder.Build(skillTables);

        if (_skillTable.Count == 0)
        {
            Debug.LogError("[조립 실패] 스킬 테이블이 비어 있습니다.");
            return false;
        }

        BuildSide(party, _party);
        BuildSide(enemies, _enemies);

        if (_party.Count == 0 || _enemies.Count == 0)
        {
            Debug.LogError("[조립 실패] 파티와 적을 최소 1명씩 등록해야 합니다.");
            return false;
        }

        _context = new CombatContext(_party, _enemies);

        _turnLoop = new TurnLoop
        {
            EchoBrain = new EchoBrain(_random, _skillTable, _basicAttackIds)
        };

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

            if (!_skillTable.Contains(slot.data.basicAttackId))
            {
                Debug.LogError($"[스킬 없음] {slot.data.unitName}의 기본공격 '{slot.data.basicAttackId}'을(를) 테이블에서 찾을 수 없습니다.", slot.data);
                continue;
            }

            target.Add(unit);
            _basicAttackIds[unit] = slot.data.basicAttackId;

            eventPlayer.Register(unit, slot.view);
        }
    }

    private IEnumerator RunBattle()
    {
        for (int turn = 0; turn < maxTurns; turn++)
        {
            // 턴 시작 — 자원 회복
            yield return eventPlayer.Play(_turnLoop.BeginTurn(_context));

            ReserveActions();

            // 계산은 이 한 줄에서 전부 끝난다. 아래는 재생일 뿐이다.
            TurnResult result = _turnLoop.ExecuteTurn(_context);

            yield return eventPlayer.Play(result.Events);

            if (result.IsBattleOver)
            {
                Debug.Log(result.Battle == BattleResult.Victory ? "[승리]" : "[패배]");
                yield break;
            }

            yield return new WaitForSeconds(turnInterval);
        }

        Debug.Log("[종료] 최대 턴에 도달했습니다.");
    }

    /// <summary>
    /// 행동 예약. 지금은 양쪽 모두 기본공격만 한다.
    ///   파티 → 첫 번째 생존 적
    ///   적   → 무작위 생존 파티원
    /// </summary>
    private void ReserveActions()
    {
        foreach (Unit actor in _party)
            ReserveBasicAttack(actor, FindFirstAlive(_enemies));

        foreach (Unit actor in _enemies)
            ReserveBasicAttack(actor, PickRandomAlive(_party));
    }

    private void ReserveBasicAttack(Unit actor, Unit target)
    {
        if (actor == null || !actor.IsAlive || target == null) return;

        if (!_basicAttackIds.TryGetValue(actor, out string skillId)) return;

        SkillSpec skill = _skillTable.Find(skillId);
        if (skill == null) return;

        // 자원이 부족하면 예약 자체가 성립하지 않는다. (기존 CombatManager 규칙)
        if (!skill.CanAfford(actor, _context.Resources)) return;

        _turnLoop.Reserve(new UseSkillCommand(actor, target, skill));
    }

    private static Unit FindFirstAlive(List<Unit> units)
    {
        foreach (Unit unit in units)
            if (unit.IsAlive) return unit;

        return null;
    }

    private Unit PickRandomAlive(List<Unit> units)
    {
        var alive = new List<Unit>();

        foreach (Unit unit in units)
            if (unit.IsAlive) alive.Add(unit);

        if (alive.Count == 0) return null;

        return alive[_random.Range(0, alive.Count)];
    }
}
