using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>전투 결과의 문구와 확인 버튼을 표시한다. 이후 화면 이동은 연결받은 동작에 맡긴다.</summary>
public sealed class BattleResultView : PopupView
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [Header("결과 문구")]
    [SerializeField] private string victoryTitle = "VICTORY";
    [SerializeField] private string defeatTitle = "DEFEAT";
    [TextArea] [SerializeField] private string victoryMessage = "전투에서 승리했습니다!";
    [TextArea] [SerializeField] private string defeatMessage = "더 이상 전투를 이어갈 수 없습니다.";
    [SerializeField] private Color victoryColor = new Color(.25f, 1f, .35f);
    [SerializeField] private Color defeatColor = new Color(1f, .3f, .3f);
    private Action _confirmAction;

    private void Awake() => confirmButton.onClick.AddListener(Confirm);

    public void SetResult(BattleResult result, string buttonLabel, Action confirmAction)
    {
        if (result == BattleResult.InProgress) throw new ArgumentException("종료된 전투 결과가 필요합니다.", nameof(result));
        bool isVictory = result == BattleResult.Victory;
        SetContent(isVictory ? victoryTitle : defeatTitle, isVictory ? victoryMessage : defeatMessage,
            isVictory ? victoryColor : defeatColor, buttonLabel, confirmAction);
    }

    public void SetTurnLimit(string buttonLabel, Action confirmAction)
    {
        // 검증용 최대 턴 제한은 Core의 승리/패배로 기록하지 않는다.
        SetContent("BATTLE ENDED", "최대 진행 턴에 도달하여 전투를 종료했습니다.", Color.white, buttonLabel, confirmAction);
    }

    private void SetContent(string title, string message, Color color, string buttonLabel, Action confirmAction)
    {
        titleText.text = title;
        titleText.color = color;
        messageText.text = message;
        confirmButtonText.text = buttonLabel;
        _confirmAction = confirmAction;
        confirmButton.interactable = confirmAction != null;
    }

    private void Confirm()
    {
        if (!IsShown || !confirmButton.interactable || _confirmAction == null) return;
        confirmButton.interactable = false;
        _confirmAction.Invoke();
        // 이동이 거절되어 창이 남아 있으면 다시 확인할 수 있다.
        if (this != null && IsShown && _confirmAction != null) confirmButton.interactable = true;
    }

    protected override void OnDisable()
    {
        _confirmAction = null;
        base.OnDisable();
    }
    private void OnDestroy() { if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm); }
}
