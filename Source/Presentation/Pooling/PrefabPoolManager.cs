using System.Collections.Generic;
using UnityEngine;

/// <summary>PoolUtil의 사전 생성/대여/반환 구조. 비활성 부모와 무관하게 대여 상태를 관리한다.</summary>
public sealed class PrefabPoolManager : MonoBehaviour
{
    [UnityEngine.Serialization.FormerlySerializedAs("poolDatas")]
    [SerializeField] private PrefabPoolSettings[] poolSettings = System.Array.Empty<PrefabPoolSettings>();
    private sealed class PrefabPoolState
    {
        public readonly Stack<PooledObjectEntry> Available = new Stack<PooledObjectEntry>();
        public int Count;
        public int Capacity;
    }
    private sealed class PooledObjectEntry
    {
        public GameObject Object;
        public PrefabPoolState OwnerPool;
        public IPooledView[] LifecycleCallbacks;
        public Vector3 PrefabLocalScale;
        public bool IsRented;
    }
    private readonly Dictionary<GameObject, PrefabPoolState> _poolsByPrefab = new Dictionary<GameObject, PrefabPoolState>();
    private readonly Dictionary<GameObject, PooledObjectEntry> _entriesByObject = new Dictionary<GameObject, PooledObjectEntry>();
    private Transform _inactiveStorage;
    public int CreatedCount { get; private set; }
    public int ActiveCount { get; private set; }
    public int PeakActiveCount { get; private set; }

    private void Awake()
    {
        foreach (var data in poolSettings)
            if (data != null) Prewarm(data.prefab, data.initialCount, data.capacity);
    }

    public void Prewarm(GameObject prefab, int initialCount = 8, int capacity = 64)
    {
        if (prefab == null) return;
        if (_inactiveStorage == null)
        {
            var storage = new GameObject("Pool storage");
            _inactiveStorage = storage.transform;
            _inactiveStorage.SetParent(transform, false);
            storage.SetActive(false);
        }
        if (!_poolsByPrefab.TryGetValue(prefab, out var prefabPool))
        {
            prefabPool = new PrefabPoolState { Capacity = Mathf.Max(1, capacity) };
            _poolsByPrefab.Add(prefab, prefabPool);
        }
        int count = Mathf.Clamp(initialCount, 0, prefabPool.Capacity);
        while (prefabPool.Count < count) prefabPool.Available.Push(Create(prefab, prefabPool));
    }

    private PooledObjectEntry Create(GameObject prefab, PrefabPoolState prefabPool)
    {
        var pooledObject = Instantiate(prefab, _inactiveStorage);
        pooledObject.SetActive(false);
        var lifecycleCallbacks = new List<IPooledView>();
        foreach (var component in pooledObject.GetComponentsInChildren<MonoBehaviour>(true))
            if (component is IPooledView lifecycleCallback) lifecycleCallbacks.Add(lifecycleCallback);
        var pooledEntry = new PooledObjectEntry { Object = pooledObject, OwnerPool = prefabPool, LifecycleCallbacks = lifecycleCallbacks.ToArray(), PrefabLocalScale = prefab.transform.localScale };
        _entriesByObject.Add(pooledObject, pooledEntry);
        prefabPool.Count++;
        CreatedCount++;
        return pooledEntry;
    }

    public GameObject Rent(GameObject prefab, Transform parent)
    {
        if (prefab == null) return null;
        if (!_poolsByPrefab.TryGetValue(prefab, out var prefabPool))
        {
            Prewarm(prefab);
            prefabPool = _poolsByPrefab[prefab];
        }
        PooledObjectEntry pooledEntry = null;
        while (prefabPool.Available.Count > 0)
        {
            var availableEntry = prefabPool.Available.Pop();
            if (availableEntry.Object != null)
            {
                pooledEntry = availableEntry;
                break;
            }
            // OnDisable에서 제자리 반환한 뒤 부모가 제거되면 자식도 파괴될 수 있다.
            // 사라진 항목을 용량에서도 제외해야 이후 대여가 상한에 막히지 않는다.
            _entriesByObject.Remove(availableEntry.Object);
            prefabPool.Count--;
        }
        if (pooledEntry == null)
        {
            if (prefabPool.Count >= prefabPool.Capacity) return null;
            pooledEntry = Create(prefab, prefabPool);
        }
        pooledEntry.IsRented = true;
        var objectTransform = pooledEntry.Object.transform;
        objectTransform.SetParent(parent, false);
        objectTransform.localPosition = Vector3.zero;
        objectTransform.localRotation = Quaternion.identity;
        objectTransform.localScale = pooledEntry.PrefabLocalScale;
        foreach (var lifecycleCallback in pooledEntry.LifecycleCallbacks) lifecycleCallback.OnRent();
        pooledEntry.Object.SetActive(true);
        ActiveCount++;
        PeakActiveCount = Mathf.Max(PeakActiveCount, ActiveCount);
        return pooledEntry.Object;
    }

    public bool Return(GameObject pooledObject, bool moveToStorage = true)
    {
        if (pooledObject == null || !_entriesByObject.TryGetValue(pooledObject, out var pooledEntry) || !pooledEntry.IsRented) return false;
        pooledEntry.IsRented = false;
        foreach (var lifecycleCallback in pooledEntry.LifecycleCallbacks) lifecycleCallback.OnReturn();
        pooledObject.SetActive(false);
        // 부모의 OnDisable 중에는 자식의 부모 변경이 금지된다. 이때는 대여 상태만
        // 반환하고, 다음 Rent가 새 부모를 설정하도록 둔다.
        if (moveToStorage) pooledObject.transform.SetParent(_inactiveStorage, false);
        pooledEntry.OwnerPool.Available.Push(pooledEntry);
        ActiveCount--;
        return true;
    }

    public void ReturnAll()
    {
        foreach (var pooledEntry in _entriesByObject.Values)
            if (pooledEntry.IsRented) Return(pooledEntry.Object);
    }
}
