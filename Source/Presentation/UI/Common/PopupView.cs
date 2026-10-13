using System;
using UnityEngine;

/// <summary>팝업별 표시 수명과 닫기 정책. 일시정지와 전투 입력은 팝업 관리자가 담당한다.</summary>
public class PopupView : UiBase
{
    [SerializeField] private bool pauseCombat;
    [SerializeField] private bool closeOnEscape = true;
    [SerializeField] private RectTransform inputBlocker;
    private RectTransform _canvasTransform;
    private Vector2 _canvasSize;
    public bool PauseCombat => pauseCombat;
    public bool CloseOnEscape => closeOnEscape;
    public event Action<PopupView> Closed;
    public override void Show(float duration = 0f)
    {
        base.Show(duration);
        if (inputBlocker == null) return;
        _canvasTransform = (RectTransform)GetComponentInParent<Canvas>().transform;
        FitInputBlocker();
    }
    protected override void Update()
    {
        base.Update();
        if (IsShown && _canvasTransform != null && _canvasSize != _canvasTransform.rect.size) FitInputBlocker();
    }
    private void FitInputBlocker()
    {
        // 창 외형은 유지하고 배경 차단 영역만 화면 크기에 맞춘다.
        var parent = inputBlocker.parent;
        Vector3 minimum = parent.InverseTransformPoint(_canvasTransform.TransformPoint(_canvasTransform.rect.min));
        Vector3 maximum = parent.InverseTransformPoint(_canvasTransform.TransformPoint(_canvasTransform.rect.max));
        inputBlocker.anchorMin = inputBlocker.anchorMax = new Vector2(.5f, .5f);
        inputBlocker.pivot = new Vector2(.5f, .5f);
        inputBlocker.sizeDelta = maximum - minimum;
        inputBlocker.localPosition = (minimum + maximum) * .5f;
        _canvasSize = _canvasTransform.rect.size;
    }
    protected virtual void OnDisable() => Closed?.Invoke(this);
}
