using System;
using System.Collections.Generic;

/// <summary>
/// 대상의 ER을 내린다. 기존 SkillData.erReduce 중 "효과로서의 감소"에 해당한다.
///
/// 증가와 규칙이 다르다. 저항이 적용되지 않고, 대신 대상이 이미 ECHO 상태이면
/// 아무 일도 일어나지 않는다. 전투 중 자력으로 ECHO에서 벗어날 수 없게 하는 규칙이다.
/// (해제는 휴식 노드의 치료로만 가능하다)
///
/// 세이렌처럼 ER을 사용 비용으로 쓰는 경우는 이 효과가 아니라
/// SkillSpec.ERCost가 담당한다. 그쪽은 부족하면 스킬 사용 자체가 막힌다.
/// </summary>
[Serializable]
public sealed class ERReduceEffect : SkillEffect
{
    /// <summary>내릴 ER의 양. 양수만 사용한다.</summary>
    public float amount;

    public ERReduceEffect() { }

    public ERReduceEffect(float amount, EffectTarget target = EffectTarget.Target)
    {
        this.amount = amount;
        effectTarget = target;
    }

    protected override void ApplyTo(CombatContext ctx, Unit actor, Unit target, List<CombatEvent> events)
    {
        if (!target.IsAlive || amount <= 0f) return;

        EchoRules.ReduceER(target, amount, events);
    }
}
