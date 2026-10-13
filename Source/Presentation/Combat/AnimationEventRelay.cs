using UnityEngine;

/// <summary>
/// 애니메이션 클립에 박혀 있는 이벤트를 받는 창구.
///
/// 클립 116개가 아래 함수명을 문자열로 참조하고 있으므로 이름은 변경 금지다.
/// (오타 포함 원본 그대로 유지 — OnE_EntryAnnimationEnd 등)
///
/// 등장 완료와 일반/강화 전환 요청은 UnitView에 전달한다.
/// 타격/발사 이벤트는 UnitView의 현재 행동 대기를 해제한다. 결과 계산은 App에서 한다.
/// </summary>
public class AnimationEventRelay : MonoBehaviour
{
    [SerializeField] private UnitView owner;
    public void OnAnimEvent_SpawnHitEffect() => owner?.OnSkillImpact();
    public void OnAnimEvent_SpawnEffect_temp() => owner?.OnSkillImpact();

    public void OnEntryAnimationEnd() => owner.OnEntryFinished();
    public void OnE_EntryAnnimationEnd() => owner.OnEntryFinished();

    public void OnEntryAnimation() => owner.RequestNormalEntry();
    public void OnEnhancedEntryAnimation() => owner.RequestEnhancedEntry();
    public void OnOpenComplete() { }


}
