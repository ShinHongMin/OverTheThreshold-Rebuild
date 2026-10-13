using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>전투 팝업 하나의 열기·닫기, ESC, 입력 차단과 일시정지 복구를 관리한다.</summary>
public sealed class CombatPopupManager : MonoBehaviour
{
    [SerializeField] private UiManager uiManager;
    [SerializeField] private OptionsView optionsView;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button monsterInfoButton;
    [SerializeField] private Button cardListButton;
    [SerializeField] private CombatInputView combatInput;
    [SerializeField] private CanvasGroup combatHudInteraction;
    [SerializeField] private BuffTooltipView buffTooltip;
    [SerializeField] private SoundManager soundManager;
    private PopupView _activePopup;
    private Action<PopupView> _popupClosed;
    private Action _returnToTitle;
    private bool _optionsAvailable;
    private bool _monsterInfoAvailable;
    private bool _cardListAvailable;
    private bool _hasOpenPopup;
    private bool _inputWasBlocked;
    private bool _hudWasInteractable;
    private bool _hudBlockedRaycasts;
    private float _previousTimeScale;
    private bool _previousAudioPause;
    private bool _ownsPause;
    public bool IsPaused => _ownsPause;
    public bool HasOpenPopup => _hasOpenPopup;

    private void Awake()
    {
        _popupClosed = OnPopupClosed;
        optionsButton.onClick.AddListener(ToggleOptions);
        if (monsterInfoButton != null)
            monsterInfoButton.onClick.AddListener(OpenMonsterInfo);
        if (cardListButton != null)
            cardListButton.onClick.AddListener(OpenCardList);
    }

    public void Configure(Action returnToTitle)
    {
        _returnToTitle = returnToTitle;
        optionsView.Configure(soundManager, returnToTitle != null ? ReturnToTitle : null);
    }

    public void SetOptionsAvailable(bool available)
    {
        _optionsAvailable = available;
        if (optionsButton != null)
            optionsButton.interactable = available;
    }

    public void SetMonsterInfoAvailable(bool available)
    {
        _monsterInfoAvailable = available;
        if (monsterInfoButton != null)
            monsterInfoButton.interactable = available;
    }

    private void OpenMonsterInfo()
    {
        if (_monsterInfoAvailable)
            Open<MonsterInfoView>();
    }

    public void SetCardListAvailable(bool available)
    {
        _cardListAvailable = available;
        if (cardListButton != null)
            cardListButton.interactable = available;
    }

    private void OpenCardList()
    {
        if (_cardListAvailable)
            Open<OwnedCardListView>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            HandleEscape();
    }

    public void HandleEscape()
    {
        if (LoadingView.IsBlockingInput || MessagePopupView.IsInputBlocked)
            return;
        if (_hasOpenPopup)
        {
            if (_activePopup != null && _activePopup.CloseOnEscape)
                CloseActivePopup();
        }
        else
            ToggleOptions();
    }

    public void ToggleOptions()
    {
        if (LoadingView.IsBlockingInput || MessagePopupView.IsInputBlocked)
            return;
        if (_activePopup == optionsView && _hasOpenPopup)
        {
            CloseActivePopup();
            return;
        }
        if (!_optionsAvailable || _hasOpenPopup)
            return;
        Open<OptionsView>();
    }

    public bool Open<T>() where T : PopupView
    {
        if (LoadingView.IsBlockingInput || !isActiveAndEnabled || _hasOpenPopup)
            return false;
        var popup = uiManager.Get<T>();
        if (popup == null)
            return false;
        _activePopup = popup;
        _hasOpenPopup = true;
        popup.Closed += _popupClosed;
        if (combatInput != null)
        {
            _inputWasBlocked = combatInput.IsInteractionBlocked;
            combatInput.SetInteractionBlocked(true);
        }
        if (combatHudInteraction != null)
        {
            _hudWasInteractable = combatHudInteraction.interactable;
            _hudBlockedRaycasts = combatHudInteraction.blocksRaycasts;
            combatHudInteraction.interactable = false;
            combatHudInteraction.blocksRaycasts = false;
        }
        if (buffTooltip != null)
            buffTooltip.Hide();
        if (popup.PauseCombat)
        {
            _previousTimeScale = Time.timeScale;
            _previousAudioPause = AudioListener.pause;
            _ownsPause = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
        uiManager.Show<T>();
        return true;
    }

    public void CloseActivePopup()
    {
        if (_activePopup != null)
            _activePopup.Hide();
        RestoreCombat();
    }

    private void OnPopupClosed(PopupView popup)
    {
        if (_activePopup == popup)
            RestoreCombat();
    }

    private void RestoreCombat()
    {
        if (!_hasOpenPopup)
            return;
        if (_activePopup != null)
            _activePopup.Closed -= _popupClosed;
        _activePopup = null;
        _hasOpenPopup = false;
        if (_ownsPause)
        {
            Time.timeScale = _previousTimeScale;
            AudioListener.pause = _previousAudioPause;
            _ownsPause = false;
        }
        if (combatInput != null)
            combatInput.SetInteractionBlocked(_inputWasBlocked);
        if (combatHudInteraction != null)
        {
            combatHudInteraction.interactable = _hudWasInteractable;
            combatHudInteraction.blocksRaycasts = _hudBlockedRaycasts;
        }
    }

    public void ResetDisplay()
    {
        SetOptionsAvailable(false);
        SetMonsterInfoAvailable(false);
        SetCardListAvailable(false);
        CloseActivePopup();
    }

    private void ReturnToTitle()
    {
        CloseActivePopup();
        _returnToTitle?.Invoke();
    }

    private void OnDisable() => ResetDisplay();
    private void OnDestroy()
    {
        if (optionsButton != null)
            optionsButton.onClick.RemoveListener(ToggleOptions);
        if (monsterInfoButton != null)
            monsterInfoButton.onClick.RemoveListener(OpenMonsterInfo);
        if (cardListButton != null)
            cardListButton.onClick.RemoveListener(OpenCardList);
    }
}
