using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>이벤트 결과와 실제 수치 변화를 표시하고 확인 동작을 전달한다.</summary>
public sealed class MapEventResultView : PopupView
{
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Button confirmButton;
    private Func<bool> _confirm;
    private void Awake() => confirmButton.onClick.AddListener(Confirm);
    public void Bind(string result, string effects, string buttonText, Func<bool> confirm)
    {
        resultText.text = result;
        effectText.text = effects;
        confirmText.text = buttonText;
        _confirm = confirm;
        confirmButton.interactable = confirm != null;
    }

    private void Confirm()
    {
        if (!IsShown || !confirmButton.interactable || _confirm == null)
            return;
        confirmButton.interactable = false;
        if (!_confirm() && IsShown)
            confirmButton.interactable = true;
    }

    protected override void OnDisable()
    {
        _confirm = null;
        base.OnDisable();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Confirm);
    }
}
