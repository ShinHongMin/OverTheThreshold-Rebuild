using System;
using System.Collections.Generic;

/// <summary>맵 난수로 한 번만 생성한다. 전투 난수와 분리되어 복귀 시 맵이 변하지 않는다.</summary>
public static class MapGenerator
{
    public sealed class Node
    {
        public int Index { get; }
        public NodeType Type { get; }
        public IReadOnlyList<int> Next { get; }
        internal Node(int index, NodeType type, int[] next)
        {
            Index = index;
            Type = type;
            Next = Array.AsReadOnly(next);
        }
    }

    private static readonly int[][] Connections = {
            new[]{1,2,3},new[]{3,4},new[]{3,5},new[]{4,5,6},new[]{6,7},new[]{6,8},
            new[]{7,8,9},new[]{9,10},new[]{9,11},new[]{10,11,12},new[]{12,13},new[]{12,14},
            new[]{13,14,15},new[]{15,16},new[]{15,17},new[]{16,17,18},new[]{18,19},new[]{18,20},
            new[]{19,20,21},new[]{21},new[]{21},Array.Empty<int>()
    };

    public static IReadOnlyList<Node> Generate(IRandom random)
    {
        if (random == null)
            throw new ArgumentNullException(nameof(random));
        var nodes = new Node[22];
        for (int i = 0; i < nodes.Length; i++)
        {
            NodeType type = i == 0 ? NodeType.Start : i == 20 ? NodeType.Rest : i == 21 ? NodeType.Boss : Roll(i / 22f, random.Value01());
            nodes[i] = new Node(i, type, Connections[i]);
        }
        return Array.AsReadOnly(nodes);
    }

    public static IReadOnlyList<Node> Restore(IReadOnlyList<NodeType> types)
    {
        if (types == null || types.Count != 22 || types[0] != NodeType.Start || types[20] != NodeType.Rest || types[21] != NodeType.Boss)
            throw new ArgumentException("저장된 맵 구성이 올바르지 않습니다.");
        var nodes = new Node[22];
        for (int i = 0; i < nodes.Length; i++)
        {
            if (!Enum.IsDefined(typeof(NodeType), types[i]) || (i > 0 && i < 21 && (types[i] == NodeType.Start || types[i] == NodeType.Boss)))
                throw new ArgumentException("저장된 노드 종류가 올바르지 않습니다.");
            nodes[i] = new Node(i, types[i], Connections[i]);
        }
        return Array.AsReadOnly(nodes);
    }

    private static NodeType Roll(float progress, float value)
    {
        if (progress > 0.7f)
            return value < 0.3f ? NodeType.EliteHard : value < 0.5f ? NodeType.Elite : value < 0.8f ? NodeType.Battle : NodeType.Event;
        if (progress > 0.3f)
            return value < 0.2f ? NodeType.Elite : value < 0.7f ? NodeType.Battle : NodeType.Event;
        return value < 0.6f ? NodeType.Battle : NodeType.Event;
    }
}
