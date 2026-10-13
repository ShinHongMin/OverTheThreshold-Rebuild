using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>로비의 편성 순서·캐릭터 목록·출격 버튼을 표시한다. 새 런과 씬 이동은 소유자에게 요청한다.</summary>
public sealed class LobbyView : UiBase
{
    [SerializeField] private LobbyCharacterView characterButtonPrefab;
    [SerializeField] private RectTransform characterContent;
    [SerializeField] private Image[] partyImages = Array.Empty<Image>();
    [SerializeField] private Button departButton;
    [SerializeField] private Button returnToTitleButton;
    private readonly List<UnitData> _selectedParty = new List<UnitData>(3);
    private readonly List<LobbyCharacterView> _visibleCharacters = new List<LobbyCharacterView>();
    private IReadOnlyList<UnitData> _availableCharacters;
    private PrefabPoolManager _pool;
    private Action<UnitData> _selectCharacter;
    private Func<IReadOnlyList<UnitData>, bool> _depart;
    private Func<bool> _returnToTitle;
    private bool _inputEnabled;

    private void Awake()
    {
        _selectCharacter = ToggleCharacter;
        departButton.onClick.AddListener(Depart);
        returnToTitleButton.onClick.AddListener(ReturnToTitle);
    }

    public void Configure(PrefabPoolManager pool, IReadOnlyList<UnitData> characters,
        Func<IReadOnlyList<UnitData>, bool> depart, Func<bool> returnToTitle)
    {
        if (pool == null || characterButtonPrefab == null || partyImages.Length != 3 || characters == null)
            throw new InvalidOperationException("로비의 캐릭터 목록·풀·편성칸 3개를 연결하세요.");
        ReleaseCharacters();
        _pool = pool;
        _availableCharacters = characters;
        _selectedParty.Clear();
        _depart = depart;
        _returnToTitle = returnToTitle;
        _inputEnabled = true;
        _pool.Prewarm(characterButtonPrefab.gameObject, characters.Count, Mathf.Max(16, characters.Count));
    }

    public override void Show(float duration = 0f)
    {
        base.Show(duration);
        if (_visibleCharacters.Count == 0)
            foreach (var character in _availableCharacters)
            {
                var rented = _pool.Rent(characterButtonPrefab.gameObject, characterContent);
                if (rented == null) throw new InvalidOperationException("로비 캐릭터 버튼의 풀 용량이 부족합니다.");
                var view = rented.GetComponent<LobbyCharacterView>();
                _visibleCharacters.Add(view);
                view.transform.SetAsLastSibling();
                view.Bind(character, _selectCharacter);
            }
        RefreshSelection();
    }

    private void ToggleCharacter(UnitData character)
    {
        if (!IsShown || !_inputEnabled || character == null) return;
        int selectedIndex = _selectedParty.IndexOf(character);
        if (selectedIndex >= 0) _selectedParty.RemoveAt(selectedIndex);
        else if (_selectedParty.Count < partyImages.Length) _selectedParty.Add(character);
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int index = 0; index < partyImages.Length; index++)
        {
            var image = partyImages[index];
            image.sprite = index < _selectedParty.Count ? _selectedParty[index].lobbyIllustration : null;
            image.enabled = image.sprite != null;
        }
        foreach (var character in _visibleCharacters)
        {
            character.SetSelected(_selectedParty.Contains(character.Character));
            character.SetInteractable(_inputEnabled);
        }
        departButton.interactable = _inputEnabled && _selectedParty.Count == partyImages.Length && _depart != null;
        returnToTitleButton.interactable = _inputEnabled && _returnToTitle != null;
    }

    private void Depart()
    {
        if (!IsShown || !departButton.interactable) return;
        _inputEnabled = false;
        RefreshSelection();
        if (_depart(_selectedParty)) return;
        _inputEnabled = true;
        RefreshSelection();
    }
    private void ReturnToTitle()
    {
        if (!IsShown || !returnToTitleButton.interactable) return;
        _inputEnabled = false;
        RefreshSelection();
        if (_returnToTitle()) return;
        _inputEnabled = true;
        RefreshSelection();
    }
    public override void Hide(float duration = 0f)
    {
        if (duration <= 0f) ReleaseCharacters();
        base.Hide(duration);
    }
    private void ReleaseCharacters(bool moveToStorage = true)
    {
        if (_pool != null) foreach (var character in _visibleCharacters)
            if (character != null) _pool.Return(character.gameObject, moveToStorage);
        _visibleCharacters.Clear();
    }
    private void OnDisable() => ReleaseCharacters(false);
    private void OnDestroy()
    {
        if (departButton != null) departButton.onClick.RemoveListener(Depart);
        if (returnToTitleButton != null) returnToTitleButton.onClick.RemoveListener(ReturnToTitle);
    }
}
