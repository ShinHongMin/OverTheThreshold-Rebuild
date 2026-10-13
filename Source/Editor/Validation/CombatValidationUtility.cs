using System;
using UnityEngine;

/// <summary>전투 검증에서 사용하는 씬 경로와 복사본 실행 제한. 씬을 생성하거나 저장하지 않는다.</summary>
public static class CombatValidationUtility
{
    public const string ScenePath = "Assets/1.Scenes/CombatScene.unity";

    public static void RequireValidationCopy()
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\', '/').Contains("/Logs/W5Validation/"))
            throw new InvalidOperationException("검증용 복사본 배치 실행 전용입니다.");
    }
}
