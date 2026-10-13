using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>런 종료 시 승패·파티·완료 노드·획득 카드를 표시한다. 런 완료와 카드 지급은 하지 않는다.</summary>
public sealed class RunResultView : PopupView
{
    [Serializable]
    public sealed class NodeCountLabel
    {
        public NodeType nodeType;
        public TMP_Text countText;
    }

    [SerializeField] private UnitStatusView[] partySlots;
    [SerializeField] private NodeCountLabel[] nodeCounts;
    [SerializeField] private CardListView cardList;
    [SerializeField] private Button titleButton;
    [SerializeField] private TMP_Text resultText;
    private RunState _run;
    private IReadOnlyList<UnitData> _partyData;
    private Action _returnToTitle;

    private void Awake() => titleButton.onClick.AddListener(ReturnToTitle);

    public void Configure(PrefabPoolManager pool)
    {
        ResetDisplay();
        cardList.Configure(pool);
    }

    public void Bind(RunState run, IReadOnlyList<UnitData> partyData, RewardTableAsset cardTable, Action returnToTitle)
    {
        if (run == null || partyData == null || partyData.Count != run.Party.Count || partySlots.Length < run.Party.Count)
            throw new ArgumentException("결산에 표시할 파티 데이터와 슬롯 연결이 필요합니다.");
        _run = run;
        _partyData = partyData;
        _returnToTitle = returnToTitle;
        cardList.Bind(run.Cards, cardTable);
    }

    public override void Show(float duration = 0f)
    {
        if (_run == null) throw new InvalidOperationException("결산 화면에 런 데이터를 먼저 연결하세요.");
        base.Show(duration);
        resultText.text = _run.IsFailed ? "DEFEAT" : "CLEAR";
        int visibleSlot = 0;
        for (int index = 0; index < _run.Party.Count; index++)
        {
            var unit = _run.Party[index];
            // 패배한 파티도 마지막 HP·ER을 확인할 수 있도록 표시한다.
            if (_run.IsFailed || unit.IsAlive) partySlots[visibleSlot++].Show(unit, _partyData[index].portraitSprite);
        }
        while (visibleSlot < partySlots.Length) partySlots[visibleSlot++].Hide();

        foreach (var label in nodeCounts)
        {
            int count = 0;
            for (int index = 0; index < _run.Map.Count; index++)
                if (_run.IsCleared(index) && _run.Map[index].Type == label.nodeType) count++;
            label.countText.text = count.ToString("D3");
        }
        cardList.ShowCards();
        titleButton.interactable = _returnToTitle != null;
    }

    public override void Hide(float duration = 0f)
    {
        if (duration <= 0f && cardList != null) cardList.ClearDisplay();
        base.Hide(duration);
    }

    public void ResetDisplay()
    {
        Hide();
        if (cardList != null) cardList.ResetDisplay();
        _run = null;
        _partyData = null;
        _returnToTitle = null;
    }

    private void ReturnToTitle()
    {
        if (!IsShown || !titleButton.interactable || _returnToTitle == null) return;
        titleButton.interactable = false;
        _returnToTitle.Invoke();
        if (this != null && IsShown && _returnToTitle != null) titleButton.interactable = true;
    }

    protected override void OnDisable()
    {
        _returnToTitle = null;
        base.OnDisable();
    }

    private void OnDestroy() { if (titleButton != null) titleButton.onClick.RemoveListener(ReturnToTitle); }
}
