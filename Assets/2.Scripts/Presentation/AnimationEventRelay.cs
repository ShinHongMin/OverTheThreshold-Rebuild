using UnityEngine;

/// <summary>
/// 애니메이션 클립에 박혀 있는 이벤트를 받는 창구.
///
/// 클립 116개가 아래 함수명을 문자열로 참조하고 있으므로 이름은 변경 금지다.
/// (오타 포함 원본 그대로 유지 — OnE_EntryAnnimationEnd 등)
///
/// 지금은 수신만 하고 아무것도 하지 않는다. "has no receiver" 경고를 없애는 것이 목적이다.
/// 실제 동작은 필요한 주차에 채운다.
///   OnAnimEvent_SpawnHitEffect — W2 (타격 이펙트 타이밍)
///   OnEntryAnimationEnd 계열   — W5 (등장 연출 완료 통지)
/// </summary>
public class AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private UnitView owner;
    public void OnAnimEvent_SpawnHitEffect() { }
    public void OnAnimEvent_SpawnEffect_temp() { }

    public void OnEntryAnimationEnd() => owner.OnEntryFinished();
    public void OnE_EntryAnnimationEnd() => owner.OnEntryFinished();

    public void OnEntryAnimation() { }
    public void OnEnhancedEntryAnimation() { }
    public void OnOpenComplete() { }


}
