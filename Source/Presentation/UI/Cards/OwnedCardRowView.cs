using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>보유 목록의 카드 한 줄. 설명 길이에 맞춰 높이를 정하고 풀에서 재사용한다.</summary>
public sealed class OwnedCardRowView : MonoBehaviour, IPooledView
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private RectTransform descriptionBox;
    [SerializeField] private LayoutElement rowLayout;
    private float _minimumDescriptionHeight;
    private float _minimumRowHeight;
    private string _displayedDescription;
    private float _measuredTextWidth;
    private bool _hasCachedLayout;

    private void Awake()
    {
        _minimumDescriptionHeight = descriptionBox.rect.height;
        _minimumRowHeight = ((RectTransform)transform).rect.height;
    }
    public void Bind(PassiveCardData card)
    {
        iconImage.sprite = card.cardImage;
        iconImage.enabled = card.cardImage != null;
        nameText.text = card.cardName;
        descriptionText.text = card.Description;
        float textWidth = descriptionText.rectTransform.rect.width - descriptionText.margin.x - descriptionText.margin.z;
        if (_hasCachedLayout && _displayedDescription == card.Description
            && Mathf.Approximately(_measuredTextWidth, textWidth)) return;

        float height = Mathf.Max(_minimumDescriptionHeight, descriptionText.GetPreferredValues(card.Description, textWidth, 0f).y);
        descriptionBox.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        rowLayout.preferredHeight = Mathf.Max(_minimumRowHeight, -descriptionBox.anchoredPosition.y + height + 6f);
        _displayedDescription = card.Description;
        _measuredTextWidth = textWidth;
        _hasCachedLayout = true;
    }
    public void OnRent() { }
    // 풀에서 비활성화한 동안 마지막 내용과 높이를 보관한다. 다른 카드를 받으면 Bind에서 갱신한다.
    public void OnReturn() { }
}
