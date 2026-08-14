/// <summary>
/// UnitData(ScriptableObject)를 읽어 Unit(순수 C#)을 만든다.
///
/// 이 클래스가 존재하는 이유는 계층 경계 때문이다.
/// Core는 UnityEngine에 의존할 수 없으므로 ScriptableObject를 직접 받을 수 없다.
/// 여기서 값을 꺼내 넘김으로써 SO 의존이 App 계층에서 끊긴다.
///
/// W2에서 패시브 카드와 세이브 영구 보너스를 여기서 함께 적용하게 된다.
/// </summary>
public static class UnitFactory
{
    public static Unit Create(UnitData data)
    {
        var stats = new UnitStats(
            maxHp: data.baseHP,
            atk: data.baseATK,
            def: data.baseDEF,
            erResist: data.baseERResist);

        return new Unit(data.unitName, stats);
    }
}
