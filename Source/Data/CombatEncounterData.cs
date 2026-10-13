using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>전투에 등장할 적 ID와 슬롯 순서. 데이터와 프리팹은 공용 적 테이블에서 찾는다.</summary>
[CreateAssetMenu(menuName = "Data/Combat Encounter")]
public sealed class CombatEncounterData : ScriptableObject
{
    [Serializable]
    public sealed class EnemyEntry
    {
        [Tooltip("EnemyTable에 등록된 UnitData의 Unit Id")]
        public string enemyId;
        [Tooltip("CombatScene의 Run Enemy Spawn Points 번호. -1이면 편성 순서대로 배치한다.")]
        public int spawnSlotIndex = -1;
    }

    [SerializeField] private EnemyTableAsset enemyTable;
    [SerializeField] private EnemyEntry[] enemies = Array.Empty<EnemyEntry>();
    [Tooltip("전투 보상 후보를 선택할 종류. 맵 노드의 종류는 변경하지 않는다.")]
    [SerializeField] private NodeType battleType = NodeType.Battle;
    public IReadOnlyList<EnemyEntry> Enemies => enemies;
    public NodeType BattleType => battleType;

    public EnemyTableAsset.EnemyDefinition GetEnemy(int enemyIndex)
    {
        var entry = enemies[enemyIndex];
        if (entry == null || enemyTable == null || !enemyTable.TryGet(entry.enemyId, out var enemy))
            throw new InvalidOperationException("편성의 적 ID와 EnemyTable 연결을 확인하세요: " + entry?.enemyId);
        return enemy;
    }

    public int GetSpawnSlotIndex(int enemyIndex)
        => enemies[enemyIndex].spawnSlotIndex < 0 ? enemyIndex : enemies[enemyIndex].spawnSlotIndex;

    public bool IsConfigured
    {
        get
        {
            if (enemyTable == null || enemies == null || enemies.Length == 0) return false;
            for (int index = 0; index < enemies.Length; index++)
            {
                var enemy = enemies[index];
                if (enemy == null || !enemyTable.TryGet(enemy.enemyId, out _) || enemy.spawnSlotIndex < -1) return false;
                for (int previous = 0; previous < index; previous++)
                    if (GetSpawnSlotIndex(previous) == GetSpawnSlotIndex(index)) return false;
            }
            return true;
        }
    }
}
