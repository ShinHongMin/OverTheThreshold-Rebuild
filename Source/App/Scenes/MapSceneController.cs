using System;
using UnityEngine;

/// <summary>현재 런과 맵 화면을 연결한다. 분기와 진행도는 RunState가 보유한다.</summary>
public sealed class MapSceneController : MonoBehaviour
{
    [SerializeField] private MapView mapView;
    [SerializeField] private PrefabPoolManager prefabPool;
    [SerializeField] private UiManager uiManager;
    [SerializeField] private MapEventView eventView;
    [SerializeField] private MapEventResultView eventResultView;
    [SerializeField] private MapRestView restView;
    [SerializeField] private MapPopupManager popupManager;
    [SerializeField] private RewardTableAsset rewardTable;

    public RunState Run { get; private set; }

    private SceneLoader _sceneLoader;

    private void Start()
    {
        ShowMap();
        _sceneLoader = SceneLoader.Instance;
        if (_sceneLoader != null) _sceneLoader.LoadingCompleted += RefreshAfterLoading;
    }

    private void RefreshAfterLoading() => mapView.Refresh();

    private void OnDestroy()
    {
        if (_sceneLoader != null) _sceneLoader.LoadingCompleted -= RefreshAfterLoading;
    }

    public void ShowMap()
    {
        var runManager = RunManager.Instance;
        if (runManager == null || mapView == null || prefabPool == null)
            throw new InvalidOperationException("RunManager와 맵 View/풀 연결이 필요합니다.");

        Run = runManager.GetOrCreateRun().Run;
        eventView.Configure(prefabPool);
        restView.Configure(prefabPool);
        uiManager.Register(eventView);
        uiManager.Register(eventResultView);
        uiManager.Register(restView);
        mapView.Bind(Run, prefabPool, SelectNode, runManager.CanEnterNode);
        mapView.Show();
        popupManager.Configure(Run, prefabPool, rewardTable, ReturnToTitle);
        ShowPendingEvent();
        ShowPendingRest();
    }

    private void SelectNode(int nodeIndex)
    {
        if (popupManager.HasOpenPopup) return;
        if (!RunManager.Instance.TryEnterNode(nodeIndex)) return;
        mapView.SetInputEnabled(false);
        if (RunManager.Instance.IsChangingScene) popupManager.SetAvailable(false);
        ShowPendingEvent();
        ShowPendingRest();
    }

    private void ShowPendingRest()
    {
        var services = RunManager.Instance.CurrentRunServices;
        var session = services.CurrentRest;
        if (session == null || session.NodeIndex != Run.PendingNode) return;
        mapView.SetInputEnabled(false);
        restView.Bind(services.PartyData, session.Feedback,
            session.CanSelect(RestRules.Action.Heal), session.CanSelect(RestRules.Action.Maintenance), session.CanSelect(RestRules.Action.CureEcho),
            SelectRestAction, LeaveRest);
        uiManager.Show<MapRestView>();
    }

    private void SelectRestAction(RestRules.Action action)
    {
        if (popupManager.HasOpenPopup) return;
        var session = RunManager.Instance.CurrentRunServices.CurrentRest;
        if (session != null && session.TrySelect(action)) ShowPendingRest();
    }

    private bool LeaveRest()
    {
        if (popupManager.HasOpenPopup) return false;
        if (!RunManager.Instance.CurrentRunServices.CompleteRest()) return false;
        uiManager.Hide<MapRestView>();
        mapView.SetInputEnabled(true);
        return true;
    }

    private void ShowPendingEvent()
    {
        var session = RunManager.Instance.CurrentRunServices.CurrentEvent;
        if (session == null || session.NodeIndex != Run.PendingNode) return;
        mapView.SetInputEnabled(false);
        if (session.IsResolved)
        {
            uiManager.Hide<MapEventView>();
            string buttonText = Run.IsFailed ? "GO TITLE" : session.BattleEncounter != null ? "FIGHT" : "CONFIRM";
            eventResultView.Bind(session.ResultText, session.EffectSummary, buttonText, ConfirmEvent);
            uiManager.Show<MapEventResultView>();
        }
        else
        {
            uiManager.Hide<MapEventResultView>();
            eventView.Bind(session.Data, SelectEventChoice);
            uiManager.Show<MapEventView>();
        }
    }

    private void SelectEventChoice(int index)
    {
        if (popupManager.HasOpenPopup) return;
        var session = RunManager.Instance.CurrentRunServices.CurrentEvent;
        if (session == null || !session.TrySelectChoice(index)) return;
        if (!Run.IsFailed && session.BattleEncounter != null && RunManager.Instance.TryStartEventBattle())
        {
            popupManager.SetAvailable(false);
            return;
        }
        ShowPendingEvent();
    }

    private bool ConfirmEvent()
    {
        if (popupManager.HasOpenPopup) return false;
        var manager = RunManager.Instance;
        if (Run.IsFailed || manager.CurrentRunServices.CurrentEvent?.BattleEncounter != null)
        {
            bool changingScene = Run.IsFailed ? manager.ReturnToTitle() : manager.TryStartEventBattle();
            if (changingScene) popupManager.SetAvailable(false);
            return changingScene;
        }
        if (!manager.CurrentRunServices.CompleteEvent()) return false;
        uiManager.Hide<MapEventResultView>();
        mapView.SetInputEnabled(true);
        return true;
    }

    private void ReturnToTitle()
    {
        if (!RunManager.Instance.ReturnToTitle()) return;
        mapView.SetInputEnabled(false);
        popupManager.SetAvailable(false);
    }
}
