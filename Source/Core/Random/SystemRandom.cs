using System;

/// <summary>시드와 추첨 횟수로 같은 난수 흐름을 이어 간다. 복원은 노드 로드 때만 수행한다.</summary>
public sealed class SystemRandom : IRandom
{
    public const int MaximumSavedDrawCount = 1000000;
    private readonly Random _random;
    private readonly int _seed;
    private int _drawCount;

    public SystemRandom() : this(Environment.TickCount) { }
    public SystemRandom(int seed)
    {
        _seed = seed;
        _random = new Random(seed);
    }

    private double NextSample()
    {
        _drawCount++;
        return _random.NextDouble();
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;
        // 일반 정수 구간은 기존 Random.Next와 같은 double 표본을 사용한다.
        return (int)(minInclusive + (long)(NextSample() * ((long)maxExclusive - minInclusive)));
    }

    public float Value01() => (float)NextSample();
    public RandomState CaptureState() => new RandomState { seed = _seed, drawCount = _drawCount };
    public static SystemRandom Restore(RandomState state)
    {
        if (state == null || state.drawCount < 0 || state.drawCount > MaximumSavedDrawCount)
            throw new ArgumentException("저장된 난수 정보가 올바르지 않습니다.");
        var random = new SystemRandom(state.seed);
        for (int i = 0; i < state.drawCount; i++)
            random.NextSample();
        return random;
    }
}
