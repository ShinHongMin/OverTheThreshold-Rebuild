using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>입력 표시와 선택만 담당한다. 예약·비용 적용은 App/Core가 처리한다.</summary>
public sealed class CombatInputView : UiBase
{
    [SerializeField] private SkillButtonView[] skillButtons;
    [SerializeField] private SkillDescriptionView skillDescription;
    private readonly Dictionary<string, SkillDefinition> _skillDefinitions = new Dictionary<string, SkillDefinition>();
    public void ConfigureSkillIcons(IReadOnlyList<SkillTableAsset> tables)
    {
        _skillDefinitions.Clear();
        foreach (var table in tables)
            if (table != null) foreach (var skill in table.skills)
                if (skill != null) _skillDefinitions[skill.skillId] = skill;
    }
    private CombatContext _context;
    private Unit _actor;
    private SkillSpec _skill;
    private Action<SkillSpec, Unit> _submit;
    private Action<SkillSpec> _selectSkill;
    private Action<SkillSpec> _previewSkill;
    public Unit CurrentActor => _actor;
    public bool IsInteractionBlocked { get; private set; }
    public void SetInteractionBlocked(bool blocked)
    {
        if (IsInteractionBlocked == blocked) return;
        IsInteractionBlocked = blocked;
        TargetSelectionChanged?.Invoke();
    }
    public event Action TargetSelectionChanged;
    public bool CanSelectWorldTarget(Unit unit)
        => IsShown && !MessagePopupView.IsInputBlocked && !IsInteractionBlocked && _submit != null && _actor != null && _actor.IsAlive && _skill != null
            && TargetingRule.NeedsManualTarget(_skill.TargetType)
            && _skill.CanAfford(_actor, _context.Resources)
            && TargetingRule.IsValidTarget(_context, _actor, _skill.TargetType, unit);

    public void BeginInput(CombatContext context, Unit actor, IReadOnlyList<SkillSpec> skills, Action<SkillSpec, Unit> submit)
    {
        _context = context;
        _actor = actor;
        _skill = null;
        _submit = submit;
        if (_selectSkill == null) _selectSkill = SelectSkill;
        if (_previewSkill == null) _previewSkill = PreviewSkill;
        skillDescription?.Clear();
        Show();
        for (int i = 0; i < skillButtons.Length; i++)
        {
            var skill = i < skills.Count ? skills[i] : null;
            Sprite icon = null;
            if (skill != null && _skillDefinitions.TryGetValue(skill.SkillId, out var definition)) icon = definition.skillIcon;
            skillButtons[i].Bind(skill, skill != null && skill.CanAfford(actor, context.Resources), _selectSkill, icon, ResonanceRules.IsEnhanced(actor), _previewSkill);
        }
        TargetSelectionChanged?.Invoke();
    }
    private void PreviewSkill(SkillSpec skill)
    {
        if (IsShown && !IsInteractionBlocked && skill != null && _skillDefinitions.TryGetValue(skill.SkillId, out var definition))
            skillDescription?.ShowSkill(definition);
    }
    private void SelectSkill(SkillSpec skill)
    {
        if (!IsShown || MessagePopupView.IsInputBlocked || IsInteractionBlocked || _submit == null || skill == null || !skill.CanAfford(_actor, _context.Resources)) return;
        _skill = skill;
        PreviewSkill(skill);
        if (!TargetingRule.NeedsManualTarget(skill.TargetType)) _submit?.Invoke(skill, null);
        else TargetSelectionChanged?.Invoke();
    }
    public void SelectWorldTarget(Unit unit)
    {
        if (CanSelectWorldTarget(unit))
        {
            _submit?.Invoke(_skill, unit);
        }
    }
    public override void Hide(float duration = 0f)
    {
        _submit = null;
        _skill = null;
        TargetSelectionChanged?.Invoke();
        base.Hide(duration);
    }
}
