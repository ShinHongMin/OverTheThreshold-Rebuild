using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>휴식 화면의 버튼·결과와 풀에서 빌린 캐릭터 표시. 효과 적용과 노드 완료는 App에 요청한다.</summary>
public sealed class MapRestView : PopupView
{
    [Serializable]
    private sealed class PartyVisual
    {
        public UnitData unitData;
        public GameObject restPrefab;
    }

    [SerializeField] private Button healButton;
    [SerializeField] private Button maintenanceButton;
    [SerializeField] private Button cureEchoButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Transform[] partySlots = Array.Empty<Transform>();
    [SerializeField] private PartyVisual[] partyVisuals = Array.Empty<PartyVisual>();
    private readonly List<GameObject> _visibleParty = new List<GameObject>(3);
    private PrefabPoolManager _pool;
    private IReadOnlyList<UnitData> _partyData;
    private Action<RestRules.Action> _selectAction;
    private Func<bool> _leave;

    private void Awake()
    {
        healButton.onClick.AddListener(Heal);
        maintenanceButton.onClick.AddListener(Maintain);
        cureEchoButton.onClick.AddListener(CureEcho);
        leaveButton.onClick.AddListener(Leave);
    }

    public void Configure(PrefabPoolManager pool) => _pool = pool;
    public void Bind(IReadOnlyList<UnitData> partyData, string feedback, bool canHeal, bool canMaintain, bool canCureEcho,
        Action<RestRules.Action> selectAction, Func<bool> leave)
    {
        if (_partyData != partyData)
            ReleaseParty();
        _partyData = partyData;
        _selectAction = selectAction;
        _leave = leave;
        feedbackText.text = feedback;
        healButton.interactable = canHeal;
        maintenanceButton.interactable = canMaintain;
        cureEchoButton.interactable = canCureEcho;
        leaveButton.interactable = leave != null;
    }

    public override void Show(float duration = 0f)
    {
        base.Show(duration);
        if (_pool == null || _partyData == null || _partyData.Count > partySlots.Length)
            throw new InvalidOperationException("휴식 화면의 파티·슬롯·풀 연결을 확인하세요.");
        if (_visibleParty.Count == _partyData.Count)
            return;
        ReleaseParty();
        for (int index = 0; index < _partyData.Count; index++)
        {
            GameObject prefab = null;
            foreach (var visual in partyVisuals)
                if (visual.unitData == _partyData[index])
                {
                    prefab = visual.restPrefab;
                    break;
                }
            if (prefab == null || partySlots[index] == null)
                throw new InvalidOperationException("휴식 캐릭터 프리팹/슬롯 연결이 없습니다: " + _partyData[index].name);
            _pool.Prewarm(prefab, 1, 1);
            var rented = _pool.Rent(prefab, partySlots[index]);
            if (rented == null)
                throw new InvalidOperationException("휴식 캐릭터 풀 용량이 부족합니다.");
            // 의자에 맞춰 둔 휴식 프리팹의 UI 위치를 유지한다.
            ((RectTransform)rented.transform).anchoredPosition = ((RectTransform)prefab.transform).anchoredPosition;
            _visibleParty.Add(rented);
        }
    }

    private void Heal()
    {
        if (IsShown && healButton.interactable)
            _selectAction?.Invoke(RestRules.Action.Heal);
    }

    private void Maintain()
    {
        if (IsShown && maintenanceButton.interactable)
            _selectAction?.Invoke(RestRules.Action.Maintenance);
    }

    private void CureEcho()
    {
        if (IsShown && cureEchoButton.interactable)
            _selectAction?.Invoke(RestRules.Action.CureEcho);
    }

    private void Leave()
    {
        if (!IsShown || !leaveButton.interactable || _leave == null)
            return;
        leaveButton.interactable = false;
        if (!_leave() && IsShown)
            leaveButton.interactable = true;
    }

    public override void Hide(float duration = 0f)
    {
        if (duration <= 0f)
            ReleaseParty();
        base.Hide(duration);
    }

    private void ReleaseParty(bool moveToStorage = true)
    {
        if (_pool != null)
            foreach (var member in _visibleParty)
                if (member != null)
                    _pool.Return(member, moveToStorage);
        _visibleParty.Clear();
    }

    protected override void OnDisable()
    {
        ReleaseParty(false);
        _partyData = null;
        _selectAction = null;
        _leave = null;
        base.OnDisable();
    }

    private void OnDestroy()
    {
        if (healButton != null)
            healButton.onClick.RemoveListener(Heal);
        if (maintenanceButton != null)
            maintenanceButton.onClick.RemoveListener(Maintain);
        if (cureEchoButton != null)
            cureEchoButton.onClick.RemoveListener(CureEcho);
        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(Leave);
    }
}
