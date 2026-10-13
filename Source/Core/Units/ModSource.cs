/// <summary>
/// 스탯 보정을 일괄 제거할 때 구분하는 출처.
/// 지속 턴이 있는 전투 버프는 ActiveBuff로 별도 관리한다.
/// </summary>
public enum ModSource
{
    /// <summary>패시브 카드로 얻은 보정. 런 전체에 유지된다.</summary>
    PassiveCard = 0,

    /// <summary>저장·복원하는 지속 보정.</summary>
    MetaProgress = 1,
}
