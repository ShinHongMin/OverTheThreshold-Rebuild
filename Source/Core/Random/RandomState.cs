using System;

/// <summary>노드 저장에 필요한 시드와 추첨 횟수. 내부 필드에 대한 리플렉션 없이 복원한다.</summary>
[Serializable]
public sealed class RandomState
{
    public int seed;
    public int drawCount;
}
