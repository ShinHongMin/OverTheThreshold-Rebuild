using System;
using System.Collections.Generic;

/// <summary>
/// 시전자의 공격력 × 배율 × 주는 회복량 보정으로 회복량을 계산한다.
/// </summary>
[Serializable]
public sealed class HealEffect : SkillEffect
{
    /// <summary>시전자의 공격력에 곱해지는 배율.</summary>
    public float multiplier = 1f;

    public HealEffect() { }

    public HealEffect(float multiplier)
    {
        this.multiplier = multiplier;
    }

    protected override void ApplyTo(CombatContext ctx, Unit actor, Unit target, List<CombatEvent> events)
    {
        // 사망한 대상은 회복되지 않는다.
        if (!target.IsAlive) return;

        float amount = actor.Stats.Get(StatType.ATK) * multiplier * (1f + actor.Stats.CardBonuses.HealingRatio);
        if (amount <= 0f) return;

        float healed = target.Heal(amount);
        if (healed <= 0f) return;

        events.Add(new HealReceived(target, healed));
    }
}
