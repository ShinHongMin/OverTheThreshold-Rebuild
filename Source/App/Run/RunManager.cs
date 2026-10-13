using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>한 런의 파티·맵·카드를 보유하고 맵과 전투 씬 사이에 전달한다.</summary>
public sealed class RunManager : MonoBehaviour
{
    [Header("새 런 데이터")]
    [SerializeField] private UnitData[] startingParty = Array.Empty<UnitData>();
    [SerializeField] private SkillTableAsset[] skillTables = Array.Empty<SkillTableAsset>();
    [Tooltip("획득 카드의 효과 정의. 보상 UI에서 사용하는 카드 테이블과 같은 에셋을 연결한다.")]
    [SerializeField] private RewardTableAsset rewardTable;
    [Tooltip("0이면 새 런마다 다른 맵, 그 외에는 같은 시드로 생성한다.")]
    [SerializeField] private int randomSeed;
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("노드 이동")]
    [SerializeField] private string mapSceneName = "MapScene";
    [SerializeField] private string combatSceneName = "CombatScene";
    [Tooltip("일반 전투의 편성. 편성 추첨은 별도로 연결한다.")]
    [SerializeField] private CombatEncounterData normalEncounter;
    [Tooltip("정예 전투의 편성. Battle Type은 Elite로 지정한다.")]
    [SerializeField] private CombatEncounterData eliteEncounter;
    [Tooltip("정예 하드 전투의 편성. Battle Type은 EliteHard로 지정한다.")]
    [SerializeField] private CombatEncounterData eliteHardEncounter;
    [Tooltip("최종 보스 전투의 편성. Battle Type은 Boss로 지정한다.")]
    [SerializeField] private CombatEncounterData bossEncounter;
    [Tooltip("현재 처리 가능한 맵 이벤트 목록. 노드 진입 시 한 번 추첨한다.")]
    [SerializeField] private EventData[] mapEvents = Array.Empty<EventData>();

    private SaveManager _saveManager;
    private bool _failedToClearFinishedRun;
    public string LastSaveError
    {
        get; private set;
    }

    public event Action<string> SaveFailed;
    private CombatEncounterData _pendingEncounter;
    private CombatSceneController _combatController;

    public static RunManager Instance
    {
        get; private set;
    }

    public RunServices CurrentRunServices
    {
        get; private set;
    }

    public bool IsChangingScene => SceneLoader.Instance != null && SceneLoader.Instance.IsLoading;
    public IReadOnlyList<UnitData> AvailableCharacters => startingParty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _saveManager = new SaveManager(SaveManager.DefaultFilePath);
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>맵 재진입에서는 기존 런을 반환한다. 새 게임 시작과 구분한다.</summary>
    public RunServices GetOrCreateRun()
        => CurrentRunServices ?? StartNewRun(startingParty);

    public RunServices StartNewRun(IReadOnlyList<UnitData> orderedParty)
    {
        if (rewardTable == null)
            throw new InvalidOperationException("런의 카드 테이블을 연결하세요.");
        int seed = randomSeed == 0 ? Environment.TickCount : randomSeed;
        var services = new RunServices(orderedParty, skillTables, seed, rewardTable);
        if (!_saveManager.TryWrite(services.CreateSaveData(), out var error))
        {
            ReportSaveError(error);
            throw new InvalidOperationException(error);
        }
        AttachRun(services);
        return services;
    }

    /// <summary>로비에서 확정한 파티 순서로 새 런을 만들고 맵으로 출발한다.</summary>
    public bool TryStartNewRun(IReadOnlyList<UnitData> orderedParty)
    {
        if (IsChangingScene || orderedParty == null || orderedParty.Count != 3
            || SceneLoader.Instance == null || !SceneLoader.Instance.CanLoad(mapSceneName))
            return false;
        for (int index = 0; index < orderedParty.Count; index++)
        {
            var character = orderedParty[index];
            if (character == null || Array.IndexOf(startingParty, character) < 0)
                return false;
            for (int previous = 0; previous < index; previous++)
                if (orderedParty[previous] == character)
                    return false;
        }
        try
        {
            StartNewRun(orderedParty);
        }
        catch (Exception error) when (error is InvalidOperationException || error is ArgumentException)
        {
            if (LastSaveError == null)
                ReportSaveError("새 런의 저장 데이터를 만들 수 없습니다. 데이터 연결을 확인해 주세요.");
            return false;
        }
        return SceneLoader.Instance.TryLoad(mapSceneName);
    }

    public void EndRun()
    {
        DisconnectCombat();
        if (CurrentRunServices != null)
        {
            CurrentRunServices.Run.CheckpointReached -= SaveCheckpoint;
            CurrentRunServices.NodeCheckpointChanged -= SaveCheckpoint;
        }
        CurrentRunServices = null;
        _pendingEncounter = null;
    }

    private void AttachRun(RunServices services)
    {
        if (CurrentRunServices != null)
        {
            CurrentRunServices.Run.CheckpointReached -= SaveCheckpoint;
            CurrentRunServices.NodeCheckpointChanged -= SaveCheckpoint;
        }
        CurrentRunServices = services;
        CurrentRunServices.Run.CheckpointReached += SaveCheckpoint;
        CurrentRunServices.NodeCheckpointChanged += SaveCheckpoint;
        LastSaveError = null;
        _failedToClearFinishedRun = false;
    }

    private void SaveCheckpoint()
    {
        if (CurrentRunServices == null)
            return;
        try
        {
            string error;
            bool saved = CurrentRunServices.Run.IsFinished
                ? _saveManager.TryClear(out error)
                : _saveManager.TryWrite(CurrentRunServices.CreateSaveData(), out error);
            if (CurrentRunServices.Run.IsFinished)
                _failedToClearFinishedRun = !saved;
            if (!saved)
                ReportSaveError(error);
            else
                LastSaveError = null;
        }
        catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
        {
            ReportSaveError("현재 런의 데이터를 저장할 수 없습니다. 데이터 연결을 확인해 주세요.");
        }
    }

    private void ReportSaveError(string message)
    {
        LastSaveError = message;
        Debug.LogWarning(message, this);
        SaveFailed?.Invoke(message);
    }

    public SaveReadStatus CheckContinue(out RunServices savedRun, out string message)
    {
        savedRun = null;
        if (_failedToClearFinishedRun)
        {
            message = "종료된 런의 기록 정리에 실패했습니다. 해당 기록은 이어갈 수 없습니다.";
            return SaveReadStatus.Unavailable;
        }
        var status = _saveManager.Read(out var data, out message);
        if (status != SaveReadStatus.Ready)
            return status;
        try
        {
            savedRun = RunServices.Restore(data, startingParty, skillTables, rewardTable, mapEvents);
            return SaveReadStatus.Ready;
        }
        catch (System.Exception error) when (error is ArgumentException || error is InvalidOperationException)
        {
            message = "저장 기록의 캐릭터·카드·진행도를 현재 데이터로 복원할 수 없습니다.";
            return SaveReadStatus.Invalid;
        }
    }

    public bool TryContinueRun(RunServices savedRun)
    {
        if (savedRun == null || savedRun.Run.IsFinished || savedRun.Run.PendingNode >= 0 || IsChangingScene ||
            SceneLoader.Instance == null || !SceneLoader.Instance.CanLoad(mapSceneName))
            return false;
        if (!SceneLoader.Instance.TryLoad(mapSceneName))
            return false;
        AttachRun(savedRun);
        _pendingEncounter = null;
        return true;
    }

    public bool CanEnterNode(int nodeIndex)
    {
        if (IsChangingScene || CurrentRunServices == null || !CurrentRunServices.Run.CanEnter(nodeIndex))
            return false;
        NodeType nodeType = CurrentRunServices.Run.Map[nodeIndex].Type;
        switch (nodeType)
        {
            case NodeType.Battle:
            case NodeType.Elite:
            case NodeType.EliteHard:
            case NodeType.Boss:
                var encounter = GetNodeEncounter(nodeType);
                return encounter != null && encounter.IsConfigured && encounter.BattleType == nodeType;
            case NodeType.Rest:
                return true;
            case NodeType.Event:
                if (mapEvents.Length == 0)
                    return false;
                foreach (var data in mapEvents)
                    if (!RunEventSession.CanPrepare(data))
                        return false;
                return true;
            default:
                return false;
        }
    }

    /// <summary>처리할 수 있는 노드만 선점한다. 연속 클릭은 씬 이동을 중복 실행하지 않는다.</summary>
    public bool TryEnterNode(int nodeIndex)
    {
        if (!CanEnterNode(nodeIndex))
            return false;
        NodeType nodeType = CurrentRunServices.Run.Map[nodeIndex].Type;
        bool isCombat = nodeType != NodeType.Rest && nodeType != NodeType.Event;
        if (isCombat && !CanLoadCombat())
            return false;
        try
        {
            var checkpoint = CurrentRunServices.CreateNodeEntrySaveData(nodeIndex, mapEvents);
            // 저장 실패 시 맵에 머물며 선택도 확정하지 않는다.
            if (!_saveManager.TryWrite(checkpoint, out var error))
            {
                ReportSaveError(error);
                return false;
            }
            if (!CurrentRunServices.BeginSelectedNode(checkpoint, mapEvents))
                return false;
            LastSaveError = null;
        }
        catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
        {
            ReportSaveError("선택한 노드의 진행 기록을 저장하거나 복원할 수 없습니다. 데이터 연결을 확인해 주세요.");
            return false;
        }
        if (isCombat)
            LoadCombat(GetNodeEncounter(nodeType));
        return true;
    }

    private CombatEncounterData GetNodeEncounter(NodeType nodeType)
    {
        switch (nodeType)
        {
            case NodeType.Battle:
                return normalEncounter;
            case NodeType.Elite:
                return eliteEncounter;
            case NodeType.EliteHard:
                return eliteHardEncounter;
            case NodeType.Boss:
                return bossEncounter;
            default:
                return null;
        }
    }

    public bool TryStartEventBattle()
    {
        if (IsChangingScene || CurrentRunServices == null || !CanLoadCombat() || !CurrentRunServices.TryBeginEventBattle())
            return false;
        LoadCombat(CurrentRunServices.CurrentEvent.BattleEncounter);
        return true;
    }

    private bool CanLoadCombat()
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.CanLoad(combatSceneName)
            && Application.CanStreamedLevelBeLoaded(mapSceneName))
            return true;
        Debug.LogWarning("맵/전투 씬을 Build Settings에서 확인하세요.", this);
        return false;
    }

    private void LoadCombat(CombatEncounterData encounter)
    {
        _pendingEncounter = encounter;
        SceneLoader.Instance.TryLoad(combatSceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DisconnectCombat();
        if (scene.name == titleSceneName)
        {
            EndRun();
            return;
        }
        if (scene.name != combatSceneName || _pendingEncounter == null)
            return;

        var encounter = _pendingEncounter;
        _pendingEncounter = null;
        _combatController = FindFirstObjectByType<CombatSceneController>();
        if (_combatController == null)
            throw new InvalidOperationException("전투 씬의 CombatSceneController 연결이 필요합니다.");
        _combatController.RewardCompleted += ReturnToMap;
        // sceneLoaded는 Start보다 먼저 호출된다. 직접 실행용 전투가 시작되기 전에 런/편성을 전달한다.
        _combatController.BeginRunEncounter(CurrentRunServices, encounter);
    }

    private void ReturnToMap()
    {
        if (SceneLoader.Instance == null || !SceneLoader.Instance.CanLoad(mapSceneName)
            || _combatController == null || CurrentRunServices == null
            || _combatController.Run != CurrentRunServices.Run || CurrentRunServices.Run.PendingNode >= 0
            || CurrentRunServices.Run.ClaimedRewardId == null)
            return;
        Action prepareExit = _combatController.PrepareSceneExit;
        DisconnectCombat();
        CurrentRunServices.ClearCompletedEvent();
        // 보상 확정에서 이미 노드를 완료했다. 같은 Run을 보유한 채 화면만 교체한다.
        SceneLoader.Instance.TryLoad(mapSceneName, prepareExit);
    }

    private void DisconnectCombat()
    {
        if (_combatController != null)
            _combatController.RewardCompleted -= ReturnToMap;
        _combatController = null;
    }

    public bool ReturnToTitle()
    {
        return SceneLoader.Instance != null && SceneLoader.Instance.TryLoad(titleSceneName);
    }

    private void OnDestroy()
    {
        EndRun();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
            Instance = null;
    }
}
