using System.Collections.Generic;
using UnityEngine;

/// <summary>유닛 위와 상태창에서 공유하는 버프 목록. Inspector 데이터로 아이콘을 연결한다.</summary>
public sealed class BuffListView : MonoBehaviour
{
    [SerializeField] private BuffIconView iconPrefab;
    [SerializeField] private BuffData[] buffDefinitions;
    private readonly List<BuffIconView> _visibleIcons = new List<BuffIconView>();
    private readonly Dictionary<string, BuffData> _definitionsByBuffId = new Dictionary<string, BuffData>();
    private PrefabPoolManager _pool;
    private BuffTooltipView _tooltip;

    public void ConfigureTooltip(BuffTooltipView tooltip)
    {
        _tooltip = tooltip;
        foreach (var icon in _visibleIcons) if (icon != null) icon.ConfigureTooltip(tooltip);
    }

    public void ConfigurePool(PrefabPoolManager pool)
    {
        Clear();
        _pool = pool;
        // Unity Object.name 조회는 문자열을 할당하므로 전투 준비 때만 읽는다.
        _definitionsByBuffId.Clear();
        if (buffDefinitions != null)
            foreach (var definition in buffDefinitions)
                if (definition != null) _definitionsByBuffId[definition.name] = definition;
        if (_pool != null && iconPrefab != null) _pool.Prewarm(iconPrefab.gameObject, 32, 128);
    }

    public void Refresh(Unit unit)
    {
        if (!isActiveAndEnabled || _pool == null || iconPrefab == null) return;
        int visibleCount = 0;
        if (unit != null && unit.IsAlive)
        {
            var buffs = unit.Stats.Buffs;
            for (int index = 0; index < buffs.Count; index++)
            {
                var buff = buffs[index];
                if (buff.DurationTurns <= 0) continue;
                var definition = FindDefinition(buff.BuffId);
                // 미연결 효과를 다른 아이콘으로 오인하게 하지 않는다.
                if (definition == null || definition.icon == null) continue;
                if (visibleCount == _visibleIcons.Count)
                {
                    var rented = _pool.Rent(iconPrefab.gameObject, transform);
                    if (rented == null) break;
                    var icon = rented.GetComponent<BuffIconView>();
                    icon.ConfigureTooltip(_tooltip);
                    _visibleIcons.Add(icon);
                }
                _visibleIcons[visibleCount++].Show(buff, definition);
            }
        }
        for (int index = _visibleIcons.Count - 1; index >= visibleCount; index--)
        {
            _pool.Return(_visibleIcons[index].gameObject);
            _visibleIcons.RemoveAt(index);
        }
    }

    private BuffData FindDefinition(string buffId)
    {
        return _definitionsByBuffId.TryGetValue(buffId, out var definition) ? definition : null;
    }

    private void OnDisable() => Clear(false);
    private void Clear(bool moveToStorage = true)
    {
        if (_pool != null)
            foreach (var icon in _visibleIcons) if (icon != null) _pool.Return(icon.gameObject, moveToStorage);
        _visibleIcons.Clear();
    }
}
