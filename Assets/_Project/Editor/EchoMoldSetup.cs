using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// One-shot editor tooling that turns the Blender-exported <c>EchoMold.fbx</c> into a
/// game-ready monster: Generic-rig import with the four baked clips split out, an
/// <see cref="AnimatorController"/> driven by a single <c>Pose</c> int, and a prefab
/// wired with Animator + NavMeshAgent + collider + NetworkObject + <see cref="EchoMold"/>.
///
/// Run via <c>Tools ▸ Black Commission ▸ Monsters ▸ Build Echo Mold Asset</c>.
/// Idempotent — safe to re-run after re-exporting the FBX.
/// </summary>
public static class EchoMoldSetup
{
    const string FbxPath = "Assets/_Project/Art/Monsters/EchoMold/EchoMold.fbx";
    const string ControllerPath = "Assets/_Project/Art/Monsters/EchoMold/EchoMold.controller";
    // In Resources so MonsterSpawnBootstrap can load it at runtime (moved 2026-07-02).
    const string PrefabPath = "Assets/_Project/Resources/Monsters/EchoMold.prefab";

    // Clip splits — mirror tools/rigging/output/EchoMold_clips.json.
    struct ClipDef { public string name; public int first; public int last; public bool loop; }
    static readonly ClipDef[] Clips =
    {
        new ClipDef { name = "EM_Idle",   first = 1,   last = 96,  loop = true  },
        new ClipDef { name = "EM_Hunt",   first = 101, last = 196, loop = true  },
        new ClipDef { name = "EM_Attack", first = 201, last = 224, loop = false },
        new ClipDef { name = "EM_Death",  first = 231, last = 286, loop = false },
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
        var controller = BuildController();
        BuildPrefab(controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[EchoMold] Build complete.");
    }

    // ── 1. FBX import: Generic rig + 4 clip splits ────────────────────────────
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

        // PASS 1 — import once with animation enabled and NO clip overrides, so the importer
        // discovers the baked take. importedTakeInfos is empty until the asset has been
        // imported with importAnimation = true at least once; without a valid takeName the
        // clip splits silently collapse to the single default take (observed: 1 clip "Scene").
        importer.clipAnimations = new ModelImporterClipAnimation[0];
        importer.SaveAndReimport();

        string takeName = importer.importedTakeInfos.Length > 0 ? importer.importedTakeInfos[0].name : "";
        if (string.IsNullOrEmpty(takeName))
            Debug.LogWarning("[EchoMold] No take info after first import — clip split will likely fail.");
        else
            Debug.Log($"[EchoMold] Baked take = '{takeName}' " +
                      $"({importer.importedTakeInfos[0].startTime:0.00}-{importer.importedTakeInfos[0].stopTime:0.00}s).");

        // PASS 2 — split that take into the four named clips.
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

        // Verify what actually came in.
        var assets = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
        var clips = assets.OfType<AnimationClip>().Where(a => !a.name.StartsWith("__")).ToList();
        Debug.Log($"[EchoMold] Imported {clips.Count} clips: " +
                  string.Join(", ", clips.Select(c => $"{c.name}({c.length:0.00}s,loop={c.isLooping})")));
    }

    // ── 2. AnimatorController: single 'Pose' int drives the reveal ────────────
    static AnimatorController BuildController()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ctrl.AddParameter("Pose", AnimatorControllerParameterType.Int);

        var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
            .OfType<AnimationClip>()
            .Where(a => !a.name.StartsWith("__"))
            .ToDictionary(a => a.name, a => a);

        AnimationClip Get(string n) => clips.TryGetValue(n, out var c) ? c : null;

        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("Idle");   idle.motion = Get("EM_Idle");
        var hunt = sm.AddState("Hunt");   hunt.motion = Get("EM_Hunt");
        var atk  = sm.AddState("Attack"); atk.motion  = Get("EM_Attack");
        var dead = sm.AddState("Dead");   dead.motion = Get("EM_Death");
        sm.defaultState = idle;

        // AnyState → state, keyed on the Pose int (0/1 Idle, 2 Hunt, 3 Attack, 4 Dead).
        AddAnyState(sm, idle, AnimatorConditionMode.Less, 2);     // Pose < 2  → Idle (Roam/Lure)
        AddAnyState(sm, hunt, AnimatorConditionMode.Equals, 2);
        AddAnyState(sm, atk,  AnimatorConditionMode.Equals, 3);
        AddAnyState(sm, dead, AnimatorConditionMode.Equals, 4);

        EditorUtility.SetDirty(ctrl);
        Debug.Log($"[EchoMold] Controller built at {ControllerPath} " +
                  $"(idle={(idle.motion ? "ok" : "MISSING")}, hunt={(hunt.motion ? "ok" : "MISSING")}, " +
                  $"attack={(atk.motion ? "ok" : "MISSING")}, death={(dead.motion ? "ok" : "MISSING")}).");
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

    // ── 3. Prefab: model + Animator + agent + collider + net + AI ─────────────
    static void BuildPrefab(AnimatorController controller)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (model == null) { Debug.LogError("[EchoMold] Could not load model for prefab build."); return; }
        var inst = (GameObject)Object.Instantiate(model);
        inst.name = "EchoMold";

        // Accurate height check: Renderer.bounds is world-space on the live instance, so it
        // accounts for every rig part's transform (mesh.bounds alone is per-mesh local space).
        var renderers = inst.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            Debug.Log($"[EchoMold] Instance bounds size = {b.size} from {renderers.Length} renderer(s) (expect ~1.8 m tall).");
        }
        else Debug.LogWarning("[EchoMold] No renderers on instantiated model.");

        if (!inst.TryGetComponent<Animator>(out var anim)) anim = inst.AddComponent<Animator>();
        anim.runtimeAnimatorController = controller;
        anim.applyRootMotion = false;
        anim.updateMode = AnimatorUpdateMode.Normal;
        anim.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

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
