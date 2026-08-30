using System;
using UnityEngine;

/// <summary>
/// 스킬의 연출 정보. 기존 SkillData에 섞여 있던 연출 필드를 분리한 것이다.
///
/// Core는 이 클래스를 모른다. "무엇이 일어나는가"는 SkillEffect가,
/// "어떻게 보이는가"는 여기가 담당한다.
/// Presentation이 SkillId로 조회해 사용한다.
/// </summary>
[Serializable]
public class SkillPresentation
{
    [Header("연출 분기")]
    [Tooltip("근접이면 대상에게 이동한 뒤 타격한다")]
    public AttackType attackType = AttackType.Melee;

    [Header("타이밍")]
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

    [Header("카메라")]
    public bool useShake;
    public float shakeIntensity = 0.2f;
    public float shakeDuration = 0.2f;
    public float cameraZoomAmount = 0.8f;

    [Header("사운드")]
    public AudioClip skillSoundEffect;
}
