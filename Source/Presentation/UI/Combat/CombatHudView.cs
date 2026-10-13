using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>공용 자원을 표시하고 호버한 유닛을 아군/적 상태창에 전달한다. 전투 값은 변경하지 않는다.</summary>
public sealed class CombatHudView : MonoBehaviour
{
    [SerializeField] private BuffTooltipView buffTooltip;
    public BuffTooltipView BuffTooltip => buffTooltip;
    private readonly System.Collections.Generic.Dictionary<Unit, Sprite> _portraits = new System.Collections.Generic.Dictionary<Unit, Sprite>();
    public void ClearPortraits() => _portraits.Clear();
    public void RegisterPortrait(Unit unit, Sprite portrait) => _portraits[unit] = portrait;
    [SerializeField] private UnitStatusView allyStatusView;
    [SerializeField] private UnitStatusView enemyStatusView;
    public void ConfigureBuffPool(PrefabPoolManager pool)
    {
        if (allyStatusView != null)
            allyStatusView.ConfigureBuffPool(pool, buffTooltip);
        if (enemyStatusView != null)
            enemyStatusView.ConfigureBuffPool(pool, buffTooltip);
        Refresh();
    }
    [SerializeField] private TMP_Text spText, opText, roundText;
    [SerializeField] private Slider opSlider;
    private CombatContext _combatContext;
    private Unit _hoveredUnit;
    public void Bind(CombatContext context)
    {
        _combatContext = context;
        _hoveredUnit = null;
        Refresh();
    }

    public void ResetDisplay()
    {
        if (buffTooltip != null)
            buffTooltip.Hide();
        _combatContext = null;
        _hoveredUnit = null;
        if (allyStatusView != null)
            allyStatusView.Hide();
        if (enemyStatusView != null)
            enemyStatusView.Hide();
    }

    public void SetHoveredUnit(Unit unit)
    {
        if (_combatContext == null || unit == null)
            return;
        _hoveredUnit = unit;
        Refresh();
    }

    public void ClearHoveredUnit(Unit unit)
    {
        // 다른 유닛으로 이동한 뒤 도착한 이전 Exit가 새 표시를 지우지 않게 한다.
        if (_hoveredUnit != unit)
            return;
        _hoveredUnit = null;
        Refresh();
    }

    public void SetRound(int round)
    {
        if (roundText != null)
            roundText.SetText("{0}", round);
    }

    public void Refresh()
    {
        if (_combatContext == null)
            return;
        var resources = _combatContext.Resources;
        if (spText != null)
            spText.SetText("SP : {0}", resources.CurrentSP);
        if (opText != null)
            opText.SetText("{0}", resources.CurrentOP);
        SetSlider(opSlider, resources.CurrentOP, resources.MaxOP);
        Sprite portrait = null;
        if (_hoveredUnit != null)
            _portraits.TryGetValue(_hoveredUnit, out portrait);
        bool hoveringAlly = _hoveredUnit != null && _combatContext.IsPartyMember(_hoveredUnit);
        if (allyStatusView != null)
            allyStatusView.Show(hoveringAlly ? _hoveredUnit : null, portrait);
        if (enemyStatusView != null)
            enemyStatusView.Show(_hoveredUnit != null && !hoveringAlly ? _hoveredUnit : null, portrait);
    }

    private static void SetSlider(Slider slider, float value, float maximum)
    {
        if (slider != null)
        {
            slider.maxValue = maximum;
            slider.value = value;
        }
    }
}
