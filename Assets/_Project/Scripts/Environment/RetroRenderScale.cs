using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Per-scene URP render scale. Mission sites keep the lo-fi 0.5 downsample-upscale
/// (the Lethal-Company chunky look), but the HQ office and the menu render at full
/// scale — PM 2026-06-12: the office read too blurry. The lo-fi identity in calm
/// scenes is carried by the 256px point textures + LC outline + grain, not by the
/// render-scale blur.
/// </summary>
public static class RetroRenderScale
{
    const float LoFiScale = 0.5f;   // mission sites (Tower*)
    const float SharpScale = 1.0f;  // HQ office + menu

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Init()
    {
        Apply(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply(scene);

    static void Apply(Scene scene)
    {
        if (GraphicsSettings.currentRenderPipeline is not UniversalRenderPipelineAsset urp) return;
        bool loFi = !string.IsNullOrEmpty(scene.name) && scene.name.StartsWith("Tower");
        float target = loFi ? LoFiScale : SharpScale;
        if (!Mathf.Approximately(urp.renderScale, target))
            urp.renderScale = target;
    }
}
