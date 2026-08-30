using System.Collections.Generic;

/// <summary>
/// 스킬 하나를 사용한다.
///
/// SkillSpec 전체를 받는다. 효과 목록만 받으면 커맨드가 스킬의 일부만 아는 상태가 되어,
/// 연출·자원 차감·세이브가 필요해질 때마다 인자가 늘어난다.
/// 기존 코드가 스킬 정보를 조각내 넘기다가 컨트롤러마다 다르게 해석했던 문제와 같은 형태다.
///
/// SkillData(ScriptableObject)가 아니라 SkillSpec(순수 C#)을 받는 이유는
/// Core가 UnityEngine에 의존하지 않기 위해서다.
/// </summary>
public sealed class UseSkillCommand : ICombatCommand
{
    private readonly List<Unit> _targets;

    public Unit Actor { get; }
    public SkillSpec Skill { get; }

    public UseSkillCommand(Unit actor, IReadOnlyList<Unit> targets, SkillSpec skill)
    {
        Actor = actor;
        Skill = skill;
        _targets = targets != null ? new List<Unit>(targets) : new List<Unit>();
    }

    /// <summary>단일 대상 편의 생성자.</summary>
    public UseSkillCommand(Unit actor, Unit target, SkillSpec skill)
        : this(actor, target != null ? new[] { target } : null, skill) { }

    public bool CanExecute(CombatContext ctx)
    {
        if (Actor == null || !Actor.IsAlive) return false;
        if (Skill == null || Skill.Effects.Count == 0) return false;

        // 대상이 하나라도 살아 있어야 실행할 의미가 있다.
        for (int i = 0; i < _targets.Count; i++)
            if (_targets[i] != null && _targets[i].IsAlive) return true;

        return false;
    }

    public CommandResult Execute(CombatContext ctx)
    {
        var events = new List<CombatEvent>();

        // 무엇을 썼는지 먼저 알린다. 재생기가 시전 모션을 재생하는 신호다.
        events.Add(new SkillCast(Actor, Skill.SkillId, Skill.Type, _targets));

        SpendResources(ctx, events);

        // 효과를 먼저 순회한다. 광역 피해가 전부 들어간 뒤에 버프가 걸리는 편이
        // "동시에 일어난 일"로 읽히기 때문이다.
        for (int e = 0; e < Skill.Effects.Count; e++)
        {
            SkillEffect effect = Skill.Effects[e];
            if (effect == null) continue;

            for (int t = 0; t < _targets.Count; t++)
                effect.Apply(ctx, Actor, _targets[t], events);
        }

        return new CommandResult(events);
    }

    /// <summary>
    /// 자원을 소비한다. 기본공격은 소모가 아니라 SP를 획득한다.
    /// (기존 CombatManager.OnPlayerActionReserved 규칙)
    ///
    /// 부족한 경우는 예약 시점에 걸러지므로 여기서는 검사하지 않는다.
    /// </summary>
    private void SpendResources(CombatContext ctx, List<CombatEvent> events)
    {
        PartyResources resources = ctx.Resources;
        if (resources == null) return;

        int before;

        switch (Skill.Type)
        {
            case SkillType.Basic:
                if (Skill.SPCost <= 0) break;

                before = resources.CurrentSP;
                resources.GainSP(Skill.SPCost);
                AddResourceEvent(events, ResourceKind.SP, before, resources.CurrentSP);
                break;

            case SkillType.Special:
                before = resources.CurrentSP;
                if (resources.TrySpendSP(Skill.SPCost))
                    AddResourceEvent(events, ResourceKind.SP, before, resources.CurrentSP);
                break;

            case SkillType.Overload:
                before = resources.CurrentOP;
                if (resources.TrySpendOP(Skill.OPCost))
                    AddResourceEvent(events, ResourceKind.OP, before, resources.CurrentOP);
                break;
        }

        // ER 비용은 파티 공용이 아니라 시전자 개인의 자원이다.
        if (Skill.ERCost > 0f)
            EchoRules.SpendER(Actor, Skill.ERCost, events);
    }

    private static void AddResourceEvent(List<CombatEvent> events, ResourceKind kind, float before, float after)
    {
        if (before == after) return;
        events.Add(new ResourceChanged(null, kind, before, after));
    }
}
