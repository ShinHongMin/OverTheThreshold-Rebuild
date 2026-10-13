/// <summary>
/// 속삭이는 메아리. 유실된 정찰병과 같은 선택 규칙에 ER 공격 스킬을 사용한다.
/// </summary>
public sealed class WhisperingEchoBrain : MonsterBrain
{
    public WhisperingEchoBrain(IRandom random, SkillTable skillTable, MonsterBrainConfig config)
        : base(random, skillTable, config) { }

    protected override SkillSpec SelectSkill(CombatContext ctx, Unit self)
        => PickSkillOrBasic();
}
