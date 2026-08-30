using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// ECHO 제어 불능 상태의 행동 결정 검증.
/// 기존 TurnManager.ExecuteActionQueue의 ECHO 분기를 옮긴 것이다.
/// </summary>
public class EchoBrainTests
{
    private const float Tolerance = 0.01f;

    private Unit _aegis, _claire, _seiren;
    private Unit _scout;
    private CombatContext _ctx;
    private List<CombatEvent> _events;

    private SkillTable _table;
    private Dictionary<Unit, string> _basicIds;

    [SetUp]
    public void SetUp()
    {
        _aegis  = new Unit("Aegis",  new UnitStats(150f, 10f, 20f), CharacterJob.Vanguard);
        _claire = new Unit("Claire", new UnitStats(100f, 15f, 15f), CharacterJob.Medic);
        _seiren = new Unit("Seiren", new UnitStats(120f, 20f, 10f), CharacterJob.Resonance);
        _scout  = new Unit("LostScout", new UnitStats(40f, 10f, 5f));

        _ctx = new CombatContext(new[] { _aegis, _claire, _seiren }, new[] { _scout });
        _events = new List<CombatEvent>();

        _table = new SkillTable(new[] { TestSkills.Make("BASIC", new DamageEffect(1f)) });

        _basicIds = new Dictionary<Unit, string>
        {
            { _aegis, "BASIC" }, { _claire, "BASIC" },
            { _seiren, "BASIC" }, { _scout, "BASIC" }
        };
    }

    private EchoBrain MakeBrain(params float[] rolls)
        => new EchoBrain(new ScriptedRandom(rolls), _table, _basicIds);

    private static SkillSpec Basic() => TestSkills.Damage();

    // ── 대상 선택 ────────────────────────────────────────────────

    [Test]
    public void 자기_자신은_대상에서_제외된다()
    {
        EchoBrain brain = MakeBrain(0f);   // 첫 번째 후보

        ICombatCommand command = brain.Decide(_ctx, _aegis);

        Assert.IsNotNull(command);
        Assert.AreSame(_aegis, command.Actor);
        // 후보는 Claire, Seiren, LostScout 셋. Aegis는 빠진다
    }

    [Test]
    public void 아군도_대상이_될_수_있다()
    {
        // 후보 순서: Claire, Seiren, LostScout → 0f면 Claire
        EchoBrain brain = MakeBrain(0f);

        brain.Decide(_ctx, _aegis).Execute(_ctx);

        Assert.Less(_claire.CurrentHP, 100f);
    }

    [Test]
    public void 적도_대상이_될_수_있다()
    {
        // 후보 3명 중 마지막(LostScout)을 고르려면 0.9
        EchoBrain brain = MakeBrain(0.9f);

        brain.Decide(_ctx, _aegis).Execute(_ctx);

        Assert.Less(_scout.CurrentHP, 40f);
    }

    [Test]
    public void 죽은_유닛은_대상에서_제외된다()
    {
        _claire.ApplyDamage(999f);
        _seiren.ApplyDamage(999f);

        EchoBrain brain = MakeBrain(0f);   // 남은 후보는 LostScout뿐

        brain.Decide(_ctx, _aegis).Execute(_ctx);

        Assert.Less(_scout.CurrentHP, 40f);
    }

    [Test]
    public void 대상이_하나도_없으면_행동하지_않는다()
    {
        _claire.ApplyDamage(999f);
        _seiren.ApplyDamage(999f);
        _scout.ApplyDamage(999f);

        EchoBrain brain = MakeBrain(0f);

        Assert.IsNull(brain.Decide(_ctx, _aegis));
    }

    [Test]
    public void 기본공격이_등록되지_않은_유닛은_행동하지_않는다()
    {
        var brain = new EchoBrain(new ScriptedRandom(0f), _table, new Dictionary<Unit, string>());

        Assert.IsNull(brain.Decide(_ctx, _aegis));
    }

    // ── 턴 루프 연동 ─────────────────────────────────────────────

    [Test]
    public void 제어를_잃으면_예약한_행동이_교체된다()
    {
        EchoRules.ApplyER(_aegis, 100f, _events);

        var loop = new TurnLoop { EchoBrain = MakeBrain(0.9f) };   // LostScout를 고르게
        loop.BeginTurn(_ctx);

        // Claire를 때리려고 예약했지만 ECHO라 교체된다
        loop.Reserve(new UseSkillCommand(_aegis, _claire, Basic()));
        loop.ExecuteTurn(_ctx);

        Assert.AreEqual(100f, _claire.CurrentHP, Tolerance);   // 예약 대상은 무사
        Assert.Less(_scout.CurrentHP, 40f);                    // 교체된 대상이 맞았다
    }

    [Test]
    public void 레조넌스는_ECHO여도_예약한_행동을_그대로_한다()
    {
        EchoRules.ApplyER(_seiren, 100f, _events);

        var loop = new TurnLoop { EchoBrain = MakeBrain(0f) };
        loop.BeginTurn(_ctx);

        loop.Reserve(new UseSkillCommand(_seiren, _scout, Basic()));
        loop.ExecuteTurn(_ctx);

        Assert.Less(_scout.CurrentHP, 40f);        // 의도한 대상이 맞았다
        Assert.AreEqual(100f, _claire.CurrentHP, Tolerance);
    }

    [Test]
    public void ECHO가_아니면_교체되지_않는다()
    {
        var loop = new TurnLoop { EchoBrain = MakeBrain(0.9f) };
        loop.BeginTurn(_ctx);

        loop.Reserve(new UseSkillCommand(_aegis, _claire, Basic()));
        loop.ExecuteTurn(_ctx);

        Assert.Less(_claire.CurrentHP, 100f);   // 예약대로 아군을 때렸다
    }

    [Test]
    public void Brain이_없으면_예약한_행동을_그대로_한다()
    {
        EchoRules.ApplyER(_aegis, 100f, _events);

        var loop = new TurnLoop();   // EchoBrain 미지정
        loop.BeginTurn(_ctx);

        loop.Reserve(new UseSkillCommand(_aegis, _scout, Basic()));
        loop.ExecuteTurn(_ctx);

        Assert.Less(_scout.CurrentHP, 40f);
    }
}
