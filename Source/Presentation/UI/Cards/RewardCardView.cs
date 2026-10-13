using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>카드 한 장의 이미지·설명·선택 표시. 획득 규칙은 소유하지 않는다.</summary>
public sealed class RewardCardView : MonoBehaviour, IPooledView
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Image rarityImage;
    [SerializeField] private Image cardImage;
    [SerializeField] private Image selectionFrame;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Color selectedColor = new Color(1f, .78f, .18f, 1f);
    private Color _normalFrameColor;
    private Action<RewardCardView> _selectCard;
    public PassiveCardData Card { get; private set; }

    private void Awake()
    {
        _normalFrameColor = selectionFrame.color;
        selectButton.onClick.AddListener(Select);
    }
    public void Bind(PassiveCardData card, Action<RewardCardView> selectCard)
    {
        Card = card;
        _selectCard = selectCard;
        nameText.text = card.cardName;
        descriptionText.text = card.Description;
        cardImage.sprite = card.cardImage;
        cardImage.enabled = card.cardImage != null;
        rarityImage.sprite = card.rarityBG;
        SetSelected(false);
        SetInteractable(selectCard != null);
    }
    public void SetSelected(bool selected) => selectionFrame.color = selected ? selectedColor : _normalFrameColor;
    public void SetInteractable(bool enabled) => selectButton.interactable = enabled;
    private void Select()
    {
        if (isActiveAndEnabled && selectButton.interactable) _selectCard?.Invoke(this);
    }
    public void OnRent() { }
    public void OnReturn()
    {
        _selectCard = null;
        Card = null;
        nameText.text = descriptionText.text = "";
        cardImage.sprite = rarityImage.sprite = null;
        cardImage.enabled = false;
        SetSelected(false);
        SetInteractable(false);
    }
    private void OnDestroy() { if (selectButton != null) selectButton.onClick.RemoveListener(Select); }
}
