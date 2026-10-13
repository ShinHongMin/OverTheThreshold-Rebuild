using UnityEngine;
using UnityEngine.UI;

/// <summary>타이틀 버튼과 확인 팝업을 연결한다. 저장 판단·런 복원은 RunManager에 요청한다.</summary>
public sealed class TitleSceneController : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private MessagePopupView messagePopup;
    [SerializeField] private string lobbySceneName = "MainLobbyScene";
    private bool _isLeaving;
    private RunServices _savedRun;
    private void Awake()
    {
        newGameButton.onClick.AddListener(StartNewGame);
        continueButton.onClick.AddListener(CheckContinue);
        exitButton.onClick.AddListener(ExitGame);
        messagePopup.Hide();
        messagePopup.Closed += OnPopupClosed;
    }

    private bool CanClick => !_isLeaving && !messagePopup.IsShown && !LoadingView.IsBlockingInput && !MessagePopupView.IsInputBlocked;
    private void CheckContinue()
    {
        if (!CanClick)
            return;
        SetButtons(false);
        var manager = RunManager.Instance;
        if (manager == null)
        {
            messagePopup.Inform("런 관리자 연결을 확인해 주세요.");
            return;
        }
        var status = manager.CheckContinue(out _savedRun, out var message);
        if (status == SaveReadStatus.Ready)
            messagePopup.Ask("이전에 진행했던 기록이 있습니다.\n계속하시겠습니까?", ContinueSavedRun, () => _savedRun = null);
        else
            messagePopup.Inform(status == SaveReadStatus.Missing
                ? "이전에 진행했던 기록이 없습니다.\nSTART를 눌러 새로 시작해 주세요."
                : message);
    }

    private void ContinueSavedRun()
    {
        var saved = _savedRun;
        _savedRun = null;
        if (RunManager.Instance != null && RunManager.Instance.TryContinueRun(saved))
            Leave();
        else
        {
            SetButtons(false);
            messagePopup.Inform("이어하기를 시작하지 못했습니다. 다시 시도해 주세요.");
        }
    }

    private void StartNewGame()
    {
        if (!CanClick)
            return;
        var manager = RunManager.Instance;
        if (manager == null)
            return;
        var status = manager.CheckContinue(out _, out _);
        if (status == SaveReadStatus.Missing)
        {
            EnterLobby();
            return;
        }
        SetButtons(false);
        messagePopup.Ask("새 게임에서 출발하면 이전 저장 기록이 교체됩니다.\n새로 시작하시겠습니까?", EnterLobby);
    }

    private void EnterLobby()
    {
        if (SceneLoader.Instance != null && SceneLoader.Instance.TryLoad(lobbySceneName))
            Leave();
        else
        {
            SetButtons(false);
            messagePopup.Inform("로비로 이동하지 못했습니다. 씬 연결을 확인해 주세요.");
        }
    }

    private void Leave()
    {
        _isLeaving = true;
        SetButtons(false);
    }

    private void SetButtons(bool enabled)
    {
        newGameButton.interactable = enabled;
        continueButton.interactable = enabled;
        exitButton.interactable = enabled;
    }

    private void OnPopupClosed(PopupView popup)
    {
        if (!_isLeaving)
            SetButtons(true);
    }

    private void ExitGame()
    {
        if (!CanClick)
            return;
        Leave();
        Application.Quit();
    }

    private void OnDestroy()
    {
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(StartNewGame);
        if (continueButton != null)
            continueButton.onClick.RemoveListener(CheckContinue);
        if (exitButton != null)
            exitButton.onClick.RemoveListener(ExitGame);
        if (messagePopup != null)
            messagePopup.Closed -= OnPopupClosed;
    }
}
