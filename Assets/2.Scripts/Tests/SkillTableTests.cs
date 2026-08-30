using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// 스킬 조회 테이블 검증.
///
/// 기존에는 각 컨트롤러가 SkillData 에셋을 인스펙터 드래그로 참조했다.
/// 에셋을 옮기거나 이름을 바꾸면 참조가 끊기고, 끊겨도 실행 전까지 알 수 없었다.
/// ID 조회로 바꾸면 실패가 즉시 드러나고, 세이브에 스킬을 기록할 수 있게 된다.
/// </summary>
public class SkillTableTests
{
    private static SkillSpec MakeSpec(string id, params SkillEffect[] effects)
        => new SkillSpec(
            skillId: id,
            displayName: id,
            type: SkillType.Basic,
            targetType: TargetType.SingleEnemy,
            spCost: 0,
            opCost: 0,
            erCost: 0f,
            effects: effects);

    [Test]
    public void 아이디로_스킬을_찾을_수_있다()
    {
        var table = new SkillTable(new[]
        {
            MakeSpec("SKILL_AEGIS_BASIC", new DamageEffect(1f)),
            MakeSpec("SKILL_CLAIRE_HEAL")
        });

        SkillSpec spec = table.Find("SKILL_AEGIS_BASIC");

        Assert.IsNotNull(spec);
        Assert.AreEqual(1, spec.Effects.Count);
    }

    [Test]
    public void 없는_아이디는_null을_반환한다()
    {
        var table = new SkillTable(new[] { MakeSpec("SKILL_A") });

        Assert.IsNull(table.Find("SKILL_없음"));
    }

    [Test]
    public void Get은_없는_아이디에_예외를_던진다()
    {
        var table = new SkillTable(new[] { MakeSpec("SKILL_A") });

        // 문자열 ID는 오타를 컴파일러가 잡아주지 못하므로
        // 조회 실패를 조용히 넘기지 않고 즉시 드러낸다
        Assert.Throws<KeyNotFoundException>(() => table.Get("SKILL_오타"));
    }

    [Test]
    public void 빈_아이디는_등록되지_않는다()
    {
        var table = new SkillTable(new[]
        {
            MakeSpec(""),
            MakeSpec("SKILL_A")
        });

        Assert.AreEqual(1, table.Count);
    }

    [Test]
    public void 같은_아이디는_나중_것으로_덮인다()
    {
        var table = new SkillTable(new[]
        {
            MakeSpec("SKILL_A", new DamageEffect(1f)),
            MakeSpec("SKILL_A")
        });

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(0, table.Get("SKILL_A").Effects.Count);
    }

    [Test]
    public void 테이블에서_꺼낸_스킬을_커맨드로_실행할_수_있다()
    {
        var table = new SkillTable(new[]
        {
            MakeSpec("SKILL_AEGIS_BASIC", new DamageEffect(1f))
        });

        var aegis = new Unit("Aegis", new UnitStats(150f, 50f, 20f));
        var scout = new Unit("LostScout", new UnitStats(40f, 10f, 0f));
        var ctx = new CombatContext(new[] { aegis }, new[] { scout });

        SkillSpec spec = table.Get("SKILL_AEGIS_BASIC");
        new UseSkillCommand(aegis, scout, spec).Execute(ctx);

        Assert.Less(scout.CurrentHP, 40f);
    }
}
