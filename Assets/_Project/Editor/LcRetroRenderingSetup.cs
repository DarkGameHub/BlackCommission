using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-click setup for the LC-style retro rendering stack
/// (docs/art/lofi-uplift-and-outline-research.md, style-lock v2):
///   1. URP render scale 0.5 + point (nearest) upscaling — the fixed-low-res look.
///   2. FullScreenPassRendererFeature running BlackCommission/LcOutline
///      (depth+normals Roberts cross, civic blue-black #0A0F14).
/// Idempotent: re-running updates the existing material/feature in place.
/// </summary>
public static class LcRetroRenderingSetup
{
    const string MatPath = "Assets/_Project/Art/Rendering/M_LcOutline.mat";
    const string FeatureName = "LC_Retro_Outline";
    const float RetroRenderScale = 0.5f;

    [MenuItem("Tools/Black Commission/Art/Setup LC Retro Rendering")]
    public static void Setup()
    {
        var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
        {
            Debug.LogError("[LcRetro] No UniversalRenderPipelineAsset in GraphicsSettings.");
            return;
        }

        // ---- 1. low-res render + chunky upscale ----
        urp.renderScale = RetroRenderScale;
        urp.upscalingFilter = UpscalingFilterSelection.Point;
        EditorUtility.SetDirty(urp);

        // ---- 2. outline material ----
        Shader shader = Shader.Find("BlackCommission/LcOutline");
        if (shader == null)
        {
            Debug.LogError("[LcRetro] Shader BlackCommission/LcOutline not found (compile error?).");
            return;
        }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        mat.shader = shader;
        ColorUtility.TryParseHtmlString("#0A0F14", out Color outlineCol);
        mat.SetColor("_OutlineColor", outlineCol);
        mat.SetFloat("_DepthThreshold", 1.5f);
        mat.SetFloat("_NormalThreshold", 0.4f);
        mat.SetFloat("_OutlineStrength", 0.6f); // PM: 0.85 too heavy (2026-06-10)
        EditorUtility.SetDirty(mat);

        // ---- 3. renderer feature on the active renderer data ----
        var pipelineSo = new SerializedObject(urp);
        var dataList = pipelineSo.FindProperty("m_RendererDataList");
        if (dataList == null || dataList.arraySize == 0)
        {
            Debug.LogError("[LcRetro] URP asset has no renderer data list.");
            return;
        }
        var rendererData = dataList.GetArrayElementAtIndex(0).objectReferenceValue as ScriptableRendererData;
        if (rendererData == null)
        {
            Debug.LogError("[LcRetro] Renderer data [0] is null.");
            return;
        }

        var existing = rendererData.rendererFeatures.FirstOrDefault(f => f != null && f.name == FeatureName)
            as FullScreenPassRendererFeature;
        if (existing == null)
        {
            existing = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
            existing.name = FeatureName;
            AssetDatabase.AddObjectToAsset(existing, rendererData);
            rendererData.rendererFeatures.Add(existing);

            // The feature map stores each feature's local file id; resolve it after import.
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(rendererData));
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(existing, out _, out long localId);
            var dataSo = new SerializedObject(rendererData);
            var map = dataSo.FindProperty("m_RendererFeatureMap");
            map.arraySize = rendererData.rendererFeatures.Count;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            dataSo.ApplyModifiedPropertiesWithoutUndo();
        }

        existing.passMaterial = mat;
        existing.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
        existing.requirements = ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth |
                                ScriptableRenderPassInput.Normal;
        existing.fetchColorBuffer = true;
        EditorUtility.SetDirty(existing);
        EditorUtility.SetDirty(rendererData);

        AssetDatabase.SaveAssets();
        Debug.Log($"[LcRetro] Setup OK: renderScale={RetroRenderScale} (point upscale), " +
                  $"outline feature '{FeatureName}' on {rendererData.name}, material {MatPath}. " +
                  "Tune _DepthThreshold/_NormalThreshold on the material while playing.");
    }

    /// <summary>Restores native-resolution rendering (keeps the outline feature).</summary>
    [MenuItem("Tools/Black Commission/Art/Disable Retro Render Scale")]
    public static void DisableRenderScale()
    {
        var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null) return;
        urp.renderScale = 1f;
        urp.upscalingFilter = UpscalingFilterSelection.Auto;
        EditorUtility.SetDirty(urp);
        AssetDatabase.SaveAssets();
        Debug.Log("[LcRetro] Render scale restored to 1.0 (outline feature left enabled).");
    }
}
