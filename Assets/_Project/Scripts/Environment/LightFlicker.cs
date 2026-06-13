using UnityEngine;

/// <summary>
/// Cheap light-anchor life: makes a Point light feel like aging municipal hardware.
/// Two characters (art bible light grammar):
///   Sway    — sodium lamp breathing slowly on unstable power (gentle, never off)
///   Sputter — a dying fluorescent/duty lamp that drops out for a beat now and then
/// Deterministic per instance (seed from position) so peers see the same mood even
/// though the light itself is purely local/cosmetic.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    public enum Character { Sway, Sputter }

    [SerializeField] Character character = Character.Sway;
    [Tooltip("Fraction of base intensity the wobble spans (Sway) or dims to during a sputter dropout.")]
    [SerializeField] float depth = 0.25f;
    [SerializeField] float swaySpeed = 0.6f;
    [Tooltip("Sputter only: average seconds between dropouts.")]
    [SerializeField] float sputterEvery = 4.5f;

    Light cachedLight;
    float baseIntensity;
    float seed;
    float sputterUntil;
    float nextSputterAt;

    void Awake()
    {
        cachedLight = GetComponent<Light>();
        baseIntensity = cachedLight.intensity;
        Vector3 p = transform.position;
        seed = (p.x * 7.13f + p.z * 3.71f) % 100f;
        nextSputterAt = Time.time + sputterEvery * (0.5f + Frac(seed) * 1.0f);
    }

    void Update()
    {
        switch (character)
        {
            case Character.Sway:
            {
                float n = Mathf.PerlinNoise(seed, Time.time * swaySpeed);
                cachedLight.intensity = baseIntensity * (1f - depth * 0.5f + n * depth);
                break;
            }
            case Character.Sputter:
            {
                if (Time.time >= nextSputterAt)
                {
                    // Dropout burst: 0.06–0.3s of struggling output.
                    sputterUntil = Time.time + 0.06f + Frac(seed * 17.7f + Time.time) * 0.24f;
                    nextSputterAt = Time.time + sputterEvery * (0.4f + Frac(seed + Time.time) * 1.2f);
                }
                bool dropping = Time.time < sputterUntil;
                float target = dropping
                    ? baseIntensity * (1f - depth) * Frac(Time.time * 31f) // unstable while dropping
                    : baseIntensity;
                cachedLight.intensity = Mathf.Lerp(cachedLight.intensity, target, 0.5f);
                break;
            }
        }
    }

    static float Frac(float v) => v - Mathf.Floor(v);

    /// <summary>Builder hook: configure in one call (editor wiring via AddComponent).</summary>
    public void Configure(Character c, float flickerDepth, float speedOrInterval)
    {
        character = c;
        depth = flickerDepth;
        if (c == Character.Sway) swaySpeed = speedOrInterval;
        else sputterEvery = speedOrInterval;
    }
}
