using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>현재 전투의 몬스터를 슬롯별로 조회한다. 능력치는 Unit, 스킬 설명은 ID 테이블에서 읽는다.</summary>
public sealed class MonsterInfoView : PopupView
{
    [SerializeField] private Button[] monsterButtons;
    [SerializeField] private Button closeButton;
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private RectTransform skillContent;
    [SerializeField] private ScrollRect skillScroll;
    [SerializeField] private SkillInfoRowView skillRowPrefab;
    private readonly Dictionary<Unit, UnitData> _dataByUnit = new Dictionary<Unit, UnitData>();
    private readonly Dictionary<string, SkillDefinition> _skillsById = new Dictionary<string, SkillDefinition>();
    private readonly List<SkillInfoRowView> _visibleRows = new List<SkillInfoRowView>();
    private readonly HashSet<string> _displayedSkillIds = new HashSet<string>();
    private UnityAction[] _selectMonsterActions;
    private CombatContext _context;
    private PrefabPoolManager _pool;

    private void Awake()
    {
        _selectMonsterActions = new UnityAction[monsterButtons.Length];
        for (int index = 0; index < monsterButtons.Length; index++)
        {
            int slotIndex = index;
            _selectMonsterActions[index] = () => SelectMonster(slotIndex);
            monsterButtons[index].onClick.AddListener(_selectMonsterActions[index]);
        }
        closeButton.onClick.AddListener(Close);
    }

    public void Configure(PrefabPoolManager pool, IReadOnlyList<SkillTableAsset> skillTables)
    {
        ResetDisplay();
        _pool = pool;
        _skillsById.Clear();
        if (skillTables != null)
            foreach (var table in skillTables)
                if (table != null)
                    foreach (var skill in table.skills)
                        if (skill != null && !string.IsNullOrEmpty(skill.skillId)) _skillsById[skill.skillId] = skill;
        if (_pool != null && skillRowPrefab != null) _pool.Prewarm(skillRowPrefab.gameObject, 8, 32);
    }

    public void RegisterUnit(Unit unit, UnitData data)
    {
        if (unit != null && data != null) _dataByUnit[unit] = data;
    }

    public void Bind(CombatContext context) => _context = context;

    public override void Show(float duration = 0f)
    {
        base.Show(duration);
        int firstAliveSlot = -1;
        for (int index = 0; index < monsterButtons.Length; index++)
        {
            bool isAlive = GetLivingMonster(index) != null;
            monsterButtons[index].interactable = isAlive;
            if (isAlive && firstAliveSlot < 0) firstAliveSlot = index;
        }
        if (firstAliveSlot >= 0) SelectMonster(firstAliveSlot);
        else ClearDetails();
    }

    private Unit GetLivingMonster(int index)
    {
        if (_context == null || index < 0 || index >= _context.Enemies.Count) return null;
        var monster = _context.Enemies[index];
        return monster != null && monster.IsAlive ? monster : null;
    }

    private void SelectMonster(int index)
    {
        if (!IsShown) return;
        var monster = GetLivingMonster(index);
        if (monster == null) return;
        ClearRows();
        nameText.text = monster.Name;
        statsText.SetText("HP : {0:0} / {1:0}\nATK : {2:0}\nDEF : {3:0}\nShield : {4:0}",
            monster.CurrentHP, monster.MaxHP, monster.Stats.Get(StatType.ATK),
            monster.Stats.Get(StatType.DEF), monster.CurrentShield);
        portraitImage.sprite = null;
        if (_dataByUnit.TryGetValue(monster, out var data))
        {
            portraitImage.sprite = data.portraitSprite;
            AddSkill(data.basicAttackId);
            foreach (var skillId in data.skillIds) AddSkill(skillId);
            AddSkill(data.enhancedSkillId);
            AddSkill(data.chargeSkillId);
            AddSkill(data.selfGroggySkillId);
        }
        portraitImage.enabled = portraitImage.sprite != null;
        LayoutRebuilder.ForceRebuildLayoutImmediate(skillContent);
        skillScroll.StopMovement();
        skillScroll.verticalNormalizedPosition = 1f;
    }

    private void AddSkill(string skillId)
    {
        if (string.IsNullOrEmpty(skillId) || !_displayedSkillIds.Add(skillId)) return;
        if (!_skillsById.TryGetValue(skillId, out var skill))
        {
            Debug.LogWarning("[몬스터 정보] 스킬 ID를 찾을 수 없습니다: " + skillId, this);
            return;
        }
        if (_pool == null || skillRowPrefab == null) return;
        var rented = _pool.Rent(skillRowPrefab.gameObject, skillContent);
        if (rented == null) return;
        var row = rented.GetComponent<SkillInfoRowView>();
        row.transform.SetAsLastSibling();
        row.Show(skill);
        _visibleRows.Add(row);
    }

    public void ResetDisplay()
    {
        Hide();
        _context = null;
        _dataByUnit.Clear();
        ClearDetails();
    }

    private void ClearDetails()
    {
        ClearRows();
        nameText.text = "";
        statsText.text = "";
        portraitImage.sprite = null;
        portraitImage.enabled = false;
    }

    private void ClearRows(bool moveToStorage = true)
    {
        if (_pool != null)
        {
            // 풀은 나중에 반환한 행부터 대여한다. 역순으로 반환해 다시 열 때 기존 내용과 순서가 맞도록 한다.
            for (int index = _visibleRows.Count - 1; index >= 0; index--)
            {
                var row = _visibleRows[index];
                if (row != null) _pool.Return(row.gameObject, moveToStorage);
            }
        }
        _visibleRows.Clear();
        _displayedSkillIds.Clear();
    }

    private void Close() => Hide();
    protected override void OnDisable()
    {
        ClearRows(false);
        base.OnDisable();
    }

    private void OnDestroy()
    {
        if (_selectMonsterActions != null)
            for (int index = 0; index < monsterButtons.Length; index++)
                if (monsterButtons[index] != null) monsterButtons[index].onClick.RemoveListener(_selectMonsterActions[index]);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }
}
