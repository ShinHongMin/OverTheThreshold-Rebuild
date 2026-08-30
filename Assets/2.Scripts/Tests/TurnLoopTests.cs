using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// 턴 진행 규칙 검증. 씬도 코루틴도 없이 전투를 완주할 수 있어야 한다.
/// </summary>
public class TurnLoopTests
{
    private const float Tolerance = 0.01f;

    private Unit _aegis, _seiren;
    private Unit _scout, _echo;
    private CombatContext _ctx;
    private TurnLoop _loop;

    [SetUp]
    public void SetUp()
    {
        _aegis  = new Unit("Aegis",  new UnitStats(150f, 10f, 20f), CharacterJob.Vanguard);
        _seiren = new Unit("Seiren", new UnitStats(120f, 20f, 10f), CharacterJob.Resonance);
        _scout  = new Unit("LostScout",      new UnitStats(40f, 10f, 5f));
        _echo   = new Unit("WhisperingEcho", new UnitStats(30f, 5f, 0f));

        _ctx = new CombatContext(new[] { _aegis, _seiren }, new[] { _scout, _echo });
        _loop = new TurnLoop();
    }

    private static int CountDamage(TurnResult result)
    {
        int count = 0;

        foreach (CombatEvent e in result.Events)
            if (e is DamageDealt) count++;

        return count;
    }

    // ── 턴 시작 ──────────────────────────────────────────────────

    [Test]
    public void 턴을_시작하면_OP가_회복되고_이벤트가_나온다()
    {
        int before = _ctx.Resources.CurrentOP;

        IReadOnlyList<CombatEvent> events = _loop.BeginTurn(_ctx);

        Assert.AreEqual(before + 1, _ctx.Resources.CurrentOP);
        Assert.IsTrue(events.Count >= 1);
    }

    [Test]
    public void 턴_시작_이벤트에_라운드가_담긴다()
    {
        IReadOnlyList<CombatEvent> events = _loop.BeginTurn(_ctx);

        TurnStarted started = null;
        foreach (CombatEvent e in events)
            if (e is TurnStarted s) started = s;

        Assert.IsNotNull(started);
        Assert.AreEqual(1, started.Round);
    }

    // ── 실행 순서 ────────────────────────────────────────────────

    [Test]
    public void 예약한_순서대로_실행된다()
    {
        _loop.BeginTurn(_ctx);
        _loop.Reserve(new UseSkillCommand(_aegis, _scout, TestSkills.Damage()));
        _loop.Reserve(new UseSkillCommand(_seiren, _echo, TestSkills.Damage()));

        TurnResult result = _loop.ExecuteTurn(_ctx);

        var damages = new List<DamageDealt>();
        foreach (CombatEvent e in result.Events)
            if (e is DamageDealt d) damages.Add(d);

        Assert.AreSame(_scout, damages[0].Target);
        Assert.AreSame(_echo, damages[1].Target);
    }

    [Test]
    public void 앞선_행동이_대상을_쓰러뜨리면_뒤_행동은_취소된다()
    {
        _loop.BeginTurn(_ctx);
        _loop.Reserve(new UseSkillCommand(_aegis, _scout, TestSkills.Damage(100f)));   // 즉사
        _loop.Reserve(new UseSkillCommand(_seiren, _scout, TestSkills.Damage()));      // 대상 소멸

        TurnResult result = _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(1, CountDamage(result));
    }

    [Test]
    public void 죽은_유닛의_예약은_실행되지_않는다()
    {
        _aegis.ApplyDamage(999f);

        _loop.BeginTurn(_ctx);
        _loop.Reserve(new UseSkillCommand(_aegis, _scout, TestSkills.Damage()));

        _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(40f, _scout.CurrentHP, Tolerance);   // 피해 없음
    }

    // ── 턴 종료 ──────────────────────────────────────────────────

    [Test]
    public void 턴이_끝나면_버프_지속이_줄어든다()
    {
        _aegis.Stats.AddBuff("BUFF_A", BuffType.ATK_Percent, 0.5f, 2);

        _loop.BeginTurn(_ctx);
        _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(1, _aegis.Stats.Buffs[0].DurationTurns);

        _loop.BeginTurn(_ctx);
        _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(0, _aegis.Stats.Buffs.Count);   // 만료
    }

    [Test]
    public void 턴이_끝나면_실드_지속도_줄어든다()
    {
        _aegis.AddShield(30f, duration: 1);

        _loop.BeginTurn(_ctx);
        _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(0f, _aegis.CurrentShield, Tolerance);
    }

    [Test]
    public void 버프는_건_턴에도_줄어든다()
    {
        // 기존에는 startRound 예외로 건 턴에는 줄이지 않았으나,
        // 턴 종료 일괄 처리로 바뀌면서 예외가 필요 없어졌다.
        _loop.BeginTurn(_ctx);

        SkillSpec skill = TestSkills.Make(
            new BuffEffect("BUFF_A", BuffType.ATK_Percent, 0.5f, 2));

        _loop.Reserve(new UseSkillCommand(_aegis, _aegis, skill));
        _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(1, _aegis.Stats.Buffs[0].DurationTurns);
    }

    [Test]
    public void 턴이_끝나면_라운드가_올라간다()
    {
        Assert.AreEqual(1, _loop.Round);

        _loop.BeginTurn(_ctx);
        _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(2, _loop.Round);
    }

    [Test]
    public void 턴이_끝나면_예약이_비워진다()
    {
        _loop.BeginTurn(_ctx);
        _loop.Reserve(new UseSkillCommand(_aegis, _scout, TestSkills.Damage()));

        _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(0, _loop.ReservedCount);
    }

    // ── 승패 판정 ────────────────────────────────────────────────

    [Test]
    public void 적을_모두_쓰러뜨리면_승리다()
    {
        _echo.ApplyDamage(999f);

        _loop.BeginTurn(_ctx);
        _loop.Reserve(new UseSkillCommand(_aegis, _scout, TestSkills.Damage(100f)));

        TurnResult result = _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(BattleResult.Victory, result.Battle);
        Assert.IsTrue(result.IsBattleOver);
    }

    [Test]
    public void 파티가_전멸하면_패배다()
    {
        _aegis.ApplyDamage(999f);
        _seiren.ApplyDamage(999f);

        Assert.AreEqual(BattleResult.Defeat, TurnLoop.CheckBattle(_ctx));
    }

    [Test]
    public void 파티가_전원_ECHO여도_패배다()
    {
        var events = new List<CombatEvent>();
        EchoRules.ApplyER(_aegis, 100f, events);
        EchoRules.ApplyER(_seiren, 100f, events);

        Assert.IsTrue(_aegis.IsAlive);   // 살아는 있다
        Assert.AreEqual(BattleResult.Defeat, TurnLoop.CheckBattle(_ctx));
    }

    // ── ECHO 페널티 ──────────────────────────────────────────────

    [Test]
    public void ECHO_상태면_자기_차례에_페널티를_받는다()
    {
        var events = new List<CombatEvent>();
        EchoRules.ApplyER(_aegis, 100f, events);   // 최대체력 150의 10% = 15

        _loop.BeginTurn(_ctx);
        _loop.Reserve(new UseSkillCommand(_aegis, _scout, TestSkills.Damage()));
        _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(135f, _aegis.CurrentHP, Tolerance);
    }

    [Test]
    public void 레조넌스의_ECHO_페널티는_두_배다()
    {
        var events = new List<CombatEvent>();
        EchoRules.ApplyER(_seiren, 100f, events);   // 최대체력 120의 20% = 24

        _loop.BeginTurn(_ctx);
        _loop.Reserve(new UseSkillCommand(_seiren, _scout, TestSkills.Damage()));
        _loop.ExecuteTurn(_ctx);

        Assert.AreEqual(96f, _seiren.CurrentHP, Tolerance);
    }

    [Test]
    public void 페널티로_쓰러지면_그_행동은_취소된다()
    {
        _aegis.ApplyDamage(140f);   // 남은 체력 10, 페널티는 15

        var events = new List<CombatEvent>();
        EchoRules.ApplyER(_aegis, 100f, events);

        _loop.BeginTurn(_ctx);
        _loop.Reserve(new UseSkillCommand(_aegis, _scout, TestSkills.Damage()));
        _loop.ExecuteTurn(_ctx);

        Assert.IsFalse(_aegis.IsAlive);
        Assert.AreEqual(40f, _scout.CurrentHP, Tolerance);   // 공격이 들어가지 않았다
    }

    // ── 전투 완주 ────────────────────────────────────────────────

    [Test]
    public void 씬_없이_전투를_끝까지_진행할_수_있다()
    {
        BattleResult result = BattleResult.InProgress;

        for (int turn = 0; turn < 20 && result == BattleResult.InProgress; turn++)
        {
            _loop.BeginTurn(_ctx);

            foreach (Unit actor in _ctx.Party)
            {
                if (!actor.IsAlive) continue;

                Unit target = FindAlive(_ctx.Enemies);
                if (target == null) break;

                _loop.Reserve(new UseSkillCommand(actor, target, TestSkills.Damage()));
            }

            result = _loop.ExecuteTurn(_ctx).Battle;
        }

        Assert.AreEqual(BattleResult.Victory, result);
    }

    private static Unit FindAlive(IReadOnlyList<Unit> units)
    {
        for (int i = 0; i < units.Count; i++)
            if (units[i].IsAlive) return units[i];

        return null;
    }
}
