using System.Collections.Generic;

/// <summary>
/// 몬스터의 스킬과 대상을 선택해 전투 커맨드를 만든다.
/// 몬스터별 규칙은 SelectSkill과 SelectTargets에서 재정의한다.
/// </summary>
public abstract class MonsterBrain : IActionBrain
{
    protected readonly IRandom Random;
    protected readonly SkillTable SkillTable;
    protected readonly MonsterBrainConfig Config;
    private readonly List<Unit> _candidates = new List<Unit>(6);

    protected MonsterBrain(IRandom random, SkillTable skillTable, MonsterBrainConfig config)
    {
        Random = random;
        SkillTable = skillTable;
        Config = config;
    }

    public virtual ICombatCommand Decide(CombatContext ctx, Unit self)
    {
        if (self == null || !self.IsAlive) return null;

        SkillSpec skill = SelectSkill(ctx, self);
        if (skill == null) return null;

        IReadOnlyList<Unit> targets = SelectTargets(ctx, self, skill);
        if (targets == null || targets.Count == 0) return null;

        return new UseSkillCommand(self, targets, skill);
    }

    /// <summary>상태를 갖는 몬스터만 재정의한다. 기본은 통지에 반응하지 않는다.</summary>
    public virtual void OnEvent(CombatEvent combatEvent) { }

    /// <summary>행동 결정·피격 반응에서 생긴 이벤트를 재생 목록에 넘긴다.</summary>
    public virtual void AppendPendingEvents(List<CombatEvent> destination) { }

    /// <summary>이번 턴에 쓸 스킬. 기본은 기본공격이다.</summary>
    protected virtual SkillSpec SelectSkill(CombatContext ctx, Unit self)
        => BasicAttack;

    /// <summary>
    /// 스킬의 대상. 기본은 타게팅 규칙에 맡기되,
    /// 단일 대상이면 살아 있는 상대 중 하나를 무작위로 고른다.
    /// </summary>
    protected virtual IReadOnlyList<Unit> SelectTargets(CombatContext ctx, Unit self, SkillSpec skill)
        => ResolveWith(ctx, self, skill, PickRandomOpponent(ctx, self));

    protected SkillSpec BasicAttack => FindSkill(Config.BasicAttackId);

    protected SkillSpec FindSkill(string skillId) => SkillTable?.Find(skillId);

    /// <summary>
    /// 확률에 걸리면 보유 스킬 중 하나를, 아니면 기본공격을 고른다.
    /// </summary>
    protected SkillSpec PickSkillOrBasic()
    {
        if (Config.SkillIds.Count > 0 && Random.Value01() < Config.SkillChance)
        {
            SkillSpec skill = FindSkill(Config.SkillIds[Random.Range(0, Config.SkillIds.Count)]);
            if (skill != null) return skill;
        }

        return BasicAttack;
    }

    /// <summary>대상 지정이 필요한 스킬이면 chosen을, 아니면 규칙에 맡긴다.</summary>
    protected static IReadOnlyList<Unit> ResolveWith(CombatContext ctx, Unit self, SkillSpec skill, Unit chosen)
    {
        return TargetingRule.NeedsManualTarget(skill.TargetType)
            ? TargetingRule.ResolveTargets(ctx, self, skill.TargetType, chosen)
            : TargetingRule.ResolveTargets(ctx, self, skill.TargetType, null);
    }

    protected Unit PickRandomOpponent(CombatContext ctx, Unit self)
    {
        var alive = Candidates(ctx, self);
        if (alive.Count == 0) return null;

        return alive[Random.Range(0, alive.Count)];
    }

    /// <summary>현재 체력이 가장 낮은 상대. 같으면 먼저 등록된 쪽.</summary>
    protected Unit FindLowestHP(CombatContext ctx, Unit self)
    {
        var alive = Candidates(ctx, self);
        if (alive.Count == 0) return null;

        Unit lowest = alive[0];

        for (int i = 1; i < alive.Count; i++)
            if (alive[i].CurrentHP < lowest.CurrentHP) lowest = alive[i];

        return lowest;
    }

    /// <summary>해당 버프가 걸린 살아 있는 상대. 없으면 null.</summary>
    protected Unit FindOpponentWithBuff(CombatContext ctx, Unit self, string buffId)
    {
        if (string.IsNullOrEmpty(buffId)) return null;

        var alive = Candidates(ctx, self);

        for (int i = 0; i < alive.Count; i++)
            if (alive[i].Stats.HasBuff(buffId)) return alive[i];

        return null;
    }

    protected static List<Unit> GetAliveOpponents(CombatContext ctx, Unit self)
    {
        var result = new List<Unit>();
        IReadOnlyList<Unit> opponents = ctx.GetOpponentsOf(self);

        for (int i = 0; i < opponents.Count; i++)
            if (opponents[i].IsAlive) result.Add(opponents[i]);

        return result;
    }

    private List<Unit> Candidates(CombatContext ctx, Unit self)
    {
        TargetingRule.FillSelectableTargets(ctx, self, TargetType.SingleEnemy, _candidates);
        return _candidates;
    }
}
