using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>이벤트 그림·설명과 선택지를 표시한다. 확률/효과는 전달받은 선택 동작에 맡긴다.</summary>
public sealed class MapEventView : PopupView
{
    [SerializeField] private Image eventImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private RectTransform choiceContent;
    [SerializeField] private EventChoiceView choicePrefab;
    private readonly List<EventChoiceView> _visibleChoices = new List<EventChoiceView>(3);
    private PrefabPoolManager _pool;
    private EventData _eventData;
    private Action<int> _selectChoice;

    public void Configure(PrefabPoolManager pool)
    {
        _pool = pool;
        _pool.Prewarm(choicePrefab.gameObject, 3, 3);
    }

    public void Bind(EventData data, Action<int> selectChoice)
    {
        _eventData = data;
        _selectChoice = selectChoice;
    }

    public override void Show(float duration = 0f)
    {
        base.Show(duration);
        ReleaseChoices();
        titleText.text = _eventData.eventTitle;
        descriptionText.text = _eventData.description;
        eventImage.sprite = _eventData.eventImage;
        eventImage.enabled = eventImage.sprite != null;
        for (int index = 0; index < _eventData.choices.Count; index++)
        {
            var rented = _pool.Rent(choicePrefab.gameObject, choiceContent);
            if (rented == null)
                throw new InvalidOperationException("이벤트 선택지 풀 용량이 부족합니다.");
            var choice = rented.GetComponent<EventChoiceView>();
            _visibleChoices.Add(choice);
            choice.transform.SetAsLastSibling();
            choice.Bind(index, _eventData.choices[index].choiceText, _selectChoice);
        }
    }

    public override void Hide(float duration = 0f)
    {
        if (duration <= 0f)
            ReleaseChoices();
        base.Hide(duration);
    }

    private void ReleaseChoices(bool moveToStorage = true)
    {
        if (_pool != null)
            foreach (var choice in _visibleChoices)
                if (choice != null)
                    _pool.Return(choice.gameObject, moveToStorage);
        _visibleChoices.Clear();
    }

    protected override void OnDisable()
    {
        ReleaseChoices(false);
        _eventData = null;
        _selectChoice = null;
        base.OnDisable();
    }
}
