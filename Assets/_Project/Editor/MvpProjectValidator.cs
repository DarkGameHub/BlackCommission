using System.IO;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class MvpProjectValidator
{
    const string HqScenePath = "Assets/_Project/Scenes/HQ.unity";
    const string SchoolScenePath = "Assets/_Project/Scenes/School_LostItem_01.unity";
    const string TaskAssetPath = "Assets/_Project/Settings/Tasks/MissingHomeworkNotebook.asset";
    const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

    [MenuItem("Tools/Accident Squad/MVP/Validate School MVP")]
    static void ValidateSchoolMvp()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        int errors = 0;
        int warnings = 0;
        string originalScene = SceneManager.GetActiveScene().path;

        CheckAssets(ref errors, ref warnings);
        CheckBuildSettings(ref errors, ref warnings);
        CheckPlayerPrefab(ref errors, ref warnings);
        CheckHqScene(ref errors, ref warnings);
        CheckSchoolScene(ref errors, ref warnings);

        if (!string.IsNullOrEmpty(originalScene) && File.Exists(originalScene))
            EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);

        string summary = errors == 0
            ? $"MVP 验证通过。警告: {warnings}"
            : $"MVP 验证发现 {errors} 个错误，{warnings} 个警告。详情见 Console。";

        EditorUtility.DisplayDialog(errors == 0 ? "MVP 验证通过" : "MVP 验证失败", summary, "OK");
    }

    static void CheckAssets(ref int errors, ref int warnings)
    {
        var task = AssetDatabase.LoadAssetAtPath<OfficeTaskDefinition>(TaskAssetPath);
        if (task == null)
        {
            Error("缺少任务资产。请运行 Tools > Accident Squad > MVP > Setup School MVP。", ref errors);
        }
        else
        {
            if (task.sceneName != "School_LostItem_01")
                Error("任务资产 sceneName 不是 School_LostItem_01。", ref errors);
            if (task.recommendedPlayersMax > 4)
                Warning("任务推荐人数超过 4。", ref warnings);
            if (task.moneyReward <= 0 || task.experienceReward <= 0)
                Error("任务资产需要正向金钱和经验奖励。", ref errors);
            if (task.failureConsolationMoney < 0)
                Warning("失败安慰金为负数，MVP 失败路径可能过于惩罚。", ref warnings);
            if (task.failureExperience != 0)
                Warning("失败任务不应给公司经验；MVP 建议 failureExperience 为 0。", ref warnings);
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HqScenePath) == null)
            Error($"缺少 HQ 场景: {HqScenePath}", ref errors);
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SchoolScenePath) == null)
            Error($"缺少学校 MVP 场景: {SchoolScenePath}", ref errors);
    }

    static void CheckBuildSettings(ref int errors, ref int warnings)
    {
        string[] paths = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        int hqIndex = System.Array.IndexOf(paths, HqScenePath);
        int schoolIndex = System.Array.IndexOf(paths, SchoolScenePath);

        if (hqIndex < 0) Error("Build Settings 中缺少 HQ 场景。", ref errors);
        if (schoolIndex < 0) Error("Build Settings 中缺少 School_LostItem_01 场景。", ref errors);
        if (hqIndex >= 0 && hqIndex != 0)
            Warning("Build Settings 中 HQ 应该排第 1。", ref warnings);
        if (schoolIndex >= 0 && schoolIndex != 1)
            Warning("Build Settings 中 School_LostItem_01 应该排第 2。", ref warnings);
    }

    static void CheckPlayerPrefab(ref int errors, ref int warnings)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            Error("缺少 Player.prefab。请先运行基础 Setup All。", ref errors);
            return;
        }

        Require<NetworkObject>(prefab, "Player.prefab", ref errors);
        Require<ClientNetworkTransform>(prefab, "Player.prefab", ref errors);
        Require<PlayerController>(prefab, "Player.prefab", ref errors);
        Require<PlayerInteraction>(prefab, "Player.prefab", ref errors);
        Require<PlayerHealth>(prefab, "Player.prefab", ref errors);
        Require<PlayerHotbar>(prefab, "Player.prefab", ref errors);
    }

    static void CheckHqScene(ref int errors, ref int warnings)
    {
        if (!File.Exists(HqScenePath)) return;

        EditorSceneManager.OpenScene(HqScenePath, OpenSceneMode.Single);

        var networkManager = Object.FindFirstObjectByType<NetworkManager>();
        if (networkManager == null)
        {
            Error("HQ 缺少 NetworkManager。", ref errors);
        }
        else
        {
            if (networkManager.NetworkConfig.PlayerPrefab == null)
                Error("HQ NetworkManager 没有设置 Player Prefab。", ref errors);
            if (!networkManager.NetworkConfig.ConnectionApproval)
                Error("HQ NetworkManager 需要开启 ConnectionApproval 来限制 4 人上限。", ref errors);
            if (networkManager.GetComponent<MvpConnectionLimiter>() == null)
                Error("HQ NetworkManager 缺少 MvpConnectionLimiter。", ref errors);
        }

        var computer = Object.FindFirstObjectByType<OfficeComputer>();
        if (computer == null)
        {
            Error("HQ 缺少 OfficeComputer。", ref errors);
        }
        else
        {
            Require<NetworkObject>(computer.gameObject, "OfficeComputer", ref errors);
            var so = new SerializedObject(computer);
            var taskProp = so.FindProperty("demoTask");
            if (taskProp == null || taskProp.objectReferenceValue == null)
                Error("OfficeComputer 没有绑定 demoTask。", ref errors);
            var returnSceneProp = so.FindProperty("returnOfficeScene");
            if (returnSceneProp == null || returnSceneProp.stringValue != "HQ")
                Warning("OfficeComputer returnOfficeScene 应该是 HQ。", ref warnings);
        }

        if (Object.FindFirstObjectByType<MvpHud>() == null)
            Warning("HQ 缺少 MVP_HUD。", ref warnings);
    }

    static void CheckSchoolScene(ref int errors, ref int warnings)
    {
        if (!File.Exists(SchoolScenePath)) return;

        EditorSceneManager.OpenScene(SchoolScenePath, OpenSceneMode.Single);

        var manager = Object.FindFirstObjectByType<LostItemMissionManager>();
        if (manager == null)
            Error("学校场景缺少 LostItemMissionManager。", ref errors);
        else
            Require<NetworkObject>(manager.gameObject, "LostItemMissionManager", ref errors);

        var notebook = Object.FindFirstObjectByType<LostHomeworkItem>();
        if (notebook == null)
            Error("学校场景缺少 LostHomeworkItem。", ref errors);
        else
            Require<NetworkObject>(notebook.gameObject, "LostHomeworkItem", ref errors);

        var exit = Object.FindFirstObjectByType<SchoolExitPoint>();
        if (exit == null)
        {
            Error("学校场景缺少 SchoolExitPoint。", ref errors);
        }
        else
        {
            Require<NetworkObject>(exit.gameObject, "SchoolExitPoint", ref errors);
            var collider = exit.GetComponent<Collider>();
            if (collider == null || !collider.isTrigger)
                Error("SchoolExitPoint 需要 trigger collider。", ref errors);
        }

        var monster = Object.FindFirstObjectByType<SchoolMonsterAI>();
        if (monster == null)
        {
            Error("学校场景缺少 SchoolMonsterAI。", ref errors);
        }
        else
        {
            Require<NetworkObject>(monster.gameObject, "SchoolMonsterAI", ref errors);
            Require<NetworkTransform>(monster.gameObject, "SchoolMonsterAI", ref errors);
            Require<NavMeshAgent>(monster.gameObject, "SchoolMonsterAI", ref errors);
            var so = new SerializedObject(monster);
            var patrolProp = so.FindProperty("patrolPoints");
            if (patrolProp == null || patrolProp.arraySize == 0)
                Error("SchoolMonsterAI 缺少 patrolPoints。", ref errors);
        }

        if (Object.FindFirstObjectByType<HQSpawnManager>() == null)
            Warning("学校场景缺少 spawn manager，玩家可能不会被传送到校门。", ref warnings);
        if (Object.FindFirstObjectByType<MvpHud>() == null)
            Warning("学校场景缺少 MVP_HUD。", ref warnings);

        if (NavMesh.CalculateTriangulation().vertices.Length == 0)
            Warning("学校场景没有检测到已烘焙 NavMesh，怪物可能无法巡逻。", ref warnings);
    }

    static void Require<T>(GameObject go, string label, ref int errors) where T : Component
    {
        if (go.GetComponent<T>() == null)
            Error($"{label} 缺少组件: {typeof(T).Name}", ref errors);
    }

    static void Error(string message, ref int errors)
    {
        Debug.LogError($"[MVP Validator] {message}");
        errors++;
    }

    static void Warning(string message, ref int warnings)
    {
        Debug.LogWarning($"[MVP Validator] {message}");
        warnings++;
    }
}
