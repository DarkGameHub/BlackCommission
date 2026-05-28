using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class MvpSmokeTestRunner
{
    const string HqScenePath = "Assets/_Project/Scenes/HQ.unity";
    const string SchoolScenePath = "Assets/_Project/Scenes/School_LostItem_01.unity";
    const string InputActionsPath = "Assets/_Project/Scripts/Player/PlayerInputActions.inputactions";
    const string ReportPath = "Logs/codex-mvp-smoke-test.txt";

    [MenuItem("Tools/Accident Squad/MVP/Run Smoke Test")]
    public static void RunMenu()
    {
        bool passed = Run();
        EditorUtility.DisplayDialog(
            passed ? "MVP Smoke Test Passed" : "MVP Smoke Test Failed",
            File.Exists(ReportPath) ? File.ReadAllText(ReportPath) : "No report generated.",
            "OK");
    }

    public static void RunFromCommandLine()
    {
        bool passed = Run();
        EditorApplication.Exit(passed ? 0 : 1);
    }

    static bool Run()
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        string originalScene = SceneManager.GetActiveScene().path;

        CheckInputBindings(errors);
        CheckHqScene(errors, warnings);
        CheckSchoolScene(errors, warnings);

        if (!string.IsNullOrEmpty(originalScene) && File.Exists(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        Directory.CreateDirectory("Logs");
        var lines = new List<string>
        {
            $"AccidentSquad MVP Smoke Test",
            $"Errors: {errors.Count}",
            $"Warnings: {warnings.Count}",
            ""
        };

        foreach (string error in errors)
            lines.Add($"ERROR: {error}");
        foreach (string warning in warnings)
            lines.Add($"WARN: {warning}");

        if (errors.Count == 0)
            lines.Add("PASS: MVP smoke checks passed.");

        File.WriteAllLines(ReportPath, lines);

        foreach (string error in errors)
            Debug.LogError($"[MVP Smoke] {error}");
        foreach (string warning in warnings)
            Debug.LogWarning($"[MVP Smoke] {warning}");
        if (errors.Count == 0)
            Debug.Log($"[MVP Smoke] Passed with {warnings.Count} warning(s). Report: {ReportPath}");

        return errors.Count == 0;
    }

    static void CheckInputBindings(List<string> errors)
    {
        string json = File.Exists(InputActionsPath) ? File.ReadAllText(InputActionsPath) : string.Empty;
        Require(json.Contains("\"path\": \"<Keyboard>/leftCtrl\""), "Crouch must be bound to left Ctrl.", errors);
        Require(json.Contains("\"path\": \"<Keyboard>/space\""), "Jump must be bound to Space.", errors);
        Require(json.Contains("\"path\": \"<Keyboard>/leftShift\""), "Sprint must be bound to left Shift.", errors);
    }

    static void NormalizeSerializedMvpValues(List<string> warnings)
    {
        NormalizeScene(HqScenePath, warnings);
        NormalizeScene(SchoolScenePath, warnings);
        NormalizeMonsterPrefab(warnings);
        AssetDatabase.SaveAssets();
    }

    static void NormalizeScene(string scenePath, List<string> warnings)
    {
        if (!File.Exists(scenePath)) return;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        bool changed = false;

        foreach (var hud in Object.FindObjectsByType<MvpHud>(FindObjectsSortMode.None))
            changed |= SetSerializedBool(hud, "showNetworkHint", false);

        foreach (var monster in Object.FindObjectsByType<SchoolMonsterAI>(FindObjectsSortMode.None))
            changed |= NormalizeMonster(monster);

        foreach (var mesh in Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
        {
            if (mesh == null || mesh.gameObject == null) continue;
            if (!mesh.gameObject.name.StartsWith("Text_")) continue;
            Object.DestroyImmediate(mesh.gameObject);
            changed = true;
        }

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            warnings.Add($"Normalized serialized MVP values in {scenePath}.");
        }
    }

    static void NormalizeMonsterPrefab(List<string> warnings)
    {
        const string path = "Assets/_Project/Prefabs/Mission/HomeworkDebtCollector.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) return;

        var monster = prefab.GetComponent<SchoolMonsterAI>();
        if (monster == null) return;

        if (NormalizeMonster(monster))
        {
            EditorUtility.SetDirty(prefab);
            warnings.Add("Normalized HomeworkDebtCollector prefab chase values.");
        }
    }

    static bool NormalizeMonster(SchoolMonsterAI monster)
    {
        bool changed = false;
        changed |= SetSerializedFloat(monster, "detectionRadius", 5.5f);
        changed |= SetSerializedFloat(monster, "loseRadius", 13f);
        changed |= SetSerializedFloat(monster, "initialGraceSeconds", 14f);
        changed |= SetSerializedFloat(monster, "chaseSpeed", 3.55f);
        return changed;
    }

    static void CheckHqScene(List<string> errors, List<string> warnings)
    {
        if (!File.Exists(HqScenePath))
        {
            errors.Add($"Missing HQ scene: {HqScenePath}");
            return;
        }

        EditorSceneManager.OpenScene(HqScenePath, OpenSceneMode.Single);
        CheckHud(errors);
        CheckOfficeComputer(errors, warnings);
        WarnIfGeneratedTextExists("HQ", warnings);
    }

    static void CheckSchoolScene(List<string> errors, List<string> warnings)
    {
        if (!File.Exists(SchoolScenePath))
        {
            errors.Add($"Missing school scene: {SchoolScenePath}");
            return;
        }

        EditorSceneManager.OpenScene(SchoolScenePath, OpenSceneMode.Single);
        CheckHud(errors);
        WarnIfGeneratedTextExists("School", warnings);

        var monster = Object.FindFirstObjectByType<SchoolMonsterAI>();
        Require(monster != null, "School scene must contain SchoolMonsterAI.", errors);
        if (monster == null) return;

        var so = new SerializedObject(monster);
        Require(so.FindProperty("detectionRadius").floatValue <= 6f, "Monster detection radius should stay tight enough for classroom stealth.", errors);
        Require(so.FindProperty("loseRadius").floatValue <= 14f, "Monster lose radius should allow players to break chase.", errors);
        Require(so.FindProperty("initialGraceSeconds").floatValue >= 14f, "Monster should give players time to enter from the school gate.", errors);
        Require(so.FindProperty("chaseSpeed").floatValue < 3.8f, "Monster chase speed should be slightly below player escape speed.", errors);
        Require(monster.GetComponent<NavMeshAgent>() != null, "Monster must keep NavMeshAgent component for baked maps.", errors);

        if (NavMesh.CalculateTriangulation().vertices.Length == 0)
            warnings.Add("School scene has no baked NavMesh. Monster should use direct-chase fallback until NavMesh is baked.");

        if (Object.FindFirstObjectByType<SchoolBonusEvidenceItem>() == null)
            warnings.Add("School scene has no saved optional evidence item. Runtime style pass should create OverdueLedgerEvidence on scene load.");
        if (Object.FindFirstObjectByType<SchoolEntranceDoor>() == null)
            warnings.Add("School scene has no saved entrance door. Runtime style pass should create the school gate on scene load.");
    }

    static void CheckOfficeComputer(List<string> errors, List<string> warnings)
    {
        var computer = Object.FindFirstObjectByType<OfficeComputer>();
        Require(computer != null, "HQ scene must contain OfficeComputer.", errors);
        if (computer == null) return;

        Require(!string.IsNullOrWhiteSpace(computer.InteractHint), "OfficeComputer must expose a non-empty interaction hint.", errors);
        var so = new SerializedObject(computer);
        SerializedProperty transit = so.FindProperty("dispatchTransitSeconds");
        if (transit == null || transit.floatValue < 1f)
            warnings.Add("OfficeComputer dispatch transit should be at least 1 second for the van ritual.");
        if (Object.FindFirstObjectByType<OfficeGroundItemPickup>() == null)
            warnings.Add("HQ scene has no saved floor storage pickups. Runtime style pass should create the G-drop storage mat.");
    }

    static void CheckHud(List<string> errors)
    {
        var hud = Object.FindFirstObjectByType<MvpHud>();
        Require(hud != null, "Scene must contain MvpHud.", errors);
        if (hud == null) return;

        var so = new SerializedObject(hud);
        SerializedProperty showNetworkHint = so.FindProperty("showNetworkHint");
        Require(showNetworkHint != null && !showNetworkHint.boolValue, "MvpHud network footer hint should be hidden by default.", errors);
    }

    static void WarnIfGeneratedTextExists(string label, List<string> warnings)
    {
        int count = 0;
        TextMesh[] meshes = Object.FindObjectsByType<TextMesh>(FindObjectsSortMode.None);
        foreach (var mesh in meshes)
        {
            if (mesh != null && mesh.gameObject.name.StartsWith("Text_"))
                count++;
        }

        if (count > 0)
            warnings.Add($"{label} scene still has {count} saved generated TextMesh object(s). Runtime cleanup should remove them on scene load.");
    }

    static void Require(bool condition, string message, List<string> errors)
    {
        if (!condition)
            errors.Add(message);
    }

    static bool SetSerializedBool(Object target, string propertyName, bool value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null || prop.boolValue == value) return false;
        prop.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        return true;
    }

    static bool SetSerializedFloat(Object target, string propertyName, float value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null || Mathf.Approximately(prop.floatValue, value)) return false;
        prop.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        return true;
    }
}
