/// <summary>
/// 스탯 종류.
///
/// 주의: 이 값들은 기존 ScriptableObject 에셋(EventData 등)에 정수로
/// 직렬화되어 있다. 순서를 바꾸거나 중간에 값을 끼워 넣으면
/// 기존 에셋이 다른 스탯을 가리키게 된다. 추가는 반드시 뒤에만 할 것.
/// </summary>
public enum StatType
{
    None      = 0,
    MaxHP     = 1,
    ATK       = 2,
    DEF       = 3,
    ER_Resist = 4,
}
