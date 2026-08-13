using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct EffectInfo
{
    [Header("효과 타입")]
    //public EventEffectType effectType; 

    [Header("수치 (힐량, ER수치 등)")]
    public float effectValue;

    [Header("영구 스탯 변경 시 대상 스탯")]
    public StatType targetStat;
    [Header("카드 보상일 경우에만 사용")]
    public PassiveCardData[] targetPassives;
    [Header("전투 발생 시 적 데이터")]
    public EnemyEncountData enemyEncounter;
}

[CreateAssetMenu(fileName ="NewEvent",menuName = "Map/Event Data")]
public class EventData : ScriptableObject
{
    [Header("이벤트 연출")]
    public string eventTitle;       
    public Sprite eventImage;       
    [TextArea(3, 5)]
    public string description;

    [Header("선택지 (보통 2~3개)")]
    public List<EventChoice> choices;
}
