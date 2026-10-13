/// <summary>레조넌스의 강화 판정. 입력 표시와 행동 선택은 같은 기준을 사용한다.</summary>
public static class ResonanceRules
{
    public const float EnhancedThreshold = 50f;
    public static bool IsEnhanced(Unit unit)
        => unit != null && unit.UsesResources && unit.Job == CharacterJob.Resonance
            && unit.CurrentER >= EnhancedThreshold;
}
