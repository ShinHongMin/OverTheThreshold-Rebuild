/// <summary>
/// 유실된 정찰병. 확률에 따라 특수기 또는 기본공격을 선택한다.
/// </summary>
public sealed class LostScoutBrain : MonsterBrain
{
    public LostScoutBrain(IRandom random, SkillTable skillTable, MonsterBrainConfig config)
        : base(random, skillTable, config) { }

    protected override SkillSpec SelectSkill(CombatContext ctx, Unit self)
        => PickSkillOrBasic();
}
