using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>전투 UI의 준비·입력 시작/종료·상태 초기화를 관리한다. 전투 계산은 하지 않는다.</summary>
public sealed class CombatUiManager : MonoBehaviour
{
    [SerializeField] private CombatInputView inputView;
    [SerializeField] private CombatHudView hudView;
    [SerializeField] private SkillDescriptionView skillDescription;
    [SerializeField] private CombatPopupManager popupManager;
    [SerializeField] private MonsterInfoView monsterInfoView;
    [SerializeField] private BattleResultView battleResultView;
    [SerializeField] private BattleRewardView battleRewardView;
    [SerializeField] private OwnedCardListView ownedCardListView;
    [SerializeField] private RunResultView runResultView;
    private CombatContext _context;

    public CombatInputView InputView => inputView;
    public CombatHudView HudView => hudView;
    public bool HasInput => inputView != null;
    public bool IsPaused => popupManager != null && popupManager.IsPaused;

    public void Prepare(IReadOnlyList<SkillTableAsset> skillTables, Action returnToTitle = null, PrefabPoolManager pool = null)
    {
        ResetDisplay();
        if (popupManager != null) popupManager.Configure(returnToTitle);
        hudView?.ClearPortraits();
        inputView?.ConfigureSkillIcons(skillTables);
        if (monsterInfoView != null) monsterInfoView.Configure(pool, skillTables);
        if (battleRewardView != null) battleRewardView.Configure(pool);
        if (ownedCardListView != null) ownedCardListView.Configure(pool);
        if (runResultView != null) runResultView.Configure(pool);
    }

    public void Bind(CombatContext context)
    {
        _context = context;
        hudView?.Bind(context);
        if (popupManager != null) popupManager.SetOptionsAvailable(context != null);
        if (monsterInfoView != null) monsterInfoView.Bind(context);
        if (popupManager != null) popupManager.SetMonsterInfoAvailable(context != null && monsterInfoView != null);
    }

    public void BeginInput(Unit actor, IReadOnlyList<SkillSpec> skills, Action<SkillSpec, Unit> submit)
    {
        if (_context == null) throw new InvalidOperationException("전투 UI에 Context를 먼저 연결해야 합니다.");
        inputView?.BeginInput(_context, actor, skills, submit);
    }

    public void BindRun(RunState run, RewardTableAsset cardTable)
    {
        if (ownedCardListView != null) ownedCardListView.Bind(run?.Cards, cardTable);
        if (popupManager != null) popupManager.SetCardListAvailable(_context != null && run != null && cardTable != null && ownedCardListView != null);
    }

    public void EndInput() { if (inputView != null) inputView.Hide(); }
    public void RegisterPortrait(Unit unit, Sprite portrait) => hudView?.RegisterPortrait(unit, portrait);
    public void RegisterMonster(Unit unit, UnitData data)
    {
        if (monsterInfoView != null) monsterInfoView.RegisterUnit(unit, data);
    }
    public void SetRound(int round) => hudView?.SetRound(round);

    public void ResetDisplay()
    {
        if (popupManager != null) popupManager.ResetDisplay();
        if (monsterInfoView != null) monsterInfoView.ResetDisplay();
        if (ownedCardListView != null) ownedCardListView.ResetDisplay();
        if (runResultView != null) runResultView.ResetDisplay();
        EndInput();
        if (inputView != null) inputView.SetInteractionBlocked(false);
        // 씬 종료 때 먼저 파괴된 Unity 뷰에는 Hide/Reset을 호출하지 않는다.
        if (skillDescription != null) skillDescription.Clear();
        if (hudView != null) hudView.ResetDisplay();
        _context = null;
    }

    private void OnDisable() => ResetDisplay();
    public void CompleteBattle()
    {
        if (popupManager != null) popupManager.ResetDisplay();
        EndInput();
        if (inputView != null) inputView.SetInteractionBlocked(true);
    }

    public void ShowBattleResult(BattleResult result, string buttonLabel, Action confirmAction)
    {
        if (battleResultView == null || popupManager == null) return;
        battleResultView.SetResult(result, buttonLabel, confirmAction);
        popupManager.Open<BattleResultView>();
    }

    public void ShowTurnLimit(Action returnToTitle)
    {
        if (battleResultView == null || popupManager == null) return;
        battleResultView.SetTurnLimit("GO TITLE", returnToTitle);
        popupManager.Open<BattleResultView>();
    }

    public void ShowRunResult(RunState run, IReadOnlyList<UnitData> partyData, RewardTableAsset cardTable, Action returnToTitle)
    {
        if (runResultView == null || popupManager == null)
            throw new InvalidOperationException("런 결산 화면과 팝업 관리자를 연결해야 합니다.");
        popupManager.CloseActivePopup();
        runResultView.Bind(run, partyData, cardTable, returnToTitle);
        popupManager.Open<RunResultView>();
    }

    public void ShowBattleReward(IReadOnlyList<PassiveCardData> choices, string claimedCardId,
        Func<string, bool> claimReward, Action continueAction, string continueLabel)
    {
        if (battleRewardView == null || popupManager == null)
            throw new InvalidOperationException("보상 화면과 팝업 관리자를 연결해야 합니다.");
        popupManager.CloseActivePopup();
        battleRewardView.SetReward(choices, claimedCardId, claimReward, continueAction, continueLabel);
        popupManager.Open<BattleRewardView>();
    }
}
