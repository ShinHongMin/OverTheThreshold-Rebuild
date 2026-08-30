
//게임 상태
public enum GameState
{
    Title,         // 타이틀 화면
    MainMenu,// 메인 로비
    Playing,     // 인게임 (전투)
    Paused,     // 일시정지
    GameOver    // 게임 오버
}

//몬스터 등급
public enum MonsterRarity
{
    Normal,
    Elite,
    Boss,
}
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

    Speical,
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
//데미지 텍스트 색깔
public enum FloatingTextType
{
    /// <summary>
    /// 일반 데미지 피격 시 (색상: 밝은 빨강)
    /// </summary>
    Damage,
    /// <summary>
    /// 체력 회복 시 (색상: 연두색)
    /// </summary>
    Heal,
    /// <summary>
    /// 쉴드 획득 시 (색상: 하얀색)
    /// </summary>
    Shield,
    /// <summary>
    /// ER(스트레스) 수치 증가 (색상: 보라색)
    /// </summary>
    ER_Up,
    /// <summary>
    /// ER(스트레스) 수치 감소/안정 (색상: 청록색)
    /// </summary>
    ER_Down,

    /// <summary>
    /// 이로운 효과/버프 획득 (색상: 하늘색)
    /// </summary>
    Buff,
    /// <summary>
    /// 해로운 효과/디버프 적용 (색상: 자주색)
    /// </summary>
    Debuff,

    /// <summary>
    /// 플레이어 행동 불가/실패 경고 (비용 부족, 쿨타임, 잘못된 타겟 등) (색상: 굵은 노란색)
    /// </summary>
    Warning,
    /// <summary>
    /// 게임 시스템 알림 (턴 시작, 스테이지 클리어 등) (색상: 흰색)
    /// </summary>
    System,
    /// <summary>
    /// 캐릭터 사망 시,ECHO등 행동불능 상태 (색상: 아주 큰 진한 빨강)
    /// </summary>
    Stuned
}