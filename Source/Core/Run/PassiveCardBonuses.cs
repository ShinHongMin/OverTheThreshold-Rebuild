using System;

/// <summary>유닛이 가진 카드 보정의 합. 획득할 때 갱신하며 전투 종료 시에도 유지한다.</summary>
public sealed class PassiveCardBonuses
{
    public float HealingRatio
    {
        get; private set;
    }

    public float ShieldRatio
    {
        get; private set;
    }

    public float StartHealRatio
    {
        get; private set;
    }

    public float StartShieldRatio
    {
        get; private set;
    }

    public float FullHpDamageRatio
    {
        get; private set;
    }

    public float DebuffDamageRatio
    {
        get; private set;
    }

    public float ReceivedERReduction
    {
        get; private set;
    }

    public float EchoResistanceChance
    {
        get; private set;
    }

    public int EchoOPGain
    {
        get; private set;
    }

    internal void Add(PassiveEffectType type, float value)
    {
        float ratio = value / 100f;
        switch (type)
        {
            case PassiveEffectType.IncreaseHeal_Percent:
                HealingRatio += ratio;
                break;
            case PassiveEffectType.IncreaseShield_Percent:
                ShieldRatio += ratio;
                break;
            case PassiveEffectType.BattleStart_Heal:
                StartHealRatio += ratio;
                break;
            case PassiveEffectType.BattleStart_Shield:
                StartShieldRatio += ratio;
                break;
            case PassiveEffectType.Damage_FullHP:
                FullHpDamageRatio += ratio;
                break;
            case PassiveEffectType.Damage_Debuffed:
                DebuffDamageRatio += ratio;
                break;
            case PassiveEffectType.ReceivedER_Reduction:
                ReceivedERReduction = Math.Min(1f, ReceivedERReduction + ratio);
                break;
            case PassiveEffectType.Echo_Resistance:
                EchoResistanceChance = Math.Min(1f, EchoResistanceChance + ratio);
                break;
            case PassiveEffectType.Echo_OP:
                EchoOPGain = Math.Min(PartyResources.DefaultMaxOP, EchoOPGain + (int)value);
                break;
        }
    }

    public float GetDamageBonus(Unit target)
    {
        float result = target.CurrentHP >= target.MaxHP ? FullHpDamageRatio : 0f;
        if (target.Stats.HasDebuff)
            result += DebuffDamageRatio;
        return result;
    }
}
