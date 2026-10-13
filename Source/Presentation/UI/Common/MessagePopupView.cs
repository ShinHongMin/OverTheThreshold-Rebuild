using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>확인·취소 또는 확인 하나를 표시하는 공통 팝업. 저장 판단은 호출자가 담당한다.</summary>
public sealed class MessagePopupView : PopupView
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private TMP_Text cancelText;
    private Action _confirm;
    private Action _cancel;
    private bool _counted;
    private static int _openCount;
    private static int _closedFrame = -1;
    public static bool IsInputBlocked => _openCount > 0 || _closedFrame == Time.frameCount;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        _openCount = 0;
        _closedFrame = -1;
    }

    private void Awake()
    {
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(Cancel);
    }

    public void Ask(string message, Action confirm, Action cancel = null)
        => Open(message, "YES", "NO", confirm, cancel);
    public void Inform(string message, Action confirm = null)
        => Open(message, "확인", null, confirm, null);
    private void Open(string message, string yes, string no, Action confirm, Action cancel)
    {
        messageText.text = message;
        confirmText.text = yes;
        cancelText.text = no ?? "";
        cancelButton.gameObject.SetActive(no != null);
        _confirm = confirm;
        _cancel = cancel;
        confirmButton.interactable = true;
        cancelButton.interactable = true;
        Show();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(confirmButton.gameObject);
    }

    public override void Show(float duration = 0f)
    {
        if (!_counted)
        {
            _counted = true;
            _openCount++;
        }
        base.Show(duration);
    }

    protected override void Update()
    {
        base.Update();
        if (IsShown && CloseOnEscape && !LoadingView.IsBlockingInput && Input.GetKeyDown(KeyCode.Escape))
            Cancel();
    }

    private void Confirm()
    {
        if (!IsShown || !confirmButton.interactable)
            return;
        var action = _confirm;
        Hide();
        action?.Invoke();
    }

    private void Cancel()
    {
        if (!IsShown)
            return;
        var action = _cancel;
        Hide();
        action?.Invoke();
    }

    protected override void OnDisable()
    {
        if (_counted)
        {
            _counted = false;
            _openCount = Mathf.Max(0, _openCount - 1);
            _closedFrame = Time.frameCount;
        }
        _confirm = null;
        _cancel = null;
        base.OnDisable();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Confirm);
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(Cancel);
    }
}
