using System;
using System.Collections.Generic;

/// <summary>
/// 앞선 효과까지 반영한 ER이 기준 이상이면 피해와 ER 부여를 한 번 더 수행한다.
/// 보스 기본기의 DamageEffect, ERGainEffect 뒤에 배치한다. 재귀적으로 반복하지 않는다.
/// </summary>
[Serializable]
public sealed class ERThresholdStrikeEffect : SkillEffect
{
    public float threshold = 60f;
    public float multiplier = 1f;
    public float erAmount = 15f;

    protected override void ApplyTo(CombatContext ctx, Unit actor, Unit target, List<CombatEvent> events)
    {
        if (!target.IsAlive || target.CurrentER < threshold) return;
        new DamageEffect(multiplier).Apply(ctx, actor, target, events);
        new ERGainEffect(erAmount).Apply(ctx, actor, target, events);
    }
}
