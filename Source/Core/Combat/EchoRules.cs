using System;
using System.Collections.Generic;

/// <summary>
/// ER 증감과 ECHO 진입 규칙.
///
/// ECHO는 단방향이다. 전투 중에는 진입만 하고 해제되지 않으며,
/// 해제는 휴식 노드에서 ER을 내리는 것으로만 가능하다.
/// 불굴의 의지는 상태에 진입하기 전에 저항한다.
/// </summary>
public static class EchoRules
{
    /// <summary>ECHO 페널티. 최대 체력에 대한 비율.</summary>
    public const float EchoPenaltyRatio = 0.1f;

    /// <summary>제어를 유지하는 직군(레조넌스)의 페널티는 두 배다.</summary>
    public const float EchoPenaltyRatioKeepingControl = 0.2f;

    /// <summary>
    /// ER을 부여한다. 대상의 ER 저항이 반영된다.
    ///   최종 = 기본값 × (1 − ER저항) × 획득 배율 (카드의 외부 ER 감소도 추가 반영)
    ///
    /// ER이 100에 도달하면 ECHO 상태가 된다.
    /// </summary>
    public static void ApplyER(Unit target, float amount, List<CombatEvent> events, IRandom random = null, PartyResources resources = null)
    {
        if (target == null || !target.IsAlive || amount <= 0f) return;

        float resist = target.Stats.Get(StatType.ER_Resist);
        float finalER = amount * (1f - resist);
        if (finalER < 0f) finalER = 0f;

        // 침착한 대응은 일반 ER 저항 계산 뒤 외부 획득량을 추가로 줄인다.
        finalER *= 1f - target.Stats.CardBonuses.ReceivedERReduction;
        finalER *= target.Stats.Get(StatType.ER_GainMultiplier);
        ChangeER(target, target.CurrentER + finalER, events, random, resources);
    }

    /// <summary>자기 스킬·맵의 ER 획득. 획득 배율은 반영하며 ER 저항은 적용하지 않는다.</summary>
    public static void GainOwnER(Unit target, float amount, List<CombatEvent> events, IRandom random = null, PartyResources resources = null)
    {
        if (target == null || !target.IsAlive || amount <= 0f) return;
        ChangeER(target, target.CurrentER + amount * target.Stats.Get(StatType.ER_GainMultiplier), events, random, resources);
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
    /// ECHO 진입 후에는 비용으로도 ER을 낮추지 않는다.
    /// 제어를 유지하는 세이렌은 스킬을 사용해도 ER 100과 ECHO 상태를 유지한다.
    /// </summary>
    public static void SpendER(Unit actor, float amount, List<CombatEvent> events)
    {
        if (actor == null || amount <= 0f) return;
        if (actor.IsEchoState) return;

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

    private static void ChangeER(Unit target, float newValue, List<CombatEvent> events, IRandom random = null, PartyResources resources = null)
    {
        float before = target.CurrentER;
        bool enteringEcho = target.UsesResources && before < Unit.EchoThreshold && newValue >= Unit.EchoThreshold;
        if (enteringEcho)
        {
            float chance = target.Stats.CardBonuses.EchoResistanceChance;
            if (chance > 0f && chance < 1f && random == null)
                throw new InvalidOperationException("ECHO 저항 판정에 카드 난수가 필요합니다.");
            // 진입 전에 저항한다. 이미 ECHO인 상태를 해제하는 경로가 아니다.
            if (chance >= 1f || (chance > 0f && random.Value01() < chance)) newValue = 50f;
        }
        float delta = target.SetER(newValue);

        if (delta == 0f) return;

        events?.Add(new ResourceChanged(target, ResourceKind.ER, before, target.CurrentER));

        // 전투에서 실제로 ECHO에 진입한 경우에만 한 번 지급한다.
        // 맵에는 현재 전투의 OP가 없으므로 다음 전투 보너스로 이월하지 않는다.
        if (enteringEcho && target.IsEchoState && resources != null)
        {
            int beforeOP = resources.CurrentOP;
            resources.GainOP(target.Stats.CardBonuses.EchoOPGain);
            if (resources.CurrentOP != beforeOP)
                events?.Add(new ResourceChanged(null, ResourceKind.OP, beforeOP, resources.CurrentOP));
        }
    }
}
