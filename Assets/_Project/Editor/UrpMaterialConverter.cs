using UnityEngine;
using UnityEditor;

/// <summary>
/// Scoped Built-in → URP material fix for the imported Asset Store packs (TirgamesAssets/Factory +
/// Sat Productions). Unity 6 replaced the old "Convert All Built-in Materials to URP" menu with the
/// interactive Render Pipeline Converter window, so this does the equivalent in-place, but ONLY for
/// materials under the pack folders (no blast radius on _Project or other assets). Re-running is safe
/// (already-URP materials are skipped).
///
/// Menu: Tools > Black Commission > MVP > Convert Pack Materials To URP
/// Only touches the common opaque shaders (Standard / Standard (Specular setup) / Autodesk Interactive);
/// leaves skybox / particle / custom shaders alone.
/// </summary>
public static class UrpMaterialConverter
{
    static readonly string[] PackFolders = { "Assets/TirgamesAssets", "Assets/Sat Productions" };

    [MenuItem("Tools/Black Commission/MVP/Convert Pack Materials To URP")]
    public static void ConvertPackMaterials()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null)
        {
            Debug.LogError("[UrpMaterialConverter] 'Universal Render Pipeline/Lit' not found — is URP installed?");
            return;
        }

        int converted = 0, skipped = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", PackFolders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null) { skipped++; continue; }

            string sn = mat.shader.name;
            bool isStandard = sn == "Standard" || sn == "Standard (Specular setup)" ||
                              sn.Contains("Autodesk Interactive");
            if (!isStandard) { skipped++; continue; } // leave URP / skybox / particle / custom alone

            // Capture the Built-in inputs before swapping the shader.
            Texture main  = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Color col     = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture bump  = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            Texture metal = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null;
            float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;
            float smooth   = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;

            mat.shader = lit;
            if (main != null)  mat.SetTexture("_BaseMap", main);
            mat.SetColor("_BaseColor", col);
            if (bump != null)  { mat.SetTexture("_BumpMap", bump); mat.EnableKeyword("_NORMALMAP"); }
            if (metal != null) { mat.SetTexture("_MetallicGlossMap", metal); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smooth);
            EditorUtility.SetDirty(mat);
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UrpMaterialConverter] Converted {converted} pack material(s) to URP/Lit, skipped {skipped} " +
                  "(already-URP / non-standard).");
    }
}
