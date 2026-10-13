using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아군·적군의 기본 스탯, 보유 스킬 ID, AI 설정을 편집하는 에셋.
/// </summary>
[CreateAssetMenu(fileName = "UnitData", menuName = "OTT/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("식별")]
    [Tooltip("저장/로드 식별자. 표시 이름과 별개로 유지하며, 출시 후에는 변경하지 않습니다.")]
    public string unitId;
    public string unitName = "New Unit";
    [Tooltip("호버 상태창에 표시할 프로필 이미지")]
    public Sprite portraitSprite;

    [Tooltip("로비의 캐릭터 선택·편성칸에 표시할 일러스트")]
    public Sprite lobbyIllustration;

    [Tooltip("아군의 ECHO 처리 직군. 몬스터는 생성 시 자원·ECHO 사용이 차단되어 이 값을 사용하지 않는다")]
    public CharacterJob job = CharacterJob.Vanguard;

    [Header("기본 스탯")]
    public float baseHP = 100f;
    public float baseATK = 10f;
    public float baseDEF = 10f;

    [Tooltip("0 ~ 1 사이의 비율. 0.15면 ER 획득량 15% 감소")]
    [Range(0f, 1f)]
    public float baseERResist = 0f;

    [Header("스킬")]
    [Tooltip("SkillTable의 스킬 ID. ECHO 제어 불능 시에도 이 스킬이 쓰인다")]
    public string basicAttackId;

    [Tooltip("특수기·필살기 등. 몬스터마다 개수가 다르므로 목록으로 둔다")]
    public List<string> skillIds = new List<string>();

    [Header("레조넌스 — 아군 강화 스킬")]
    [Tooltip("ER 강화 상태에서 사용할 기본공격 ID. 비어 있으면 일반 기본공격을 유지한다")]
    public string enhancedBasicAttackId;
    [Tooltip("ER 강화 상태에서 사용할 특수기 ID. 비어 있으면 일반 특수기를 유지한다")]
    public string enhancedSpecialSkillId;
    [Tooltip("ER 강화 상태에서 사용할 필살기 ID. 비어 있으면 일반 필살기를 유지한다")]
    public string enhancedOverloadSkillId;

    [Header("AI — 적 전용")]
    [Tooltip("행동 규칙을 정하는 Brain 클래스. 아군은 None으로 둔다")]
    public MonsterBrainKind brainKind = MonsterBrainKind.None;

    [Tooltip("보유 스킬을 쓸 확률")]
    [Range(0f, 1f)]
    public float skillChance = 0.5f;

    [Tooltip("충전형: 충전 중 사용 / 추적형: 표식 대상에게 사용 / 보스: 강화기")]
    public string enhancedSkillId;

    [Tooltip("충전형: 충전 상태 버프 ID / 추적형: 표식 버프 ID")]
    public string stateBuffId;

    [Header("AI — 보스 전용")]
    [Tooltip("충전을 시작하는 스킬. 자기에게 실드를 건다")]
    public string chargeSkillId;

    [Tooltip("충전 중 실드가 깨졌을 때 쓰는 자체 그로기 스킬")]
    public string selfGroggySkillId;

    [Tooltip("매 턴 상대 전원에게 부여하는 ER. 0이면 사용하지 않는다")]
    public float passiveERPerTurn = 0f;
}
