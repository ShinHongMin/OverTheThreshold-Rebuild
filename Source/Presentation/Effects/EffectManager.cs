using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>스킬 테이블의 VFX를 공용 풀에서 대여하고, 발사체 도착과 효과 반환을 관리한다.</summary>
public sealed class EffectManager : MonoBehaviour
{
    private PrefabPoolManager _pool;
    private readonly HashSet<GameObject> _preparedPrefabs = new HashSet<GameObject>();
    private readonly HashSet<GameObject> _projectilePrefabs = new HashSet<GameObject>();
    private readonly List<ProjectileView> _projectiles = new List<ProjectileView>(8);
    public bool HasActiveEffects
    {
        get
        {
            for (int i = 0; i < transform.childCount; i++)
                if (transform.GetChild(i).gameObject.activeSelf) return true;
            return false;
        }
    }

    public void Prepare(PrefabPoolManager pool, IReadOnlyList<SkillTableAsset> tables)
    {
        _pool = pool;
        _preparedPrefabs.Clear();
        _projectilePrefabs.Clear();
        foreach (var table in tables)
            if (table != null) foreach (var skill in table.skills)
                if (skill != null && skill.presentation != null)
                {
                    PreparePrefab(skill.presentation.effectPrefab);
                    PreparePrefab(skill.presentation.hitEffectPrefab);
                }
    }

    private void PreparePrefab(GameObject prefab)
    {
        if (_pool == null || prefab == null || !_preparedPrefabs.Add(prefab)) return;
        if (prefab.GetComponent<EffectPlayer>() == null)
        {
            Debug.LogWarning("VFX 풀 컴포넌트 연결이 필요합니다: " + prefab.name, prefab);
            _preparedPrefabs.Remove(prefab);
            return;
        }
        bool projectile = prefab.GetComponent<ProjectileView>() != null;
        if (projectile) _projectilePrefabs.Add(prefab);
        _pool.Prewarm(prefab, projectile ? 2 : 3, projectile ? 8 : 12);
    }

    public bool IsProjectile(GameObject prefab) => prefab != null && _projectilePrefabs.Contains(prefab);

    public void PlayEffect(GameObject prefab, Vector3 position, SortingGroup targetOrder, float speedScale)
    {
        if (prefab == null || !_preparedPrefabs.Contains(prefab) || IsProjectile(prefab)) return;
        var instance = _pool.Rent(prefab, transform);
        if (instance != null) instance.GetComponent<EffectPlayer>().Play(_pool, position, targetOrder, speedScale);
    }

    public void LaunchProjectile(GameObject prefab, Vector3 position, Transform target, SortingGroup targetOrder, float speed, float speedScale)
    {
        if (!IsProjectile(prefab) || target == null) return;
        var instance = _pool.Rent(prefab, transform);
        if (instance == null) return;
        instance.GetComponent<EffectPlayer>().Play(_pool, position, targetOrder, speedScale, waitForRelease: true);
        var projectile = instance.GetComponent<ProjectileView>();
        projectile.Launch(target, speed, speedScale);
        _projectiles.Add(projectile);
    }

    public IEnumerator WaitForProjectiles()
    {
        for (int i = 0; i < _projectiles.Count; i++)
        {
            var projectile = _projectiles[i];
            while (projectile != null && projectile.IsFlying) yield return null;
            if (projectile != null) _pool.Return(projectile.gameObject);
        }
        _projectiles.Clear();
    }

    public void ResetEffects(bool moveToStorage = true)
    {
        if (_pool != null)
            for (int i = transform.childCount - 1; i >= 0; i--)
                _pool.Return(transform.GetChild(i).gameObject, moveToStorage);
        _projectiles.Clear();
    }

    private void OnDisable() => ResetEffects(moveToStorage: false);
}
