

//공격 방식
public enum AttackType
{
    Melee,  // 근접
    Ranged,  // 원거리
    Support // 지원
}

/// <summary>
/// 맵 관련 노드 타입
/// </summary>
public enum NodeType
{
    Start,
    Battle,
    Elite,
    EliteHard,
    Event,
    Rest,
    Boss,
}
public enum NodeState
{
    Locked,   // 잠김 (갈 수 없음)
    Active,   // 활성 (갈 수 있음)
    Cleared,  // 클리어 (이미 지나옴)
    Current   // 현재 위치
}
/// <summary>
/// 카드 타입
/// </summary>
public enum CardRartiy
{
    Normal,
    Rare,
    Epic
}
public enum PassiveEffectType
{
    None,
    // [스탯 강화형]
    IncreaseATK_Percent,        
    IncreaseDEF_Percent,        
    IncreaseMaxHP_Percent, 
    IncreaseHeal_Percent,
    IncreaseShield_Percent,
    IncreaseER_Resist,

    BattleStart_SP,        
    BattleStart_OP,        

    Speical, // Legacy serialized value 9. Use the explicit effects below for card data.
    BattleStart_Heal = 10,
    BattleStart_Shield = 11,
    Damage_FullHP = 12,
    Damage_Debuffed = 13,
    ReceivedER_Reduction = 14,
    Echo_Resistance = 15,
    Echo_OP = 16,
}
//이벤트 사건
public enum EventEffectType
{
    None,

    HealHP_Percent,
    DamageHP_Percent,
    IncreaseER,
    DecreaseER,       

    GetRandomPassive,

    GetSP,              // SP 획득 추가
    GetOP,              // OP 획득 추가
    StartBattle,        // 전투 시작 추가
    BuffSingleRandom,    // 랜덤 1명 버프/디버프 (사건 5용)
    BuffAll
}
