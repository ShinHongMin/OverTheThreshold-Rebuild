using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>보유 카드 팝업의 표시와 닫기를 담당한다. 목록은 공통 CardListView가 그린다.</summary>
public sealed class OwnedCardListView : PopupView
{
    [SerializeField] private CardListView cardList;
    [SerializeField] private Button closeButton;

    private void Awake() => closeButton.onClick.AddListener(Close);

    public void Configure(PrefabPoolManager pool)
    {
        Hide();
        cardList.Configure(pool);
    }

    public void Bind(IReadOnlyList<string> cardIds, RewardTableAsset cardTable) => cardList.Bind(cardIds, cardTable);

    public override void Show(float duration = 0f)
    {
        base.Show(duration);
        cardList.ShowCards();
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
    }

    private void Close() => Hide();
    private void OnDestroy() { if (closeButton != null) closeButton.onClick.RemoveListener(Close); }
}
