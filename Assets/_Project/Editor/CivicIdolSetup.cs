using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// One-shot editor tooling that turns <c>CivicIdol.fbx</c> (市政圣像 — the freeze-when-watched
/// municipal statue; CC0 base mesh "Demon" from Quaternius' Ultimate Monsters pack, re-skinned
/// to weathered verdigris bronze via <c>CivicIdol_Atlas.png</c>, statue mode of
/// <c>tools/rigging/recolor_atlas.py</c>) into a game-ready monster. Mirrors
/// <see cref="FileWardenSetup"/>: Generic-rig import, take-name clip selection, a Pose-int
/// AnimatorController, and a prefab in Resources/Monsters so <see cref="MonsterSpawnBootstrap"/>
/// can spawn it (seed keywords "IDOL" / "STATUE").
///
/// The source model carries a handheld trident mesh (node "Trident") — stripped at prefab
/// build so the statue reads as a bare civic monument (horns + wings + halo carry the read).
/// The brain is <see cref="CivicIdol"/>; freezing is done by pausing the Animator, so the
/// controller stays a plain pose switch with no blend trees.
///
/// Run via <c>Tools ▸ Black Commission ▸ Monsters ▸ Build Civic Idol Asset</c>.
/// Idempotent — safe to re-run after swapping the FBX or atlas.
/// </summary>
public static class CivicIdolSetup
{
    const string FbxPath = "Assets/_Project/Art/Monsters/CivicIdol/CivicIdol.fbx";
    const string AtlasPath = "Assets/_Project/Art/Monsters/CivicIdol/CivicIdol_Atlas.png";
    const string MaterialPath = "Assets/_Project/Art/Monsters/CivicIdol/CivicIdol.mat";
    const string ControllerPath = "Assets/_Project/Art/Monsters/CivicIdol/CivicIdol.controller";
    const string PrefabPath = "Assets/_Project/Resources/Monsters/CivicIdol.prefab";

    // Handheld prop nodes stripped from the statue at prefab build.
    static readonly string[] StripNodes = { "Trident" };

    // Map source takes (suffix match) onto the four Pose-controller clips.
    struct ClipDef { public string name; public string takeSuffix; public bool loop; }
    static readonly ClipDef[] Clips =
    {
        new ClipDef { name = "CI_Idle",   takeSuffix = "Idle",  loop = true  },
        new ClipDef { name = "CI_Stalk",  takeSuffix = "Run",   loop = true  },
        new ClipDef { name = "CI_Attack", takeSuffix = "Punch", loop = false },
        new ClipDef { name = "CI_Death",  takeSuffix = "Death", loop = false },
    };

    [MenuItem("Tools/Black Commission/Monsters/Build Civic Idol Asset")]
    public static void BuildCivicIdolAsset()
    {
        if (!System.IO.File.Exists(FbxPath))
        {
            Debug.LogError($"[CivicIdol] FBX not found at {FbxPath}");
            return;
        }

        ConfigureImporter();
        var material = BuildMaterial();
        var controller = BuildController();
        BuildPrefab(controller, material);
        RegisterNetworkPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CivicIdol] Build complete.");
    }

    static void ConfigureImporter()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.useFileScale = true;
        // Source model is 3.13 m tall; 0.68 lands the idol at ~2.1 m — an over-scaled
        // plaza monument next to the 1.8 m crew.
        importer.globalScale = 0.68f;
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

        // PASS 2 — pick the four wanted takes by suffix, rename to the CI_* pose clips.
        var defaults = importer.defaultClipAnimations;
        var defs = new List<ModelImporterClipAnimation>();
        foreach (var c in Clips)
        {
            var take = defaults.FirstOrDefault(t => t.takeName.EndsWith(c.takeSuffix));
            if (take == null)
            {
                Debug.LogError($"[CivicIdol] Take '*{c.takeSuffix}' not found. Available: " +
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
        Debug.Log($"[CivicIdol] Imported {clips.Count} clips: " +
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
        mat.SetFloat("_Smoothness", 0.05f);   // weathered stone-bronze: dead matte
        EditorUtility.SetDirty(mat);
        return mat;
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
        var dormant = sm.AddState("Dormant"); dormant.motion = Get("CI_Idle");
        var stalk   = sm.AddState("Stalk");   stalk.motion   = Get("CI_Stalk");
        var atk     = sm.AddState("Attack");  atk.motion     = Get("CI_Attack");
        var dead    = sm.AddState("Dead");    dead.motion    = Get("CI_Death");
        sm.defaultState = dormant;

        AddAnyState(sm, dormant, AnimatorConditionMode.Less, 1);
        AddAnyState(sm, stalk, AnimatorConditionMode.Equals, 1);
        AddAnyState(sm, atk,  AnimatorConditionMode.Equals, 2);
        AddAnyState(sm, dead, AnimatorConditionMode.Equals, 3);

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
        if (model == null) { Debug.LogError("[CivicIdol] Could not load model for prefab build."); return; }

        // Root-node identity keys in imported takes would pin an Animator-on-root prefab to the
        // origin (see EchoMoldSetup) — components on a clean root, animated model as a child.
        var inst = new GameObject("CivicIdol");
        var modelChild = (GameObject)Object.Instantiate(model, inst.transform);
        modelChild.name = "Model";
        modelChild.transform.localPosition = Vector3.zero;
        modelChild.transform.localRotation = Quaternion.identity;

        // Statue carries nothing: strip the handheld prop nodes.
        foreach (var t in inst.GetComponentsInChildren<Transform>(true).ToArray())
        {
            if (t == null) continue;
            if (StripNodes.Any(n => t.name == n))
            {
                Debug.Log($"[CivicIdol] Stripped prop node '{t.name}'.");
                Object.DestroyImmediate(t.gameObject);
            }
        }

        foreach (var r in inst.GetComponentsInChildren<Renderer>())
            r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();

        var renderers = inst.GetComponentsInChildren<Renderer>();
        Bounds b = default;
        if (renderers.Length > 0)
        {
            b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            Debug.Log($"[CivicIdol] Instance bounds size = {b.size}, min.y = {b.min.y:0.00} (idol target ~2.1 m tall).");
        }

        if (!modelChild.TryGetComponent<Animator>(out var anim)) anim = modelChild.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        // NOT culled: a paused Animator must hold its frozen pose even off-screen.
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        foreach (var rootAnim in inst.GetComponents<Animator>()) Object.DestroyImmediate(rootAnim);

        // Stamp-red eye glow, lit by the brain only while it has prey (threat telegraph).
        var glow = new GameObject("SealEyes");
        glow.transform.SetParent(inst.transform, false);
        glow.transform.localPosition = new Vector3(0f, Mathf.Max(1.4f, b.size.y * 0.75f), 0.3f);
        var light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1.0f, 0.22f, 0.12f);
        light.intensity = 2.2f;
        light.range = 4f;
        light.shadows = LightShadows.None;
        light.enabled = false;

        if (!inst.TryGetComponent<NavMeshAgent>(out var agent)) agent = inst.AddComponent<NavMeshAgent>();
        agent.radius = 0.5f;
        agent.height = 2.1f;
        agent.baseOffset = 0f;
        agent.speed = 4.6f;          // brain re-applies its tuned stalkSpeed in Awake
        agent.angularSpeed = 720f;
        agent.acceleration = 48f;
        agent.stoppingDistance = 1.2f;
        agent.autoBraking = true;

        if (!inst.TryGetComponent<CapsuleCollider>(out var col)) col = inst.AddComponent<CapsuleCollider>();
        col.radius = 0.5f;
        col.height = 2.1f;
        col.center = new Vector3(0f, 1.05f, 0f);

        if (inst.GetComponent<NetworkObject>() == null) inst.AddComponent<NetworkObject>();
        if (inst.GetComponent<CivicIdol>() == null) inst.AddComponent<CivicIdol>();

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
        var prefab = PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath, out bool ok);
        Object.DestroyImmediate(inst);
        Debug.Log($"[CivicIdol] Prefab saved at {PrefabPath} (ok={ok}).");
    }

    /// <summary>NGO's import hook usually auto-registers; this is the explicit fallback.</summary>
    static void RegisterNetworkPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
        if (prefab == null || list == null)
        {
            Debug.LogWarning("[CivicIdol] Could not load prefab or DefaultNetworkPrefabs for registration check.");
            return;
        }
        foreach (var e in list.PrefabList)
            if (e.Prefab == prefab) { Debug.Log("[CivicIdol] Already network-registered."); return; }
        list.Add(new NetworkPrefab { Prefab = prefab });
        EditorUtility.SetDirty(list);
        Debug.Log("[CivicIdol] Added to DefaultNetworkPrefabs.");
    }
}
