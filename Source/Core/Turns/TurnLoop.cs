using System.Collections.Generic;

/// <summary>
/// 예약된 행동을 실행하고 턴 종료를 처리한다.
/// 아군과 적군은 같은 큐를 사용하며, 예약 순서대로 실행한다.
/// 계산 결과는 이벤트로 반환하고 연출은 호출자가 처리한다.
/// </summary>
public sealed class TurnLoop
{
    private readonly List<ICombatCommand> _queue = new List<ICombatCommand>();
    private readonly Dictionary<Unit, IActionBrain> _brains = new Dictionary<Unit, IActionBrain>();
    private readonly Dictionary<Unit, BrainTurnCommand> _brainTurns = new Dictionary<Unit, BrainTurnCommand>();

    /// <summary>현재 라운드. 1부터 시작한다.</summary>
    public int Round { get; private set; } = 1;

    public BattleState State { get; private set; } = BattleState.PlayerTurn_Input;

    /// <summary>예약된 행동 수.</summary>
    public int ReservedCount => _queue.Count;

    public IReadOnlyList<ICombatCommand> Reservations => _queue;

    /// <summary>
    /// ECHO로 제어를 잃은 유닛의 행동을 대신 정한다.
    /// 지정하지 않으면 해당 유닛은 예약해 둔 행동을 그대로 수행한다.
    /// </summary>
    public IActionBrain EchoBrain
    {
        get; set;
    }

    /// <summary>
    /// 유닛의 행동을 정하는 Brain을 등록한다. 적 AI가 여기 들어간다.
    ///
    /// 등록된 Brain은 자신과 관련된 이벤트를 통지받는다.
    /// 보스가 충전 중 실드를 잃으면 즉시 그로기에 빠지는 기믹이 이 경로를 쓴다.
    /// </summary>
    public void RegisterBrain(Unit unit, IActionBrain brain)
    {
        if (unit == null || brain == null)
            return;
        _brains[unit] = brain;
        _brainTurns[unit] = new BrainTurnCommand(unit, brain);
    }

    public IActionBrain FindBrain(Unit unit)
        => unit != null && _brains.TryGetValue(unit, out IActionBrain brain) ? brain : null;

    /// <summary>
    /// 턴을 시작한다. 자원을 회복하고 입력 대기 상태로 들어간다.
    /// </summary>
    public IReadOnlyList<CombatEvent> BeginTurn(CombatContext ctx)
    {
        var events = new List<CombatEvent>();

        State = BattleState.PlayerTurn_Input;
        _queue.Clear();

        ctx.ApplyBattleStartCards(events);

        int beforeOP = ctx.Resources.CurrentOP;
        ctx.Resources.GainOPOnTurnStart();

        if (ctx.Resources.CurrentOP != beforeOP)
            events.Add(new ResourceChanged(null, ResourceKind.OP, beforeOP, ctx.Resources.CurrentOP));

        events.Add(new TurnStarted(Round));
        return events;
    }

    /// <summary>행동을 예약한다. 아군·적군 구분 없이 같은 큐에 들어간다.</summary>
    public void Reserve(ICombatCommand command)
    {
        if (command == null)
            return;
        _queue.Add(command);
    }

    /// <summary>플레이어 입력 예약. 자원 변화 이벤트는 다음 입력 전에 즉시 표시한다.</summary>
    public bool TryReserve(UseSkillCommand command, CombatContext ctx, out IReadOnlyList<CombatEvent> events)
    {
        var changes = new List<CombatEvent>();
        events = changes;
        if (State != BattleState.PlayerTurn_Input || command == null)
            return false;
        for (int i = 0; i < _queue.Count; i++)
            if (_queue[i].Actor == command.Actor)
                return false;
        if (!command.TryPayReservation(ctx, changes))
            return false;
        _queue.Add(command);
        return true;
    }

    /// <summary>
    /// AI의 차례만 예약한다. 실제 행동·대상은 자기 차례가 왔을 때 결정한다.
    /// </summary>
    public void ReserveBrainActions(CombatContext ctx, IReadOnlyList<Unit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            Unit unit = units[i];
            if (unit == null || !unit.IsAlive)
                continue;

            IActionBrain brain = FindBrain(unit);
            if (brain == null)
                continue;

            Reserve(_brainTurns[unit]);
        }
    }

    public void ClearReservations() => _queue.Clear();

    private sealed class BrainTurnCommand : ICombatCommand
    {
        public Unit Actor { get; }
        private readonly IActionBrain _brain;
        public BrainTurnCommand(Unit actor, IActionBrain brain)
        {
            Actor = actor;
            _brain = brain;
        }

        public bool CanExecute(CombatContext ctx) => Actor != null && Actor.IsAlive;
        public ICombatCommand Prepare(CombatContext ctx, List<CombatEvent> events)
        {
            var command = _brain.Decide(ctx, Actor);
            (_brain as MonsterBrain)?.AppendPendingEvents(events);
            return command;
        }

        public CommandResult Execute(CombatContext ctx)
        {
            var events = new List<CombatEvent>();
            var command = Prepare(ctx, events);
            if (command != null && command.CanExecute(ctx))
                events.AddRange(command.Execute(ctx).Events);
            return new CommandResult(events);
        }
    }

    /// <summary>
    /// 예약된 행동을 순서대로 실행하고 턴을 마감한다.
    /// 연출 대기 없이 계산할 때 사용한다. 행동별 연출은 ExecuteSteps를 사용한다.
    /// </summary>
    public TurnResult ExecuteTurn(CombatContext ctx)
    {
        var events = new List<CombatEvent>();
        BattleResult battle = BattleResult.InProgress;
        foreach (var step in ExecuteSteps(ctx))
        {
            events.AddRange(step.Events);
            if (step.Command != null)
                events.AddRange(ExecuteAction(ctx, step.Command));
            battle = step.Battle;
        }
        return new TurnResult(events, battle);
    }

    /// <summary>다음 MoveNext는 현재 행동의 연출까지 마친 뒤 호출한다.</summary>
    public IEnumerable<TurnStep> ExecuteSteps(CombatContext ctx)
    {
        State = BattleState.PlayerTurn_Action;
        BattleResult battle = CheckBattle(ctx);
        for (int i = 0; i < _queue.Count && battle == BattleResult.InProgress; i++)
        {
            ICombatCommand command = _queue[i];
            Unit actor = command?.Actor;
            if (actor == null || !actor.IsAlive)
                continue;
            State = ctx.IsPartyMember(actor) ? BattleState.PlayerTurn_Action : BattleState.EnemyTurn;
            if (actor.IsEchoState)
            {
                var penaltyEvents = new List<CombatEvent>();
                ApplyEchoPenalty(actor, penaltyEvents);
                battle = CheckBattle(ctx);
                yield return new TurnStep(null, penaltyEvents, battle);
                if (!actor.IsAlive || battle != BattleResult.InProgress)
                    continue;
                if (EchoRules.IsOutOfControl(actor) && EchoBrain != null)
                {
                    var original = command as UseSkillCommand;
                    command = EchoBrain.Decide(ctx, actor);
                    (command as UseSkillCommand)?.InheritBasicPayment(original, ctx);
                }
            }
            if (command is BrainTurnCommand brainTurn)
            {
                var pendingEvents = new List<CombatEvent>();
                command = brainTurn.Prepare(ctx, pendingEvents);
                if (pendingEvents.Count > 0)
                    yield return new TurnStep(null, pendingEvents, battle);
            }
            if (command == null || !command.CanExecute(ctx))
                continue;
            // 타격/효과 시점까지 실행을 보류한다. 호출자가 ExecuteAction 후 재생을 마친다.
            yield return new TurnStep(command, System.Array.Empty<CombatEvent>(), battle);
            battle = CheckBattle(ctx);
        }
        var endEvents = new List<CombatEvent>();
        EndTurn(ctx, endEvents);
        yield return new TurnStep(null, endEvents, battle);
    }

    public IReadOnlyList<CombatEvent> ExecuteAction(CombatContext ctx, ICombatCommand command)
    {
        var events = new List<CombatEvent>();
        if (command == null || !command.CanExecute(ctx))
            return events;
        var result = command.Execute(ctx);
        events.AddRange(result.Events);
        NotifyBrains(result.Events, events);
        return events;
    }

    /// <summary>
    /// 이벤트의 영향을 받은 유닛의 Brain에 알리고, 후속 이벤트를 수집한다.
    /// </summary>
    private void NotifyBrains(IReadOnlyList<CombatEvent> events, List<CombatEvent> output)
    {
        if (_brains.Count == 0)
            return;

        for (int i = 0; i < events.Count; i++)
        {
            Unit affected = GetAffectedUnit(events[i]);
            if (affected == null)
                continue;

            IActionBrain brain = FindBrain(affected);
            brain?.OnEvent(events[i]);
            (brain as MonsterBrain)?.AppendPendingEvents(output);
        }
    }

    /// <summary>이벤트가 어느 유닛에 관한 것인가. 유닛과 무관하면 null.</summary>
    private static Unit GetAffectedUnit(CombatEvent combatEvent)
    {
        switch (combatEvent)
        {
            case DamageDealt damage:
                return damage.Target;
            case HealReceived heal:
                return heal.Target;
            case ShieldGranted shield:
                return shield.Target;
            case BuffApplied buff:
                return buff.Target;
            case UnitDied died:
                return died.Unit;
            case ResourceChanged res:
                return res.Target;
            default:
                return null;
        }
    }

    /// <summary>전투 종료 여부를 판정한다.</summary>
    public static BattleResult CheckBattle(CombatContext ctx)
    {
        if (AllDead(ctx.Enemies))
            return BattleResult.Victory;
        if (AllDefeated(ctx.Party))
            return BattleResult.Defeat;

        return BattleResult.InProgress;
    }

    /// <summary>적은 전원 사망했을 때만 전멸이다. ER은 승리 조건이 아니다.</summary>
    private static bool AllDead(IReadOnlyList<Unit> units)
    {
        for (int i = 0; i < units.Count; i++)
            if (units[i].IsAlive)
                return false;

        return true;
    }

    /// <summary>아군은 전원이 사망 또는 ECHO이면 패배한다. 둘의 혼합도 포함한다.</summary>
    private static bool AllDefeated(IReadOnlyList<Unit> units)
    {
        for (int i = 0; i < units.Count; i++)
            if (units[i].IsAlive && !units[i].IsEchoState)
                return false;

        return true;
    }

    private static void ApplyEchoPenalty(Unit unit, List<CombatEvent> events)
    {
        float penalty = EchoRules.GetEchoPenalty(unit);
        if (penalty <= 0f)
            return;

        DamageBreakdown breakdown = unit.ApplyDamage(penalty);
        events.Add(new DamageDealt(unit, breakdown));

        if (!unit.IsAlive)
            events.Add(new UnitDied(unit));
    }

    /// <summary>
    /// 행동 순서에 관계없이 턴 종료 시 버프와 실드의 지속 턴을 일괄 감소시킨다.
    /// </summary>
    private void EndTurn(CombatContext ctx, List<CombatEvent> events)
    {
        TickDurations(ctx.Party, events);
        TickDurations(ctx.Enemies, events);

        events.Add(new TurnEnded(Round));

        Round++;
        _queue.Clear();
    }

    private static void TickDurations(IReadOnlyList<Unit> units, List<CombatEvent> events)
    {
        for (int i = 0; i < units.Count; i++)
        {
            Unit unit = units[i];
            if (!unit.IsAlive)
                continue;

            unit.Stats.TickBuffDurations();
            if (unit.TickShieldDuration())
                events.Add(new ShieldChanged(unit));
        }
    }
}
