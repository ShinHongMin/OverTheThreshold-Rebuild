using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// 여러 턴을 진행하며 보스의 충전, 그로기, 체력 조건 패턴을 검증한다.
/// </summary>
public class AvatarBrainTests
{
    private const float Tolerance = 0.01f;

    private Unit _aegis, _claire;
    private Unit _boss;
    private CombatContext _ctx;
    private SkillTable _table;

    [SetUp]
    public void SetUp()
    {
        _aegis  = new Unit("Aegis",  new UnitStats(150f, 10f, 0f));
        _claire = new Unit("Claire", new UnitStats(100f, 15f, 0f));
        _boss   = new Unit("Avatar", new UnitStats(200f, 20f, 0f));

        _ctx = new CombatContext(new[] { _aegis, _claire }, new[] { _boss });

        _table = new SkillTable(new[]
        {
            TestSkills.Make("BOSS_BASIC", new DamageEffect(1f)),
            TestSkills.Make("BOSS_ENHANCED", new DamageEffect(3f)),
            TestSkills.Make("BOSS_CHARGE", SkillType.Basic, TargetType.Self,
                new ShieldEffect(0.3f, 4, useMaxHpAsBase: true) { effectTarget = EffectTarget.Self }),
            TestSkills.Make("BOSS_SELFGROGGY", SkillType.Basic, TargetType.Self,
                new BuffEffect("Buff_Groggy", BuffType.DEF_Percent, -0.5f, 2)
                    { effectTarget = EffectTarget.Self }),
        });
    }

    private AvatarBrain MakeBoss(float passiveER = 0f)
        => new AvatarBrain(
            new ScriptedRandom(0f), _table,
            new MonsterBrainConfig("BOSS_BASIC"),
            new AvatarBrainConfig("BOSS_CHARGE", "BOSS_ENHANCED", "BOSS_SELFGROGGY", passiveER));

    private static string SkillOf(ICombatCommand command)
        => command == null ? null : ((UseSkillCommand)command).Skill.SkillId;

    /// <summary>한 턴을 진행하고 사용한 스킬 ID를 반환한다. 행동하지 않으면 null.</summary>
    private string Act(AvatarBrain brain)
    {
        ICombatCommand command = brain.Decide(_ctx, _boss);
        command?.Execute(_ctx);

        return SkillOf(command);
    }

    /// <summary>충전 → 대기 → 강화기까지 한 사이클을 마친다.</summary>
    private void RunChargeCycle(AvatarBrain brain)
    {
        Act(brain);   // 1턴 기본
        Act(brain);   // 2턴 기본
        Act(brain);   // 3턴 충전
        Act(brain);   // 대기
        Act(brain);   // 강화기
    }

    // ── 사이클 ───────────────────────────────────────────────────

    [Test]
    public void 세_턴마다_충전한다()
    {
        AvatarBrain brain = MakeBoss();

        Assert.AreEqual("BOSS_BASIC", Act(brain));
        Assert.AreEqual("BOSS_BASIC", Act(brain));
        Assert.AreEqual("BOSS_CHARGE", Act(brain));   // 3턴째
    }

    [Test]
    public void 충전하면_충전_상태가_된다()
    {
        AvatarBrain brain = MakeBoss();

        Act(brain); Act(brain); Act(brain);

        Assert.IsTrue(brain.IsCharging);
        Assert.IsTrue(_boss.HasShield);   // 충전 스킬이 자기에게 실드를 건다
    }

    [Test]
    public void 충전_직후_한_턴은_대기한다()
    {
        AvatarBrain brain = MakeBoss();

        Act(brain); Act(brain); Act(brain);   // 충전

        Assert.IsNull(Act(brain));            // 대기 턴 — 행동하지 않는다
    }

    [Test]
    public void 실드가_남아_있으면_강화기를_쓴다()
    {
        AvatarBrain brain = MakeBoss();

        Act(brain); Act(brain); Act(brain);   // 충전
        Act(brain);                           // 대기

        Assert.AreEqual("BOSS_ENHANCED", Act(brain));
        Assert.IsFalse(brain.IsCharging);     // 충전 해제
    }

    [Test]
    public void 강화기를_쓰고_나면_사이클이_처음부터_다시_돈다()
    {
        AvatarBrain brain = MakeBoss();
        RunChargeCycle(brain);

        Assert.AreEqual("BOSS_BASIC", Act(brain));
        Assert.AreEqual("BOSS_BASIC", Act(brain));
        Assert.AreEqual("BOSS_CHARGE", Act(brain));
    }

    // ── 체력 트리거 ──────────────────────────────────────────────

    [Test]
    public void 체력_80퍼센트에_도달하면_강제로_충전한다()
    {
        AvatarBrain brain = MakeBoss();
        _boss.ApplyDamage(45f);   // 200 → 155 (77.5%)

        Assert.AreEqual("BOSS_CHARGE", Act(brain));
    }

    [Test]
    public void 체력_트리거는_한_번만_발동한다()
    {
        AvatarBrain brain = MakeBoss();
        _boss.ApplyDamage(45f);

        Act(brain);   // 트리거 충전
        Act(brain);   // 대기
        Act(brain);   // 강화기

        // 여전히 80% 아래지만 다시 발동하지 않는다
        Assert.AreEqual("BOSS_BASIC", Act(brain));
    }

    [Test]
    public void 체력_50퍼센트에서도_한_번_발동한다()
    {
        AvatarBrain brain = MakeBoss();

        _boss.ApplyDamage(45f);               // 200 → 155 (77.5%)
        Act(brain);                           // 80% 트리거 → 충전 (실드 60이 걸린다)
        Act(brain);                           // 대기
        Act(brain);                           // 강화기 — 여기서 충전이 풀린다

        // 남은 실드를 먼저 걷어내야 체력에 피해가 들어간다
        while (_boss.HasShield) _boss.TickShieldDuration();

        _boss.ApplyDamage(60f);               // 155 → 95 (47.5%)

        Assert.AreEqual("BOSS_CHARGE", Act(brain));
    }

    // ── 그로기 ───────────────────────────────────────────────────

    [Test]
    public void 충전_중_실드가_깨지면_즉시_그로기에_빠진다()
    {
        AvatarBrain brain = MakeBoss();
        Act(brain); Act(brain); Act(brain);   // 충전 (실드 60)

        BreakShield(brain);

        Assert.IsTrue(brain.IsGroggy);
        Assert.IsFalse(brain.IsCharging);
    }

    [Test]
    public void 실드가_없을_때_맞아도_그로기에_빠지지_않는다()
    {
        AvatarBrain brain = MakeBoss();

        BreakShield(brain);

        Assert.IsFalse(brain.IsGroggy);
    }

    [Test]
    public void 그로기_중에는_행동하지_않는다()
    {
        AvatarBrain brain = MakeBoss();
        Act(brain); Act(brain); Act(brain);
        BreakShield(brain);

        Assert.IsNull(Act(brain));
        Assert.IsNull(Act(brain));
    }

    [Test]
    public void 두_턴이_지나면_그로기에서_회복한다()
    {
        AvatarBrain brain = MakeBoss();
        Act(brain); Act(brain); Act(brain);
        BreakShield(brain);

        Assert.IsTrue(brain.IsGroggy);

        Act(brain);   // 그로기 1턴
        Act(brain);   // 그로기 2턴 — 여기서 회복

        Assert.IsFalse(brain.IsGroggy);

        // 회복 후에는 다시 행동한다. 사이클이 초기화되었으므로 기본공격부터다
        Assert.IsNotNull(Act(brain));
    }

    [Test]
    public void 충전_중_실드가_스스로_만료되면_자체_그로기_스킬을_쓴다()
    {
        AvatarBrain brain = MakeBoss();
        Act(brain); Act(brain); Act(brain);   // 충전
        Act(brain);                           // 대기 턴

        // 피격이 아니라 실드 지속이 다해 사라진 경우
        while (_boss.HasShield) _boss.TickShieldDuration();

        Assert.AreEqual("BOSS_SELFGROGGY", Act(brain));
    }

    // ── 패시브 ───────────────────────────────────────────────────

    [Test]
    public void 매_턴_상대_전원의_ER을_올린다()
    {
        AvatarBrain brain = MakeBoss(passiveER: 5f);

        Act(brain);

        Assert.AreEqual(5f, _aegis.CurrentER, Tolerance);
        Assert.AreEqual(5f, _claire.CurrentER, Tolerance);
    }

    [Test]
    public void 죽은_상대에게는_ER을_주지_않는다()
    {
        AvatarBrain brain = MakeBoss(passiveER: 5f);
        _claire.ApplyDamage(999f);

        Act(brain);

        Assert.AreEqual(5f, _aegis.CurrentER, Tolerance);
        Assert.AreEqual(0f, _claire.CurrentER, Tolerance);
    }

    [Test]
    public void 죽은_보스는_행동하지_않는다()
    {
        AvatarBrain brain = MakeBoss();
        _boss.ApplyDamage(999f);

        Assert.IsNull(brain.Decide(_ctx, _boss));
    }

    [Test]
    public void 강화기_이후_실드와_지속시간이_제거된다()
    {
        AvatarBrain brain = MakeBoss();
        RunChargeCycle(brain);
        Assert.AreEqual(0f, _boss.CurrentShield);
        Assert.AreEqual(0, _boss.ShieldDuration);
    }

    [Test]
    public void 실드_파괴가_그로기_디버프도_즉시_적용한다()
    {
        AvatarBrain brain = MakeBoss();
        Act(brain); Act(brain); Act(brain);
        BreakShield(brain);
        Assert.IsTrue(_boss.Stats.HasBuff("Buff_Groggy"));
    }

    [Test]
    public void 자체_그로기는_시전_차례를_포함해_두_번_쉰다()
    {
        AvatarBrain brain = MakeBoss();
        Act(brain); Act(brain); Act(brain);
        Act(brain);
        _boss.ClearShield();
        Assert.AreEqual("BOSS_SELFGROGGY", Act(brain));
        Assert.IsTrue(brain.IsGroggy);
        Assert.IsNull(Act(brain));
        Assert.IsFalse(brain.IsGroggy);
        Assert.AreEqual("BOSS_BASIC", Act(brain));
    }

    [Test]
    public void 턴루프에서_충전_대기_강화_순서와_실드_제거를_검증한다()
    {
        AvatarBrain brain = MakeBoss();
        var loop = new TurnLoop();
        loop.RegisterBrain(_boss, brain);
        string[] expected = { "BOSS_BASIC", "BOSS_BASIC", "BOSS_CHARGE", null, "BOSS_ENHANCED" };
        foreach (string id in expected)
        {
            loop.BeginTurn(_ctx);
            loop.ReserveBrainActions(_ctx, _ctx.Enemies);
            TurnResult result = loop.ExecuteTurn(_ctx);
            string actual = null;
            foreach (var e in result.Events) if (e is SkillCast cast) actual = cast.SkillId;
            Assert.AreEqual(id, actual);
        }
        Assert.IsFalse(_boss.HasShield);
    }

    [Test]
    public void 대기_차례에도_보스_패시브_ER_이벤트가_나온다()
    {
        AvatarBrain brain = MakeBoss(5f);
        Act(brain); Act(brain); Act(brain);
        var discard = new List<CombatEvent>();
        brain.AppendPendingEvents(discard);
        var loop = new TurnLoop();
        loop.RegisterBrain(_boss, brain);
        loop.BeginTurn(_ctx);
        loop.ReserveBrainActions(_ctx, _ctx.Enemies);
        var result = loop.ExecuteTurn(_ctx);
        int count = 0;
        foreach (var e in result.Events) if (e is ResourceChanged r && r.Kind == ResourceKind.ER) count++;
        Assert.AreEqual(2, count);
    }

    // ── 도우미 ───────────────────────────────────────────────────

    /// <summary>플레이어가 보스를 때려 실드를 깬다. Brain에게 이벤트가 전달된다.</summary>
    private void BreakShield(AvatarBrain brain)
    {
        var loop = new TurnLoop();
        loop.RegisterBrain(_boss, brain);
        loop.BeginTurn(_ctx);
        loop.Reserve(new UseSkillCommand(_aegis, _boss, TestSkills.Damage(20f)));
        loop.ExecuteTurn(_ctx);
    }
}
