using System;
using UnityEngine;

public enum SkillSoundTiming { AnimationStart, EffectApplied }
public enum SkillImpactTiming { AnimationTime, AnimationEvent }
public enum SkillEffectAnchor { EventDefault = 0, Hit = 1, Buff = 2 }
// 기존 에셋에 저장된 번호를 유지한다. 제거한 Automatic의 0은 재사용하지 않는다.
public enum SkillCameraFocus { Caster = 1, Targets = 2, None = 3, CasterAndTargets = 4 }

/// <summary>시전→발사/타격→복귀 구도를 스킬마다 명시한다. Enabled인 스킬에만 적용한다.</summary>
[Serializable]
public class SkillCameraSequence
{
    public bool enabled;
    public SkillCameraFocus castFocus = SkillCameraFocus.Caster;
    [Min(0.1f)] public float castZoomRatio = 0.9f;
    [Min(0f)] public float castMoveDuration = 0.25f;
    public SkillCameraFocus attackFocus = SkillCameraFocus.Targets;
    [Tooltip("공격 구도 중심에 더할 월드 좌표 보정. 유닛이나 슬롯 위치는 바꾸지 않는다")]
    public Vector2 attackFocusOffset;
    [Min(0.1f)] public float attackZoomRatio = 1f;
    [Min(0f)] public float attackMoveDuration = 0.25f;
    [Tooltip("켜면 모델 중심을 기준으로 공격 구도에 두 유닛을 담도록 보정한다. 끄면 유닛 위치와 지정한 줌을 그대로 사용한다")]
    public bool keepUnitsInAttackFrame = true;
    [Tooltip("명중 화면을 유지하는 시간. 기존 후딜레이 안에서 배분한다")]
    [Min(0f)] public float impactHoldDuration = 0.15f;
    [Min(0f)] public float returnMoveDuration = 0.35f;
}

/// <summary>
/// 스킬의 애니메이션, 카메라, 이펙트, 사운드 설정.
/// 전투 계산과 분리하며, Presentation이 SkillId로 조회한다.
/// </summary>
[Serializable]
public class SkillPresentation
{
    [Header("연출 분기")]
    [Tooltip("근접이면 대상에게 이동한 뒤 타격한다")]
    public AttackType attackType = AttackType.Melee;

    [Tooltip("비우면 기본/특수/필살기 분류를 따른다. 다른 동작을 쓸 때 Animator의 Trigger 이름을 지정한다.")]
    public string animationTrigger;
    [Tooltip("버프가 상태 동작을 제어하는 스킬은 켠다. 예: 보스 자체 그로기")]
    public bool skipSkillAnimation;

    [Header("타이밍")]
    [Tooltip("AnimationEvent는 클립의 타격/발사 이벤트를 기다린다. 이벤트가 없는 스킬은 AnimationTime 사용")]
    public SkillImpactTiming impactTiming = SkillImpactTiming.AnimationTime;
    [Tooltip("클립 이벤트가 누락되었을 때 진행을 복구할 최대 대기 시간. Animator의 실제 재생 시간 기준")]
    [Min(0.1f)] public float impactEventTimeout = 5f;
    [Tooltip("애니메이션 시작부터 타격 판정까지")]
    public float animationTime = 0.5f;

    [Tooltip("카메라 줌인 등 선딜레이")]
    public float preDelay = 0.5f;

    [Tooltip("타격 후 후딜레이")]
    public float postDelay = 0.8f;

    [Tooltip("연출이 끝나고 다음 행동으로 넘어가기까지의 총 시간")]
    public float totalDuration = 1.5f;

    [Header("이펙트")]
    public GameObject effectPrefab;
    public GameObject hitEffectPrefab;
    [Tooltip("EventDefault: 피해는 HitPos, 지원 효과는 BuffPos. 외형상 다른 기준점이 필요한 스킬은 직접 지정한다.")]
    public SkillEffectAnchor effectAnchor = SkillEffectAnchor.EventDefault;
    [Tooltip("Effect Prefab에 ProjectileView가 연결된 스킬의 월드 이동 속도")]
    [Min(0.1f)] public float projectileSpeed = 20f;

    [Header("카메라")]
    [Tooltip("스킬마다 직접 지정한다. Caster: 시전자. Targets: 생존 대상들의 중심. None: 기본 구도 유지")]
    public SkillCameraFocus cameraFocus = SkillCameraFocus.Caster;
    public bool useShake;
    public float shakeIntensity = 0.2f;
    public float shakeDuration = 0.2f;
    [Tooltip("씬의 기본 카메라 크기에 곱할 비율. 0.8이면 확대, 1이면 원래 크기")]
    [Min(0.1f)] public float cameraZoomAmount = 0.8f;

    [Header("단계별 카메라 (Enabled인 스킬만 적용)")]
    public SkillCameraSequence cameraSequence = new SkillCameraSequence();

    [Header("사운드")]
    [Tooltip("미연결이면 소리 없이 진행한다. 일반/강화 스킬마다 별도로 지정한다.")]
    public AudioClip skillSoundEffect;
    public SkillSoundTiming soundTiming = SkillSoundTiming.AnimationStart;
    [Range(0f, 1f)] public float soundVolume = 1f;
}
