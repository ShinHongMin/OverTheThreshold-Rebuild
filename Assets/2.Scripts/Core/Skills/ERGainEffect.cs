using System;
using System.Collections.Generic;

/// <summary>
/// 대상의 ER을 올린다. 기존 SkillData.erGain에 해당한다.
///
/// 증가에는 대상의 ER 저항이 반영된다.
///   실제 증가량 = amount × (1 − ER저항)
///
/// effectTarget으로 누구의 ER인지 정한다.
///   Self   — 세이렌이 적을 때리며 자신의 ER을 올리는 경우
///   Target — 몬스터가 공격한 플레이어의 ER을 올리는 경우
///
/// ER 감소는 규칙이 달라(ECHO 상태에서 차단) ERReduceEffect로 나뉘어 있다.
/// </summary>
[Serializable]
public sealed class ERGainEffect : SkillEffect
{
    /// <summary>올릴 ER의 양. 양수만 사용한다.</summary>
    public float amount;

    public ERGainEffect() { }

    public ERGainEffect(float amount, EffectTarget target = EffectTarget.Target)
    {
        this.amount = amount;
        effectTarget = target;
    }

    protected override void ApplyTo(CombatContext ctx, Unit actor, Unit target, List<CombatEvent> events)
    {
        if (!target.IsAlive || amount <= 0f) return;

        EchoRules.ApplyER(target, amount, events);
    }
}
