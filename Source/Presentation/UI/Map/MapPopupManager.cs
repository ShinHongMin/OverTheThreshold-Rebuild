using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>맵의 카드·옵션 창과 ESC 입력을 연결한다. 이벤트·휴식 선택은 창 뒤에서 그대로 유지한다.</summary>
public sealed class MapPopupManager : MonoBehaviour
{
    [SerializeField] private UiManager uiManager;
    [SerializeField] private MapView mapView;
    [SerializeField] private OptionsView optionsView;
    [SerializeField] private OwnedCardListView ownedCardListView;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button cardListButton;
    [SerializeField] private SoundManager soundManager;
    [SerializeField] private CanvasGroup[] nodePopupInteraction = Array.Empty<CanvasGroup>();

    private RunState _run;
    private Action _returnToTitle;
    private Action<PopupView> _popupClosed;
    private PopupView _activePopup;
    private bool[] _previousInteractable;
    private bool[] _previousBlocksRaycasts;
    private bool _available;
    public bool HasOpenPopup { get; private set; }

    private void Awake()
    {
        _popupClosed = OnPopupClosed;
        _previousInteractable = new bool[nodePopupInteraction.Length];
        _previousBlocksRaycasts = new bool[nodePopupInteraction.Length];
        optionsButton.onClick.AddListener(OpenOptions);
        cardListButton.onClick.AddListener(OpenCardList);
    }

    public void Configure(RunState run, PrefabPoolManager pool, RewardTableAsset cardTable, Action returnToTitle)
    {
        CloseActivePopup();
        _run = run;
        _returnToTitle = returnToTitle;
        uiManager.Register(optionsView);
        uiManager.Register(ownedCardListView);
        soundManager.ConfigurePool(pool);
        optionsView.Hide();
        optionsView.Configure(soundManager, ReturnToTitle);
        ownedCardListView.Configure(pool);
        ownedCardListView.Bind(run.Cards, cardTable);
        SetAvailable(true);
    }

    public void SetAvailable(bool available)
    {
        _available = available;
        if (!available) CloseActivePopup();
        RefreshButtons();
    }

    private void Update() { if (Input.GetKeyDown(KeyCode.Escape)) HandleEscape(); }

    public void HandleEscape()
    {
        if (LoadingView.IsBlockingInput || MessagePopupView.IsInputBlocked) return;
        if (HasOpenPopup)
        {
            if (_activePopup != null && _activePopup.CloseOnEscape) CloseActivePopup();
        }
        else OpenOptions();
    }

    private void OpenOptions() => Open<OptionsView>();
    private void OpenCardList() => Open<OwnedCardListView>();

    private void Open<T>() where T : PopupView
    {
        if (LoadingView.IsBlockingInput || !isActiveAndEnabled || !_available || HasOpenPopup) return;
        var popup = uiManager.Get<T>();
        if (popup == null) return;
        _activePopup = popup;
        HasOpenPopup = true;
        popup.Closed += _popupClosed;
        for (int index = 0; index < nodePopupInteraction.Length; index++)
        {
            var group = nodePopupInteraction[index];
            _previousInteractable[index] = group.interactable;
            _previousBlocksRaycasts[index] = group.blocksRaycasts;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        mapView.SetInputEnabled(false);
        RefreshButtons();
        // 이미 열린 이벤트·휴식 창을 닫거나 다시 바인딩하지 않고 그 위에 표시한다.
        popup.transform.SetAsLastSibling();
        uiManager.Show<T>();
    }

    public void CloseActivePopup()
    {
        if (_activePopup != null) _activePopup.Hide();
        RestoreMapInput();
    }

    private void OnPopupClosed(PopupView popup)
    {
        if (_activePopup == popup) RestoreMapInput();
    }

    private void RestoreMapInput()
    {
        if (!HasOpenPopup) return;
        if (_activePopup != null) _activePopup.Closed -= _popupClosed;
        _activePopup = null;
        HasOpenPopup = false;
        for (int index = 0; index < nodePopupInteraction.Length; index++)
        {
            var group = nodePopupInteraction[index];
            if (group == null) continue;
            group.interactable = _previousInteractable[index];
            group.blocksRaycasts = _previousBlocksRaycasts[index];
        }
        if (mapView != null) mapView.SetInputEnabled(_available && _run != null && !_run.IsFinished && _run.PendingNode < 0);
        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (optionsButton != null) optionsButton.interactable = _available && !HasOpenPopup;
        if (cardListButton != null) cardListButton.interactable = _available && !HasOpenPopup;
    }

    private void ReturnToTitle()
    {
        CloseActivePopup();
        _returnToTitle?.Invoke();
    }

    private void OnDisable()
    {
        _available = false;
        RestoreMapInput();
        RefreshButtons();
    }

    private void OnDestroy()
    {
        if (optionsButton != null) optionsButton.onClick.RemoveListener(OpenOptions);
        if (cardListButton != null) cardListButton.onClick.RemoveListener(OpenCardList);
    }
}
