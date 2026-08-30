using System.Collections.Generic;

/// <summary>
/// ER 증감과 ECHO 진입 규칙. 기존 ERManager(싱글톤)를 대체한다.
///
/// ECHO는 단방향이다. 전투 중에는 진입만 하고 해제되지 않으며,
/// 해제는 휴식 노드에서 ER을 내리는 것으로만 가능하다.
/// </summary>
public static class EchoRules
{
    /// <summary>ECHO 페널티. 최대 체력에 대한 비율.</summary>
    public const float EchoPenaltyRatio = 0.1f;

    /// <summary>제어를 유지하는 직군(레조넌스)의 페널티는 두 배다.</summary>
    public const float EchoPenaltyRatioKeepingControl = 0.2f;

    /// <summary>
    /// ER을 부여한다. 대상의 ER 저항이 반영된다.
    ///   최종 = 기본값 × (1 − ER저항)
    ///
    /// ER이 100에 도달하면 ECHO 상태가 된다.
    /// </summary>
    public static void ApplyER(Unit target, float amount, List<CombatEvent> events)
    {
        if (target == null || !target.IsAlive || amount <= 0f) return;

        float resist = target.Stats.Get(StatType.ER_Resist);
        float finalER = amount * (1f - resist);
        if (finalER < 0f) finalER = 0f;

        // 패시브 P_CALM_RESPONSE(ER 획득량 감소)는 카드 시스템과 함께 추가한다.

        ChangeER(target, target.CurrentER + finalER, events);
    }

    /// <summary>
    /// ER을 줄인다. ECHO 상태에서는 적용되지 않는다.
    /// 전투 중 자력 탈출이 불가능한 이유가 이 규칙이다.
    /// </summary>
    public static void ReduceER(Unit target, float amount, List<CombatEvent> events)
    {
        if (target == null || amount <= 0f) return;
        if (target.IsEchoState) return;

        ChangeER(target, target.CurrentER - amount, events);
    }

    /// <summary>
    /// 스킬 사용 비용으로 ER을 소비한다.
    ///
    /// ReduceER과 달리 ECHO 상태에서도 적용된다. 자원 소비이지 상태 회복이 아니며,
    /// 애초에 ECHO 상태에서는 비용을 낼 만큼의 여유가 있으므로 사용 자체가 가능하다.
    /// 레조넌스가 ECHO에서도 제어를 유지하는 것과 짝을 이루는 규칙이다.
    /// </summary>
    public static void SpendER(Unit actor, float amount, List<CombatEvent> events)
    {
        if (actor == null || amount <= 0f) return;

        ChangeER(actor, actor.CurrentER - amount, events);
    }

    /// <summary>
    /// 휴식 노드의 ECHO 치료. ER을 0으로 만들어 상태를 해제한다.
    /// ECHO 상태에서도 적용되는 감소 경로다.
    /// </summary>
    public static void CureEcho(Unit target, List<CombatEvent> events)
        => ChangeER(target, 0f, events);

    /// <summary>ECHO 상태에서 턴마다 받는 고정 피해량.</summary>
    public static float GetEchoPenalty(Unit unit)
    {
        if (unit == null || !unit.IsEchoState) return 0f;

        float ratio = unit.KeepsControlInEcho
            ? EchoPenaltyRatioKeepingControl
            : EchoPenaltyRatio;

        return unit.MaxHP * ratio;
    }

    /// <summary>ECHO 상태이면서 제어를 잃었는가. 시스템이 행동을 대신 정해야 하는 경우다.</summary>
    public static bool IsOutOfControl(Unit unit)
        => unit != null && unit.IsEchoState && !unit.KeepsControlInEcho;

    private static void ChangeER(Unit target, float newValue, List<CombatEvent> events)
    {
        float before = target.CurrentER;
        float delta = target.SetER(newValue);

        if (delta == 0f) return;

        events?.Add(new ResourceChanged(target, ResourceKind.ER, before, target.CurrentER));

        // 패시브 P_INDOMITABLE_WIL(확률 저항), P_TWILIGHT_POWER(진입 시 OP 획득)는
        // 카드 시스템과 함께 추가한다.
    }
}
