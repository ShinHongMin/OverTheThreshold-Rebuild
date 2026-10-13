/// <summary>
/// UnitData의 기본 스탯과 직군으로 전투용 Unit을 생성한다.
/// </summary>
public static class UnitFactory
{
    public static Unit Create(UnitData data, bool usesResources)
    {
        var stats = new UnitStats(
            maxHp: data.baseHP,
            atk: data.baseATK,
            def: data.baseDEF,
            erResist: data.baseERResist);

        return new Unit(data.unitName, stats, data.job, usesResources);
    }
}
