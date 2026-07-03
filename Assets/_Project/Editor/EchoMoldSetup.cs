using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// One-shot editor tooling that turns <c>EchoAlien.fbx</c> (回声菌视觉v2 — the bestiary's
/// "infected fungal humanoid"; CC0 base mesh "Alien" from Quaternius' Ultimate Monsters pack,
/// re-skinned to the Black Commission palette via <c>EchoAlien_Atlas.png</c>) into the
/// game-ready Echo Mold: Generic-rig import, a Pose-int AnimatorController whose Idle state
/// is a Speed-driven Idle↔Walk blend (humanoid rigs slide without it — the retired mushroom
/// model glided by design), and the prefab in Resources/Monsters.
///
/// The source FBX carries one take per clip, so clips are selected by take name.
/// Run via <c>Tools ▸ Black Commission ▸ Monsters ▸ Build Echo Mold Asset</c>.
/// Idempotent — safe to re-run after swapping the FBX or atlas.
/// </summary>
public static class EchoMoldSetup
{
    const string FbxPath = "Assets/_Project/Art/Monsters/EchoMold/EchoAlien.fbx";
    const string AtlasPath = "Assets/_Project/Art/Monsters/EchoMold/EchoAlien_Atlas.png";
    const string MaterialPath = "Assets/_Project/Art/Monsters/EchoMold/EchoAlien.mat";
    const string ControllerPath = "Assets/_Project/Art/Monsters/EchoMold/EchoMold.controller";
    // In Resources so MonsterSpawnBootstrap can load it at runtime (moved 2026-07-02).
    const string PrefabPath = "Assets/_Project/Resources/Monsters/EchoMold.prefab";

    // Roam speed of the brain — full-walk threshold of the locomotion blend.
    const float WalkBlendSpeed = 1.2f;

    // Map source takes (suffix match) onto the named clips.
    struct ClipDef { public string name; public string takeSuffix; public bool loop; }
    static readonly ClipDef[] Clips =
    {
        new ClipDef { name = "EM_Idle",   takeSuffix = "Idle",  loop = true  },
        new ClipDef { name = "EM_Walk",   takeSuffix = "Walk",  loop = true  },
        new ClipDef { name = "EM_Hunt",   takeSuffix = "Run",   loop = true  },
        new ClipDef { name = "EM_Attack", takeSuffix = "Punch", loop = false },
        new ClipDef { name = "EM_Death",  takeSuffix = "Death", loop = false },
    };

    [MenuItem("Tools/Black Commission/Monsters/Build Echo Mold Asset")]
    public static void BuildEchoMoldAsset()
    {
        if (!System.IO.File.Exists(FbxPath))
        {
            Debug.LogError($"[EchoMold] FBX not found at {FbxPath}");
            return;
        }

        ConfigureImporter();
        var material = BuildMaterial();
        var controller = BuildController();
        BuildPrefab(controller, material);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EchoMold] Build complete.");
    }

    // ── 1. FBX import: Generic rig + clips picked by take name ────────────────
    static void ConfigureImporter()
    {
        var importer = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.useFileScale = true;
        // Quaternius Big-series imports ~3.2 m tall; 0.58 lands the hunter at ~1.9 m.
        importer.globalScale = 0.58f;
        importer.importConstraints = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        // Source materials are palette-atlas Standard stubs; the prefab overrides every
        // renderer with the URP re-skin material, so skip importing them entirely.
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importAnimation = true;

        // PASS 1 — plain import so takes are discoverable (importedTakeInfos is empty until
        // the asset has been imported once with importAnimation = true).
        importer.clipAnimations = new ModelImporterClipAnimation[0];
        importer.SaveAndReimport();

        // PASS 2 — pick the wanted takes by suffix ("|Idle" first so "Jump_Idle" can't win).
        var defaults = importer.defaultClipAnimations;
        var defs = new List<ModelImporterClipAnimation>();
        foreach (var c in Clips)
        {
            var take = defaults.FirstOrDefault(t => t.takeName.EndsWith("|" + c.takeSuffix))
                       ?? defaults.FirstOrDefault(t => t.takeName.EndsWith(c.takeSuffix));
            if (take == null)
            {
                Debug.LogError($"[EchoMold] Take '*{c.takeSuffix}' not found. Available: " +
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
        Debug.Log($"[EchoMold] Imported {clips.Count} clips: " +
                  string.Join(", ", clips.Select(c => $"{c.name}({c.length:0.00}s,loop={c.isLooping})")));
    }

    // ── 2. Re-skin material ────────────────────────────────────────────────────
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

    // ── 3. AnimatorController: Pose int + Speed-blended locomotion ────────────
    static AnimatorController BuildController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Pose", AnimatorControllerParameterType.Int);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
            .OfType<AnimationClip>()
            .Where(a => !a.name.StartsWith("__"))
            .ToDictionary(a => a.name, a => a);
        AnimationClip Get(string n) => clips.TryGetValue(n, out var c) ? c : null;

        var sm = ctrl.layers[0].stateMachine;

        // Idle pose (Roam/Lure) = 1D blend Idle↔Walk on Speed, so the humanoid walks while
        // roaming and stands while luring instead of gliding.
        var idle = ctrl.CreateBlendTreeInController("Idle", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(Get("EM_Idle"), 0f);
        tree.AddChild(Get("EM_Walk"), WalkBlendSpeed);
        // CreateBlendTreeInController registers a default "Blend" float — drop the stray.
        foreach (var p in ctrl.parameters.Where(p => p.name == "Blend").ToArray())
            ctrl.RemoveParameter(p);

        var hunt = sm.AddState("Hunt");   hunt.motion = Get("EM_Hunt");
        var atk  = sm.AddState("Attack"); atk.motion  = Get("EM_Attack");
        var dead = sm.AddState("Dead");   dead.motion = Get("EM_Death");
        sm.defaultState = idle;

        // AnyState → state, keyed on the Pose int (0/1 Idle, 2 Hunt, 3 Attack, 4 Dead).
        AddAnyState(sm, idle, AnimatorConditionMode.Less, 2);
        AddAnyState(sm, hunt, AnimatorConditionMode.Equals, 2);
        AddAnyState(sm, atk,  AnimatorConditionMode.Equals, 3);
        AddAnyState(sm, dead, AnimatorConditionMode.Equals, 4);

        EditorUtility.SetDirty(ctrl);
        Debug.Log($"[EchoMold] Controller built at {ControllerPath} " +
                  $"(idleTree={(tree.children.Length == 2 ? "ok" : "MISSING CHILD")}, " +
                  $"hunt={(hunt.motion ? "ok" : "MISSING")}, attack={(atk.motion ? "ok" : "MISSING")}, " +
                  $"death={(dead.motion ? "ok" : "MISSING")}).");
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

    // ── 4. Prefab: model + Animator + agent + collider + net + AI ─────────────
    static void BuildPrefab(AnimatorController controller, Material material)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (model == null) { Debug.LogError("[EchoMold] Could not load model for prefab build."); return; }

        // Root-node identity keys in imported takes would pin an Animator-on-root prefab to
        // the origin (caught in the 2026-07-02 mission smoke) — gameplay components live on a
        // clean root, the animated model is a CHILD.
        var inst = new GameObject("EchoMold");
        var modelChild = (GameObject)Object.Instantiate(model, inst.transform);
        modelChild.name = "Model";
        modelChild.transform.localPosition = Vector3.zero;
        modelChild.transform.localRotation = Quaternion.identity;

        foreach (var r in inst.GetComponentsInChildren<Renderer>())
            r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();

        var renderers = inst.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            Debug.Log($"[EchoMold] Instance bounds size = {b.size} from {renderers.Length} renderer(s) (expect ~1.9 m tall).");
        }
        else Debug.LogWarning("[EchoMold] No renderers on instantiated model.");

        // Animator must sit on the MODEL child (curve paths bind from there; root stays free).
        if (!modelChild.TryGetComponent<Animator>(out var anim)) anim = modelChild.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        anim.updateMode = AnimatorUpdateMode.Normal;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        foreach (var rootAnim in inst.GetComponents<Animator>()) Object.DestroyImmediate(rootAnim);

        if (!inst.TryGetComponent<NavMeshAgent>(out var agent)) agent = inst.AddComponent<NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 1.8f;
        agent.baseOffset = 0f;
        agent.speed = 1.2f;
        agent.angularSpeed = 360f;
        agent.acceleration = 12f;
        agent.stoppingDistance = 1.2f;
        agent.autoBraking = true;

        if (!inst.TryGetComponent<CapsuleCollider>(out var col)) col = inst.AddComponent<CapsuleCollider>();
        col.radius = 0.4f;
        col.height = 1.8f;
        col.center = new Vector3(0f, 0.9f, 0f);

        if (inst.GetComponent<NetworkObject>() == null) inst.AddComponent<NetworkObject>();
        if (inst.GetComponent<EchoMold>() == null) inst.AddComponent<EchoMold>();

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
        var prefab = PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath, out bool ok);
        Object.DestroyImmediate(inst);
        Debug.Log($"[EchoMold] Prefab saved at {PrefabPath} (ok={ok}, asset={(prefab ? prefab.name : "null")}).");
    }
}
