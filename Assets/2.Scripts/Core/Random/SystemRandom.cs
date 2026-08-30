using System;

/// <summary>
/// System.Random을 감싼 기본 구현.
///
/// 임시 구현이다. 전투 도중 저장·복원이 필요해지면 내부 상태를 꺼낼 수 있는
/// 구현체로 교체해야 한다. 교체해도 호출하는 쪽 코드는 바뀌지 않는다.
/// </summary>
public sealed class SystemRandom : IRandom
{
    private readonly Random _random;

    public SystemRandom() : this(Environment.TickCount) { }

    public SystemRandom(int seed)
    {
        _random = new Random(seed);
    }

    public int Range(int minInclusive, int maxExclusive)
        => maxExclusive <= minInclusive ? minInclusive : _random.Next(minInclusive, maxExclusive);

    public float Value01() => (float)_random.NextDouble();
}
