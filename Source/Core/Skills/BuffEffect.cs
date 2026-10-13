using System;
using System.Collections.Generic;

/// <summary>
/// 대상에게 버프나 디버프를 적용한다. BuffId가 같으면 수치와 지속 턴을 갱신한다.
/// </summary>
[Serializable]
public sealed class BuffEffect : SkillEffect
{
    /// <summary>버프 식별자. 현재는 BuffData 에셋의 파일 이름을 쓴다.</summary>
    public string buffId;

    public BuffType buffType;
    public bool isDebuff;

    /// <summary>보정 수치. 0.2는 +20%. Taunt처럼 수치가 없는 버프는 0.</summary>
    public float value;

    /// <summary>지속 턴 수. 턴 종료마다 1씩 줄어든다.</summary>
    public int durationTurns = 2;

    public BuffEffect() { }

    public BuffEffect(string buffId, BuffType buffType, float value, int durationTurns, bool isDebuff = false)
    {
        this.buffId = buffId;
        this.buffType = buffType;
        this.isDebuff = isDebuff;
        this.value = value;
        this.durationTurns = durationTurns;
    }

    protected override void ApplyTo(CombatContext ctx, Unit actor, Unit target, List<CombatEvent> events)
    {
        if (!target.IsAlive) return;

        target.Stats.AddBuff(buffId, buffType, value, durationTurns, isDebuff);
        events.Add(new BuffApplied(target, buffId, buffType, value, durationTurns));
    }
}
