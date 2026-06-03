using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Self-bootstrapping global brightness via a URP post-processing Volume (ColorAdjustments
/// post-exposure). Also applies persisted fullscreen/quality once at startup, and keeps the
/// active camera's post-processing enabled so exposure is actually visible.
/// </summary>
public class BrightnessController : MonoBehaviour
{
    static BrightnessController instance;

    Volume volume;
    ColorAdjustments colorAdjust;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() => EnsureInstance();

    public static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("AS_BrightnessController");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BrightnessController>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;

        BuildVolume();
        DisplaySettings.ApplyFullscreen();
        DisplaySettings.ApplyQuality();
        ApplyBrightness();
    }

    void BuildVolume()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        colorAdjust = profile.Add<ColorAdjustments>(true);
        colorAdjust.postExposure.overrideState = true;
        colorAdjust.postExposure.value = DisplaySettings.Brightness;

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        volume.weight = 1f;
        volume.profile = profile;
    }

    void Update()
    {
        // Player cameras are created at runtime; make sure whichever is active renders
        // post-processing, otherwise the exposure override has no visible effect.
        Camera cam = Camera.main;
        if (cam != null && cam.TryGetComponent(out UniversalAdditionalCameraData data) && !data.renderPostProcessing)
            data.renderPostProcessing = true;
    }

    /// <summary>Re-read DisplaySettings.Brightness and apply it live.</summary>
    public static void Apply()
    {
        EnsureInstance();
        instance.ApplyBrightness();
    }

    void ApplyBrightness()
    {
        if (colorAdjust == null) return;
        colorAdjust.postExposure.overrideState = true;
        colorAdjust.postExposure.value = DisplaySettings.Brightness;
    }
}
