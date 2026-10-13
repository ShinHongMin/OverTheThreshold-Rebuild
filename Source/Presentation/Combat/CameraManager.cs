using UnityEngine;

/// <summary>전투 카메라의 이동·줌·흔들림을 담당한다. 시작 구도는 씬에서 읽어 보존한다.</summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public sealed class CameraManager : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float moveSmoothTime = 0.2f;
    [SerializeField, Min(0.01f)] private float zoomSmoothTime = 0.2f;
    [Tooltip("두 유닛을 담는 구도의 화면 가장자리 여백. 하단 전투 UI를 고려한 값")]
    [SerializeField] private Vector2 framePadding = new Vector2(0.08f, 0.23f);
    private Camera _combatCamera;
    private Vector3 _defaultPosition, _targetPosition, _currentPosition, _moveVelocity;
    private float _defaultSize, _targetSize, _zoomVelocity;
    private float _shakeIntensity, _shakeDuration, _shakeElapsed;
    private bool _hasFocus, _initialized;
    private bool _isTransitioning;
    private Vector3 _transitionStartPosition;
    private float _transitionStartSize, _transitionDuration, _transitionElapsed;

    public float SpeedScale { get; set; } = 1f;
    public bool IsAtRest => !_initialized || (!_hasFocus && _shakeDuration <= 0f
        && !_isTransitioning && _currentPosition == _defaultPosition && _combatCamera.orthographicSize == _defaultSize);

    private void Awake()
    {
        _combatCamera = GetComponent<Camera>();
        _defaultPosition = transform.position;
        _defaultSize = _combatCamera.orthographicSize;
        _initialized = true;
        RestoreImmediately();
    }

    public void FocusOn(Vector3 position, float zoomRatio)
    {
        if (!_initialized || !isActiveAndEnabled)
            return;
        _isTransitioning = false;
        _targetPosition = new Vector3(position.x, position.y, _defaultPosition.z);
        _targetSize = Mathf.Max(0.1f, _defaultSize * Mathf.Max(0.1f, zoomRatio));
        _hasFocus = true;
    }

    /// <summary>단계 전환은 지정한 시간에 도달한다. 필요한 경우 두 유닛이 담기는 구도로 보정한다.</summary>
    public void MoveTo(Vector3 position, float zoomRatio, float duration, Bounds? visibleBounds = null)
    {
        if (!_initialized || !isActiveAndEnabled)
            return;
        position.z = _defaultPosition.z;
        float size = Mathf.Max(0.1f, _defaultSize * Mathf.Max(0.1f, zoomRatio));
        if (visibleBounds.HasValue)
        {
            Bounds bounds = visibleBounds.Value;
            float widthFraction = 1f - 2f * Mathf.Clamp(framePadding.x, 0f, 0.45f);
            float heightFraction = 1f - 2f * Mathf.Clamp(framePadding.y, 0f, 0.45f);
            float aspect = Mathf.Max(0.1f, _combatCamera.aspect);
            size = Mathf.Max(size, Mathf.Max(bounds.extents.x / (aspect * widthFraction), bounds.extents.y / heightFraction));
            // 대상 방향을 우선하되 한쪽 유닛이 잘릴 정도로 중심을 밀지 않는다.
            float halfWidth = size * aspect * widthFraction;
            float halfHeight = size * heightFraction;
            position.x = Mathf.Clamp(position.x, bounds.max.x - halfWidth, bounds.min.x + halfWidth);
            position.y = Mathf.Clamp(position.y, bounds.max.y - halfHeight, bounds.min.y + halfHeight);
        }
        _hasFocus = true;
        StartTransition(position, size, duration);
    }

    private void StartTransition(Vector3 position, float size, float duration)
    {
        _transitionStartPosition = _currentPosition;
        _transitionStartSize = _combatCamera.orthographicSize;
        _targetPosition = position;
        _targetSize = size;
        _transitionDuration = Mathf.Max(0f, duration);
        _transitionElapsed = 0f;
        _moveVelocity = Vector3.zero;
        _zoomVelocity = 0f;
        _isTransitioning = true;
    }

    public void Shake(float intensity, float duration)
    {
        if (!_initialized || !isActiveAndEnabled || intensity <= 0f || duration <= 0f)
            return;
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _shakeElapsed = 0f;
    }

    /// <summary>후딜레이·유닛 복귀 중 원래 구도로 돌아간다. 타격 흔들림은 끝까지 재생한다.</summary>
    public void ReturnToDefault()
    {
        if (!_initialized)
            return;
        _isTransitioning = false;
        _hasFocus = false;
        _targetPosition = _defaultPosition;
        _targetSize = _defaultSize;
    }

    public void ReturnToDefault(float duration)
    {
        if (!_initialized)
            return;
        if (!isActiveAndEnabled || IsAtRest)
        {
            RestoreImmediately();
            return;
        }
        _hasFocus = false;
        StartTransition(_defaultPosition, _defaultSize, duration);
    }

    /// <summary>씬/전투 초기화와 연출 중단 시 이동 잔여량과 흔들림을 모두 지운다.</summary>
    public void RestoreImmediately()
    {
        if (!_initialized || this == null || _combatCamera == null)
            return;
        ReturnToDefault();
        _currentPosition = _defaultPosition;
        _moveVelocity = Vector3.zero;
        _zoomVelocity = 0f;
        _shakeDuration = _shakeElapsed = 0f;
        transform.position = _defaultPosition;
        _combatCamera.orthographicSize = _defaultSize;
    }

    private void LateUpdate()
    {
        if (IsAtRest || Time.timeScale <= 0f)
            return;
        float deltaTime = Time.deltaTime * Mathf.Max(0f, SpeedScale);
        if (deltaTime <= 0f)
            return;
        // 흔들린 화면 좌표를 다음 이동 계산에 다시 넣지 않아 위치가 누적해서 밀리지 않는다.
        float size;
        if (_isTransitioning)
        {
            _transitionElapsed += deltaTime;
            float progress = _transitionDuration > 0f ? Mathf.Clamp01(_transitionElapsed / _transitionDuration) : 1f;
            float easedProgress = progress * progress * (3f - 2f * progress);
            _currentPosition = Vector3.Lerp(_transitionStartPosition, _targetPosition, easedProgress);
            size = Mathf.Lerp(_transitionStartSize, _targetSize, easedProgress);
            if (progress >= 1f)
                _isTransitioning = false;
        }
        else
        {
            _currentPosition = Vector3.SmoothDamp(_currentPosition, _targetPosition, ref _moveVelocity,
                moveSmoothTime, Mathf.Infinity, deltaTime);
            size = Mathf.SmoothDamp(_combatCamera.orthographicSize, _targetSize, ref _zoomVelocity,
                zoomSmoothTime, Mathf.Infinity, deltaTime);
        }
        if ((_currentPosition - _targetPosition).sqrMagnitude < 0.000001f)
        {
            _currentPosition = _targetPosition;
            _moveVelocity = Vector3.zero;
        }
        if (Mathf.Abs(size - _targetSize) < 0.0001f)
        {
            size = _targetSize;
            _zoomVelocity = 0f;
        }

        Vector3 shakeOffset = Vector3.zero;
        if (_shakeDuration > 0f)
        {
            _shakeElapsed += deltaTime;
            if (_shakeElapsed >= _shakeDuration)
                _shakeDuration = 0f;
            else
            {
                float strength = _shakeIntensity * (1f - _shakeElapsed / _shakeDuration);
                float phase = _shakeElapsed * 45f;
                // UnityEngine.Random/전투 IRandom을 소비하지 않는 연출용 노이즈.
                shakeOffset = new Vector3(Mathf.PerlinNoise(phase, 11f) * 2f - 1f,
                    Mathf.PerlinNoise(29f, phase) * 2f - 1f, 0f) * strength;
            }
        }
        transform.position = _currentPosition + shakeOffset;
        _combatCamera.orthographicSize = size;
    }

    private void OnDisable() => RestoreImmediately();
}
