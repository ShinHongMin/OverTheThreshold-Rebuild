using UnityEngine;

/// <summary>
/// UnitData의 AI 종류와 설정을 읽어 MonsterBrain을 생성한다.
/// </summary>
public static class MonsterBrainFactory
{
    public static IActionBrain Create(UnitData data, IRandom random, SkillTable skillTable)
    {
        if (data == null || data.brainKind == MonsterBrainKind.None) return null;

        var config = new MonsterBrainConfig(
            basicAttackId: data.basicAttackId,
            skillIds: data.skillIds,
            skillChance: data.skillChance,
            enhancedSkillId: data.enhancedSkillId,
            stateBuffId: data.stateBuffId);

        switch (data.brainKind)
        {
            case MonsterBrainKind.LostScout:
                return new LostScoutBrain(random, skillTable, config);

            case MonsterBrainKind.WhisperingEcho:
                return new WhisperingEchoBrain(random, skillTable, config);

            case MonsterBrainKind.CorruptedBulwark:
                return new CorruptedBulwarkBrain(random, skillTable, config);

            case MonsterBrainKind.ThresholdMass:
                return new ThresholdMassBrain(random, skillTable, config);

            case MonsterBrainKind.VoidHunter:
                return new VoidHunterBrain(random, skillTable, config);

            case MonsterBrainKind.Avatar:
                return new AvatarBrain(random, skillTable, config, new AvatarBrainConfig(
                    chargeSkillId: data.chargeSkillId,
                    enhancedSkillId: data.enhancedSkillId,
                    selfGroggySkillId: data.selfGroggySkillId,
                    passiveERPerTurn: data.passiveERPerTurn));

            default:
                Debug.LogWarning($"[Brain 없음] {data.unitName}의 {data.brainKind}가 아직 구현되지 않았습니다.", data);
                return null;
        }
    }
}
