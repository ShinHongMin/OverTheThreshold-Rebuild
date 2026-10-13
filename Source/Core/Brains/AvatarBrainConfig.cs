/// <summary>
/// 보스 전용 설정값. 일반 몬스터와 달리 스킬 역할이 정해져 있어 별도로 둔다.
/// </summary>
public sealed class AvatarBrainConfig
{
    /// <summary>충전을 시작하는 스킬. 자기에게 실드를 건다.</summary>
    public string ChargeSkillId { get; }

    /// <summary>충전 중 실드가 남아 있을 때 쓰는 강화기.</summary>
    public string EnhancedSkillId { get; }

    /// <summary>충전 중 실드가 깨졌을 때 쓰는 자체 그로기 스킬.</summary>
    public string SelfGroggySkillId { get; }

    /// <summary>매 턴 상대 전원에게 부여하는 ER. 0이면 사용하지 않는다.</summary>
    public float PassiveERPerTurn { get; }

    public AvatarBrainConfig(
        string chargeSkillId,
        string enhancedSkillId,
        string selfGroggySkillId,
        float passiveERPerTurn = 0f)
    {
        ChargeSkillId = chargeSkillId;
        EnhancedSkillId = enhancedSkillId;
        SelfGroggySkillId = selfGroggySkillId;
        PassiveERPerTurn = passiveERPerTurn;
    }
}
