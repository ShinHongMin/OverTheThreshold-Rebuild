using UnityEngine;

/// <summary>UI_INITIAL의 CanvasGroup 표시 수명. 페이드 중 재호출도 현재 값에서 이어진다.</summary>
[RequireComponent(typeof(CanvasGroup))]
public class UiBase : MonoBehaviour
{
    private CanvasGroup _canvas;
    private float _targetAlpha;
    private float _speed;
    public bool IsShown { get; private set; }
    private void EnsureCanvas() { if (_canvas == null) _canvas = GetComponent<CanvasGroup>(); }
    public virtual void Show(float duration = 0f)
    {
        EnsureCanvas();
        gameObject.SetActive(true);
        IsShown = true;
        _canvas.interactable = true;
        _canvas.blocksRaycasts = true;
        SetFade(1f, duration);
    }
    public virtual void Hide(float duration = 0f)
    {
        EnsureCanvas();
        IsShown = false;
        _canvas.interactable = false;
        _canvas.blocksRaycasts = false;
        SetFade(0f, duration);
    }
    private void SetFade(float target, float duration)
    {
        _targetAlpha = target;
        _speed = duration > 0f ? 1f / duration : 0f;
        if (_speed == 0f)
        {
            _canvas.alpha = target;
            if (!IsShown) gameObject.SetActive(false);
        }
    }
    protected virtual void Update()
    {
        if (_canvas == null || _speed <= 0f) return;
        _canvas.alpha = Mathf.MoveTowards(_canvas.alpha, _targetAlpha, _speed * Time.unscaledDeltaTime);
        if (_canvas.alpha != _targetAlpha) return;
        _speed = 0f;
        if (!IsShown) gameObject.SetActive(false);
    }
}
