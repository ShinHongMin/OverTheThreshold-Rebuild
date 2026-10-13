using System;
using System.Collections.Generic;

/// <summary>
/// 스킬을 구성하는 개별 효과. 대상 해석은 공통으로 처리하고 ApplyTo에서 수치를 적용한다.
/// 파생 효과는 SkillDefinition의 SerializeReference 목록에서 조합한다.
/// </summary>
[Serializable]
public abstract class SkillEffect
{
    /// <summary>
    /// 이 효과가 적용될 대상. 기본은 스킬이 지정한 대상이다.
    /// Self로 두면 시전자 자신에게 적용된다.
    /// </summary>
    public EffectTarget effectTarget = EffectTarget.Target;

    /// <summary>
    /// 효과를 적용하고 일어난 일을 events에 기록한다.
    ///
    /// 대상 해석은 여기서 처리하므로 파생 클래스는 ApplyTo만 구현하면 된다.
    /// </summary>
    public void Apply(CombatContext ctx, Unit actor, Unit target, List<CombatEvent> events)
    {
        Unit resolved = effectTarget == EffectTarget.Self ? actor : target;
        if (resolved == null) return;

        ApplyTo(ctx, actor, resolved, events);
    }

    /// <summary>대상이 확정된 뒤의 실제 처리.</summary>
    protected abstract void ApplyTo(CombatContext ctx, Unit actor, Unit target, List<CombatEvent> events);
}
