using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EventChoice
{
    public string choiceText;
    [TextArea] public string resultText; // 성공 시 텍스트

    [Header("확률 설정 (0~100, 100이면 무조건 성공)")]
    [Range(0, 100)] public int successRate = 100;

    [Header("성공 효과")]
    public List<EffectInfo> effects;

    [Header("실패 시 (도박 실패)")]
    [TextArea] public string failureText; // 실패 시 텍스트
    public List<EffectInfo> failureEffects; // 실패 시 효과

}
