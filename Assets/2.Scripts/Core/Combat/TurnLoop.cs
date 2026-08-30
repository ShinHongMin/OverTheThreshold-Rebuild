using System.Collections.Generic;

/// <summary>
/// 턴 진행. 예약된 행동을 순서대로 실행하고 턴 종료 처리를 한다.
///
/// 기존 TurnManager는 383줄이었고 코루틴 안에서 계산·연출·UI 갱신을 함께 했다.
/// 여기서는 계산만 하고 일어난 일을 CombatEvent 목록으로 돌려준다.
/// 연출은 CombatEventPlayer가 그 목록을 재생하며 담당한다.
///
/// 아군과 적군이 같은 큐를 쓴다. 실행 순서는 예약된 순서 그대로이며,
/// 속도 스탯이 없으므로 정렬하지 않는다.
/// </summary>
public sealed class TurnLoop
{
    private readonly List<ICombatCommand> _queue = new List<ICombatCommand>();

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
    public IActionBrain EchoBrain { get; set; }

    /// <summary>
    /// 턴을 시작한다. 자원을 회복하고 입력 대기 상태로 들어간다.
    ///
    /// OP 회복이 턴 시작인 것은 기존 ResourceManager.GainOpOnTurnStart를 따른 것이다.
    /// (기획서 5.10은 턴 종료로 적었으나 실제 코드는 턴 시작이었다)
    /// </summary>
    public IReadOnlyList<CombatEvent> BeginTurn(CombatContext ctx)
    {
        var events = new List<CombatEvent>();

        State = BattleState.PlayerTurn_Input;
        _queue.Clear();

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
        if (command == null) return;
        _queue.Add(command);
    }

    public void ClearReservations() => _queue.Clear();

    /// <summary>
    /// 예약된 행동을 순서대로 실행하고 턴을 마감한다.
    ///
    /// 각 행동마다
    ///   1. 주체가 살아 있는지 확인
    ///   2. ECHO 상태면 페널티를 먼저 적용 (기존과 동일하게 행동 직전)
    ///   3. 제어를 잃었으면 EchoBrain이 정한 행동으로 교체
    ///   4. CanExecute로 대상 유효성 재검사
    ///   5. 실행
    ///   6. 전투 종료 조건을 만족하면 남은 예약을 취소
    /// </summary>
    public TurnResult ExecuteTurn(CombatContext ctx)
    {
        var events = new List<CombatEvent>();

        State = BattleState.PlayerTurn_Action;

        BattleResult battle = BattleResult.InProgress;

        for (int i = 0; i < _queue.Count; i++)
        {
            ICombatCommand command = _queue[i];
            Unit actor = command?.Actor;

            if (actor == null || !actor.IsAlive) continue;

            if (!ctx.IsPartyMember(actor))
                State = BattleState.EnemyTurn;

            // ECHO 페널티는 자기 차례가 왔을 때 적용된다.
            if (actor.IsEchoState)
            {
                ApplyEchoPenalty(actor, events);

                // 페널티로 쓰러지면 이번 행동은 없던 일이 된다.
                if (!actor.IsAlive)
                {
                    battle = CheckBattle(ctx);
                    if (battle != BattleResult.InProgress) break;
                    continue;
                }

                // 제어를 잃었다면 예약해 둔 행동 대신 시스템이 정한 행동을 한다.
                if (EchoRules.IsOutOfControl(actor) && EchoBrain != null)
                {
                    command = EchoBrain.Decide(ctx, actor);
                    if (command == null) continue;
                }
            }

            // 앞선 행동이 대상을 쓰러뜨렸을 수 있다.
            if (!command.CanExecute(ctx)) continue;

            CommandResult result = command.Execute(ctx);
            events.AddRange(result.Events);

            battle = CheckBattle(ctx);
            if (battle != BattleResult.InProgress) break;   // 남은 예약 취소
        }

        EndTurn(ctx, events);

        return new TurnResult(events, battle);
    }

    /// <summary>전투 종료 여부를 판정한다.</summary>
    public static BattleResult CheckBattle(CombatContext ctx)
    {
        if (AllDefeated(ctx.Enemies)) return BattleResult.Victory;
        if (AllDefeated(ctx.Party)) return BattleResult.Defeat;

        return BattleResult.InProgress;
    }

    /// <summary>
    /// 한 진영이 전멸했는가.
    /// 파티는 사망뿐 아니라 전원 ECHO 상태여도 패배로 판정된다. (기존 IsBattleOver 규칙)
    /// </summary>
    private static bool AllDefeated(IReadOnlyList<Unit> units)
    {
        for (int i = 0; i < units.Count; i++)
            if (units[i].IsAlive && !units[i].IsEchoState) return false;

        return true;
    }

    private static void ApplyEchoPenalty(Unit unit, List<CombatEvent> events)
    {
        float penalty = EchoRules.GetEchoPenalty(unit);
        if (penalty <= 0f) return;

        DamageBreakdown breakdown = unit.ApplyDamage(penalty);
        events.Add(new DamageDealt(unit, breakdown));

        if (!unit.IsAlive)
            events.Add(new UnitDied(unit));
    }

    /// <summary>
    /// 턴 마감. 버프와 실드의 지속을 함께 줄인다.
    ///
    /// 기존에는 각 유닛이 행동하기 직전에 자기 버프를 줄였기 때문에
    /// 예약 순서가 실제 지속 턴 수에 영향을 주었고,
    /// "건 턴에는 줄이지 않는다"는 예외 보정이 필요했다.
    /// 턴 종료에 일괄로 처리하면 그 예외가 필요 없어진다.
    /// </summary>
    private void EndTurn(CombatContext ctx, List<CombatEvent> events)
    {
        TickDurations(ctx.Party);
        TickDurations(ctx.Enemies);

        events.Add(new TurnEnded(Round));

        Round++;
        _queue.Clear();
    }

    private static void TickDurations(IReadOnlyList<Unit> units)
    {
        for (int i = 0; i < units.Count; i++)
        {
            Unit unit = units[i];
            if (!unit.IsAlive) continue;

            unit.Stats.TickBuffDurations();
            unit.TickShieldDuration();
        }
    }
}
