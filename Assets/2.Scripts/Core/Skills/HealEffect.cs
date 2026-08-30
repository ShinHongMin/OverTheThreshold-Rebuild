using System;
using System.Collections.Generic;

/// <summary>
/// 대상의 체력을 회복시킨다.
///
/// 기존 CombatCalculator.CalculateHeal 규칙을 옮긴 것이다.
///   회복량 = 시전자의 공격력 × 배율
///
/// 배율의 의미는 효과 타입이 정한다. 같은 1.5라도 DamageEffect면 피해,
/// HealEffect면 회복, ShieldEffect면 실드다.
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
        // 사망한 대상은 회복되지 않는다. (기존 TakeHeal 규칙)
        if (!target.IsAlive) return;

        float amount = actor.Stats.Get(StatType.ATK) * multiplier;
        if (amount <= 0f) return;

        float healed = target.Heal(amount);
        if (healed <= 0f) return;

        events.Add(new HealReceived(target, healed));
    }
}
