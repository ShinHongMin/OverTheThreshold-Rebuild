using UnityEngine;
using TMPro;

/// <summary>
/// 데미지 숫자 하나. 대상 위에서 떠오르며 사라진다.
///
/// 기존 FloatingText 프리팹을 그대로 재사용한다. 그 프리팹은 월드 오브젝트가 아니라
/// Canvas 자식이며, 대상의 월드 좌표를 WorldToScreenPoint로 변환해 위치를 잡는다.
/// 그래서 TextMeshPro(월드용)가 아니라 TMP_Text를 사용한다.
///
/// 반복 표시는 PrefabPool에서 대여하고 페이드가 끝나면 반환한다.
/// </summary>
public enum CombatNumberKind
{
    Damage, Healing, ShieldGain, ShieldLoss
}

public class DamageTextView : MonoBehaviour, IPooledView
{
    [SerializeField] private TMP_Text label;

    [Header("연출")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float fadeSpeed = 2.0f;
    [SerializeField] private float initialHeight = 1.5f;

    [SerializeField] private Color damageColor = new Color(1f, 0.25f, 0.25f);
    [SerializeField] private Color healingColor = new Color(0.3f, 1f, 0.45f);
    [SerializeField] private Color shieldColor = Color.white;
    private RectTransform _parentRect;
    private Canvas _canvas;
    private Vector3 _worldPosition;
    private float _alpha = 1f;
    private PrefabPoolManager _pool;
    private Camera _camera;

    public void SetPool(PrefabPoolManager pool) => _pool = pool;
    public void OnRent()
    {
        _alpha = 1f;
        _camera = Camera.main;
        _parentRect = transform.parent as RectTransform;
        _canvas = GetComponentInParent<Canvas>();
        if (label != null)
        {
            var color = label.color;
            color.a = 1f;
            label.color = color;
        }
    }

    public void OnReturn()
    {
        _camera = null;
        _parentRect = null;
        _canvas = null;
    }

    private void Reset()
    {
        label = GetComponentInChildren<TMP_Text>();
    }

    /// <summary>
    /// 표시할 값을 설정한다.
    /// 정수 변환은 여기서 한 번만 일어난다. Core는 float를 그대로 들고 있다.
    /// </summary>
    public void Show(Transform target, float amount, CombatNumberKind kind = CombatNumberKind.Damage, int row = 0)
    {
        _worldPosition = target.position + Vector3.up * (initialHeight + row * 0.5f);

        if (label != null)
        {
            label.color = kind == CombatNumberKind.Damage ? damageColor :
                kind == CombatNumberKind.Healing ? healingColor : shieldColor;
            bool gain = kind == CombatNumberKind.Healing || kind == CombatNumberKind.ShieldGain;
            label.SetText(gain ? "+{0}" : "-{0}", Mathf.RoundToInt(amount));
        }

        UpdateScreenPosition();
    }

    private void Update()
    {
        _worldPosition += Vector3.up * (moveSpeed * Time.deltaTime);

        _alpha -= fadeSpeed * Time.deltaTime;

        if (label != null)
        {
            Color c = label.color;
            c.a = _alpha;
            label.color = c;
        }

        if (_alpha <= 0f)
        {
            if (_pool != null)
                _pool.Return(gameObject);
            else
                gameObject.SetActive(false);
        }
    }

    // CameraManager의 LateUpdate 이동을 반영한 뒤 화면 좌표를 계산한다.
    private void LateUpdate() => UpdateScreenPosition();

    private void UpdateScreenPosition()
    {
        if (_camera == null)
            return;
        Vector3 screenPosition = _camera.WorldToScreenPoint(_worldPosition);
        if (label != null)
            label.enabled = screenPosition.z > 0;
        if (_parentRect == null || _canvas == null)
            return;
        Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_parentRect, screenPosition, uiCamera, out var localPoint))
            transform.localPosition = new Vector3(localPoint.x, localPoint.y, 0);
    }
}
