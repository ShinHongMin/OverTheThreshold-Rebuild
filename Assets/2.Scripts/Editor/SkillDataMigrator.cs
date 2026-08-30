#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 기존 SkillData 에셋들을 소유자별 SkillTableAsset으로 옮긴다.
///
/// 일회성 도구다. 이관이 끝나고 결과를 확인했으면 이 파일은 지워도 된다.
///
/// 폴더 구조를 그대로 살린다.
///   SkillDatas/Character/Aegis/*.asset   →  SkillTable_Aegis.asset
///   SkillDatas/Monster/LostScout/*.asset →  SkillTable_LostScout.asset
/// </summary>
public static class SkillDataMigrator
{
    private const string SourceRoot = "Assets/8.Datas/SkillDatas";
    private const string OutputFolder = "Assets/8.Datas/SkillTables";
    private const int DefaultBuffDuration = 2;

    [MenuItem("OTT/기존 SkillData 이관")]
    public static void Migrate()
    {
        Dictionary<string, List<SkillData>> grouped = LoadGroupedByOwner();

        if (grouped.Count == 0)
        {
            Debug.LogWarning($"[이관] {SourceRoot} 아래에서 SkillData를 찾지 못했습니다.");
            return;
        }

        EnsureFolder(OutputFolder);

        var needsReview = new List<string>();
        var created = new List<SkillTableAsset>();
        int totalSkills = 0;

        foreach (KeyValuePair<string, List<SkillData>> pair in grouped)
        {
            SkillTableAsset table = GetOrCreateTable(pair.Key);
            table.skills.Clear();

            foreach (SkillData source in pair.Value)
            {
                table.skills.Add(Convert(source, needsReview));
                totalSkills++;
            }

            EditorUtility.SetDirty(table);
            created.Add(table);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Report(totalSkills, created, needsReview);
    }

    /// <summary>
    /// 에셋 경로의 마지막 폴더 이름을 소유자로 삼는다.
    /// .../Character/Aegis/Aegis_BasicAttack.asset → "Aegis"
    /// </summary>
    private static Dictionary<string, List<SkillData>> LoadGroupedByOwner()
    {
        var result = new Dictionary<string, List<SkillData>>();

        foreach (string guid in AssetDatabase.FindAssets("t:SkillData", new[] { SourceRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SkillData>(path);
            if (asset == null) continue;

            string owner = Path.GetFileName(Path.GetDirectoryName(path));

            if (!result.TryGetValue(owner, out List<SkillData> list))
            {
                list = new List<SkillData>();
                result[owner] = list;
            }

            list.Add(asset);
        }

        return result;
    }

    private static SkillDefinition Convert(SkillData source, List<string> needsReview)
    {
        var definition = new SkillDefinition
        {
            skillId = source.name,
            displayName = source.skillName,
            description = source.description,
            skillIcon = source.skillIcon,
            type = source.type,
            targetType = source.targetType,
            spCost = source.spCost,
            opCost = source.opCost,
            presentation = ConvertPresentation(source),
            effects = new List<SkillEffect>()
        };

        AddMultiplierEffect(source, definition, needsReview);
        AddBuffEffects(source, definition);
        AddEREffects(source, definition, needsReview);

        return definition;
    }

    /// <summary>
    /// skillMultiplier를 어떤 효과로 옮길지 정한다.
    ///
    /// 적을 대상으로 하는 스킬은 피해가 확실하다. 아군을 대상으로 하면
    /// 회복일 수도 실드일 수도 있어 자동으로 정할 수 없다.
    /// </summary>
    private static void AddMultiplierEffect(SkillData source, SkillDefinition definition, List<string> needsReview)
    {
        if (source.skillMultiplier <= 0f) return;

        if (TargetsEnemy(source.targetType))
        {
            definition.effects.Add(new DamageEffect(source.skillMultiplier));
            return;
        }

        needsReview.Add($"{source.name} — 배율 {source.skillMultiplier} (대상 {source.targetType}). 회복/실드 중 선택 필요");
    }

    private static void AddBuffEffects(SkillData source, SkillDefinition definition)
    {
        if (source.buffToApply == null) return;

        foreach (BuffData buff in source.buffToApply)
        {
            if (buff == null) continue;

            definition.effects.Add(new BuffEffect(
                buff.name,          // 인스펙터 참조를 문자열 ID로 전환하는 지점
                buff.type,
                buff.value,
                DefaultBuffDuration));
        }
    }

    /// <summary>
    /// ER 관련 필드를 옮긴다.
    ///
    /// 기존 코드에서 erGain / erReduce는 스킬마다 쓰임이 달랐다.
    ///   세이렌  : 적을 때리며 자신의 ER을 올리고, erReduce는 사용 비용이었다
    ///   클레어  : 아군의 ER을 내렸다
    ///   몬스터  : 공격한 플레이어의 ER을 올렸다
    ///
    /// erGain은 대상 판단이 필요해 검토 목록에 남기고,
    /// erReduce는 대상 종류로 비용과 효과를 나눈다.
    /// </summary>
    private static void AddEREffects(SkillData source, SkillDefinition definition, List<string> needsReview)
    {
        bool targetsEnemy = TargetsEnemy(source.targetType);

        if (source.erGain > 0f)
        {
            // 적 대상 스킬의 ER 증가는 시전자 자신일 가능성이 높다(세이렌).
            // 다만 몬스터가 플레이어의 ER을 올리는 경우도 같은 형태라 확인이 필요하다.
            EffectTarget who = targetsEnemy && source.type != SkillType.Basic
                ? EffectTarget.Self
                : EffectTarget.Target;

            definition.effects.Add(new ERGainEffect(source.erGain, who));

            if (targetsEnemy)
                needsReview.Add($"{source.name} — ER 증가 {source.erGain} → 현재 {who}. 자신용인지 대상용인지 확인 필요");
        }

        if (source.erReduce > 0f)
        {
            if (targetsEnemy)
            {
                // 세이렌 계열: 사용 비용. 부족하면 스킬 자체를 쓸 수 없다
                definition.erCost = source.erReduce;
            }
            else
            {
                // 클레어 계열: 아군의 ER을 내리는 효과
                definition.effects.Add(new ERReduceEffect(source.erReduce, EffectTarget.Target));
            }
        }
    }

    private static bool TargetsEnemy(TargetType targetType)
        => targetType == TargetType.SingleEnemy || targetType == TargetType.AllEnemy;

    private static SkillPresentation ConvertPresentation(SkillData source)
    {
        return new SkillPresentation
        {
            attackType = source.attackType,
            animationTime = source.animationTime,
            preDelay = source.preDelay,
            postDelay = source.postDelay,
            totalDuration = source.totalDuration,
            effectPrefab = source.effectPrefab,
            hitEffectPrefab = source.hitEffectPrefab,
            useShake = source.useShake,
            shakeIntensity = source.shakeIntensity,
            shakeDuration = source.shakeDuration,
            cameraZoomAmount = source.cameraZoomAmount,
            skillSoundEffect = source.skillSoundEffect
        };
    }

    private static SkillTableAsset GetOrCreateTable(string owner)
    {
        string path = $"{OutputFolder}/SkillTable_{owner}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<SkillTableAsset>(path);
        if (existing != null) return existing;

        var created = ScriptableObject.CreateInstance<SkillTableAsset>();
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf = Path.GetFileName(folder);

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static void Report(int totalSkills, List<SkillTableAsset> tables, List<string> needsReview)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[이관 완료] SkillData {totalSkills}개 → 테이블 {tables.Count}개");
        sb.AppendLine($"위치: {OutputFolder}");

        foreach (SkillTableAsset table in tables)
            sb.AppendLine($"  · {table.name} ({table.skills.Count}개)");

        if (needsReview.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"■ 손으로 확인해야 하는 항목 {needsReview.Count}개");

            foreach (string line in needsReview)
                sb.AppendLine($"  · {line}");
        }

        sb.AppendLine();
        sb.AppendLine($"■ 버프 지속 턴은 전부 {DefaultBuffDuration}로 넣었습니다. 원본 코드를 보고 조정하세요.");

        Debug.Log(sb.ToString());

        if (tables.Count > 0)
            Selection.activeObject = tables[0];
    }
}
#endif
