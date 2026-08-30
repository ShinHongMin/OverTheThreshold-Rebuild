using System.Collections.Generic;

/// <summary>
/// ECHO 상태로 제어를 잃은 유닛의 행동을 대신 정한다.
///
/// 규칙 (기존 TurnManager.ExecuteActionQueue와 동일)
///   - 예약해 둔 행동 대신 기본공격을 강제한다
///   - 대상은 아군을 포함한 전원 중 무작위로 고른다
///   - 레조넌스는 제어를 유지하므로 이 Brain을 거치지 않는다
///
/// 적 AI와 같은 인터페이스를 쓰는 이유는, "시스템이 유닛 대신 커맨드를 만든다"는
/// 구조가 완전히 같기 때문이다. 덕분에 TurnLoop에 별도 분기가 생기지 않는다.
/// </summary>
public sealed class EchoBrain : IActionBrain
{
    private readonly IRandom _random;
    private readonly SkillTable _skillTable;
    private readonly IReadOnlyDictionary<Unit, string> _basicAttackIds;

    /// <param name="basicAttackIds">유닛별 기본공격 스킬 ID</param>
    public EchoBrain(IRandom random, SkillTable skillTable, IReadOnlyDictionary<Unit, string> basicAttackIds)
    {
        _random = random;
        _skillTable = skillTable;
        _basicAttackIds = basicAttackIds;
    }

    public ICombatCommand Decide(CombatContext ctx, Unit self)
    {
        if (self == null || !self.IsAlive) return null;

        SkillSpec basic = FindBasicAttack(self);
        if (basic == null) return null;

        Unit target = PickRandomTarget(ctx, self);
        if (target == null) return null;

        return new UseSkillCommand(self, target, basic);
    }

    /// <summary>상태를 갖지 않으므로 통지에 반응하지 않는다.</summary>
    public void OnEvent(CombatEvent combatEvent) { }

    private SkillSpec FindBasicAttack(Unit self)
    {
        if (_skillTable == null || _basicAttackIds == null) return null;

        return _basicAttackIds.TryGetValue(self, out string skillId)
            ? _skillTable.Find(skillId)
            : null;
    }

    /// <summary>
    /// 아군과 적군을 가리지 않고 살아 있는 유닛 중 하나를 고른다.
    /// 제어를 잃었다는 것을 아군 오사로 표현한 기존 규칙을 그대로 옮긴 것이다.
    /// </summary>
    private Unit PickRandomTarget(CombatContext ctx, Unit self)
    {
        var candidates = new List<Unit>();

        AddAlive(ctx.Party, self, candidates);
        AddAlive(ctx.Enemies, self, candidates);

        if (candidates.Count == 0) return null;

        return candidates[_random.Range(0, candidates.Count)];
    }

    private static void AddAlive(IReadOnlyList<Unit> units, Unit exclude, List<Unit> result)
    {
        for (int i = 0; i < units.Count; i++)
        {
            Unit unit = units[i];
            if (unit == exclude || !unit.IsAlive) continue;

            result.Add(unit);
        }
    }
}
