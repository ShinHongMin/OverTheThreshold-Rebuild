using System.Collections.Generic;

/// <summary>
/// 스킬 하나를 대상에게 사용한다.
///
/// W1 범위이므로 단일 대상 · 피해만 처리한다.
/// 효과 목록(SkillEffect[])과 타게팅 규칙은 W2 이후에 붙인다.
/// SkillData(ScriptableObject)를 직접 받지 않는 이유는 Core가
/// UnityEngine에 의존하지 않기 위해서다. 필요한 수치만 넘겨받는다.
/// </summary>
public sealed class UseSkillCommand : ICombatCommand
{
    private readonly Unit _actor;
    private readonly Unit _target;
    private readonly float _skillMultiplier;

    public UseSkillCommand(Unit actor, Unit target, float skillMultiplier)
    {
        _actor = actor;
        _target = target;
        _skillMultiplier = skillMultiplier;
    }

    public bool CanExecute(CombatContext ctx)
    {
        if (_actor == null || _target == null) return false;
        return _actor.IsAlive && _target.IsAlive;
    }

    public CommandResult Execute(CombatContext ctx)
    {
        float damage = DamageFormula.Calculate(_actor, _target, _skillMultiplier);
        float applied = _target.ApplyDamage(damage);

        var events = new List<CombatEvent>
        {
            new DamageDealt(_target, applied, _target.CurrentHP)
        };

        return new CommandResult(events);
    }
}
