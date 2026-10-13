using System.Collections.Generic;

/// <summary>
/// 지정한 대상에게 스킬 효과를 순서대로 적용한다.
/// SP·OP는 예약 시 처리하며, 예약 없이 실행하면 실행 시 처리한다.
/// ER 비용은 효과 적용 후 차감한다.
/// </summary>
public sealed class UseSkillCommand : ICombatCommand
{
    private readonly List<Unit> _targets;
    private CombatContext _paidContext;
    private bool _executed;

    public Unit Actor { get; }
    public SkillSpec Skill { get; }
    public IReadOnlyList<Unit> Targets => _targets;

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
        if (_executed || (_paidContext != null && _paidContext != ctx)) return false;
        if (Actor == null || !Actor.IsAlive) return false;
        if (Skill == null || Skill.Effects.Count == 0) return false;
        if (_paidContext == null && Actor.UsesResources && ctx.IsPartyMember(Actor)
            && !Skill.CanAfford(Actor, ctx.Resources)) return false;

        // ER은 실행 시 소모한다. 앞선 행동으로 부족해졌다면 실행하지 않는다.
        if (Actor.UsesResources && ctx.IsPartyMember(Actor) && Actor.CurrentER < Skill.ERCost) return false;

        // 대상이 하나라도 살아 있어야 실행할 의미가 있다.
        for (int i = 0; i < _targets.Count; i++)
            if (_targets[i] != null && _targets[i].IsAlive) return true;

        return false;
    }

    /// <summary>입력 예약 시 비용을 한 번 처리한다. 실패하면 자원과 큐를 바꾸지 않는다.</summary>
    internal bool TryPayReservation(CombatContext ctx, List<CombatEvent> events)
    {
        if (_paidContext != null || !CanExecute(ctx)) return false;
        SpendResources(ctx, events);
        _paidContext = ctx;
        return true;
    }

    /// <summary>ECHO 기본공격으로 바뀌어도 이미 예약한 동일 기본공격의 자원을 중복 처리하지 않는다.</summary>
    internal void InheritBasicPayment(UseSkillCommand original, CombatContext ctx)
    {
        if (original != null && original._paidContext == ctx && original.Actor == Actor
            && original.Skill.SkillId == Skill.SkillId && Skill.Type == SkillType.Basic)
            _paidContext = ctx;
    }

    public CommandResult Execute(CombatContext ctx)
    {
        var events = new List<CombatEvent>();
        if (!CanExecute(ctx)) return new CommandResult(events);
        _executed = true;

        // 효과 결과보다 먼저 사용한 스킬을 기록한다.
        events.Add(new SkillCast(Actor, Skill.SkillId, Skill.Type, _targets));

        if (_paidContext == null) SpendResources(ctx, events);

        // 스킬에 정의된 효과 순서는 유지한다.
        // Self 효과는 시전자에게 한 번, Target 효과는 지정된 각 대상에게 적용한다.
        for (int e = 0; e < Skill.Effects.Count; e++)
        {
            SkillEffect effect = Skill.Effects[e];
            if (effect == null) continue;

            if (effect.effectTarget == EffectTarget.Self)
            {
                effect.Apply(ctx, Actor, Actor, events);
            }
            else
            {
                for (int t = 0; t < _targets.Count; t++)
                    effect.Apply(ctx, Actor, _targets[t], events);
            }
        }

        // 선택한 강화 효과가 끝난 뒤 ER 비용과 상태 변화를 적용한다.
        if (Actor.UsesResources && ctx.IsPartyMember(Actor) && Skill.ERCost > 0f)
            EchoRules.SpendER(Actor, Skill.ERCost, events);
        return new CommandResult(events);
    }

    /// <summary>
    /// 자원을 소비한다. 기본공격은 소모가 아니라 SP를 획득한다.
    /// 예약과 직접 실행 모두 CanExecute에서 자원 부족을 먼저 검사한다.
    /// </summary>
    private void SpendResources(CombatContext ctx, List<CombatEvent> events)
    {
        // 적 AI는 스킬의 비용 필드와 무관하게 행동하며 파티 자원을 건드리지 않는다.
        if (!Actor.UsesResources || !ctx.IsPartyMember(Actor)) return;
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


    }

    private static void AddResourceEvent(List<CombatEvent> events, ResourceKind kind, float before, float after)
    {
        if (before == after) return;
        events.Add(new ResourceChanged(null, kind, before, after));
    }
}
