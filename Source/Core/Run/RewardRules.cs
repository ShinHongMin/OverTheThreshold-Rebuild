using System;
using System.Collections.Generic;

public static class RewardRules
{
    /// <summary>목록을 복사하거나 섞지 않고 서로 다른 두 위치를 균등 추첨한다.</summary>
    public static string[] Draw(IReadOnlyList<string> pool, IRandom random)
    {
        if (pool == null || pool.Count == 0) return Array.Empty<string>();
        if (random == null) throw new ArgumentNullException(nameof(random));
        int first = random.Range(0, pool.Count);
        if (pool.Count == 1) return new[] { pool[first] };
        int second = random.Range(0, pool.Count - 1);
        if (second >= first) second++;
        return new[] { pool[first], pool[second] };
    }
}
