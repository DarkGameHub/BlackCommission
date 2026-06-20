using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Creates the ready-to-Play map-2 scene (ADR-0003, direction B): a light + an overview camera + a
/// <c>MapSite</c> object carrying <c>MapSiteRuntime</c>, which on Play generates a FRESH RANDOM site
/// (outdoor approach + van drop-off + edge-based interior) from a new seed each run. Kept as a runtime scene
/// (not a baked one) because the whitebox geometry uses runtime-created materials that wouldn't persist if
/// saved into the scene — and because per-run randomness is the point.
///
/// Headless: <c>Unity.exe -batchmode -nographics -projectPath . -executeMethod Map2SceneBuilder.BuildAndSave
/// -quit -logFile &lt;log&gt;</c>. Also available interactively: Tools ▸ Black Commission ▸ Map ▸ Create Map2 Scene.
/// </summary>
public static class Map2SceneBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/Map2_Procedural.unity";

    [MenuItem("Tools/Black Commission/Map/Create Map2 Scene")]
    public static void BuildAndSave()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.28f;                       // moonlit-dark (LC-style night)
        light.color = new Color(0.6f, 0.7f, 0.95f);    // cold moonlight
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Dark + foggy so the building is NOT visible from the far drop-off — you navigate the woods to find it.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.06f, 0.09f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.03f, 0.04f, 0.06f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.018f;

        var camGo = new GameObject("Overview Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.Skybox;
        camGo.transform.position = new Vector3(28f, 46f, -46f); // looks north+down over outdoor→building
        camGo.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

        var site = new GameObject("MapSite");
        site.AddComponent<MapSiteRuntime>(); // Assembly-CSharp-Editor references Assembly-CSharp, so this resolves

        bool ok = EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[Map2Scene] {(ok ? "saved" : "FAILED to save")} {ScenePath} " +
                  $"(light + overview camera + MapSite[MapSiteRuntime]). Hit Play → fresh random site each run.");
    }
}
