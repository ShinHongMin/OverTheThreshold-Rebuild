/// <summary>
/// 전투·맵·이벤트에서 사용하는 난수 공급자.
/// 호출부에서 구현을 주입해 테스트 결과를 고정하거나 난수 흐름을 분리한다.
/// </summary>
public interface IRandom
{
    /// <summary>minInclusive 이상 maxExclusive 미만의 정수.</summary>
    int Range(int minInclusive, int maxExclusive);

    /// <summary>0 이상 1 미만의 실수.</summary>
    float Value01();
}
