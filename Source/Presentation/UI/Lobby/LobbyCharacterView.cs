using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>로비 캐릭터 선택 버튼 한 개. 선택 여부를 표시하고 클릭을 소유자에게 전달한다.</summary>
public sealed class LobbyCharacterView : MonoBehaviour, IPooledView
{
    [SerializeField] private Button selectButton;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image selectionFrame;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Color selectedColor = new Color(1f, .78f, .35f);
    private Color _normalFrameColor;
    private Action<UnitData> _selectCharacter;
    public UnitData Character { get; private set; }

    private void Awake()
    {
        _normalFrameColor = selectionFrame.color;
        selectButton.onClick.AddListener(Select);
    }
    public void Bind(UnitData character, Action<UnitData> selectCharacter)
    {
        Character = character;
        _selectCharacter = selectCharacter;
        characterImage.sprite = character.lobbyIllustration;
        characterImage.enabled = characterImage.sprite != null;
        nameText.text = character.unitName;
        SetSelected(false);
        SetInteractable(true);
    }
    public void SetSelected(bool selected) => selectionFrame.color = selected ? selectedColor : _normalFrameColor;
    public void SetInteractable(bool interactable) => selectButton.interactable = interactable;
    private void Select()
    {
        if (isActiveAndEnabled && selectButton.interactable) _selectCharacter?.Invoke(Character);
    }
    public void OnRent() { }
    public void OnReturn()
    {
        Character = null;
        _selectCharacter = null;
        characterImage.sprite = null;
        characterImage.enabled = false;
        nameText.text = "";
        SetSelected(false);
        SetInteractable(false);
    }
    private void OnDestroy() { if (selectButton != null) selectButton.onClick.RemoveListener(Select); }
}
