using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 씬 조립. 데이터와 뷰를 짝지어 Unit을 만들고 TurnLoop을 돌린다.
///
/// 이 클래스에는 전투 규칙도 연출도 없다.
///   규칙 → Core (TurnLoop, UseSkillCommand, SkillEffect, IActionBrain)
///   연출 → CombatEventPlayer
///   조립 → 여기
///
/// 적의 행동은 UnitData에 지정된 Brain이 정한다.
/// 입력 뷰가 있으면 수동 입력을 사용한다. 초기 배치 검증에서는 전투를 시작하지 않는다.
/// </summary>
public class CombatSceneController : MonoBehaviour
{
    /// <summary>데이터와 화면 오브젝트를 한 쌍으로 묶는다.</summary>
    [System.Serializable]
    public class UnitSlot
    {
        [UnityEngine.Serialization.FormerlySerializedAs("data")]
        public UnitData unitData;
        [UnityEngine.Serialization.FormerlySerializedAs("view")]
        public UnitView sceneUnitView;
        [Tooltip("런타임 생성 위치. 기존 씬의 직접 view 연결도 지원한다.")]
        public Transform spawnPoint;
        [UnityEngine.Serialization.FormerlySerializedAs("prefab")]
        public UnitView unitViewPrefab;
        [System.NonSerialized] internal UnitView spawnedView;
    }

    [Header("연결")]
    [SerializeField] private CombatEventPlayer eventPlayer;
    [SerializeField] private CombatUiManager combatUi;
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private RewardTableAsset rewardTable;
    [Tooltip("씬을 직접 실행할 때의 전투 종류. 런에서는 현재 노드 종류를 사용한다.")]
    [SerializeField] private NodeType encounterType = NodeType.Battle;
    [UnityEngine.Serialization.FormerlySerializedAs("startAutomatically")]
    [SerializeField] private bool prepareOnSceneStart = true;
    [Tooltip("유닛 생성과 Core 연결까지만 진행하고 초기 상태를 유지한다.")]
    [UnityEngine.Serialization.FormerlySerializedAs("prepareOnly")]
    [SerializeField] private bool stopAfterPreparation;

    [Header("스킬 테이블")]
    [Tooltip("유닛별로 나뉜 테이블을 모두 등록한다. 조회는 하나로 합쳐서 이루어진다")]
    [SerializeField] private List<SkillTableAsset> skillTables = new List<SkillTableAsset>();

    [Header("파티")]
    [SerializeField] private List<UnitSlot> party = new List<UnitSlot>();

    [Header("적")]
    [Tooltip("같은 UnitData를 여러 칸에 넣으면 같은 종류의 몬스터가 여러 마리 등장한다")]
    [SerializeField] private List<UnitSlot> enemies = new List<UnitSlot>();

    [Tooltip("런 편성의 적을 순서대로 배치할 씬 슬롯. 직접 실행용 enemies와 구분한다.")]
    [SerializeField] private Transform[] runEnemySpawnPoints = System.Array.Empty<Transform>();

    [Header("진행")]
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float turnInterval = 0.6f;
    [SerializeField] private int maxTurns = 30;

    [Header("난수")]
    [Tooltip("0이면 매번 다른 결과, 그 외에는 같은 시드로 고정된다.")]
    [SerializeField] private int randomSeed = 0;

    private CombatContext _combatContext;
    private TurnLoop _turnLoop;
    private IRandom _combatRandom;
    private SkillTable _skillTable;

    private readonly List<Unit> _partyUnits = new List<Unit>();
    private readonly List<Unit> _enemyUnits = new List<Unit>();

    /// <summary>유닛별 기본공격 스킬 ID. ECHO 강제 행동에도 쓰인다.</summary>
    private readonly Dictionary<Unit, string> _basicAttackIds = new Dictionary<Unit, string>();
    private readonly Dictionary<Unit, SkillSpec[]> _inputSkills = new Dictionary<Unit, SkillSpec[]>();
    private readonly Dictionary<Unit, SkillSpec[]> _enhancedInputSkills = new Dictionary<Unit, SkillSpec[]>();
    private Unit _inputActor;
    private IReadOnlyList<CombatEvent> _reservationEvents;
    private bool _awaitingInput;
    private System.Action<SkillSpec, Unit> _submitInput;
    public event System.Action<BattleResult> BattleFinished;
    private RunServices _runServices;
    private CombatEncounterData _runEncounter;
    private NodeType _battleType;
    private List<UnitSlot> _runPartySlots;
    private List<UnitSlot> _runEnemySlots;
    private bool _startedFromRun;
    private bool _rewardContinued;
    private IReadOnlyList<string> _rewardPool;
    private IReadOnlyList<PassiveCardData> _rewardCards;
    public event System.Action RewardCompleted;
    public RunState Run => _runServices?.Run;
    public CombatContext Context => _combatContext;

    /// <summary>같은 전투를 여러 번 준비해도 유닛을 중복 생성하지 않는다.</summary>
    public bool PrepareEncounter() => _combatContext != null || Build();

    private IEnumerator Start()
    {
        if (prepareOnSceneStart && !_startedFromRun)
            yield return StartEncounter();
    }

    public void BeginRunEncounter(RunServices services, CombatEncounterData encounter = null)
    {
        if (services == null || services.Run.PendingNode < 0)
            throw new System.ArgumentException("진입한 런 노드가 필요합니다.");
        if (_combatContext != null || _startedFromRun)
            throw new System.InvalidOperationException("런 전투는 씬당 한 번만 시작할 수 있습니다.");
        if (encounter != null)
            ConfigureRunSlots(services, encounter);
        StopAllCoroutines();
        combatUi?.ResetDisplay();
        _runServices = services;
        _runEncounter = encounter;
        _startedFromRun = true;
        _rewardContinued = false;
        _rewardCards = null;
        _combatContext = null;
        _partyUnits.Clear();
        _enemyUnits.Clear();
        _basicAttackIds.Clear();
        _inputSkills.Clear();
        _enhancedInputSkills.Clear();
        eventPlayer.ResetViews();
        StartCoroutine(StartEncounter());
    }

    private void ConfigureRunSlots(RunServices services, CombatEncounterData encounter)
    {
        if (!encounter.IsConfigured || encounter.Enemies.Count > runEnemySpawnPoints.Length)
            throw new System.InvalidOperationException("런 적 편성과 배치 슬롯 연결을 확인하세요.");
        if (services.PartyData.Count != party.Count)
            throw new System.InvalidOperationException("런 파티 수와 전투 씬 슬롯 수가 다릅니다.");

        _runPartySlots = new List<UnitSlot>(party.Count);
        for (int index = 0; index < services.PartyData.Count; index++)
        {
            var unitData = services.PartyData[index];
            var sourceSlot = party.Find(slot => slot.unitData == unitData);
            if (sourceSlot == null || sourceSlot.unitViewPrefab == null || party[index].spawnPoint == null)
                throw new System.InvalidOperationException("런 파티의 프리팹/배치 슬롯 연결이 없습니다: " + unitData.name);
            _runPartySlots.Add(new UnitSlot { unitData = unitData, unitViewPrefab = sourceSlot.unitViewPrefab, spawnPoint = party[index].spawnPoint });
        }

        _runEnemySlots = new List<UnitSlot>(encounter.Enemies.Count);
        for (int index = 0; index < encounter.Enemies.Count; index++)
        {
            int spawnSlotIndex = encounter.GetSpawnSlotIndex(index);
            if (spawnSlotIndex >= runEnemySpawnPoints.Length || runEnemySpawnPoints[spawnSlotIndex] == null)
                throw new System.InvalidOperationException("런 적 편성이 지정한 배치 슬롯을 확인하세요.");
            var enemy = encounter.GetEnemy(index);
            _runEnemySlots.Add(new UnitSlot { unitData = enemy.unitData, unitViewPrefab = enemy.unitViewPrefab, spawnPoint = runEnemySpawnPoints[spawnSlotIndex] });
        }
    }

    private IEnumerator StartEncounter()
    {
        if (!PrepareEncounter())
            yield break;
        if (stopAfterPreparation)
        {
            Debug.Log($"[전투 준비 완료] 아군 {_partyUnits.Count} / 적 {_enemyUnits.Count}. 초기 상태 검증 중이며 행동은 시작하지 않습니다.", this);
            yield break;
        }

        // 진입 연출과 첫 행동은 로딩 화면이 열린 뒤 시작한다.
        while (SceneLoader.Instance != null && SceneLoader.Instance.IsLoading)
            yield return null;
        yield return new WaitForSeconds(startDelay);
        yield return eventPlayer.PlayEntry();
        yield return RunBattle();
    }

    private bool Build()
    {
        if (eventPlayer == null)
        {
            Debug.LogError("CombatEventPlayer 연결이 필요합니다.", this);
            return false;
        }
        var pool = GetComponent<PrefabPoolManager>();
        if (pool == null)
            pool = gameObject.AddComponent<PrefabPoolManager>();
        eventPlayer.ConfigurePool(pool);
        _submitInput = SubmitInput;
        combatUi?.EndInput();
        _skillTable = _runServices != null ? _runServices.Skills : SkillTableBuilder.Build(skillTables);

        if (_skillTable.Count == 0)
        {
            Debug.LogError("[조립 실패] 스킬 테이블이 비어 있습니다.");
            return false;
        }

        var partySlots = _runPartySlots ?? party;
        var enemySlots = _runEnemySlots ?? enemies;
        if (!ValidateUnitSlots(partySlots) || !ValidateUnitSlots(enemySlots))
            return false;
        if (_runServices == null)
        {
            // 씬 직접 실행도 실제 파티와 카드 기록을 가진다. 맵 노드 진입은 임의로 만들지 않는다.
            var partyData = new UnitData[party.Count];
            for (int index = 0; index < party.Count; index++)
                partyData[index] = party[index].unitData;
            if (rewardTable == null)
                throw new System.InvalidOperationException("전투의 카드 테이블을 연결하세요.");
            _runServices = new RunServices(partyData, _skillTable, randomSeed == 0 ? System.Environment.TickCount : randomSeed, rewardTable);
        }
        _combatRandom = _runServices.CombatRandom;
        _battleType = _startedFromRun
            ? (_runEncounter != null ? _runEncounter.BattleType : Run.Map[Run.PendingNode].Type)
            : encounterType;
        _rewardPool = rewardTable != null && _battleType != NodeType.Boss ? rewardTable.GetCardIds(_battleType) : null;
        if (_rewardPool != null)
            foreach (var id in _rewardPool)
                rewardTable.GetCard(id);
        _turnLoop = new TurnLoop();
        combatUi?.Prepare(skillTables, ReturnToTitle, pool);

        CreateTeamUnits(partySlots, _partyUnits, usesResources: true);
        CreateTeamUnits(enemySlots, _enemyUnits, usesResources: false);

        if (_partyUnits.Count == 0 || _enemyUnits.Count == 0)
        {
            Debug.LogError("[조립 실패] 파티와 적을 최소 1명씩 등록해야 합니다.");
            return false;
        }

        _combatContext = _startedFromRun ? Run.BeginCombat(_enemyUnits) : Run.CreateCombatContext(_enemyUnits);
        combatUi?.Bind(_combatContext);
        combatUi?.BindRun(Run, rewardTable);
        eventPlayer.ConfigureHud(combatUi != null ? combatUi.HudView : null);
        eventPlayer.ConfigureSkills(skillTables);
        _turnLoop.EchoBrain = new EchoBrain(_combatRandom, _skillTable, _basicAttackIds);

        return true;
    }

    private bool ValidateUnitSlots(List<UnitSlot> slots)
    {
        if (slots.Count == 0)
        {
            Debug.LogError("[조립 실패] 비어 있는 진영입니다.", this);
            return false;
        }
        foreach (var slot in slots)
        {
            if (slot == null || slot.unitData == null ||
                (slot.sceneUnitView == null && (slot.unitViewPrefab == null || slot.spawnPoint == null)))
            {
                Debug.LogError("[조립 실패] UnitData와 view 또는 prefab/spawnPoint 연결이 필요합니다.", this);
                return false;
            }
            if (!_skillTable.Contains(slot.unitData.basicAttackId))
            {
                Debug.LogError("[조립 실패] 기본공격 ID를 찾을 수 없습니다: " + slot.unitData.basicAttackId, this);
                return false;
            }
            foreach (var id in slot.unitData.skillIds)
                if (string.IsNullOrEmpty(id) || !_skillTable.Contains(id))
                {
                    Debug.LogError("[조립 실패] 스킬 ID를 찾을 수 없습니다: " + id, this);
                    return false;
                }
        }
        return true;
    }

    private UnitView GetOrCreateUnitView(UnitSlot slot)
    {
        if (slot.sceneUnitView != null)
            return slot.sceneUnitView;
        if (slot.spawnedView == null)
        {
            slot.spawnedView = Instantiate(slot.unitViewPrefab, slot.spawnPoint, false);
            slot.spawnedView.name = slot.unitData.unitName;
            slot.spawnedView.transform.localPosition = Vector3.zero;
        }
        return slot.spawnedView;
    }

    private void CreateTeamUnits(List<UnitSlot> slots, List<Unit> target, bool usesResources)
    {
        int partyIndex = 0;
        foreach (UnitSlot slot in slots)
        {
            if (slot.unitData == null)
            {
                Debug.LogWarning("[건너뜀] data 또는 view가 비어 있는 칸이 있습니다.");
                continue;
            }

            if (!_skillTable.Contains(slot.unitData.basicAttackId))
            {
                Debug.LogError($"[스킬 없음] {slot.unitData.unitName}의 기본공격 '{slot.unitData.basicAttackId}'을(를) 테이블에서 찾을 수 없습니다.", slot.unitData);
                continue;
            }

            // 같은 UnitData를 여러 칸에 넣어도 Unit은 칸마다 새로 만들어진다.
            if (usesResources && _runServices != null && (partyIndex >= _runServices.Run.Party.Count
                || _runServices.PartyData[partyIndex] != slot.unitData))
                throw new System.InvalidOperationException("런 파티 순서와 전투 뷰 슬롯 순서가 다릅니다.");
            Unit unit = usesResources && _runServices != null
                ? _runServices.Run.Party[partyIndex++] : UnitFactory.Create(slot.unitData, usesResources);

            target.Add(unit);
            combatUi?.RegisterPortrait(unit, slot.unitData.portraitSprite);
            if (!usesResources)
                combatUi?.RegisterMonster(unit, slot.unitData);
            _basicAttackIds[unit] = slot.unitData.basicAttackId;
            if (usesResources)
            {
                var skills = new SkillSpec[1 + slot.unitData.skillIds.Count];
                skills[0] = _skillTable.Get(slot.unitData.basicAttackId);
                for (int i = 0; i < slot.unitData.skillIds.Count; i++)
                    skills[i + 1] = _skillTable.Get(slot.unitData.skillIds[i]);
                _inputSkills[unit] = skills;
                if (!string.IsNullOrEmpty(slot.unitData.enhancedBasicAttackId) || !string.IsNullOrEmpty(slot.unitData.enhancedSpecialSkillId) || !string.IsNullOrEmpty(slot.unitData.enhancedOverloadSkillId))
                {
                    var enhancedSkills = (SkillSpec[])skills.Clone();
                    if (!string.IsNullOrEmpty(slot.unitData.enhancedBasicAttackId))
                        enhancedSkills[0] = GetEnhancedSkill(slot.unitData.enhancedBasicAttackId, SkillType.Basic);
                    if (!string.IsNullOrEmpty(slot.unitData.enhancedSpecialSkillId))
                        for (int i = 1; i < enhancedSkills.Length; i++)
                            if (skills[i].Type == SkillType.Special)
                                enhancedSkills[i] = GetEnhancedSkill(slot.unitData.enhancedSpecialSkillId, SkillType.Special);
                    if (!string.IsNullOrEmpty(slot.unitData.enhancedOverloadSkillId))
                        for (int i = 1; i < enhancedSkills.Length; i++)
                            if (skills[i].Type == SkillType.Overload)
                                enhancedSkills[i] = GetEnhancedSkill(slot.unitData.enhancedOverloadSkillId, SkillType.Overload);
                    _enhancedInputSkills[unit] = enhancedSkills;
                }
            }
            var unitView = GetOrCreateUnitView(slot);
            eventPlayer.Register(unit, unitView);
            if (combatUi != null && combatUi.HasInput)
            {
                var targetView = unitView.GetComponent<UnitTargetView>();
                if (targetView == null)
                    targetView = unitView.gameObject.AddComponent<UnitTargetView>();
                targetView.Bind(unit, combatUi.InputView, combatUi.HudView);
            }

            IActionBrain brain = MonsterBrainFactory.Create(slot.unitData, _combatRandom, _skillTable);
            if (brain != null)
                _turnLoop.RegisterBrain(unit, brain);
        }
    }

    private SkillSpec GetEnhancedSkill(string skillId, SkillType expectedType)
    {
        var skill = _skillTable.Get(skillId);
        if (skill.Type != expectedType)
            throw new System.InvalidOperationException($"강화 스킬 종류 불일치: {skillId}");
        return skill;
    }

    private SkillSpec[] GetInputSkills(Unit actor)
        => ResonanceRules.IsEnhanced(actor) && _enhancedInputSkills.TryGetValue(actor, out var skills)
            ? skills : _inputSkills[actor];

    private IEnumerator RunBattle()
    {
        for (int turn = 0; turn < maxTurns; turn++)
        {
            while (combatUi != null && combatUi.IsPaused)
                yield return null;
            combatUi?.SetRound(turn + 1);
            // 턴 시작 — 자원 회복
            yield return eventPlayer.Play(_turnLoop.BeginTurn(_combatContext));

            yield return ReservePartyActions();
            while (combatUi != null && combatUi.IsPaused)
                yield return null;
            _turnLoop.ReserveBrainActions(_combatContext, _enemyUnits);

            BattleResult battle = BattleResult.InProgress;
            foreach (var step in _turnLoop.ExecuteSteps(_combatContext))
            {
                yield return eventPlayer.Play(step.Events);
                if (step.Command != null)
                {
                    var skillCommand = step.Command as UseSkillCommand;
                    yield return eventPlayer.BeginAction(skillCommand);
                    while (combatUi != null && combatUi.IsPaused)
                        yield return null;
                    yield return eventPlayer.PlayActionEffects(_turnLoop.ExecuteAction(_combatContext, step.Command));
                    yield return eventPlayer.EndAction(skillCommand);
                    while (combatUi != null && combatUi.IsPaused)
                        yield return null;
                }
                battle = step.Battle;
            }
            var result = new TurnResult(System.Array.Empty<CombatEvent>(), battle);

            if (result.IsBattleOver)
            {
                Debug.Log(result.Battle == BattleResult.Victory ? "[승리]" : "[패배]");
                _runServices?.FinishBattle(result.Battle);
                // 승리 시 정리된 전투용 실드·버프를 결과창 뒤의 표시에도 반영한다.
                eventPlayer.RefreshUnitDisplays();
                _awaitingInput = false;
                eventPlayer.ShowTurnIndicator(null);
                combatUi?.CompleteBattle();
                bool hasReward = result.Battle == BattleResult.Victory && _rewardPool != null;
                if (result.Battle == BattleResult.Defeat || (result.Battle == BattleResult.Victory && _battleType == NodeType.Boss))
                    combatUi?.ShowBattleResult(result.Battle, "RESULT", ShowRunResult);
                else if (hasReward)
                {
                    Run.PrepareBattleReward(_rewardPool, _runServices.RewardRandom);
                    var cards = new PassiveCardData[Run.RewardChoices.Count];
                    for (int index = 0; index < cards.Length; index++)
                        cards[index] = rewardTable.GetCard(Run.RewardChoices[index]);
                    _rewardCards = cards;
                    combatUi?.ShowBattleResult(result.Battle, "REWARD", ShowBattleReward);
                }
                else
                    combatUi?.ShowBattleResult(result.Battle, "GO TITLE", ReturnToTitle);
                BattleFinished?.Invoke(result.Battle);
                yield break;
            }

            yield return new WaitForSeconds(turnInterval);
        }

        Debug.Log("[종료] 최대 턴에 도달했습니다.");
        _awaitingInput = false;
        eventPlayer.ShowTurnIndicator(null);
        combatUi?.CompleteBattle();
        combatUi?.ShowTurnLimit(ReturnToTitle);
    }

    private void ShowRunResult()
        => combatUi.ShowRunResult(Run, _runServices.PartyData, rewardTable, ReturnToTitle);

    private void ShowBattleReward()
    {
        combatUi.ShowBattleReward(_rewardCards, Run.ClaimedRewardId, Run.TryClaimBattleReward,
            ContinueAfterReward, _startedFromRun && RewardCompleted != null ? "ACQUIRED\nCONTINUE" : "ACQUIRED\nGO TITLE");
    }

    private void ContinueAfterReward()
    {
        if (Run.ClaimedRewardId == null || _rewardContinued)
            return;
        _rewardContinued = true;
        // 맵 소유자가 전달한 RunServices는 이 씬 밖에서 계속 보유한다.
        if (_startedFromRun && RewardCompleted != null)
            RewardCompleted.Invoke();
        else
            ReturnToTitle();
    }

    private void ReturnToTitle()
    {
        if (SceneLoader.Instance == null || !SceneLoader.Instance.CanLoad(titleSceneName))
        {
            Debug.LogWarning("타이틀 씬을 Build Settings에서 확인하세요: " + titleSceneName, this);
            return;
        }
        // 닫힘 연출 동안 현재 화면을 유지하고, 가려진 뒤 뷰를 반납한다.
        StopAllCoroutines();
        _awaitingInput = false;
        SceneLoader.Instance.TryLoad(titleSceneName, PrepareSceneExit);
    }

    /// <summary>씬을 떠나기 전에 입력·팝업·진행 중 연출을 정리한다.</summary>
    public void PrepareSceneExit()
    {
        StopAllCoroutines();
        _awaitingInput = false;
        if (combatUi != null)
            combatUi.ResetDisplay();
        if (eventPlayer != null)
            eventPlayer.ResetViews();
    }

    /// <summary>
    /// 입력 뷰가 있으면 수동 예약, 없으면 간단한 자동 기본공격을 사용한다.
    /// </summary>
    private IEnumerator ReservePartyActions()
    {
        foreach (Unit actor in _partyUnits)
        {
            if (!actor.IsAlive)
                continue;

            if (combatUi != null && combatUi.HasInput && !EchoRules.IsOutOfControl(actor))
            {
                _inputActor = actor;
                _awaitingInput = true;
                eventPlayer.ShowTurnIndicator(actor);
                combatUi.BeginInput(actor, GetInputSkills(actor), _submitInput);
                while (_awaitingInput)
                    yield return null;
                yield return eventPlayer.Play(_reservationEvents);
                _reservationEvents = null;
                continue;
            }

            Unit target = FindFirstAlive(_enemyUnits);
            if (target == null)
                yield break;

            SkillSpec skill = GetInputSkills(actor)[0];
            if (skill == null)
                continue;

            if (_turnLoop.TryReserve(new UseSkillCommand(actor, target, skill), _combatContext, out var changes))
                yield return eventPlayer.Play(changes);
        }
    }

    private static Unit FindFirstAlive(List<Unit> units)
    {
        foreach (Unit unit in units)
            if (unit.IsAlive)
                return unit;

        return null;
    }

    private void SubmitInput(SkillSpec skill, Unit chosen)
    {
        if (!_awaitingInput || skill == null)
            return;
        if (TargetingRule.NeedsManualTarget(skill.TargetType)
            && !TargetingRule.IsValidTarget(_combatContext, _inputActor, skill.TargetType, chosen))
            return;
        var targets = TargetingRule.ResolveTargets(_combatContext, _inputActor, skill.TargetType, chosen);
        if (!_turnLoop.TryReserve(new UseSkillCommand(_inputActor, targets, skill), _combatContext, out var changes))
            return;
        _reservationEvents = changes;
        _awaitingInput = false;
        eventPlayer.ShowTurnIndicator(null);
        combatUi?.EndInput();
    }
}
