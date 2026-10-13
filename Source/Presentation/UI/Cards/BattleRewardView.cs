using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>보상 선택과 확정 화면. 카드는 풀에서 빌리고, 획득은 전달받은 Run 동작에 요청한다.</summary>
public sealed class BattleRewardView : PopupView
{
    [SerializeField] private RewardCardView cardPrefab;
    [SerializeField] private RectTransform cardContent;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonText;
    private readonly List<RewardCardView> _visibleCards = new List<RewardCardView>(2);
    private PrefabPoolManager _pool;
    private IReadOnlyList<PassiveCardData> _choices;
    private Func<string, bool> _claimReward;
    private Action _continueAction;
    private Action<RewardCardView> _selectCard;
    private string _selectedCardId;
    private string _claimedCardId;
    private string _continueLabel;

    private void Awake()
    {
        _selectCard = SelectCard;
        confirmButton.onClick.AddListener(Confirm);
    }

    public void Configure(PrefabPoolManager pool)
    {
        _pool = pool;
        if (_pool != null && cardPrefab != null)
            _pool.Prewarm(cardPrefab.gameObject, 2, 2);
    }

    public void SetReward(IReadOnlyList<PassiveCardData> choices, string claimedCardId,
        Func<string, bool> claimReward, Action continueAction, string continueLabel)
    {
        _choices = choices;
        _claimedCardId = claimedCardId;
        _selectedCardId = claimedCardId;
        _claimReward = claimReward;
        _continueAction = continueAction;
        _continueLabel = continueLabel;
    }

    public override void Show(float duration = 0f)
    {
        base.Show(duration);
        ClearCards();
        if (_choices == null || _pool == null || cardPrefab == null)
            throw new InvalidOperationException("보상 데이터와 카드 풀 연결이 필요합니다.");
        foreach (var card in _choices)
        {
            var rented = _pool.Rent(cardPrefab.gameObject, cardContent);
            if (rented == null)
                throw new InvalidOperationException("보상 카드 풀 용량이 부족합니다.");
            var view = rented.GetComponent<RewardCardView>();
            view.transform.SetAsLastSibling();
            view.Bind(card, _selectCard);
            _visibleCards.Add(view);
        }
        RefreshSelection();
    }

    private void SelectCard(RewardCardView card)
    {
        if (!IsShown || _claimedCardId != null)
            return;
        _selectedCardId = card.Card.cardId;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        foreach (var view in _visibleCards)
        {
            view.SetSelected(view.Card.cardId == _selectedCardId);
            view.SetInteractable(_claimedCardId == null);
        }
        confirmButtonText.text = _claimedCardId != null ? _continueLabel : "CONFIRM";
        confirmButton.interactable = _claimedCardId != null ? _continueAction != null : _selectedCardId != null;
    }

    private void Confirm()
    {
        if (!IsShown || !confirmButton.interactable)
            return;
        // 연속 클릭의 두 번째 입력으로 획득 직후 화면까지 넘어가지 않도록 한 프레임 뒤 다시 연다.
        confirmButton.interactable = false;
        if (_claimedCardId != null)
        {
            _continueAction?.Invoke();
            return;
        }
        if (_claimReward != null && _claimReward(_selectedCardId))
            _claimedCardId = _selectedCardId;
        RefreshSelection();
        confirmButton.interactable = false;
        StartCoroutine(EnableConfirmationNextFrame());
    }

    private System.Collections.IEnumerator EnableConfirmationNextFrame()
    {
        yield return null;
        if (IsShown)
            RefreshSelection();
    }

    private void ClearCards(bool moveToStorage = true)
    {
        if (_pool != null)
            foreach (var card in _visibleCards)
                if (card != null)
                    _pool.Return(card.gameObject, moveToStorage);
        _visibleCards.Clear();
    }

    protected override void OnDisable()
    {
        StopAllCoroutines();
        ClearCards(false);
        _choices = null;
        _claimReward = null;
        _continueAction = null;
        _selectedCardId = _claimedCardId = null;
        base.OnDisable();
    }

    private void OnDestroy()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(Confirm);
    }
}
