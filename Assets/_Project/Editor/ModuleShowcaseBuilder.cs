using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds a standalone "showcase" scene that lays the three dressed room modules
/// (Office / Utility / Large) in a row on a ground slab — lit, labelled, and fitted
/// with a <see cref="PreviewWalker"/> rig so the team can press Play and walk through
/// each room to check scale and furniture. This is a throwaway viewing/validation
/// scene; it never touches HQ.unity or Tower_EarthCoast_01.unity.
///
/// Menu: Tools > Black Commission > MVP > Modules > Build Module Showcase
/// Output scene: Assets/_Project/Scenes/Module_Showcase.unity
///
/// Run AFTER 'Build All Shared Modules' + 'Dress Room Modules' so the rooms exist and
/// carry their furniture. Re-running rebuilds the scene from scratch (idempotent).
/// </summary>
public static class ModuleShowcaseBuilder
{
    const string ModuleDir = "Assets/_Project/Art/Maps/Shared/Modules";
    const string SceneDir  = "Assets/_Project/Scenes";
    const string ScenePath = SceneDir + "/Module_Showcase.unity";
    const string GroundMat = "Assets/_Project/Art/Maps/Tower_EarthCoast_01/Materials/Whitebox/V8_Concrete_Poured.mat";

    // prefab id, label, world-X offset, interior light (x,z) + range. Rooms keep their
    // own local layout (doors on the south / -Z face), so they all open toward the spawn.
    struct Room { public string id, label; public float x, lx, lz, lrange; }

    [MenuItem("Tools/Black Commission/MVP/Modules/Build Module Showcase")]
    public static void Build()
    {
        var rooms = new[]
        {
            new Room { id = "Room_Office_4x8",  label = "OFFICE  4x8",  x = 0f,  lx = 2f,  lz = 4f, lrange = 12f },
            new Room { id = "Room_Utility_4x4", label = "UTILITY  4x4", x = 8f,  lx = 10f, lz = 2f, lrange = 9f  },
            new Room { id = "Room_Large_8x8",   label = "STORAGE  8x8", x = 16f, lx = 20f, lz = 4f, lrange = 14f },
        };

        // Verify the dressed rooms exist before wiping the current scene.
        foreach (var r in rooms)
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{ModuleDir}/{r.id}.prefab") == null)
            {
                Debug.LogError($"[ModuleShowcase] Missing module prefab: {r.id}. " +
                    "Run 'Build All Shared Modules' + 'Dress Room Modules' first.");
                return;
            }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Ambient so interiors are never pitch black even before the point lights reach.
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.40f);

        // Key directional light (exterior + ground).
        var sun = new GameObject("Directional Light");
        var dl  = sun.AddComponent<Light>();
        dl.type      = LightType.Directional;
        dl.intensity = 1.0f;
        dl.color     = new Color(1f, 0.96f, 0.90f);
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Ground slab covering the row + spawn; top a hair below the room floors (y=0) so
        // the walker steps up onto each room floor instead of z-fighting it.
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position   = new Vector3(12f, -0.55f, 3f);
        ground.transform.localScale = new Vector3(32f, 1f, 18f);
        var gm = AssetDatabase.LoadAssetAtPath<Material>(GroundMat);
        if (gm != null) ground.GetComponent<Renderer>().sharedMaterial = gm;

        // Rooms + per-room interior light + name label.
        foreach (var r in rooms)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModuleDir}/{r.id}.prefab");
            var inst   = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            inst.transform.position = new Vector3(r.x, 0f, 0f);

            var lgo = new GameObject($"Light_{r.id}");
            var lt  = lgo.AddComponent<Light>();
            lt.type      = LightType.Point;
            lt.range     = r.lrange;
            lt.intensity = 2.5f;
            lt.color     = new Color(1f, 0.93f, 0.82f);
            lgo.transform.position = new Vector3(r.lx, 2.8f, r.lz);

            MakeLabel(r.label, new Vector3(r.lx, 3.7f, -0.05f));
        }

        BuildWalker(new Vector3(10f, 0.2f, -5f));   // centred, 5 m back — gallery vantage of the row

        if (!AssetDatabase.IsValidFolder(SceneDir))
            AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[ModuleShowcase] Built {ScenePath} — 3 dressed rooms, lights, labels, PreviewWalker. " +
                  "Press Play to walk through: WASD move, mouse look, Shift sprint, Esc frees the cursor.");
    }

    /// First-person rig: CharacterController + child camera, so PreviewWalker finds its eye.
    static void BuildWalker(Vector3 pos)
    {
        var rig = new GameObject("PreviewWalker");
        rig.transform.position = pos;     // identity rotation → faces +Z (into the row)

        var cc = rig.AddComponent<CharacterController>();
        cc.height = 1.9f;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.95f, 0f);

        var camGo = new GameObject("Camera");
        camGo.transform.SetParent(rig.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);   // eye height
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();

        rig.AddComponent<PreviewWalker>();
    }

    /// Floating name plate above each room's open (south) face, rotated to read from the spawn.
    static void MakeLabel(string text, Vector3 pos)
    {
        var go = new GameObject($"Label_{text}");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);   // front face toward -Z (the walker)
        var tm = go.AddComponent<TextMesh>();
        tm.text          = text;
        tm.characterSize = 0.18f;
        tm.fontSize      = 64;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.color         = new Color(0.95f, 0.90f, 0.75f);
    }
}
