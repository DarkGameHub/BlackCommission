using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// One-shot editor tooling that turns <c>ArchiveWraith.fbx</c> (档案怨灵 — the archive-warden
/// horror; CC0 base mesh "Ghost Skull" from Quaternius' Ultimate Monsters pack, re-skinned to
/// the Black Commission palette via <c>ArchiveWraith_Atlas.png</c>) into a game-ready monster.
/// Mirrors <see cref="EchoMoldSetup"/>: Generic-rig import, a Pose-int AnimatorController, and
/// a prefab in Resources/Monsters so <see cref="MonsterSpawnBootstrap"/> can spawn it (seed
/// keyword "WARDEN"). Unlike the Blender-baked v1, the source FBX carries one take per clip,
/// so clips are selected by take name instead of frame ranges.
///
/// The brain is the shared <see cref="EchoMold"/> state machine with warden tuning: slower,
/// harder-hitting, never lures — a patrolling area-denial threat instead of a baiting hunter.
///
/// Run via <c>Tools ▸ Black Commission ▸ Monsters ▸ Build File Warden Asset</c>.
/// Idempotent — safe to re-run after swapping the FBX or atlas.
/// </summary>
public static class FileWardenSetup
{
    const string FbxPath = "Assets/_Project/Art/Monsters/FileWarden/ArchiveWraith.fbx";
    const string AtlasPath = "Assets/_Project/Art/Monsters/FileWarden/ArchiveWraith_Atlas.png";
    const string MaterialPath = "Assets/_Project/Art/Monsters/FileWarden/ArchiveWraith.mat";
    const string ControllerPath = "Assets/_Project/Art/Monsters/FileWarden/FileWarden.controller";
    const string PrefabPath = "Assets/_Project/Resources/Monsters/FileWarden.prefab";

    // Map source takes (suffix match) onto the four Pose-controller clips.
    struct ClipDef { public string name; public string takeSuffix; public bool loop; }
    static readonly ClipDef[] Clips =
    {
        new ClipDef { name = "FW_Idle",   takeSuffix = "Flying_Idle", loop = true  },
        new ClipDef { name = "FW_Hunt",   takeSuffix = "Fast_Flying", loop = true  },
        new ClipDef { name = "FW_Attack", takeSuffix = "Headbutt",    loop = false },
        new ClipDef { name = "FW_Death",  takeSuffix = "Death",       loop = false },
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
        var material = BuildMaterial();
        var controller = BuildController();
        BuildPrefab(controller, material);
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
        // Source model imports at 3.23 m tall / 5.3 m hand-span; 0.55 lands the wraith at ~1.8 m.
        importer.globalScale = 0.55f;
        importer.importConstraints = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        // Source materials are palette-atlas Standard stubs; the prefab overrides every
        // renderer with the URP re-skin material, so skip importing them entirely.
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importAnimation = true;

        // PASS 1 — plain import so takes are discoverable (see EchoMoldSetup).
        importer.clipAnimations = new ModelImporterClipAnimation[0];
        importer.SaveAndReimport();

        // PASS 2 — pick the four wanted takes by suffix, rename to the FW_* pose clips.
        var defaults = importer.defaultClipAnimations;
        var defs = new List<ModelImporterClipAnimation>();
        foreach (var c in Clips)
        {
            var take = defaults.FirstOrDefault(t => t.takeName.EndsWith(c.takeSuffix));
            if (take == null)
            {
                Debug.LogError($"[FileWarden] Take '*{c.takeSuffix}' not found. Available: " +
                               string.Join(", ", defaults.Select(t => t.takeName)));
                continue;
            }
            take.name = c.name;
            take.loopTime = c.loop;
            take.wrapMode = c.loop ? WrapMode.Loop : WrapMode.Once;
            take.loop = c.loop;
            defs.Add(take);
        }
        importer.clipAnimations = defs.ToArray();
        importer.SaveAndReimport();

        var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
            .OfType<AnimationClip>().Where(a => !a.name.StartsWith("__")).ToList();
        Debug.Log($"[FileWarden] Imported {clips.Count} clips: " +
                  string.Join(", ", clips.Select(c => $"{c.name}({c.length:0.00}s,loop={c.isLooping})")));
    }

    static Material BuildMaterial()
    {
        // Palette atlas: point filtering keeps the flat cells from bleeding at UV seams.
        if (AssetImporter.GetAtPath(AtlasPath) is TextureImporter texImp &&
            (texImp.filterMode != FilterMode.Point || texImp.mipmapEnabled))
        {
            texImp.filterMode = FilterMode.Point;
            texImp.mipmapEnabled = false;
            texImp.SaveAndReimport();
        }
        var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        mat.SetTexture("_BaseMap", atlas);
        mat.SetFloat("_Smoothness", 0.08f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static AnimatorController BuildController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Pose", AnimatorControllerParameterType.Int);
        // The shared EchoMold brain drives a Speed float for locomotion blends; the wraith
        // hovers (no blend needed) but the parameter must exist for the SetFloat calls.
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);

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

    static void BuildPrefab(AnimatorController controller, Material material)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (model == null) { Debug.LogError("[FileWarden] Could not load model for prefab build."); return; }

        // Root-node identity keys in imported takes would pin an Animator-on-root prefab to the
        // origin (see EchoMoldSetup) — components on a clean root, animated model as a child.
        var inst = new GameObject("FileWarden");
        var modelChild = (GameObject)Object.Instantiate(model, inst.transform);
        modelChild.name = "Model";
        modelChild.transform.localPosition = Vector3.zero;
        modelChild.transform.localRotation = Quaternion.identity;

        foreach (var r in inst.GetComponentsInChildren<Renderer>())
            r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();

        var renderers = inst.GetComponentsInChildren<Renderer>();
        Bounds b = default;
        if (renderers.Length > 0)
        {
            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            Debug.Log($"[FileWarden] Instance bounds size = {b.size}, min.y = {b.min.y:0.00} (wraith target ~1.6-2 m tall).");
        }

        if (!modelChild.TryGetComponent<Animator>(out var anim)) anim = modelChild.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        foreach (var rootAnim in inst.GetComponents<Animator>()) Object.DestroyImmediate(rootAnim);

        // Seal-red glow in the skull: reads in dark corridors, doubles as a threat telegraph.
        var glow = new GameObject("SealGlow");
        glow.transform.SetParent(inst.transform, false);
        glow.transform.localPosition = new Vector3(0f, Mathf.Max(1.2f, b.size.y * 0.72f), 0.25f);
        var light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1.0f, 0.22f, 0.12f);
        light.intensity = 2.4f;
        light.range = 3.5f;
        light.shadows = LightShadows.None;

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
