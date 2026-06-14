using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class HqOfficeLightingPass
{
    const string SceneName = "HQ";
    const string RootName = "MVP_HQ_LightingPass";

    static readonly Color ColdIndustrial = Rgb(0xD9, 0xE2, 0xDD);
    static readonly Color WarmTungsten = Rgb(0xFF, 0xBB, 0x73);
    static readonly Color FogConcrete = Rgb(0x2E, 0x30, 0x2D);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RuntimeBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ApplyIfHq(SceneManager.GetActiveScene());
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void EditorBootstrap()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (ApplyIfHq(scene))
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        };
    }

    [UnityEditor.MenuItem("Tools/Black Commission/Art/Apply HQ Lighting Pass")]
    static void ApplyHqLightingMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (ApplyIfHq(scene))
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    }
#endif

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyIfHq(scene);
    }

    static bool ApplyIfHq(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName)
            return false;

        ConfigureRenderSettings();
        RemoveExtraLightingRoot();

        ConfigureExistingLight("HQ_LampFluorescent_Computer_Light", ColdIndustrial, 0.52f, 4.7f);
        ConfigureExistingLight("HQ_LampFluorescent_ToolRack_Light", ColdIndustrial, 0.46f, 4.25f);
        ConfigureExistingLight("HQ_LampDesk_A_Light", WarmTungsten, 0.3f, 1.55f);
        ConfigureExistingLight("HQ_LampDesk_B_Light", WarmTungsten, 0.26f, 1.35f);
        ConfigureExistingLight("HQ_PlaceholderLight", ColdIndustrial, 0.0f, 0.1f);

        // The fluorescent over the tool rack is the office's "we can't afford a new
        // tube" statement piece — give it the dying-lamp sputter.
        GameObject sputterLamp = FindSceneObject("HQ_LampFluorescent_ToolRack_Light");
        if (sputterLamp != null && sputterLamp.GetComponent<LightFlicker>() == null)
            sputterLamp.AddComponent<LightFlicker>().Configure(LightFlicker.Character.Sputter, 0.7f, 7f);

        // DECISIVE shadow kill (PM 2026-06-13): a real-time cast shadow was following the
        // camera in the office. Turn shadow casting OFF on every light in the HQ so nothing —
        // player body, prop, or flashlight — can throw it. The office reads by light pools and
        // baked darkness, not real-time shadows.
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            l.shadows = LightShadows.None;

        if (Application.isPlaying)
            EnsurePostVolume();

        return true;
    }

    /// <summary>
    /// Same LC post stack as the tower (vignette/grain/bloom/grade), built in memory
    /// at runtime — the HQ scene never needs a serialized Volume object. Editor-time
    /// ApplyIfHq skips this so it doesn't dirty the scene with unsaveable state.
    /// </summary>
    static void EnsurePostVolume()
    {
        if (Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>() != null) return;

        var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();

        // Vignette REMOVED 2026-06-13 (PM: the office edge-darkening / "black shadow ring"
        // was unwanted). NOTE: this whole method short-circuits when ANY Volume already exists
        // (see EnsurePostVolume guard) — and BrightnessController creates a global Volume at
        // startup — so the earlier "soften the vignette" edit was dead code and never ran.
        // No vignette is added here anymore; the office relies on fog + lights for mood.

        var grain = profile.Add<UnityEngine.Rendering.Universal.FilmGrain>(true);
        grain.type.Override(UnityEngine.Rendering.Universal.FilmGrainLookup.Medium1);
        grain.intensity.Override(0.20f);
        grain.response.Override(0.7f);

        var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
        bloom.threshold.Override(1.05f);
        bloom.intensity.Override(0.40f);
        bloom.scatter.Override(0.6f);

        var color = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true);
        color.saturation.Override(-10f);
        color.contrast.Override(7f);

        var go = new GameObject("LC_PostVolume (Runtime)");
        var volume = go.AddComponent<UnityEngine.Rendering.Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.profile = profile;
    }

    static void ConfigureRenderSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = FogConcrete;
        // Thinned 2026-06-13 (PM: dark fog read as a "black shadow following the camera" in
        // the small office). 0.0018 → 0.0006 keeps faint depth without the closing-in murk.
        RenderSettings.fogDensity = 0.0006f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Rgb(0x4B, 0x4C, 0x48);
        RenderSettings.ambientEquatorColor = Rgb(0x3D, 0x3F, 0x3A);
        RenderSettings.ambientGroundColor = Rgb(0x26, 0x28, 0x25);
        RenderSettings.ambientIntensity = 0.92f;
    }

    static void RemoveExtraLightingRoot()
    {
        GameObject root = FindSceneObject(RootName);
        if (root == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(root);
        else
            Object.DestroyImmediate(root);
    }

    static void ConfigureExistingLight(string name, Color color, float intensity, float range)
    {
        GameObject go = FindSceneObject(name);
        if (go == null)
            return;

        Light light = go.GetComponent<Light>();
        if (light == null)
            return;

        ApplyLight(light, color, intensity, range);
    }

    static void ApplyLight(Light light, Color color, float intensity, float range)
    {
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.55f;
        light.bounceIntensity = 0.35f;
        light.renderMode = LightRenderMode.Auto;
    }

    static GameObject FindSceneObject(string name)
    {
        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.name == name)
                return obj;
        }
        return null;
    }

    static Color Rgb(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }
}
