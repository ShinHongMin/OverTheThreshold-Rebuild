using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>유닛 하나의 상태·초상화·버프를 표시한다. 아군/적 구분과 전투 계산은 하지 않는다.</summary>
public sealed class UnitStatusView : MonoBehaviour
{
    [SerializeField] private TMP_Text unitNameText, healthText, erText, attackText, defenseText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private BuffListView buffList;
    [SerializeField] private Slider healthSlider, erSlider, shieldSlider;

    public void ConfigureBuffPool(PrefabPoolManager pool, BuffTooltipView tooltip)
    {
        if (buffList == null) return;
        buffList.ConfigureTooltip(tooltip);
        buffList.ConfigurePool(pool);
    }

    public void Show(Unit unit, Sprite portrait = null)
    {
        // 루트를 끄기 전에 아이콘을 풀 보관소로 돌려 상태창 교체/제거와 수명을 분리한다.
        if (unit == null && buffList != null) buffList.Refresh(null);
        gameObject.SetActive(unit != null);
        if (buffList != null) buffList.Refresh(unit);
        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = unit != null && portrait != null;
        }
        if (unit == null) return;
        if (unitNameText != null) unitNameText.text = unit.Name;
        if (healthText != null) healthText.SetText("{0}/{1}", Mathf.RoundToInt(unit.CurrentHP), Mathf.RoundToInt(unit.MaxHP));
        if (attackText != null) attackText.SetText("{0}", unit.Stats.Get(StatType.ATK));
        if (defenseText != null) defenseText.SetText("{0}", unit.Stats.Get(StatType.DEF));
        SetSlider(healthSlider, unit.CurrentHP, unit.MaxHP);
        SetSlider(shieldSlider, unit.CurrentShield, unit.MaxHP);
        if (shieldSlider != null) shieldSlider.gameObject.SetActive(unit.HasShield);
        if (erSlider != null)
        {
            erSlider.gameObject.SetActive(unit.UsesResources);
            SetSlider(erSlider, unit.CurrentER, Unit.MaxER);
        }
        if (erText != null)
        {
            if (unit.UsesResources) erText.SetText("{0}/{1}", Mathf.RoundToInt(unit.CurrentER), Unit.MaxER);
            else erText.text = "";
        }
    }

    public void Hide() => Show(null);

    private static void SetSlider(Slider slider, float value, float maximum)
    {
        if (slider == null) return;
        slider.maxValue = maximum;
        slider.value = value;
    }
}
