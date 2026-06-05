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

        return true;
    }

    static void ConfigureRenderSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = FogConcrete;
        RenderSettings.fogDensity = 0.0018f;

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
