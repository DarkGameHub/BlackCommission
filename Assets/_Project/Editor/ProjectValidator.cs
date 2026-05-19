using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools → Accident Squad → Validate Project
/// Checks for common mistakes before running the game.
/// </summary>
public static class ProjectValidator
{
    [MenuItem("Tools/Accident Squad/Validate Project")]
    static void Validate()
    {
        int errors = 0;
        int warnings = 0;

        Check(ref errors, ref warnings);

        string summary = errors == 0
            ? $"✓ 验证通过！({warnings} 个警告)"
            : $"✗ 发现 {errors} 个错误，{warnings} 个警告 — 详情见 Console";

        EditorUtility.DisplayDialog(
            errors == 0 ? "验证通过" : "发现问题",
            summary, "OK");
    }

    static void Check(ref int errors, ref int warnings)
    {
        // ── 1. 必需脚本类型存在 ────────────────────────────────────
        Type[] required =
        {
            typeof(PlayerController),
            typeof(PlayerCameraController),
            typeof(PlayerInteraction),
            typeof(CarrySystem),
            typeof(WaterLevelManager),
            typeof(WaterVisual),
            typeof(GameManager),
            typeof(PumpRepairInteraction),
            typeof(EvacuationPoint),
            typeof(SurvivorController),
            typeof(ConnectionManager),
            typeof(SimpleHUD),
            typeof(QuickNetworkUI),
            typeof(AutoPort),
        };

        foreach (var t in required)
        {
            if (t == null)
            {
                Debug.LogError($"[Validator] 脚本类型缺失: {t?.Name ?? "unknown"}");
                errors++;
            }
        }

        // ── 2. IInteractable 接口 ─────────────────────────────────
        CheckImplements<IInteractable>(typeof(PumpRepairInteraction), ref errors);
        CheckImplements<IInteractable>(typeof(EvacuationPoint), ref errors);

        // ── 3. Player Prefab 存在 ─────────────────────────────────
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Project/Prefabs/Player/Player.prefab");
        if (playerPrefab == null)
        {
            Debug.LogError("[Validator] Player.prefab 不存在 — 运行 Setup All");
            errors++;
        }
        else
        {
            RequireComponent<PlayerController>(playerPrefab, ref errors);
            RequireComponent<PlayerCameraController>(playerPrefab, ref errors, inChildren: true);
            RequireComponent<PlayerInteraction>(playerPrefab, ref errors);
            RequireComponent<CarrySystem>(playerPrefab, ref errors);
        }

        // ── 4. Mission Prefabs 存在 ───────────────────────────────
        CheckPrefab("Assets/_Project/Prefabs/Mission/Pump.prefab",
            new[] { typeof(PumpRepairInteraction) }, ref errors);
        CheckPrefab("Assets/_Project/Prefabs/Mission/EvacuationPoint.prefab",
            new[] { typeof(EvacuationPoint) }, ref errors);

        // ── 5. 场景存在 ───────────────────────────────────────────
        foreach (var scenePath in new[]
        {
            "Assets/_Project/Scenes/HQ.unity",
            "Assets/_Project/Scenes/Mall_B2.unity"
        })
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scenePath) == null)
            {
                Debug.LogError($"[Validator] 场景不存在: {scenePath} — 运行 Setup All");
                errors++;
            }
        }

        // ── 6. InputActions asset ─────────────────────────────────
        var ia = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
            "Assets/_Project/Scripts/Player/PlayerInputActions.inputactions");
        if (ia == null)
        {
            Debug.LogWarning("[Validator] PlayerInputActions.inputactions 不存在");
            warnings++;
        }

        if (errors == 0 && warnings == 0)
            Debug.Log("[Validator] ✓ 所有检查通过");
    }

    static void CheckImplements<T>(Type type, ref int errors)
    {
        if (!typeof(T).IsAssignableFrom(type))
        {
            Debug.LogError($"[Validator] {type.Name} 没有实现 {typeof(T).Name}");
            errors++;
        }
    }

    static void RequireComponent<T>(GameObject go, ref int errors, bool inChildren = false)
        where T : Component
    {
        bool found = inChildren
            ? go.GetComponentInChildren<T>() != null
            : go.GetComponent<T>() != null;

        if (!found)
        {
            Debug.LogError($"[Validator] {go.name} 缺少组件: {typeof(T).Name}");
            errors++;
        }
    }

    static void CheckPrefab(string path, Type[] components, ref int errors)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogError($"[Validator] Prefab 不存在: {path} — 运行 Setup All");
            errors++;
            return;
        }
        foreach (var t in components)
        {
            if (prefab.GetComponentInChildren(t) == null)
            {
                Debug.LogError($"[Validator] {prefab.name} 缺少组件: {t.Name}");
                errors++;
            }
        }
    }
}
