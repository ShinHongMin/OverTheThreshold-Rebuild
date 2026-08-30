using NUnit.Framework;

/// <summary>
/// 버프와 스탯 보정 규칙 검증.
/// 기존 CharactersStat.AddBuff / GetModified* 규칙을 옮긴 것이다.
/// </summary>
public class BuffTests
{
    private const float Tolerance = 0.01f;

    private static UnitStats MakeStats()
        => new UnitStats(maxHp: 100f, atk: 100f, def: 100f, erResist: 0.1f);

    // ── 스탯 계산 ────────────────────────────────────────────────

    [Test]
    public void 보정이_없으면_기본값_그대로다()
    {
        UnitStats stats = MakeStats();

        Assert.AreEqual(100f, stats.Get(StatType.ATK), Tolerance);
    }

    [Test]
    public void 공격력_버프는_가산_퍼센트로_곱해진다()
    {
        UnitStats stats = MakeStats();

        stats.AddBuff("BUFF_A", BuffType.ATK_Percent, 0.3f, 2);

        Assert.AreEqual(130f, stats.Get(StatType.ATK), Tolerance);
    }

    [Test]
    public void 서로_다른_버프는_퍼센트가_합산된다()
    {
        UnitStats stats = MakeStats();

        stats.AddBuff("BUFF_A", BuffType.ATK_Percent, 0.3f, 2);
        stats.AddBuff("BUFF_B", BuffType.ATK_Percent, 0.2f, 2);

        // 100 * (1 + 0.3 + 0.2) = 150. 곱연산(1.3*1.2=1.56)이 아니다
        Assert.AreEqual(150f, stats.Get(StatType.ATK), Tolerance);
    }

    [Test]
    public void 패시브_보정과_버프는_같은_합에_더해진다()
    {
        UnitStats stats = MakeStats();

        stats.AddModifier(new StatModifier(StatType.ATK, 0.5f, ModSource.PassiveCard));
        stats.AddBuff("BUFF_A", BuffType.ATK_Percent, 0.3f, 2);

        Assert.AreEqual(180f, stats.Get(StatType.ATK), Tolerance);
    }

    [Test]
    public void ER저항은_곱이_아니라_합으로_계산된다()
    {
        UnitStats stats = MakeStats();   // base 0.1

        stats.AddBuff("BUFF_A", BuffType.ER_Resist_Percent, 0.2f, 2);

        // 0.1 + 0.2 = 0.3 (0.1 * 1.2 = 0.12 이 아니다)
        Assert.AreEqual(0.3f, stats.Get(StatType.ER_Resist), Tolerance);
    }

    [Test]
    public void ER저항은_1을_넘지_않는다()
    {
        UnitStats stats = MakeStats();

        stats.AddBuff("BUFF_A", BuffType.ER_Resist_Percent, 5f, 2);

        Assert.AreEqual(1f, stats.Get(StatType.ER_Resist), Tolerance);
    }

    [Test]
    public void 디버프는_음수_퍼센트로_적용된다()
    {
        UnitStats stats = MakeStats();

        stats.AddBuff("DEBUFF_A", BuffType.DEF_Percent, -0.4f, 2);

        Assert.AreEqual(60f, stats.Get(StatType.DEF), Tolerance);
    }

    [Test]
    public void 스탯에_작용하지_않는_버프는_계산에_끼어들지_않는다()
    {
        UnitStats stats = MakeStats();

        stats.AddBuff("BUFF_TAUNT", BuffType.Taunt, 0f, 2);
        stats.AddBuff("BUFF_VULN", BuffType.Damage_Taken_Percent, 0.5f, 2);

        Assert.AreEqual(100f, stats.Get(StatType.ATK), Tolerance);
        Assert.AreEqual(100f, stats.Get(StatType.DEF), Tolerance);
    }

    // ── 버프 관리 ────────────────────────────────────────────────

    [Test]
    public void 같은_버프를_다시_걸면_중첩되지_않고_갱신된다()
    {
        UnitStats stats = MakeStats();

        stats.AddBuff("BUFF_A", BuffType.ATK_Percent, 0.3f, 2);
        stats.AddBuff("BUFF_A", BuffType.ATK_Percent, 0.3f, 5);

        Assert.AreEqual(1, stats.Buffs.Count);
        Assert.AreEqual(5, stats.Buffs[0].DurationTurns);
        Assert.AreEqual(130f, stats.Get(StatType.ATK), Tolerance);   // 160이 아니다
    }

    [Test]
    public void 지속턴이_다하면_버프가_제거되고_스탯이_돌아온다()
    {
        UnitStats stats = MakeStats();
        stats.AddBuff("BUFF_A", BuffType.ATK_Percent, 0.3f, 2);

        stats.TickBuffDurations();
        Assert.AreEqual(130f, stats.Get(StatType.ATK), Tolerance);   // 아직 유효

        stats.TickBuffDurations();
        Assert.AreEqual(0, stats.Buffs.Count);
        Assert.AreEqual(100f, stats.Get(StatType.ATK), Tolerance);   // 원상복구
    }

    [Test]
    public void 버프를_아이디로_찾을_수_있다()
    {
        UnitStats stats = MakeStats();

        stats.AddBuff("Buff_VoidHunter_Mark", BuffType.Damage_Taken_Percent, 0.2f, 2);

        Assert.IsTrue(stats.HasBuff("Buff_VoidHunter_Mark"));
        Assert.IsFalse(stats.HasBuff("Buff_Aegis_Taunt"));
    }

    [Test]
    public void 같은_종류의_버프_수치를_합산할_수_있다()
    {
        UnitStats stats = MakeStats();

        stats.AddBuff("DEBUFF_A", BuffType.Damage_Taken_Percent, 0.2f, 2);
        stats.AddBuff("DEBUFF_B", BuffType.Damage_Taken_Percent, 0.3f, 2);

        Assert.AreEqual(0.5f, stats.SumBuffValue(BuffType.Damage_Taken_Percent), Tolerance);
    }

    // ── 보정 ─────────────────────────────────────────────────────

    [Test]
    public void 출처별로_보정을_제거할_수_있다()
    {
        UnitStats stats = MakeStats();

        stats.AddModifier(new StatModifier(StatType.ATK, 0.3f, ModSource.PassiveCard));
        stats.AddModifier(new StatModifier(StatType.ATK, 0.2f, ModSource.MetaProgress));

        stats.RemoveModifiersFrom(ModSource.PassiveCard);

        Assert.AreEqual(120f, stats.Get(StatType.ATK), Tolerance);
    }
}
