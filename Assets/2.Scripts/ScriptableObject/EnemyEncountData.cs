using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyPool", menuName = "EnemyPool/Enemey Ecount Data")]
public class EnemyEncountData : ScriptableObject
{
    [Header("전투 정보")]
    public string encounterName; // 예: "정찰병 2마리 + 메아리 1마리"

    [Header("등장할 몬스터들 (순서대로 배치)")]
    // 실제 몬스터 프리팹이나 데이터를 넣습니다.
    public List<GameObject> enemyPrefabs;

    [Header("보스 유무")]
    public bool isBoss;

    //[Header("보상 가중치")]
    //카드 리워드를 여기에 넣어도 될듯
}
