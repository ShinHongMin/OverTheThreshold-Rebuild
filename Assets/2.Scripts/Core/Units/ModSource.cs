/// <summary>
/// 스탯 보정의 출처. 기존 CharactersStat이 bonus_ / added_ 두 갈래로
/// 나눠 들고 있던 것을 하나로 합치되, 제거 시 구분할 수 있도록 표시한다.
///
/// 버프는 수명과 제거 방식이 달라 여기 포함하지 않고 ActiveBuff로 따로 관리한다.
/// </summary>
public enum ModSource
{
    /// <summary>패시브 카드. 런 전체 유지 (기존 bonus_X_Percent)</summary>
    PassiveCard = 0,

    /// <summary>세이브 데이터의 영구 보너스 (기존 added_X_Percent)</summary>
    MetaProgress = 1,
}
