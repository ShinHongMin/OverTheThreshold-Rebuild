using UnityEngine;
using UnityEngine.Rendering;

/// <summary>스프라이트 VFX 한 개를 처음부터 재생하고 종료 시 공용 풀에 반환한다.</summary>
[RequireComponent(typeof(Animator), typeof(SortingGroup))]
public sealed class EffectPlayer : MonoBehaviour, IPooledView
{
    [SerializeField, Min(0.1f)] private float maxLifetime = 5f;
    private Animator _animator;
    private SortingGroup _sortingGroup;
    private PrefabPoolManager _pool;
    private float _elapsed;
    private bool _isPlaying, _waitForRelease;
    public float SpeedScale { get; set; } = 1f;

    public void OnRent()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_sortingGroup == null) _sortingGroup = GetComponent<SortingGroup>();
        _elapsed = 0f;
        _isPlaying = false;
    }

    public void Play(PrefabPoolManager pool, Vector3 position, SortingGroup targetOrder, float speedScale, bool waitForRelease = false)
    {
        _pool = pool;
        transform.position = position;
        _sortingGroup.sortingLayerID = targetOrder != null ? targetOrder.sortingLayerID : 0;
        _sortingGroup.sortingOrder = targetOrder != null ? targetOrder.sortingOrder + 1 : 2000;
        SpeedScale = Mathf.Max(0f, speedScale);
        _waitForRelease = waitForRelease;
        _animator.speed = SpeedScale;
        _animator.Play(0, 0, 0f);
        _animator.Update(0f);
        _isPlaying = true;
    }

    private void Update()
    {
        if (!_isPlaying || Time.timeScale <= 0f) return;
        _animator.speed = Mathf.Max(0f, SpeedScale);
        _elapsed += Time.deltaTime * _animator.speed;
        if (!_waitForRelease && (_elapsed >= maxLifetime ||
            (_animator.runtimeAnimatorController != null && _animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)))
            _pool.Return(gameObject);
    }

    public void OnReturn()
    {
        _isPlaying = false;
        _waitForRelease = false;
        _animator.speed = 1f;
        _pool = null;
    }

    private void OnDisable()
    {
        if (_pool != null) _pool.Return(gameObject, moveToStorage: false);
    }
}
