using System.Collections.Generic;

/// <summary>
/// 테스트용 난수. 정해둔 값을 순서대로 돌려주며, 다 쓰면 처음으로 돌아간다.
/// "이 확률이면 이 결과가 나와야 한다"를 검증할 때 쓴다.
/// </summary>
public sealed class ScriptedRandom : IRandom
{
    private readonly List<float> _values;
    private int _index;

    /// <param name="values">Value01이 순서대로 돌려줄 값들 (0~1)</param>
    public ScriptedRandom(params float[] values)
    {
        _values = new List<float>(values);
        if (_values.Count == 0) _values.Add(0f);
    }

    public float Value01()
    {
        float value = _values[_index];
        _index = (_index + 1) % _values.Count;
        return value;
    }

    public int Range(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) return minInclusive;

        int span = maxExclusive - minInclusive;
        return minInclusive + (int)(Value01() * span);
    }
}
