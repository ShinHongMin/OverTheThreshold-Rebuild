using UnityEngine;

[CreateAssetMenu(fileName = "NewSkill", menuName = "Skill/Skill Data")]
public class SkillData : ScriptableObject
{

    [Header("기본 정보 (UI 표시용)")]
    public string skillName;
    public string description;
    public Sprite skillIcon;

    [Header("스킬 타입")]
    public SkillType type;

    [Header("버프 정보 (이 스킬 사용 시 적용)")]
    public BuffData[] buffToApply;

    [Header("근거리/원거리/지원")]
    public AttackType attackType;

    [Header("타겟팅 종류(아군,단일 적 등)")]
    public TargetType targetType;

    [Header("핵심 계산식(CombatCalculator)용")]
    public float skillMultiplier; 

    [Header("자원(ResourceManager)용, 양수만")]
    public int spCost;
    public int opCost; 

    [Header("ER(ERManager)용, 양수만")]
    public float erGain; 
    public float erReduce; 

    [Header("연출 시간 (Timing)")]
    [Tooltip("애니메이션 시작부터 '타격 판정'까지 걸리는 시간")]
    public float animationTime = 0.5f;
    [Tooltip("카메라가 줌인하거나 타겟을 잡을 때 필요한 선딜레이 (기본 0.5초)")]
    public float preDelay = 0.5f;
    [Tooltip("때리고 나서 상황을 지켜볼 후딜레이 (기본 0.5f ~ 1.0f)")]
    public float postDelay = 0.8f;
    [Tooltip("연출이 모두 끝나고 다음 행동으로 넘어가기까지의 총 시간")]
    public float totalDuration = 1.5f;

    [Header("연출 이펙트 (VFX)")]
    [Tooltip("세이렌의 총알, 아이기스 발동 이펙트 등")]
    public GameObject effectPrefab;
    [Tooltip("타격 지점에서 터지는 이펙트")]
    public GameObject hitEffectPrefab;

    [Header("연출 - 카메라 흔들림")]
    public bool useShake; 
    public float shakeIntensity = 0.2f;
    public float shakeDuration = 0.2f;

    [Header("연출 - 카메라 줌인")]
    public float cameraZoomAmount = 0.8f;

    [Header("Sound Effect")]
    public AudioClip skillSoundEffect;
}