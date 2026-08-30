using System.Collections.Generic;

/// <summary>
/// 스킬의 대상을 정하는 규칙.
///
/// 기존에는 CombatManager.CheckIfValidTarget과 각 컨트롤러의 ExecuteSkill이
/// 대상 판정을 나눠 갖고 있어서 규칙이 흩어져 있었다.
/// 여기로 모으면 "이 스킬을 누구에게 쓸 수 있는가"가 한 곳에서 결정된다.
/// </summary>
public static class TargetingRule
{
    /// <summary>도발 버프가 걸린 유닛을 판정하는 종류.</summary>
    public const BuffType TauntBuff = BuffType.Taunt;

    /// <summary>대상 지정이 필요한 종류인가. 아니면 자동으로 결정된다.</summary>
    public static bool NeedsManualTarget(TargetType type)
        => type == TargetType.SingleEnemy || type == TargetType.Ally;

    /// <summary>
    /// 지정 가능한 대상 목록. UI가 클릭 가능한 유닛을 표시할 때 쓴다.
    /// </summary>
    public static List<Unit> GetSelectableTargets(CombatContext ctx, Unit actor, TargetType type)
    {
        var result = new List<Unit>();

        switch (type)
        {
            case TargetType.SingleEnemy:
            case TargetType.AllEnemy:
                AddValidEnemies(ctx, actor, result);
                break;

            case TargetType.Ally:
            case TargetType.Team:
                AddAliveUnits(ctx.GetAlliesOf(actor), result);
                break;

            case TargetType.Self:
            case TargetType.Groggy:
                if (actor.IsAlive) result.Add(actor);
                break;
        }

        return result;
    }

    /// <summary>
    /// 실제로 효과가 적용될 대상 목록.
    ///
    /// 단일 대상이면 지정된 유닛 하나, 전체 대상이면 생존자 전원을 반환한다.
    /// chosen이 필요 없는 종류(Self, Team, AllEnemy, Groggy)에서는 무시된다.
    /// </summary>
    public static List<Unit> ResolveTargets(CombatContext ctx, Unit actor, TargetType type, Unit chosen)
    {
        var result = new List<Unit>();

        switch (type)
        {
            case TargetType.SingleEnemy:
            case TargetType.Ally:
                if (chosen != null && chosen.IsAlive) result.Add(chosen);
                break;

            case TargetType.Self:
            case TargetType.Groggy:
                if (actor.IsAlive) result.Add(actor);
                break;

            case TargetType.Team:
                AddAliveUnits(ctx.GetAlliesOf(actor), result);
                break;

            case TargetType.AllEnemy:
                AddAliveUnits(ctx.GetOpponentsOf(actor), result);
                break;
        }

        return result;
    }

    /// <summary>
    /// 이 유닛을 대상으로 지정할 수 있는가.
    /// 기존 CombatManager.CheckIfValidTarget에 해당한다.
    /// </summary>
    public static bool IsValidTarget(CombatContext ctx, Unit actor, TargetType type, Unit candidate)
    {
        if (candidate == null || !candidate.IsAlive) return false;

        switch (type)
        {
            case TargetType.SingleEnemy:
                if (!Contains(ctx.GetOpponentsOf(actor), candidate)) return false;
                return !IsBlockedByTaunt(ctx, actor, candidate);

            case TargetType.Ally:
                return Contains(ctx.GetAlliesOf(actor), candidate);

            case TargetType.Self:
            case TargetType.Groggy:
                return candidate == actor;

            default:
                return true;
        }
    }

    /// <summary>
    /// 도발 규칙: 도발 중인 적이 하나라도 살아 있으면
    /// 도발 중이 아닌 적은 지정할 수 없다.
    /// </summary>
    public static bool IsBlockedByTaunt(CombatContext ctx, Unit actor, Unit candidate)
    {
        if (candidate.Stats.HasBuffType(TauntBuff)) return false;

        IReadOnlyList<Unit> opponents = ctx.GetOpponentsOf(actor);

        for (int i = 0; i < opponents.Count; i++)
        {
            Unit other = opponents[i];
            if (other.IsAlive && other.Stats.HasBuffType(TauntBuff)) return true;
        }

        return false;
    }

    private static void AddValidEnemies(CombatContext ctx, Unit actor, List<Unit> result)
    {
        IReadOnlyList<Unit> opponents = ctx.GetOpponentsOf(actor);

        for (int i = 0; i < opponents.Count; i++)
        {
            Unit enemy = opponents[i];
            if (!enemy.IsAlive) continue;
            if (IsBlockedByTaunt(ctx, actor, enemy)) continue;

            result.Add(enemy);
        }
    }

    private static void AddAliveUnits(IReadOnlyList<Unit> source, List<Unit> result)
    {
        for (int i = 0; i < source.Count; i++)
            if (source[i].IsAlive) result.Add(source[i]);
    }

    private static bool Contains(IReadOnlyList<Unit> source, Unit unit)
    {
        for (int i = 0; i < source.Count; i++)
            if (source[i] == unit) return true;

        return false;
    }
}
