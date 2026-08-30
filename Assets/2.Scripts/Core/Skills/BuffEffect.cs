using System;
using System.Collections.Generic;

/// <summary>
/// 대상에게 버프나 디버프를 건다.
///
/// BuffData(ScriptableObject)를 직접 들지 않는다. Core는 UnityEngine을 모르므로
/// 필요한 값만 갖고, 식별은 BuffId 문자열로 한다.
/// </summary>
[Serializable]
public sealed class BuffEffect : SkillEffect
{
    /// <summary>버프 식별자. 현재는 BuffData 에셋의 파일 이름을 쓴다.</summary>
    public string buffId;

    public BuffType buffType;

    /// <summary>보정 수치. 0.2는 +20%. Taunt처럼 수치가 없는 버프는 0.</summary>
    public float value;

    /// <summary>지속 턴 수. 턴 종료마다 1씩 줄어든다.</summary>
    public int durationTurns = 2;

    public BuffEffect() { }

    public BuffEffect(string buffId, BuffType buffType, float value, int durationTurns)
    {
        this.buffId = buffId;
        this.buffType = buffType;
        this.value = value;
        this.durationTurns = durationTurns;
    }

    protected override void ApplyTo(CombatContext ctx, Unit actor, Unit target, List<CombatEvent> events)
    {
        if (!target.IsAlive) return;

        target.Stats.AddBuff(buffId, buffType, value, durationTurns);
        events.Add(new BuffApplied(target, buffId, buffType, value, durationTurns));
    }
}
