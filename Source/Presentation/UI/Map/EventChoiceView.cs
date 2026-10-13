using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>이벤트 선택지 버튼 하나. 반환 시 이전 선택 콜백을 비운다.</summary>
public sealed class EventChoiceView : MonoBehaviour, IPooledView
{
    [SerializeField] private Button selectButton;
    [SerializeField] private TMP_Text choiceText;
    private Action<int> _selectChoice;
    private int _choiceIndex;
    private void Awake() => selectButton.onClick.AddListener(Select);
    public void Bind(int index, string text, Action<int> selectChoice)
    {
        _choiceIndex = index;
        choiceText.text = text;
        _selectChoice = selectChoice;
        selectButton.interactable = selectChoice != null;
    }

    private void Select()
    {
        if (!isActiveAndEnabled || !selectButton.interactable || _selectChoice == null)
            return;
        _selectChoice(_choiceIndex);
    }

    public void OnRent()
    {
    }

    public void OnReturn()
    {
        _selectChoice = null;
        _choiceIndex = -1;
        choiceText.text = "";
        selectButton.interactable = false;
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveListener(Select);
    }
}
