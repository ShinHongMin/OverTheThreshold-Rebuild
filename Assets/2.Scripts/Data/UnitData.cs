using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛 하나의 기본 스탯과 보유 스킬. 아군·적군 공통으로 사용한다.
///
/// 기존에는 이 값들이 각 프리팹의 컨트롤러 컴포넌트에 기록되어 있어
/// 밸런싱 시 프리팹을 하나씩 열어야 했다. (기획서 문제 9)
///
/// 스킬은 SkillData 에셋을 인스펙터로 끌어다 놓는 대신 ID 문자열로 참조한다.
/// 에셋을 옮기거나 이름을 바꿔도 끊기지 않고, 세이브에 기록할 수 있다. (기획서 5.7)
/// </summary>
[CreateAssetMenu(fileName = "UnitData", menuName = "OTT/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("식별")]
    public string unitName = "New Unit";

    [Tooltip("ECHO 상태에서의 처리가 직군에 따라 달라진다")]
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

    [Tooltip("특수기·필살기·보스 패턴 등. 몬스터마다 개수가 다르므로 목록으로 둔다")]
    public List<string> skillIds = new List<string>();
}
