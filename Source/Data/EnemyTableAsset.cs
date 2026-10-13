using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>적 ID에 대응하는 데이터와 프리팹을 한 곳에 등록한다. 편성들이 같은 조회 캐시를 사용한다.</summary>
[CreateAssetMenu(menuName = "Data/Enemy Table")]
public sealed class EnemyTableAsset : ScriptableObject
{
    [Serializable]
    public sealed class EnemyDefinition
    {
        public UnitData unitData;
        public UnitView unitViewPrefab;
    }

    [Tooltip("UnitData의 Unit Id가 조회 키다. 같은 종류는 한 번만 등록한다.")]
    [SerializeField] private EnemyDefinition[] enemies = Array.Empty<EnemyDefinition>();
    private Dictionary<string, EnemyDefinition> _enemiesById;
    private bool _isValid;

    public bool TryGet(string enemyId, out EnemyDefinition enemy)
    {
        enemy = null;
        if (string.IsNullOrWhiteSpace(enemyId)) return false;
        EnsureLookup();
        return _isValid && _enemiesById.TryGetValue(enemyId, out enemy);
    }

    private void EnsureLookup()
    {
        if (_enemiesById != null) return;
        _enemiesById = new Dictionary<string, EnemyDefinition>(enemies?.Length ?? 0, StringComparer.Ordinal);
        _isValid = enemies != null && enemies.Length > 0;
        if (!_isValid) return;
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.unitData == null || enemy.unitViewPrefab == null ||
                enemy.unitData.brainKind == MonsterBrainKind.None || string.IsNullOrWhiteSpace(enemy.unitData.unitId) ||
                _enemiesById.ContainsKey(enemy.unitData.unitId))
            {
                _enemiesById.Clear();
                _isValid = false;
                return;
            }
            _enemiesById.Add(enemy.unitData.unitId, enemy);
        }
    }

    private void OnEnable() => _enemiesById = null;
    private void OnValidate() => _enemiesById = null;
}
