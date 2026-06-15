using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the mission-site commission van by REUSING the office van model
/// (Resources/GeneratedArt/AS_OfficeVan.prefab) instead of the old procedural box
/// (removed 2026-06-13, PM: "too ugly"). The office van is a single closed Meshy shell,
/// so the rear "cargo door" is opened here in code: the rear end-cap triangles are
/// stripped to expose a permanently-open bay (no animated door — the van is parked),
/// and a simple dark interior liner makes the bay read from outside. The result wraps
/// the existing gameplay anchors (VAN_CargoZone / VAN_ExitPoint / VAN_DepartLever /
/// PlayerSpawnPoint) so the loop reads spatially: open rear over the cargo zone (north,
/// building side), cab toward the forecourt/spawn (south), west face beside the exit +
/// lever. The cut mesh is saved once as an asset; placement is deterministic from the
/// MEASURED prefab bounds (see production/session-state/active.md 2026-06-14).
///
/// One source of truth: <see cref="Build"/> both the menu and the whitebox builder call,
/// so a manual run and a full tower rebuild produce the identical van (idempotent).
/// </summary>
public static class MissionVanBuilder
{
    const string VanPrefabPath = "Assets/_Project/Resources/GeneratedArt/AS_OfficeVan.prefab";
    const string CutMeshPath   = "Assets/_Project/Art/Generated/Office_v1/AS_MissionVan_Mesh.asset";
    const string BayMatPath    = "Assets/_Project/Art/Generated/Office_v1/AS_MissionVan_BayInterior.mat";

    // LOCKED placement (measured: AS_OfficeVan bounds 2.19 W x 2.00 H x 5.14 L, pivot ~ bounds
    // centre on XZ, min.y = 0 at root.y = 0). Root pos/rot reproduce the verified parking:
    // rear (open) end at z = -2.0 = VAN pad north edge = CargoZone north edge; cab at z = -7.14.
    static readonly Vector3 RootPos = new Vector3(20f, 0f, -4.571f);
    static readonly Vector3 RootEuler = new Vector3(0f, 90f, 0f);

    // Rear end-cap strip (mesh-local space; for THIS model cab = +X, rear = -X).
    const float RearSlabFraction = 0.18f;   // rearmost X fraction scanned for the back wall
    const float RearNormalMaxX   = -0.25f;  // only strip faces whose normal points outward (-X)

    // Van/bay footprint in WORLD space at the locked placement.
    const float Cx = 20f;            // van centre line (east/west)
    const float ZRear = -2.0f;       // open rear (north)
    const float ZBayFront = -4.78f;  // interior front wall of the bay
    const float ZCab = -7.14f;       // cab (south) end
    const float Top = 2.0f;          // shell height

    [MenuItem("Tools/Black Commission/MVP/Tower/Build Mission Van (reuse office van)")]
    static void BuildMenu()
    {
        var anchor = GameObject.Find("VAN_CargoZone");
        Transform parent = anchor != null ? anchor.transform.parent : null;
        if (parent == null)
        {
            // Fall back to the whitebox root if the anchor is absent.
            var root = GameObject.Find("Tower_v8_Whitebox");
            parent = root != null ? root.transform : null;
        }
        if (parent == null)
        {
            Debug.LogError("[MissionVan] No VAN_CargoZone / Tower_v8_Whitebox in scene — " +
                           "build the tower whitebox first, then re-run.");
            return;
        }

        var van = Build(parent);
        if (van != null)
        {
            Selection.activeGameObject = van;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(van.scene);
            Debug.Log("[MissionVan] Built. Walk it in preview, then Ctrl+S to keep. " +
                      "Re-bake NavMesh (Tools > Black Commission > MVP > Tower > Bake Tower NavMesh) " +
                      "so agents path around the shell.");
        }
    }

    /// <summary>
    /// Construct (or rebuild) the mission van under <paramref name="parent"/>. Idempotent:
    /// any existing "MissionVan" under the parent is removed first. Ensures the cut-mesh and
    /// bay-interior material assets exist (creates them on first run).
    /// </summary>
    public static GameObject Build(Transform parent)
    {
        // Idempotent: clear a previous build.
        var existing = parent.Find("MissionVan");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        Mesh cutMesh = EnsureCutMesh();
        if (cutMesh == null) return null;
        Material bayMat = EnsureBayMaterial();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VanPrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[MissionVan] Office van prefab not found at {VanPrefabPath}.");
            return null;
        }

        // Root container (identity) so the world-space fixtures are axis-aligned.
        var root = new GameObject("MissionVan");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;

        // ── Shell: the office van model with the rear cap stripped ──────────────────
        var shell = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
        PrefabUtility.UnpackPrefabInstance(shell, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        shell.name = "Shell";
        shell.transform.localPosition = RootPos;
        shell.transform.localRotation = Quaternion.Euler(RootEuler);
        var mf = shell.GetComponentInChildren<MeshFilter>();
        if (mf != null) mf.sharedMesh = cutMesh;

        // ── Fixtures: bay interior liner + colliders (identity space = world) ───────
        var fix = new GameObject("Fixtures");
        fix.transform.SetParent(root.transform, false);

        float zBayCtr = (ZRear + ZBayFront) * 0.5f;          // -3.39
        float zBayDepth = ZRear - ZBayFront;                 // 2.78
        float zCabCtr = (ZBayFront + ZCab) * 0.5f;           // -5.96
        float zCabDepth = ZBayFront - ZCab;                  // 2.34

        // Liner (visible, dark interior): roof + two sides + interior front wall. No rear wall
        // (the loading opening) and no bay floor mesh (the column rests on the pad ground).
        AddBox(fix.transform, "Bay_Roof",  new Vector3(Cx, Top - 0.08f, zBayCtr), new Vector3(2.02f, 0.08f, zBayDepth + 0.05f), bayMat, true, true);
        AddBox(fix.transform, "Bay_SideW", new Vector3(Cx - 1.0f, Top * 0.5f, zBayCtr), new Vector3(0.08f, Top - 0.06f, zBayDepth + 0.05f), bayMat, true, true);
        AddBox(fix.transform, "Bay_SideE", new Vector3(Cx + 1.0f, Top * 0.5f, zBayCtr), new Vector3(0.08f, Top - 0.06f, zBayDepth + 0.05f), bayMat, true, true);
        AddBox(fix.transform, "Bay_Front", new Vector3(Cx, Top * 0.5f, ZBayFront), new Vector3(2.0f, Top - 0.06f, 0.08f), bayMat, true, true);

        // Insurance bay floor (collider-only, flush): guarantees a rest surface inside the
        // cargo zone even if the pad ground has a seam. Top ~ y0.02.
        AddBox(fix.transform, "Bay_Floor", new Vector3(Cx, -0.04f, zBayCtr), new Vector3(2.0f, 0.12f, zBayDepth), null, true, false);

        // Cab/front solid (collider-only, inside the shell): makes the front half solid so the
        // van reads as one obstacle; the bay cavity stays the only walk-in opening.
        AddBox(fix.transform, "Cab_Solid", new Vector3(Cx, Top * 0.5f, zCabCtr), new Vector3(2.16f, Top, zCabDepth), null, true, false);

        return root;
    }

    static void AddBox(Transform parent, string name, Vector3 center, Vector3 size,
                       Material mat, bool collider, bool visible)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localScale = size;

        var bc = go.GetComponent<BoxCollider>();
        if (!collider && bc != null) Object.DestroyImmediate(bc);

        if (!visible)
        {
            var r = go.GetComponent<MeshRenderer>();
            if (r != null) Object.DestroyImmediate(r);
            var m = go.GetComponent<MeshFilter>();
            if (m != null) Object.DestroyImmediate(m);
        }
        else if (mat != null)
        {
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }

    /// <summary>Create (once) the rear-opened copy of the office van mesh.</summary>
    static Mesh EnsureCutMesh()
    {
        var cached = AssetDatabase.LoadAssetAtPath<Mesh>(CutMeshPath);
        if (cached != null) return cached;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VanPrefabPath);
        var srcMf = prefab != null ? prefab.GetComponentInChildren<MeshFilter>() : null;
        Mesh src = srcMf != null ? srcMf.sharedMesh : null;
        if (src == null)
        {
            Debug.LogError($"[MissionVan] Could not read source mesh from {VanPrefabPath}.");
            return null;
        }

        Vector3[] verts = src.vertices;
        Vector3[] normals = src.normals;
        int[] tris = src.triangles;
        bool hasNormals = normals != null && normals.Length == verts.Length;

        float minX = src.bounds.min.x;
        float sizeX = Mathf.Max(src.bounds.size.x, 1e-6f);
        float cutX = minX + RearSlabFraction * sizeX;

        var keep = new List<int>(tris.Length);
        int stripped = 0;
        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i], b = tris[i + 1], c = tris[i + 2];
            Vector3 ctr = (verts[a] + verts[b] + verts[c]) / 3f;
            float nx = 0f;
            if (hasNormals) nx = ((normals[a] + normals[b] + normals[c]) / 3f).normalized.x;
            bool isRearCap = ctr.x < cutX && nx < RearNormalMaxX;
            if (isRearCap) { stripped++; continue; }
            keep.Add(a); keep.Add(b); keep.Add(c);
        }

        Mesh cut = Object.Instantiate(src);
        cut.name = "AS_MissionVan_Mesh";
        cut.triangles = keep.ToArray();
        cut.RecalculateBounds();

        AssetDatabase.CreateAsset(cut, CutMeshPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[MissionVan] Cut mesh saved ({stripped} rear-cap tris removed, " +
                  $"{keep.Count / 3} kept) → {CutMeshPath}");
        return cut;
    }

    /// <summary>Create (once) a plain dark URP material for the bay interior.</summary>
    static Material EnsureBayMaterial()
    {
        var cached = AssetDatabase.LoadAssetAtPath<Material>(BayMatPath);
        if (cached != null) return cached;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var mat = new Material(shader) { name = "AS_MissionVan_BayInterior" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.16f, 0.155f, 0.15f));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.16f, 0.155f, 0.15f));
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);
        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.15f);
        AssetDatabase.CreateAsset(mat, BayMatPath);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
