using System;
using System.Collections.Generic;

/// <summary>
/// 스킬이 일으키는 효과 하나. (전략 패턴)
///
/// 기존에는 SkillData의 필드 조합(skillMultiplier, buffToApply, erGain...)을
/// 각 컨트롤러의 ExecuteSkill이 if문으로 해석했기 때문에
/// 새로운 종류의 효과를 추가하려면 코드를 수정해야 했다.
/// 효과를 클래스로 나누면 에셋에서 조합만 바꿔도 새 스킬이 만들어진다.
///
/// [SerializeReference]로 직렬화되므로 다음 제약을 지켜야 한다.
///   - record나 struct가 아닌 일반 class
///   - 매개변수 없는 생성자 (필드 초기화만)
///   - 프로퍼티가 아닌 public 필드
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
