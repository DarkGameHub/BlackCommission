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
        
        // Golden hour effect specifically for Snow Lotus evening atmosphere
        // Peaks around 17:45 - 18:15
        float goldenHour = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(16.5f, 18.0f, hour)) * 
                           Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(18.2f, 19.5f, hour));

        Color dayAmbient = new Color(0.18f, 0.22f, 0.21f);
        Color goldAmbient = new Color(0.24f, 0.18f, 0.12f);
        Color nightAmbient = new Color(0.10f, 0.12f, 0.13f);
        
        RenderSettings.ambientLight = Color.Lerp(
            Color.Lerp(nightAmbient, dayAmbient, daylight),
            goldAmbient,
            goldenHour * 0.7f);

        if (lateNight > 0f)
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, nightAmbient, 0.45f);

        RenderSettings.fogColor = Color.Lerp(
            Color.Lerp(new Color(0.06f, 0.09f, 0.10f), new Color(0.1f, 0.12f, 0.105f), daylight),
            new Color(0.15f, 0.12f, 0.10f), 
            goldenHour);
            
        RenderSettings.fogDensity = Mathf.Lerp(0.020f, 0.014f, daylight);

        if (directionalLight == null) return;

        // Transition from bright day -> golden sunset -> dark night
        float sunIntensity = Mathf.Lerp(0.28f, 0.82f, daylight);
        // Boost intensity during golden hour for the "Golden Mountain" effect
        sunIntensity = Mathf.Max(sunIntensity, goldenHour * 1.5f);
        
        Color sunColor = Color.Lerp(new Color(0.18f, 0.36f, 0.52f), new Color(1f, 0.88f, 0.62f), daylight);
        sunColor = Color.Lerp(sunColor, new Color(1f, 0.65f, 0.35f), goldenHour);
        
        directionalLight.intensity = sunIntensity;
        directionalLight.color = sunColor;

        // Sun rotation: high during day, low/setting during golden hour
        float sunAngle = Mathf.Lerp(-10f, 50f, daylight); 
        // Force low angle during 17:00-19:00 for the long shadows and mountain lighting
        if (hour > 16f && hour < 20f)
        {
            float t = Mathf.InverseLerp(16f, 20f, hour);
            sunAngle = Mathf.Lerp(15f, -5f, t);
        }
        
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 35f + hour * 4f, 0f);
    }
}
