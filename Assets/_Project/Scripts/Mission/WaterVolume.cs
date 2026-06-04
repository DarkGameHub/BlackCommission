using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// A box of water. The trigger tells the local player when it is inside the water body
/// (PlayerController decides "submerged" from head height vs. the surface), and this
/// component drives the cheap underwater look for the local owner: a URP Volume
/// (color filter + saturation + vignette, built at runtime like BrightnessController)
/// plus dense blue-green RenderSettings fog. No water shader — MVP placeholder.
///
/// The surface plane visual is a separate transparent quad placed by the scene generator.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class WaterVolume : MonoBehaviour
{
    [Header("Underwater look")]
    [SerializeField] Color underwaterFogColor = new(0.04f, 0.17f, 0.21f);
    [SerializeField] float underwaterFogDensity = 0.14f;
    [SerializeField] float effectLerpSpeed = 6f;

    BoxCollider box;
    float surfaceY;

    Volume volume;
    float weight;

    // Above-water RenderSettings fog, captured when we start overriding and restored on surfacing.
    bool fogOverridden;
    bool savedFog;
    Color savedFogColor;
    FogMode savedFogMode;
    float savedFogDensity;

    PlayerController localPlayer;

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        surfaceY = box.bounds.max.y;
        BuildVolume();
    }

    void BuildVolume()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var color = profile.Add<ColorAdjustments>(true);
        color.colorFilter.overrideState = true;
        color.colorFilter.value = new Color(0.40f, 0.62f, 0.78f);  // darken + push toward blue-green
        color.saturation.overrideState = true;
        color.saturation.value = -22f;
        color.contrast.overrideState = true;
        color.contrast.value = 8f;

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.45f;
        vignette.color.overrideState = true;
        vignette.color.value = new Color(0.02f, 0.08f, 0.10f);
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.9f;

        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 50f;     // below BrightnessController (100) so brightness still wins on exposure
        volume.weight = 0f;
        volume.profile = profile;
    }

    void Update()
    {
        if (localPlayer == null || !localPlayer.IsOwner)
            localPlayer = FindLocalPlayer();

        bool submerged = localPlayer != null && localPlayer.IsSubmergedLocal;

        weight = Mathf.MoveTowards(weight, submerged ? 1f : 0f, effectLerpSpeed * Time.deltaTime);
        if (volume != null) volume.weight = weight;

        if (weight > 0.001f)
        {
            if (!fogOverridden) CacheSceneFog();
            Color baseColor = savedFog ? savedFogColor : underwaterFogColor;
            float baseDensity = savedFog ? savedFogDensity : 0.01f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(baseColor, underwaterFogColor, weight);
            RenderSettings.fogDensity = Mathf.Lerp(baseDensity, underwaterFogDensity, weight);

            EnsurePostProcessing();
        }
        else if (fogOverridden)
        {
            RestoreFog();
        }
    }

    void CacheSceneFog()
    {
        savedFog = RenderSettings.fog;
        savedFogColor = RenderSettings.fogColor;
        savedFogMode = RenderSettings.fogMode;
        savedFogDensity = RenderSettings.fogDensity;
        fogOverridden = true;
    }

    void RestoreFog()
    {
        RenderSettings.fog = savedFog;
        RenderSettings.fogColor = savedFogColor;
        RenderSettings.fogMode = savedFogMode;
        RenderSettings.fogDensity = savedFogDensity;
        fogOverridden = false;
    }

    static void EnsurePostProcessing()
    {
        Camera cam = Camera.main;
        if (cam != null && cam.TryGetComponent(out UniversalAdditionalCameraData data) && !data.renderPostProcessing)
            data.renderPostProcessing = true;
    }

    static PlayerController FindLocalPlayer()
    {
        var all = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in all)
            if (p.IsOwner) return p;
        return null;
    }

    void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null && pc.IsOwner)
            pc.SetWaterState(true, surfaceY);
    }

    void OnTriggerExit(Collider other)
    {
        var pc = other.GetComponentInParent<PlayerController>();
        if (pc != null && pc.IsOwner)
            pc.SetWaterState(false, surfaceY);
    }

    void OnDisable()
    {
        if (fogOverridden) RestoreFog();
    }
}
