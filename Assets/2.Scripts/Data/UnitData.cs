using UnityEngine;

/// <summary>
/// 유닛 하나의 기본 스탯. 아군·적군 공통으로 사용한다.
///
/// 기존에는 이 값들이 각 프리팹의 컨트롤러 컴포넌트에 기록되어 있어
/// 밸런싱 시 프리팹을 하나씩 열어야 했다. (기획서 문제 9)
/// 데이터로 빼면 한 폴더에서 전체를 비교할 수 있다.
///
/// 이 클래스는 ScriptableObject이므로 Core가 아니라 Data 계층에 있다.
/// Core는 이 타입을 모르며, UnitFactory가 값만 꺼내 Unit에 넘긴다.
/// </summary>
[CreateAssetMenu(fileName = "UnitData", menuName = "OTT/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("식별")]
    public string unitName = "New Unit";

    [Header("기본 스탯")]
    public float baseHP = 100f;
    public float baseATK = 10f;
    public float baseDEF = 10f;

    [Tooltip("0 ~ 1 사이의 비율. 0.15 이면 ER 획득량 15% 감소")]
    [Range(0f, 1f)]
    public float baseERResist = 0f;

    [Header("스킬")]
    [Tooltip("기본공격 배율. 추후 SkillData로 대체된다.")]
    public float basicAttackMultiplier = 1f;
}
