using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 에디터에서 편집하는 스킬 정의. SkillTableAsset 안에 목록으로 들어간다.
///
/// Core의 SkillSpec과 필드가 대응하며, SkillTableBuilder가 변환한다.
/// 두 벌로 나눈 이유는 [SerializeReference]가 UnityEngine 속성이기 때문이다.
/// 이 속성을 Core에 두면 Core가 UnityEngine을 참조하게 되어
/// "Core는 엔진에 의존하지 않는다"는 규칙을 컴파일러가 강제할 수 없게 된다.
/// </summary>
[Serializable]
public class SkillDefinition
{
    [Header("식별")]
    [Tooltip("고유 ID. 기존 SkillData 에셋 이름을 그대로 쓴다.")]
    public string skillId;

    [Tooltip("화면에 표시할 이름")]
    public string displayName;

    [TextArea(2, 4)]
    public string description;

    public Sprite skillIcon;

    [Header("분류")]
    public SkillType type = SkillType.Basic;
    public TargetType targetType = TargetType.SingleEnemy;

    [Header("자원")]
    [Tooltip("기본공격은 소모가 아니라 획득량으로 쓰인다")]
    public int spCost;

    public int opCost;

    [Tooltip("ER을 자원으로 쓰는 스킬(세이렌)에만 0보다 크게. 사용 조건이자 소모량")]
    public float erCost;

    [Header("효과")]
    [Tooltip("위에서부터 순서대로 적용된다")]
    [SerializeReference]
    public List<SkillEffect> effects = new List<SkillEffect>();

    [Header("연출")]
    public SkillPresentation presentation = new SkillPresentation();
}
