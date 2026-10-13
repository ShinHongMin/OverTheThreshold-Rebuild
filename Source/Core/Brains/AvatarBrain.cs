using System.Collections.Generic;

/// <summary>
/// 임계점의 화신(보스)의 충전·강화 공격·그로기 상태를 관리한다.
/// 그로기와 충전을 먼저 처리하고, 체력 80%·50%의 최초 도달 또는 주기마다 충전한다.
/// 대기·그로기 차례에도 상대에게 패시브 ER을 부여한다.
/// </summary>
public sealed class AvatarBrain : MonsterBrain
{
    /// <summary>그로기가 유지되는 턴 수.</summary>
    public const int GroggyTurns = 2;

    /// <summary>충전을 시작한 뒤 대기하는 턴 수.</summary>
    public const int ChargeWaitTurns = 1;

    /// <summary>이 턴 수마다 충전을 시도한다.</summary>
    public const int ChargeCycle = 3;

    private readonly AvatarBrainConfig _avatarConfig;
    private readonly List<CombatEvent> _pendingEvents = new List<CombatEvent>(8);

    // ── 상태 ─────────────────────────────────────────────────────
    private bool _isGroggy;
    private int _groggyCount;
    private bool _isCharging;
    private int _chargeCount;
    private int _turnCycleCount;
    private bool _triggered80;
    private bool _triggered50;

    public AvatarBrain(IRandom random, SkillTable skillTable,
                       MonsterBrainConfig config, AvatarBrainConfig avatarConfig)
        : base(random, skillTable, config)
    {
        _avatarConfig = avatarConfig;
    }

    public bool IsGroggy => _isGroggy;
    public bool IsCharging => _isCharging;

    public override ICombatCommand Decide(CombatContext ctx, Unit self)
    {
        if (self == null || !self.IsAlive) return null;

        ApplyPassiveER(ctx, self);

        // 그로기 중에는 지속 턴만 갱신하고 행동을 건너뛴다.
        if (_isGroggy)
        {
            TickGroggy();
            return null;
        }

        SkillSpec skill = SelectPatternSkill(self);
        if (skill == null) return null;   // 충전 대기 턴

        IReadOnlyList<Unit> targets = SelectTargets(ctx, self, skill);
        if (targets == null || targets.Count == 0) return null;

        return new UseSkillCommand(self, targets, skill);
    }

    /// <summary>
    /// 충전 중 실드가 파괴되면 즉시 그로기에 빠진다.
    ///
    /// 자기 턴이 아닌 시점에 상태가 바뀌는 유일한 경로다.
    /// TurnLoop이 이 유닛에 관한 이벤트만 골라 전달한다.
    /// </summary>
    public override void OnEvent(CombatEvent combatEvent)
    {
        if (!_isCharging) return;
        if (!(combatEvent is DamageDealt damage)) return;

        // 이 피해로 실드가 깨진 경우에만 해당한다.
        // 애초에 실드가 없었던 피해는 방어를 뚫은 것이 아니다.
        bool shieldBroken = damage.ShieldAbsorbed > 0f && damage.ShieldAfter <= 0f;
        if (!shieldBroken) return;

        EndCharge(damage.Target);
        if (!damage.Target.IsAlive) return;
        EnterGroggy();
        // 실드가 깨진 즉시 디버프를 적용하여 다음 아군의 피해에도 반영한다.
        SkillSpec groggy = FindSkill(_avatarConfig.SelfGroggySkillId);
        if (groggy == null || !damage.Target.IsAlive) return;
        for (int i = 0; i < groggy.Effects.Count; i++)
        {
            if (!(groggy.Effects[i] is BuffEffect buff)) continue;
            damage.Target.Stats.AddBuff(buff.buffId, buff.buffType, buff.value, GroggyTurns, buff.isDebuff);
            _pendingEvents.Add(new BuffApplied(damage.Target, buff.buffId, buff.buffType, buff.value, GroggyTurns));
        }
    }

    public override void AppendPendingEvents(List<CombatEvent> destination)
    {
        destination.AddRange(_pendingEvents);
        _pendingEvents.Clear();
    }

    // ── 패턴 ─────────────────────────────────────────────────────

    private SkillSpec SelectPatternSkill(Unit self)
    {
        if (_isCharging) return HandleCharging(self);

        if (CheckHealthTrigger(self))
        {
            _turnCycleCount = 0;
            return StartCharge();
        }

        _turnCycleCount++;

        return _turnCycleCount >= ChargeCycle ? StartCharge() : BasicAttack;
    }

    /// <summary>충전 스킬을 고르고 충전 상태로 들어간다.</summary>
    private SkillSpec StartCharge()
    {
        SkillSpec charge = FindSkill(_avatarConfig.ChargeSkillId);
        if (charge == null) return BasicAttack;

        _isCharging = true;
        _chargeCount = 0;

        return charge;
    }

    /// <summary>
    /// 충전 중의 행동. 실드가 남아 있으면 강화기, 깨졌으면 자체 그로기.
    /// 충전 직후 한 턴은 대기한다.
    /// </summary>
    private SkillSpec HandleCharging(Unit self)
    {
        if (_chargeCount < ChargeWaitTurns)
        {
            _chargeCount++;
            return null;   // 이번 턴은 대기
        }

        bool enhanced = self.HasShield;
        SkillSpec skill = enhanced
            ? FindSkill(_avatarConfig.EnhancedSkillId)
            : FindSkill(_avatarConfig.SelfGroggySkillId);

        EndCharge(self);

        if (enhanced)
        {
            self.ClearShield();
            _pendingEvents.Add(new ShieldChanged(self));
        }
        else
        {
            EnterGroggy();
            // 자체 그로기를 시전한 이번 차례가 첫 무행동 턴이다.
            _groggyCount = 1;
        }

        return skill ?? BasicAttack;
    }

    /// <summary>체력 80%·50%에 처음 도달했는가. 각각 한 번만 발동한다.</summary>
    private bool CheckHealthTrigger(Unit self)
    {
        float max = self.MaxHP;

        if (!_triggered80 && self.CurrentHP <= max * 0.8f)
        {
            _triggered80 = true;
            return true;
        }

        if (!_triggered50 && self.CurrentHP <= max * 0.5f)
        {
            _triggered50 = true;
            return true;
        }

        return false;
    }

    private void EnterGroggy()
    {
        _isGroggy = true;
        _groggyCount = 0;
        _isCharging = false;
        _chargeCount = 0;
        _turnCycleCount = 0;
    }

    private void EndCharge(Unit self)
    {
        _isCharging = false;
        _chargeCount = 0;
        _turnCycleCount = 0;
        // 충전 표시는 UnitData에서 지정한다. 다른 버프나 능력치 보정은 건드리지 않는다.
        string chargeBuffId = Config.StateBuffId;
        if (!string.IsNullOrEmpty(chargeBuffId) && self.Stats.RemoveBuff(chargeBuffId))
            _pendingEvents.Add(new BuffRemoved(self, chargeBuffId));
    }

    private void TickGroggy()
    {
        _groggyCount++;

        if (_groggyCount >= GroggyTurns)
        {
            _isGroggy = false;
            _groggyCount = 0;
        }
    }

    // ── 패시브 ───────────────────────────────────────────────────

    /// <summary>
    /// 매 턴 살아 있는 상대 전원의 ER을 올린다.
    ///
    /// 대기·그로기 차례에도 이벤트를 남겨 ER 표시가 갱신되게 한다.
    /// </summary>
    private void ApplyPassiveER(CombatContext ctx, Unit self)
    {
        if (_avatarConfig.PassiveERPerTurn <= 0f) return;

        IReadOnlyList<Unit> alive = ctx.GetOpponentsOf(self);

        for (int i = 0; i < alive.Count; i++)
            EchoRules.ApplyER(alive[i], _avatarConfig.PassiveERPerTurn, _pendingEvents, ctx.CardRandom, ctx.Resources);
    }
}
