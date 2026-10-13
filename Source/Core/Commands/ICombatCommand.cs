/// <summary>
/// 아군과 적군이 공통으로 사용하는 전투 행동.
/// 실행 시 전투 수치를 계산하고, 연출에 필요한 결과 이벤트를 반환한다.
/// </summary>
public interface ICombatCommand
{
    /// <summary>행동 주체. TurnLoop이 ECHO 페널티 적용과 커맨드 교체에 사용한다.</summary>
    Unit Actor { get; }

    /// <summary>실행 직전에 검사한다. 예약 시점엔 살아 있었으나 순서가 밀려 죽은 경우 등.</summary>
    bool CanExecute(CombatContext ctx);

    /// <summary>즉시 계산하고 일어난 일들을 반환한다. 연출 없음.</summary>
    CommandResult Execute(CombatContext ctx);
}
