/// <summary>
/// 버프 종류. 기존 GameEnum.BuffType을 Core로 옮긴 것이다.
///
/// 주의: 이 값들은 기존 BuffData 에셋에 정수로 직렬화되어 있다.
/// 순서를 바꾸거나 중간에 값을 끼워 넣으면 기존 에셋이 다른 버프를 가리키게 된다.
/// 추가는 반드시 뒤에만 할 것.
///
/// 처리 위치가 셋으로 갈린다.
///   ATK_Percent / DEF_Percent / ER_Resist_Percent → 스탯 계산 (UnitStats)
///   Damage_Taken_Percent                         → 데미지 계산 (damageAmp)
///   Taunt                                        → 타게팅 규칙
/// </summary>
public enum BuffType
{
    ATK_Percent          = 0,
    DEF_Percent          = 1,
    Damage_Taken_Percent = 2,
    ER_Resist_Percent    = 3,
    Taunt                = 4,
}
