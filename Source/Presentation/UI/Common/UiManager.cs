using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>씬에 조립된 패널을 캐싱한다. 반복 열기에는 Instantiate/Destroy가 없다.</summary>
public sealed class UiManager : MonoBehaviour
{
    [SerializeField] private UiBase[] panels = Array.Empty<UiBase>();
    private readonly Dictionary<Type, UiBase> _cache = new Dictionary<Type, UiBase>();
    private void Awake()
    {
        foreach (var panel in panels) if (panel != null) Register(panel);
    }
    public void Register(UiBase panel) => _cache[panel.GetType()] = panel;
    public T Get<T>() where T : UiBase
        => _cache.TryGetValue(typeof(T), out var panel) && panel != null ? (T)panel : null;
    public T Show<T>() where T : UiBase
    {
        var panel = Get<T>();
        if (panel == null) return null;
        panel.Show();
        return (T)panel;
    }
    public void Hide<T>() where T : UiBase
    {
        var panel = Get<T>();
        if (panel != null) panel.Hide();
    }
}
