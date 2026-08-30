using System;
using System.Collections.Generic;

/// <summary>
/// 대상에게 실드를 부여한다.
///
/// 기존 CombatCalculator.CalculateShield 규칙을 옮긴 것이다.
///   기본  : 대상의 방어력 × 배율
///   HP기반: 대상의 최대 체력 × 배율
/// </summary>
[Serializable]
public sealed class ShieldEffect : SkillEffect
{
    /// <summary>기준 스탯에 곱해지는 배율.</summary>
    public float multiplier = 1f;

    /// <summary>true면 최대 체력 기준, false면 방어력 기준으로 계산한다.</summary>
    public bool useMaxHpAsBase = false;

    /// <summary>실드가 유지되는 턴 수.</summary>
    public int durationTurns = 2;

    public ShieldEffect() { }

    public ShieldEffect(float multiplier, int durationTurns, bool useMaxHpAsBase = false)
    {
        this.multiplier = multiplier;
        this.durationTurns = durationTurns;
        this.useMaxHpAsBase = useMaxHpAsBase;
    }

    protected override void ApplyTo(CombatContext ctx, Unit actor, Unit target, List<CombatEvent> events)
    {
        if (!target.IsAlive) return;

        float baseValue = useMaxHpAsBase
            ? target.Stats.Get(StatType.MaxHP)
            : target.Stats.Get(StatType.DEF);

        float amount = baseValue * multiplier;
        if (amount <= 0f) return;

        float applied = target.AddShield(amount, durationTurns);
        if (applied <= 0f) return;

        events.Add(new ShieldGranted(target, applied));
    }
}
