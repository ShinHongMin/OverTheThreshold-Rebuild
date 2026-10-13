using UnityEngine;

/// <summary>저장 실패 알림만 표시한다. 런 관리자와 함께 유지되며 같은 팝업을 재사용한다.</summary>
public sealed class SaveErrorPresenter : MonoBehaviour
{
    [SerializeField] private RunManager runManager;
    [SerializeField] private MessagePopupView messagePopup;
    private void Awake() => runManager.SaveFailed += ShowError;
    private void ShowError(string message) => messagePopup.Inform(message);
    private void OnDestroy() { if (runManager != null) runManager.SaveFailed -= ShowError; }
}
