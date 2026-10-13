using UnityEngine;

/// <summary>기존 셔터·기어 애니메이션과 전환 중 입력 차단. 씬이나 런 데이터는 알지 않는다.</summary>
public sealed class LoadingView : UiBase
{
    [SerializeField] private Animator animator;
    private static LoadingView _activeView;
    private static readonly int IdleState = Animator.StringToHash("LoadingIdle");
    private static readonly int ClosedState = Animator.StringToHash("CloseIdle");
    private static readonly int CloseTrigger = Animator.StringToHash("doClose");
    private static readonly int OpenTrigger = Animator.StringToHash("doOpen");
    public static bool IsBlockingInput => _activeView != null && _activeView.IsShown;
    public bool IsClosed => animator.GetCurrentAnimatorStateInfo(0).shortNameHash == ClosedState;
    public bool IsOpen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetActiveView() => _activeView = null;

    public void CloseScreen()
    {
        _activeView = this;
        IsOpen = false;
        Show();
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        animator.ResetTrigger(OpenTrigger);
        animator.ResetTrigger(CloseTrigger);
        animator.Play(IdleState, 0, 0f);
        animator.Update(0f);
        animator.SetTrigger(CloseTrigger);
    }

    public void OpenScreen() => animator.SetTrigger(OpenTrigger);

    // LoadingOpenScreen 클립 마지막 프레임의 기존 AnimationEvent가 호출한다.
    public void OnOpenComplete() => IsOpen = true;

    private void OnDestroy()
    {
        if (_activeView == this) _activeView = null;
    }
}
