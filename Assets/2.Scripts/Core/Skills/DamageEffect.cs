using System;
using System.Collections.Generic;

/// <summary>
/// 대상에게 피해를 준다.
///
/// 받는 피해 증가 디버프(Damage_Taken_Percent)가 여기서 반영된다.
/// 이 버프는 스탯이 아니라 데미지 파이프라인에서 처리되는 종류다.
/// </summary>
[Serializable]
public sealed class DamageEffect : SkillEffect
{
    /// <summary>공격력에 곱해지는 배율. 1이면 공격력 그대로.</summary>
    public float multiplier = 1f;

    public DamageEffect() { }

    public DamageEffect(float multiplier)
    {
        this.multiplier = multiplier;
    }

    protected override void ApplyTo(CombatContext ctx, Unit actor, Unit target, List<CombatEvent> events)
    {
        if (!target.IsAlive) return;

        float amp = 1f + target.Stats.SumBuffValue(BuffType.Damage_Taken_Percent);
        float damage = DamageFormula.Calculate(actor, target, multiplier, amp);

        DamageBreakdown breakdown = target.ApplyDamage(damage);
        events.Add(new DamageDealt(target, breakdown));

        if (!target.IsAlive)
            events.Add(new UnitDied(target));
    }
}
