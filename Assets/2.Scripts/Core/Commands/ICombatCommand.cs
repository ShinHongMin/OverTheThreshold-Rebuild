/// <summary>
/// 예약된 행동 하나. 기존 ReservedAction 구조체를 대체한다.
///
/// 핵심은 Execute가 계산만 하고 연출을 전혀 하지 않는다는 점이다.
/// 기존에는 TurnManager의 코루틴 안에서 계산과 연출이 뒤섞여 있어
/// 연출을 건너뛰면 결과가 달라질 수 있었다.
///
/// 기존 ReservedAction은 actor가 PlayerController 타입이라 몬스터가 큐에
/// 들어갈 수 없었고, 그래서 실행 경로가 둘로 갈려 있었다. 여기서는
/// 아군·적군이 같은 큐를 쓴다.
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
