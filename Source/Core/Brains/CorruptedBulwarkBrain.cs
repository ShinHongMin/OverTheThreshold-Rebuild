/// <summary>
/// 오염된 방벽. 확률에 따라 자기 방어 스킬을 선택한다.
/// 실드와 도발 적용은 선택한 스킬의 효과가 처리한다.
/// </summary>
public sealed class CorruptedBulwarkBrain : MonsterBrain
{
    public CorruptedBulwarkBrain(IRandom random, SkillTable skillTable, MonsterBrainConfig config)
        : base(random, skillTable, config) { }

    protected override SkillSpec SelectSkill(CombatContext ctx, Unit self)
        => PickSkillOrBasic();
}
