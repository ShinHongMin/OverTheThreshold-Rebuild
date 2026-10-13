using UnityEngine;

/// <summary>발사체의 이동·도착 표시만 담당한다. 피해 계산은 전투 진행자가 도착을 기다린 뒤 수행한다.</summary>
[RequireComponent(typeof(EffectPlayer))]
public sealed class ProjectileView : MonoBehaviour, IPooledView
{
    [SerializeField, Min(0.1f)] private float maxFlightTime = 5f;
    private Transform _target;
    private float _speed, _elapsed;
    public float SpeedScale { get; set; } = 1f;
    public bool IsFlying
    {
        get; private set;
    }

    public bool HasArrived
    {
        get; private set;
    }

    public void Launch(Transform target, float speed, float speedScale)
    {
        _target = target;
        _speed = Mathf.Max(0.1f, speed);
        SpeedScale = Mathf.Max(0f, speedScale);
        IsFlying = target != null;
        FaceTarget();
    }

    private void FaceTarget()
    {
        if (_target == null)
            return;
        Vector3 direction = _target.position - transform.position;
        if (direction.sqrMagnitude > 0.000001f)
            transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, Vector3.forward);
    }

    private void Update()
    {
        if (!IsFlying || Time.timeScale <= 0f)
            return;
        if (_target == null || !_target.gameObject.activeInHierarchy)
        {
            IsFlying = false;
            return;
        }
        float deltaTime = Time.deltaTime * Mathf.Max(0f, SpeedScale);
        if (deltaTime <= 0f)
            return;
        _elapsed += deltaTime;
        FaceTarget();
        transform.position = Vector3.MoveTowards(transform.position, _target.position, _speed * deltaTime);
        if ((transform.position - _target.position).sqrMagnitude <= 0.0001f)
        {
            transform.position = _target.position;
            HasArrived = true;
            IsFlying = false;
        }
        else if (_elapsed >= maxFlightTime)
            IsFlying = false;
    }

    public void OnRent()
    {
        _elapsed = 0f;
        HasArrived = false;
        IsFlying = false;
        _target = null;
    }

    public void OnReturn()
    {
        IsFlying = false;
        _target = null;
    }
}
