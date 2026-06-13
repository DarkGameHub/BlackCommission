using UnityEngine;

/// <summary>
/// Slow emissive breathing for the eco column's sealed glass — the only living
/// thing in the building reads as faintly alive. Uses a per-renderer
/// MaterialPropertyBlock so the shared whitebox material asset is never touched.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class EmissionPulse : MonoBehaviour
{
    [SerializeField] float period = 3.4f;
    [Tooltip("Emission scale swings between 1-depth and 1+depth.")]
    [SerializeField] float depth = 0.30f;

    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    Renderer cachedRenderer;
    Color baseEmission;
    MaterialPropertyBlock mpb;

    void Awake()
    {
        cachedRenderer = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
        Material m = cachedRenderer.sharedMaterial;
        baseEmission = m != null && m.HasProperty(EmissionColorId)
            ? m.GetColor(EmissionColorId)
            : Color.white;
    }

    void Update()
    {
        float s = 1f + Mathf.Sin(Time.time * (2f * Mathf.PI / period)) * depth;
        cachedRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(EmissionColorId, baseEmission * s);
        cachedRenderer.SetPropertyBlock(mpb);
    }
}
