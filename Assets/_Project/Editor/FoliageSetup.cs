using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BlackCommission.Level;

/// <summary>
/// One-click setup for the imported "Foliage Free" pack (PM 2026-06-20): converts its legacy built-in nature
/// materials to URP/Lit (alpha-clipped, double-sided leaf cards) recoloured to the dead Municipal Debt Noir
/// palette, then authors <c>Assets/Resources/FoliageSet.asset</c> pointing <see cref="MapSiteBuilder"/> at the
/// pine models (trees) and the fern model (bushes). After running, Build Full Map 2 (or Play) scatters real
/// foliage instead of whitebox primitives. Re-running is safe (idempotent).
///
/// Menu: Tools ▸ Black Commission ▸ Map ▸ Setup Foliage (URP + Resources)
/// </summary>
public static class FoliageSetup
{
    const string PackFolder = "Assets/Foliage Free";
    const string SetPath = "Assets/Resources/FoliageSet.asset";
    static readonly Color DeadTint = new Color(0.62f, 0.60f, 0.46f); // desaturated dead olive (kills vibrant green)

    [MenuItem("Tools/Black Commission/Map/Setup Foliage (URP + Resources)")]
    public static void Setup()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) { Debug.LogError("[FoliageSetup] URP/Lit shader not found — is URP installed?"); return; }
        if (!AssetDatabase.IsValidFolder(PackFolder))
        {
            Debug.LogError($"[FoliageSetup] '{PackFolder}' not found — import the Foliage Free pack first.");
            return;
        }

        // 1. Convert the pack's legacy materials to URP/Lit: alpha-clipped, double-sided cards, dead-tinted.
        int conv = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { PackFolder }))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat == null) continue;
            Texture main = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            mat.shader = lit;
            if (main != null) mat.SetTexture("_BaseMap", main);
            mat.SetColor("_BaseColor", DeadTint);
            mat.SetFloat("_Smoothness", 0.12f);
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_AlphaClip", 1f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetFloat("_Cutoff", 0.45f);
            mat.SetFloat("_Cull", 0f);              // double-sided leaf / fern cards
            mat.renderQueue = 2450;                 // AlphaTest queue
            EditorUtility.SetDirty(mat);
            conv++;
        }

        // 2. Author the FoliageSet (trees = pines, bushes = ferns) into Resources for the runtime builder.
        var trees = new List<GameObject>();
        foreach (var n in new[] { "pine1a", "pine1b", "pine2a", "pine2b" })
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackFolder}/models/{n}.fbx");
            if (go != null) trees.Add(go);
        }
        var ferns = AssetDatabase.LoadAssetAtPath<GameObject>($"{PackFolder}/models/ferns.fbx");

        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        var set = AssetDatabase.LoadAssetAtPath<FoliageSet>(SetPath);
        bool created = set == null;
        if (created) set = ScriptableObject.CreateInstance<FoliageSet>();
        set.trees = trees.ToArray();
        set.bushes = ferns != null ? new[] { ferns } : new GameObject[0];
        set.treeHeight = 6f;
        set.bushHeight = 0.9f;
        set.trunkRadius = 0.35f;
        // Force-applied materials (the FBX material binding is unreliable) + a tiled grass ground.
        set.treeMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{PackFolder}/materials/pinetree.mat");
        set.bushMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{PackFolder}/materials/fern.mat");
        set.groundMaterial = MakeGroundMaterial(lit);
        // Unity-Terrain outdoor (PM 2026-06-21 pivot, LC-style): a base grass TerrainLayer for the heightmap
        // surface, a bladed detail-grass billboard texture (the "真草" carpet), and a URP terrain material so the
        // Terrain renders lit instead of magenta. Non-null grassLayer flips MapSiteBuilder onto the Terrain path.
        set.grassLayer = MakeGrassLayer();
        set.grassDetail = AssetDatabase.LoadAssetAtPath<Texture2D>($"{PackFolder}/detail/grass_detail1.tga");
        set.terrainMaterial = MakeTerrainMaterial();
        set.grassDetailPrefab = MakeGrassTuft(lit);   // real 3-D grass cards (URP-reliable instanced Vertex-Lit mesh)
        if (created) AssetDatabase.CreateAsset(set, SetPath);
        else EditorUtility.SetDirty(set);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FoliageSetup] converted {conv} material(s) to URP; FoliageSet → {trees.Count} trees, " +
                  $"{(ferns != null ? 1 : 0)} bush, grass ground + Terrain (layer={(set.grassLayer != null)}, " +
                  $"detailTex={(set.grassDetail != null)}, grassMesh={(set.grassDetailPrefab != null)}, " +
                  $"urpMat={(set.terrainMaterial != null)}). {(created ? "created" : "updated")} {SetPath}.");
    }

    // Build/refresh a tiled, desaturated grass material for the outdoor ground from the pack's grass texture.
    static Material MakeGroundMaterial(Shader lit)
    {
        const string path = "Assets/Resources/GrassGround.mat";
        const string texPath = PackFolder + "/textures/grass2_diffuse.tga";
        if (AssetImporter.GetAtPath(texPath) is TextureImporter ti && ti.wrapMode != TextureWrapMode.Repeat)
        {
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);

        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool created = m == null;
        if (created) m = new Material(lit); else m.shader = lit;
        if (tex != null) m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", new Color(0.46f, 0.58f, 0.30f)); // muted grass green (reads as a dry lawn, not dirt)
        m.SetFloat("_Smoothness", 0.05f);
        m.SetFloat("_Metallic", 0f);
        m.SetTextureScale("_BaseMap", new Vector2(50f, 65f));     // ~4 m tiles across the ~200×266 m ground
        if (created) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
        return m;
    }

    // Base grass TerrainLayer painted over the whole Terrain surface (the heightmap ground texture). tileSize 8 m
    // → the grass diffuse repeats every ~8 m so it reads as ground, not a stretched sheet.
    static TerrainLayer MakeGrassLayer()
    {
        const string path = "Assets/Resources/GrassTerrainLayer.terrainlayer";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(PackFolder + "/textures/grass2_diffuse.tga");
        var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
        bool created = layer == null;
        if (created) layer = new TerrainLayer();
        layer.diffuseTexture = tex;
        layer.tileSize = new Vector2(8f, 8f);
        layer.tileOffset = Vector2.zero;
        layer.specular = Color.black;
        layer.metallic = 0f;
        layer.smoothness = 0f;
        if (created) AssetDatabase.CreateAsset(layer, path); else EditorUtility.SetDirty(layer);
        return layer;
    }

    // URP terrain material template so the Terrain renders lit (not magenta) under URP. Returns null (with a
    // warning) if the URP Terrain shader is missing — the builder then leaves the Terrain on its default material.
    static Material MakeTerrainMaterial()
    {
        const string path = "Assets/Resources/TerrainURP.mat";
        var shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (shader == null)
        {
            Debug.LogWarning("[FoliageSetup] 'Universal Render Pipeline/Terrain/Lit' not found — Terrain may render magenta.");
            return null;
        }
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool created = m == null;
        if (created) m = new Material(shader); else m.shader = shader;
        if (created) AssetDatabase.CreateAsset(m, path); else EditorUtility.SetDirty(m);
        return m;
    }

    // Author the grass-card MESH detail prototype (the "真草"). URP fails to draw texture/billboard terrain grass,
    // but INSTANCED Vertex-Lit detail MESHES render across all pipelines (Unity Manual: "Instancing details work
    // with all render pipelines"). So we build: a URP/Lit alpha-clipped GPU-instanced material textured with the
    // grass detail tga, a 3-quad crossed mesh, and a Resources prefab the runtime terrain builder references.
    static GameObject MakeGrassTuft(Shader lit)
    {
        const string matPath = "Assets/Resources/GrassDetail.mat";
        const string meshPath = "Assets/Resources/GrassTuftMesh.asset";
        const string prefabPath = "Assets/Resources/GrassTuft.prefab";
        const string texPath = PackFolder + "/detail/grass_detail1.tga";
        // Make sure the grass card's alpha is treated as transparency (so the quad reads as a grass cutout, not an
        // opaque rectangle) — same importer-touching pattern as MakeGroundMaterial's wrap fix.
        if (AssetImporter.GetAtPath(texPath) is TextureImporter ti &&
            (ti.alphaSource != TextureImporterAlphaSource.FromInput || !ti.alphaIsTransparency))
        {
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        // (1) URP/Lit material: alpha-clipped, double-sided card, dead-olive, GPU instancing ON (required for
        //     instanced terrain details).
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        bool matNew = mat == null;
        if (matNew) mat = new Material(lit); else mat.shader = lit;
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex); // VertexLit details read MainTex
        }
        mat.SetColor("_BaseColor", new Color(0.42f, 0.50f, 0.30f)); // dry desaturated green (Municipal Debt Noir)
        mat.SetFloat("_Smoothness", 0.05f);
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_AlphaClip", 1f);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.SetFloat("_Cutoff", 0.4f);
        mat.SetFloat("_Cull", 0f);              // double-sided so the card shows from behind
        mat.renderQueue = 2450;                 // AlphaTest queue
        mat.enableInstancing = true;
        if (matNew) AssetDatabase.CreateAsset(mat, matPath); else EditorUtility.SetDirty(mat);

        // (2) 3-quad crossed mesh (base on y=0), saved/refreshed in place.
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        bool meshNew = mesh == null;
        if (meshNew) mesh = new Mesh();
        FillCrossQuadMesh(mesh);
        if (meshNew) AssetDatabase.CreateAsset(mesh, meshPath); else EditorUtility.SetDirty(mesh);

        // (3) prefab: GameObject(MeshFilter+MeshRenderer) the terrain DetailPrototype points at.
        var go = new GameObject("GrassTuft");
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // Fill (clear + rebuild) a Mesh as three alpha-card quads crossed at 0/60/120° around Y, base at y=0,
    // ~0.5 m wide × 1 m tall, normals up (even soft lighting on grass cards). Width/height are scaled per-instance
    // by the DetailPrototype min/max at paint time.
    static void FillCrossQuadMesh(Mesh mesh)
    {
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        const float w = 0.25f, h = 1.0f;
        for (int q = 0; q < 3; q++)
        {
            float a = q * 60f * Mathf.Deg2Rad;
            Vector3 right = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * w;
            Vector3 up = new Vector3(0f, h, 0f);
            int b = verts.Count;
            verts.Add(-right); verts.Add(right); verts.Add(right + up); verts.Add(-right + up);
            for (int k = 0; k < 4; k++) norms.Add(Vector3.up);
            uvs.Add(new Vector2(0, 0)); uvs.Add(new Vector2(1, 0)); uvs.Add(new Vector2(1, 1)); uvs.Add(new Vector2(0, 1));
            tris.Add(b + 0); tris.Add(b + 2); tris.Add(b + 1);
            tris.Add(b + 0); tris.Add(b + 3); tris.Add(b + 2);
        }
        mesh.Clear();
        mesh.name = "GrassTuftMesh";
        mesh.SetVertices(verts);
        mesh.SetNormals(norms);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();
    }
}
