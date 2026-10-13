using UnityEngine;

/// <summary>강사 PoolUtil과 같은 프리팹별 사전 생성 설정. 상한은 무제한 증가를 막는다.</summary>
[CreateAssetMenu(fileName = "PrefabPoolSettings", menuName = "OTT/Pool Data")]
public sealed class PrefabPoolSettings : ScriptableObject
{
    public GameObject prefab;
    [Min(0)] public int initialCount = 8;
    [Min(1)] public int capacity = 64;
}
