using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

using System.Collections.Generic;

public static class ProjectValidation
{
    private static void Check(bool success, string message)
    {
        if (!success) throw new InvalidOperationException(message);
    }
    public static void VerifyData()
    {
        var assets = new List<SkillTableAsset>();
        foreach (string guid in AssetDatabase.FindAssets("t:SkillTableAsset", new[] { "Assets/8.Datas" }))
            assets.Add(AssetDatabase.LoadAssetAtPath<SkillTableAsset>(AssetDatabase.GUIDToAssetPath(guid)));
        var table = SkillTableBuilder.Build(assets);
        Check(table.Count == 28, "28개 스킬의 Unity 역직렬화 확인 실패");
        foreach (var asset in assets)
            foreach (var skill in asset.skills)
                foreach (var effect in skill.effects) Check(effect != null, "효과 역직렬화 누락: " + skill.skillId);
        var ally = new Unit("Ally", new UnitStats(10000f, 30f, 100f));
        var boss = new Unit("Boss", new UnitStats(2000f, 10f, 20f), usesResources: false);
        var ctx = new CombatContext(new[] { ally }, new[] { boss });
        new UseSkillCommand(boss, ally, new SkillSpec("HIT", "", SkillType.Basic, TargetType.SingleEnemy, 0, 0, 0,
            new SkillEffect[] { new DamageEffect(10f) })).Execute(ctx);
        float before = ally.CurrentHP;
        new UseSkillCommand(ally, ally, table.Get("Claire_Skill")).Execute(ctx);
        Check(ally.CurrentHP > before, "실제 Claire SO 회복 실패");
        var brain = new AvatarBrain(new ScriptedRandom(0f), table, new MonsterBrainConfig("AvatarofTheThreshold_BasicAttack"),
            new AvatarBrainConfig("AvatarofTheThreshold_Skill", "AvatarofTheThreshold_EnhancedSkill", "AvatarofTheThreshold_Groggy"));
        var loop = new TurnLoop();
        loop.RegisterBrain(boss, brain);
        string[] expected = { "AvatarofTheThreshold_BasicAttack", "AvatarofTheThreshold_BasicAttack", "AvatarofTheThreshold_Skill", null, "AvatarofTheThreshold_EnhancedSkill" };
        foreach (string id in expected)
        {
            loop.BeginTurn(ctx);
            loop.ReserveBrainActions(ctx, ctx.Enemies);
            var result = loop.ExecuteTurn(ctx);
            string actual = null;
            foreach (var e in result.Events) if (e is SkillCast cast) actual = cast.SkillId;
            Check(actual == id, "실제 보스 SO 사이클 불일치: " + actual + " / " + id);
        }
        Check(!boss.HasShield && !brain.IsCharging, "강화기 후 충전/실드 잔존");
    }
    public static void VerifyPool()
    {
        var owner = new GameObject("Pool test");
        var template = new GameObject("Template");
        var parent = new GameObject("Hidden parent");
        try
        {
            var pool = owner.AddComponent<PrefabPoolManager>();
            pool.Prewarm(template, 2, 2);
            var a = pool.Rent(template, parent.transform);
            parent.SetActive(false);
            var b = pool.Rent(template, parent.transform);
            Check(a != b, "비활성 부모의 대여 객체 중복 반환");
            Check(pool.Rent(template, parent.transform) == null, "풀 상한 미적용");
            Check(pool.Return(a) && !pool.Return(a), "중복 반납 처리 오류");
            for (int i = 0; i < 1000; i++)
            {
                var reused = pool.Rent(template, parent.transform);
                Check(reused == a, "동일 객체 재사용 실패");
                pool.Return(reused);
            }
            Check(pool.CreatedCount == 2, "반복 대여 중 추가 생성");
            pool.Return(b);
            Check(pool.ActiveCount == 0, "대여 객체 누수");

            // 비활성화 중 제자리 반환한 아이콘의 부모 UI가 제거된 경우에도 다시 대여할 수 있어야 한다.
            var removedWithParent = pool.Rent(template, parent.transform);
            Check(pool.Return(removedWithParent, moveToStorage: false), "제자리 반납 실패");
            UnityEngine.Object.DestroyImmediate(parent);
            var first = pool.Rent(template, owner.transform);
            var second = pool.Rent(template, owner.transform);
            Check(first != null && second != null && first != second, "부모 제거 후 풀 대여 복구 실패");
            Check(pool.Rent(template, owner.transform) == null, "파괴 항목 정리 후 풀 상한 미적용");
            Check(pool.CreatedCount == 3 && pool.ActiveCount == 2, "파괴 항목 수량 정리 실패");
            pool.ReturnAll();
            Check(pool.ActiveCount == 0, "복구된 풀의 대여 객체 누수");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(owner);
            UnityEngine.Object.DestroyImmediate(template);
            UnityEngine.Object.DestroyImmediate(parent);
        }
    }
    public static void VerifyRun()
    {
        var party = new UnitData[3];
        string[] names = { "Aegis", "Claire", "Seiren" };
        for (int i = 0; i < 3; i++) party[i] = AssetDatabase.LoadAssetAtPath<UnitData>("Assets/8.Datas/UnitData/" + names[i] + ".asset");
        var tables = new List<SkillTableAsset>();
        foreach (string guid in AssetDatabase.FindAssets("t:SkillTableAsset", new[] { "Assets/8.Datas" }))
            tables.Add(AssetDatabase.LoadAssetAtPath<SkillTableAsset>(AssetDatabase.GUIDToAssetPath(guid)));
        var services = new RunServices(party, tables, 12345);
        var sameSeed = new RunServices(party, tables, 12345);
        for (int i = 0; i < 100; i++) services.CombatRandom.Value01();
        Check(services.MapRandom.Value01() == sameSeed.MapRandom.Value01(), "전투 난수가 맵 난수에 영향을 줍니다.");
        var member = services.Run.Party[0];
        int battles = 0;
        while (battles < 2 && !services.Run.IsFinished)
        {
            int next = services.Run.Map[services.Run.CurrentNode].Next[0];
            Check(services.Run.TryEnter(next), "런 이동 실패");
            if (services.Run.Map[next].Type == NodeType.Event || services.Run.Map[next].Type == NodeType.Rest)
            { services.Run.CompleteNode(); continue; }
            var enemies = new[] { new Unit("Scout", new UnitStats(100, 10, 0), usesResources: false) };
            var ctx = services.Run.BeginCombat(enemies);
            Check(ReferenceEquals(member, ctx.Party[0]), "전투마다 파티를 재생성합니다.");
            services.FinishBattle(BattleResult.Victory);
            services.Run.CompleteNode();
            battles++;
        }
        Check(battles == 2, "연속 전투 진입/복귀 검증 실패");
    }
}
