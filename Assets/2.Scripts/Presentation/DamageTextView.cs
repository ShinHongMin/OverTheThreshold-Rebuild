using UnityEngine;
using TMPro;

/// <summary>
/// 데미지 숫자 하나. 대상 위에서 떠오르며 사라진다.
///
/// 기존 FloatingText 프리팹을 그대로 재사용한다. 그 프리팹은 월드 오브젝트가 아니라
/// Canvas 자식이며, 대상의 월드 좌표를 WorldToScreenPoint로 변환해 위치를 잡는다.
/// 그래서 TextMeshPro(월드용)가 아니라 TMP_Text를 사용한다.
///
/// 대상별 스택 오프셋과 오브젝트 풀은 W6에서 붙인다. 지금은 생성 후 파괴한다.
/// </summary>
public class DamageTextView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    [Header("연출")]
    [SerializeField] private float moveSpeed = 1.0f;
    [SerializeField] private float fadeSpeed = 2.0f;
    [SerializeField] private float initialHeight = 1.5f;

    private Transform _target;
    private Vector3 _worldPosition;
    private float _alpha = 1f;

    private void Reset()
    {
        label = GetComponentInChildren<TMP_Text>();
    }

    /// <summary>
    /// 표시할 값을 설정한다.
    /// 정수 변환은 여기서 한 번만 일어난다. Core는 float를 그대로 들고 있다.
    /// </summary>
    public void Show(Transform target, float amount)
    {
        _target = target;
        _worldPosition = target.position + Vector3.up * initialHeight;

        if (label != null)
            label.text = Mathf.RoundToInt(amount).ToString();

        UpdateScreenPosition();
    }

    private void Update()
    {
        _worldPosition += Vector3.up * (moveSpeed * Time.deltaTime);
        UpdateScreenPosition();

        _alpha -= fadeSpeed * Time.deltaTime;

        if (label != null)
        {
            Color c = label.color;
            c.a = _alpha;
            label.color = c;
        }

        if (_alpha <= 0f)
            Destroy(gameObject);
    }

    private void UpdateScreenPosition()
    {
        if (Camera.main == null) return;
        transform.position = Camera.main.WorldToScreenPoint(_worldPosition);
    }
}
