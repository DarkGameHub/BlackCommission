using UnityEngine;

public class MissionTimeOfDayDirector : MonoBehaviour
{
    [SerializeField] Light directionalLight;
    [SerializeField] float updateInterval = 0.2f;

    float nextUpdateTime;

    void Awake()
    {
        if (directionalLight != null) return;

        foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light != null && light.type == LightType.Directional)
            {
                directionalLight = light;
                return;
            }
        }
    }

    void Update()
    {
        if (Time.time < nextUpdateTime) return;
        nextUpdateTime = Time.time + Mathf.Max(0.05f, updateInterval);

        LostItemMissionManager mission = LostItemMissionManager.Instance;
        if (mission == null) return;

        ApplyClockLight(mission.CurrentClockHour);
    }

    void ApplyClockLight(float absoluteClockHour)
    {
        float hour = Mathf.Repeat(absoluteClockHour, 24f);
        float daylight = Mathf.Clamp01(Mathf.Sin((hour - 6f) / 12f * Mathf.PI));
        float lateNight = hour >= 20f || hour < 5.5f ? 1f : 0f;
        float dusk = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(16.5f, 20f, hour));

        Color dayAmbient = new Color(0.18f, 0.22f, 0.21f);
        Color duskAmbient = new Color(0.16f, 0.13f, 0.1f);
        Color nightAmbient = new Color(0.035f, 0.055f, 0.065f);
        RenderSettings.ambientLight = Color.Lerp(
            Color.Lerp(nightAmbient, dayAmbient, daylight),
            duskAmbient,
            dusk * 0.55f);
        if (lateNight > 0f)
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, nightAmbient, 0.65f);

        RenderSettings.fogColor = Color.Lerp(new Color(0.04f, 0.07f, 0.075f), new Color(0.1f, 0.12f, 0.105f), daylight);
        RenderSettings.fogDensity = Mathf.Lerp(0.038f, 0.014f, daylight);

        if (directionalLight == null) return;

        directionalLight.intensity = Mathf.Lerp(0.08f, 0.82f, daylight);
        directionalLight.color = Color.Lerp(new Color(0.18f, 0.36f, 0.52f), new Color(1f, 0.88f, 0.62f), daylight);
        directionalLight.transform.rotation = Quaternion.Euler(Mathf.Lerp(8f, 58f, daylight), 35f + hour * 4f, 0f);
    }
}
