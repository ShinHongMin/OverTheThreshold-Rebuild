/// <summary>
/// 스킬 분류. 자원 소비 방식이 여기에 따라 갈린다.
///
/// 주의: 기존 SkillData 에셋에 정수로 직렬화되어 있으므로 순서 변경 금지.
/// </summary>
public enum SkillType
{
    Basic = 0,
    Special = 1,
    Overload = 2,
    ECHO = 3,
}
