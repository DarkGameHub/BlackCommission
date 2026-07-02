using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// One-shot editor tooling that turns the Blender-exported <c>FileWarden.fbx</c> (档案看守 —
/// the archive-custodian horror, built by <c>tools/rigging/build_file_warden.py</c>) into a
/// game-ready monster. Mirrors <see cref="EchoMoldSetup"/>: Generic-rig import with the four
/// baked clips split out, a Pose-int AnimatorController, and a prefab in Resources/Monsters
/// so <see cref="MonsterSpawnBootstrap"/> can spawn it (seed keyword "WARDEN").
///
/// The brain is the shared <see cref="EchoMold"/> state machine with warden tuning: slower,
/// harder-hitting, never lures — a patrolling area-denial threat instead of a baiting hunter.
///
/// Run via <c>Tools ▸ Black Commission ▸ Monsters ▸ Build File Warden Asset</c>.
/// Idempotent — safe to re-run after re-exporting the FBX.
/// </summary>
public static class FileWardenSetup
{
    const string FbxPath = "Assets/_Project/Art/Monsters/FileWarden/FileWarden.fbx";
    const string ControllerPath = "Assets/_Project/Art/Monsters/FileWarden/FileWarden.controller";
    const string PrefabPath = "Assets/_Project/Resources/Monsters/FileWarden.prefab";

    // Clip splits — mirror tools/rigging/output/FileWarden_clips.json.
    struct ClipDef { public string name; public int first; public int last; public bool loop; }
    static readonly ClipDef[] Clips =
    {
        new ClipDef { name = "FW_Idle",   first = 1,   last = 96,  loop = true  },
        new ClipDef { name = "FW_Hunt",   first = 101, last = 196, loop = true  },
        new ClipDef { name = "FW_Attack", first = 201, last = 224, loop = false },
        new ClipDef { name = "FW_Death",  first = 231, last = 286, loop = false },
    };

    [MenuItem("Tools/Black Commission/Monsters/Build File Warden Asset")]
    public static void BuildFileWardenAsset()
    {
        if (!System.IO.File.Exists(FbxPath))
        {
            Debug.LogError($"[FileWarden] FBX not found at {FbxPath}");
            return;
        }

        ConfigureImporter();
        var controller = BuildController();
        BuildPrefab(controller);
        RegisterNetworkPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[FileWarden] Build complete.");
    }

    static void ConfigureImporter()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.useFileScale = true;
        importer.importConstraints = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.importAnimation = true;

        // PASS 1 — discover the baked take (see EchoMoldSetup for why two passes are needed).
        importer.clipAnimations = new ModelImporterClipAnimation[0];
        importer.SaveAndReimport();

        string takeName = importer.importedTakeInfos.Length > 0 ? importer.importedTakeInfos[0].name : "";
        if (string.IsNullOrEmpty(takeName))
            Debug.LogWarning("[FileWarden] No take info after first import — clip split will likely fail.");

        // PASS 2 — split into the four named clips.
        var defs = new List<ModelImporterClipAnimation>();
        foreach (var c in Clips)
        {
            defs.Add(new ModelImporterClipAnimation
            {
                name = c.name,
                takeName = takeName,
                firstFrame = c.first,
                lastFrame = c.last,
                loopTime = c.loop,
                wrapMode = c.loop ? WrapMode.Loop : WrapMode.Once,
                loop = c.loop,
            });
        }
        importer.clipAnimations = defs.ToArray();
        importer.SaveAndReimport();

        var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
            .OfType<AnimationClip>().Where(a => !a.name.StartsWith("__")).ToList();
        Debug.Log($"[FileWarden] Imported {clips.Count} clips: " +
                  string.Join(", ", clips.Select(c => $"{c.name}({c.length:0.00}s,loop={c.isLooping})")));
    }

    static AnimatorController BuildController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Pose", AnimatorControllerParameterType.Int);

        var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
            .OfType<AnimationClip>().Where(a => !a.name.StartsWith("__"))
            .ToDictionary(a => a.name, a => a);
        AnimationClip Get(string n) => clips.TryGetValue(n, out var c) ? c : null;

        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("Idle");   idle.motion = Get("FW_Idle");
        var hunt = sm.AddState("Hunt");   hunt.motion = Get("FW_Hunt");
        var atk  = sm.AddState("Attack"); atk.motion  = Get("FW_Attack");
        var dead = sm.AddState("Dead");   dead.motion = Get("FW_Death");
        sm.defaultState = idle;

        AddAnyState(sm, idle, AnimatorConditionMode.Less, 2);
        AddAnyState(sm, hunt, AnimatorConditionMode.Equals, 2);
        AddAnyState(sm, atk,  AnimatorConditionMode.Equals, 3);
        AddAnyState(sm, dead, AnimatorConditionMode.Equals, 4);

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static void AddAnyState(AnimatorStateMachine sm, AnimatorState dst, AnimatorConditionMode mode, int threshold)
    {
        var t = sm.AddAnyStateTransition(dst);
        t.AddCondition(mode, threshold, "Pose");
        t.duration = 0.12f;
        t.hasExitTime = false;
        t.canTransitionToSelf = false;
    }

    static void BuildPrefab(AnimatorController controller)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (model == null) { Debug.LogError("[FileWarden] Could not load model for prefab build."); return; }

        // Root-node identity keys in the baked take would pin an Animator-on-root prefab to the
        // origin (see EchoMoldSetup) — components on a clean root, animated model as a child.
        var inst = new GameObject("FileWarden");
        var modelChild = (GameObject)Object.Instantiate(model, inst.transform);
        modelChild.name = "Model";
        modelChild.transform.localPosition = Vector3.zero;
        modelChild.transform.localRotation = Quaternion.identity;

        var renderers = inst.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            Debug.Log($"[FileWarden] Instance bounds size = {b.size} (expect ~2.2 m tall).");
        }

        if (!modelChild.TryGetComponent<Animator>(out var anim)) anim = modelChild.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        foreach (var rootAnim in inst.GetComponents<Animator>()) Object.DestroyImmediate(rootAnim);

        if (!inst.TryGetComponent<NavMeshAgent>(out var agent)) agent = inst.AddComponent<NavMeshAgent>();
        agent.radius = 0.45f;
        agent.height = 2.2f;
        agent.baseOffset = 0f;
        agent.speed = 0.8f;
        agent.angularSpeed = 240f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 1.3f;
        agent.autoBraking = true;

        if (!inst.TryGetComponent<CapsuleCollider>(out var col)) col = inst.AddComponent<CapsuleCollider>();
        col.radius = 0.45f;
        col.height = 2.2f;
        col.center = new Vector3(0f, 1.1f, 0f);

        if (inst.GetComponent<NetworkObject>() == null) inst.AddComponent<NetworkObject>();
        if (!inst.TryGetComponent<EchoMold>(out var brain)) brain = inst.AddComponent<EchoMold>();

        // Warden tuning on the shared brain: a slow, heavy, never-luring area guardian.
        var so = new SerializedObject(brain);
        so.FindProperty("senseRadius").floatValue = 18f;
        so.FindProperty("huntTriggerRange").floatValue = 10f;
        so.FindProperty("attackRange").floatValue = 1.8f;
        so.FindProperty("loseTargetRange").floatValue = 20f;
        so.FindProperty("loseTargetGrace").floatValue = 6f;
        so.FindProperty("roamSpeed").floatValue = 0.8f;
        so.FindProperty("huntSpeed").floatValue = 2.6f;
        so.FindProperty("roamRadius").floatValue = 8f;
        so.FindProperty("lureInterval").floatValue = 999999f; // wardens never throw voices
        so.FindProperty("dmgPerTick").floatValue = 16f;
        so.FindProperty("dmgTick").floatValue = 0.8f;
        so.ApplyModifiedPropertiesWithoutUndo();

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
        var prefab = PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath, out bool ok);
        Object.DestroyImmediate(inst);
        Debug.Log($"[FileWarden] Prefab saved at {PrefabPath} (ok={ok}).");
    }

    /// <summary>NGO's import hook usually auto-registers; this is the explicit fallback.</summary>
    static void RegisterNetworkPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
        if (prefab == null || list == null)
        {
            Debug.LogWarning("[FileWarden] Could not load prefab or DefaultNetworkPrefabs for registration check.");
            return;
        }
        foreach (var e in list.PrefabList)
            if (e.Prefab == prefab) { Debug.Log("[FileWarden] Already network-registered."); return; }
        list.Add(new NetworkPrefab { Prefab = prefab });
        EditorUtility.SetDirty(list);
        Debug.Log("[FileWarden] Added to DefaultNetworkPrefabs.");
    }
}
