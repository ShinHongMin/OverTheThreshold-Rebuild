/// <summary>
/// 난수 공급자. Core는 UnityEngine.Random을 쓸 수 없고,
/// 테스트에서 결과를 고정해야 하므로 인터페이스로 주입받는다.
///
/// 기존 코드의 난수 사용처는 전투 4곳, 맵·이벤트 6곳으로 갈린다.
/// 전투용과 맵용 스트림을 나누면 전투를 다시 시작해도 맵 구성이 흔들리지 않는다.
/// </summary>
public interface IRandom
{
    /// <summary>minInclusive 이상 maxExclusive 미만의 정수.</summary>
    int Range(int minInclusive, int maxExclusive);

    /// <summary>0 이상 1 미만의 실수.</summary>
    float Value01();
}
