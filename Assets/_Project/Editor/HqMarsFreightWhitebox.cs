using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// MATERIALIZED builder for the LOCKED HQ concept「废弃火星轨道货运堂」纯室内 (Option B), per
/// <c>design/hq/hq-architecture-pass.md</c> (§7 materiality, §8 light), section <c>HQ_Section_AA_v3.png</c>, plan
/// <c>HQ_SitePlan_v5.png</c> (PM David, 2026-06-24).
///
/// This SUPERSEDES the gray-box pass (PM 2026-06-24: "贴上材质 + 用我原来HQ里的模型 + 屋顶镂空 + 建曲面"):
///   • SHELL = ONE continuous swept CURVED MESH (Catmull-Rom cross-section lofted along Z), watertight, smooth-
///     normalled and double-sided — a true fluid「扎哈=火星」surface, NOT faceted boxes, and never hollow. REDIRECT
///     (2026-06-24, PM「太丑」+「突出曾经的火星·被遗弃」): one DIRECTIONAL sweep — low south haunch rising to a single
///     NORTH crest 13.6 m over the door, then a short sharp fall to a TORN CANTILEVER prow; the apex drifts east as it
///     climbs (Section P2 lerp) so it SWEEPS, not extrudes. Material pushed COLD PALE BLUE-GREY (outside the Earth
///     palette = Mars-alien). ALL decay (torn prow + rebar, missing panels, exposed ribs, hanging conduit, vertical
///     stains, the daylight crack) clusters NORTH; the south nest corner stays sound. Steel dead conveyor down spine.
///   • PROPS = the real authored <c>AS_Office*</c> prefabs (CRT computer, debt board, gear wall, folding table,
///     sofa, sentinel, fire-ext, lamps, the DispatchVan) — the same models the old wedge used, not hand-built cubes.
///   • NEST = cheap painted-metal cladding (6.6×7.0 m, 2.8 m roof — NEVER enlarged); the warmth is the tungsten
///     light (§8), not the paint.
///   • LIGHT = a cold directional KEY with soft shadows (grounds the materials) + the §8 interior pools (warm nest
///     tungsten, CRT phosphor, one cold crack-daylight on the muster, one sodium van bay, door backglow) + a
///     sputtering Tirgames fluorescent tube (the broke office), and an LC grain/bloom/desaturate post volume.
///   • The 4.8 m freight roll-up door (real Tirgames LargeGates, fit as a RETRACTED coil) FRAMES the NON-WALKABLE
///     Mars-scrapyard backdrop (real Tirgames debris/barrels/gas-tank + silhouettes) — seen, never entered.
///
/// Built at FINAL metres (no ×1.2 — locked dims are already post-scale). Player stays 2 m. Self-contained + ADDITIVE:
/// does NOT touch <c>HqOptionAProductionBuilder</c>; deactivates (never deletes) legacy geometry; repositions the
/// spawn + office-computer anchors into the nest; preserves NetworkManager/UI/cameras/van. Does NOT auto-save.
/// Every Tirgames/AS load falls back to a labelled box, so the build never breaks if an asset is missing.
///
/// Menu: Tools ▸ Black Commission ▸ Map ▸ Build HQ White-box - Mars Freight Option B
///       Tools ▸ Black Commission ▸ Map ▸ Remove HQ White-box (restore)
/// </summary>
public static class HqMarsFreightWhitebox
{
    const string RootName = "HQ_MarsWhitebox";
    const string SceneName = "HQ";

    // ---- LOCKED final metres (section v3 + plan v5). Spine runs +Z from the nest (z0) to the door (z27). ----
    const float Depth = 27f;      // nest (~6) + hall (~21)
    const float DoorZ = 27f;
    const float DoorH = 4.8f;     // the roll-up reveals the whole van head + a sliver of sky
    const float DoorW = 4.0f;
    const float WestX = -1f;      // west wall plane (the nest / low-eave side)
    const float NestRoofY = 2.8f; // the warm human ceiling — NEVER enlarged (the contrast IS the thesis)

    // ---- in-project kits (reused, not re-created) ----
    const string TIR = "Assets/TirgamesAssets/Factory/Models/Materials/";
    const string TIRP = "Assets/TirgamesAssets/Factory/Prefabs/";
    const string GA = "GeneratedArt/"; // Resources.Load prefix for the authored AS_Office props

    // Shell RIDGE height profile along Z (z, y) — the section's top silhouette. REDIRECT (2026-06-24): one
    // DIRECTIONAL sweep — low south haunch (~5.8) rising to a SINGLE north crest 13.6 over the door (z~20), then a
    // short sharp fall to the door head (~8.6); the torn cantilever continues past z27. NOT the old symmetric lump.
    static readonly Vector2[] Ridge =
    {
        new(0f, 5.8f), new(5f, 6.7f), new(10f, 9.5f), new(14f, 11.6f), new(18f, 13.3f),
        new(20f, 13.6f), new(22f, 13.4f), new(25f, 10.8f), new(27f, 8.6f),
    };
    // East wall plane along Z (z, x) — the plan's curved EAST wall. REDIRECT: belly fattest (~12.9) NORTH at z~19,
    // then tightening to a narrow prow (9.0) at the door — a swept figure, not a symmetric tube.
    static readonly Vector2[] EastX =
    {
        new(0f, 10.0f), new(5f, 10.8f), new(10f, 11.8f), new(15f, 12.6f),
        new(19f, 12.9f), new(22f, 12.2f), new(25f, 10.2f), new(27f, 9.0f),
    };

    // ---- materials (real kit surfaces; flat fallbacks only if an asset is missing) ----
    static Material mShell, mShellDark, mFloor, mSteel, mGrime, mNest, mBackdrop, mCrack, mGreen, mRed, mAmber;

    // ---- light colours (§8 + Art Bible) ----
    static readonly Color WarmTungsten = Rgb(0xEC, 0xC4, 0x78);
    static readonly Color ColdDaylight = Rgb(0xC6, 0xD2, 0xC8);
    static readonly Color SodiumAmber = Rgb(0xE0, 0xB0, 0x5C);
    static readonly Color CrtGreen = Rgb(0x96, 0xE2, 0x8C);
    static readonly Color ColdIndustrial = Rgb(0xD9, 0xE2, 0xDD);

    // Gameplay/UI/camera objects never deactivated by the retire pass.
    static readonly HashSet<string> Keep = new()
    {
        "NetworkManager", "ConnectionManager", "DisconnectHandler", "MVP_OfficeComputer", "MVP_HUD", "HQUI",
        "SettlementUI", "MainMenu_UGUI", "HQMenuCamera", "HQSpawnManager", "PlayerSpawnPoint",
        "MVP_RuntimeStyle_Office_ExteriorDispatch", RootName,
    };

    [MenuItem("Tools/Black Commission/Map/Build HQ White-box - Mars Freight Option B")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName)
            Debug.LogWarning($"[HQ Mars] Active scene is '{scene.name}', not '{SceneName}'. Building into the active " +
                             "scene anyway (non-blocking). Open Assets/_Project/Scenes/HQ.unity and rebuild if this " +
                             "isn't where you want it.");

        var existing = GameObject.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing);
        int retired = RetireLegacy();

        BuildPalette();
        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        BuildShell(root.transform);
        BuildNest(root.transform);
        BuildInterior(root.transform);
        BuildVanBay(root.transform);
        BuildBackdrop(root.transform);
        ConfigureMood();
        BuildLights(root.transform);
        BuildPost(root.transform);
        BuildAtmosphere(root.transform);
        RepositionAnchors();

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        SceneView.FrameLastActiveSceneView();
        Debug.Log($"[HQ Mars] Built the MATERIALIZED Mars-freight Option-B HQ into '{scene.name}' (retired {retired} " +
                  "legacy objects). WALK IT: press Play — you spawn in the tiny warm NEST (2.8 m) under tungsten, turn " +
                  "to the CRT to pick a job, step out under the vast cold CURVED shell (apex 13.6 m, real concrete) and " +
                  "down the spine past the gear wall + dead conveyor to the DispatchVan in its sodium bay; the 4.8 m " +
                  "door frames the dead Mars yard. Save only if you want it (Ctrl+S; no auto-save). Undo: Tools ▸ Black " +
                  "Commission ▸ Map ▸ Remove HQ White-box (restore).");
    }

    [MenuItem("Tools/Black Commission/Map/Remove HQ White-box (restore)")]
    public static void Remove()
    {
        var root = GameObject.Find(RootName);
        if (root != null) Object.DestroyImmediate(root);
        int n = 0;
        foreach (GameObject go in AllObjects())
            if (!go.activeSelf && (go.GetComponent<Renderer>() != null || go.GetComponent<Collider>() != null))
            { go.SetActive(true); n++; }
        var van = GameObject.Find("MVP_RuntimeStyle_Office_ExteriorDispatch");
        if (van != null) foreach (var r in van.GetComponents<Renderer>()) r.enabled = true;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[HQ Mars] Removed the build, reactivated {n} legacy objects. For an exact baseline: " +
                  "`git checkout -- Assets/_Project/Scenes/HQ.unity`.");
    }

    // ----------------------------------------------------------------- shell (one swept curved mesh + ancillary)

    static void BuildShell(Transform root)
    {
        var p = Sub(root, "Shell");
        // Polished concrete floor (the walkable interior).
        Box(p, "Floor", new Vector3(5.5f, -0.1f, Depth * 0.5f), new Vector3(16f, 0.2f, Depth + 1f), mFloor);

        // The vast cold dead shell as ONE continuous swept CURVED MESH — watertight, smooth, double-sided.
        BuildShellMesh(p, mShell);

        // South back wall closes the z0 cross-section behind the nest.
        float backE = Sample(EastX, 0f);
        Box(p, "Wall_Back", new Vector3((WestX + backE) * 0.5f, 3.2f, -0.25f), new Vector3(backE - WestX + 0.8f, 6.6f, 0.4f), mShell);

        // North loading wall — frames the rolled-up freight door + the dead-yard view (z27).
        float nE = Sample(EastX, DoorZ);
        float ridgeN = Sample(Ridge, DoorZ);
        float doorL = 2.5f, doorR = doorL + DoorW;
        // Side walls stop near the LOCAL curved-skin height (≈6.8 W / ≈5.0 E) instead of the full apex, so they don't
        // read as a flat slab poking above the swept roofline; the lintel still rises to the apex under the crest.
        float wlH = 6.8f, wrH = 5.0f;
        Box(p, "Wall_N_L", new Vector3((WestX + doorL) * 0.5f, wlH * 0.5f, DoorZ), new Vector3(doorL - WestX, wlH, 0.35f), mShell);
        Box(p, "Wall_N_R", new Vector3((doorR + nE) * 0.5f, wrH * 0.5f, DoorZ), new Vector3(nE - doorR, wrH, 0.35f), mShell);
        Box(p, "Wall_N_Lintel", new Vector3((doorL + doorR) * 0.5f, (DoorH + ridgeN) * 0.5f, DoorZ), new Vector3(DoorW, ridgeN - DoorH, 0.35f), mShell);

        // The real Tirgames roll-up door, fit as a RETRACTED coil at the lintel (rolled up — the door FRAMES the yard).
        Vector3 coilPos = new((doorL + doorR) * 0.5f, DoorH - 0.05f, DoorZ - 0.18f);
        if (!FitPrefab(p, TIRP + "LargeGates1_2.prefab", "RollUpDoor_Coil", coilPos, 0f, new Vector3(DoorW, 0.55f, 0.5f)))
            Box(p, "RollUpDoor_Coil", coilPos, new Vector3(DoorW, 0.5f, 0.45f), mSteel);

        // Overhead story (§7). REDIRECT (2026-06-24, PM「突出曾经的火星·被遗弃」): ALL the wreckage clusters NORTH
        // (the torn prow end); the south nest corner stays sound — squatters chose the soundest corner.
        Box(p, "DeadConveyor", new Vector3(4.0f, 5.6f, 15.0f), new Vector3(0.6f, 0.4f, 16f), mSteel); // hangs UNDER the rising skin (the south crest is only ~7-8 m)

        // The TORN CANTILEVER bursts past the door head and breaks off over the backdrop; rebar juts from the tear.
        BoxEuler(p, "BrokenCantilever", new Vector3(5.0f, 7.8f, 28.6f), new Vector3(5.0f, 0.32f, 3.4f), new Vector3(15f, 0f, 7f), mShellDark);
        for (int i = 0; i < 5; i++)
            BoxEuler(p, $"TornRebar_{i}", new Vector3(3.0f + i * 1.0f, 6.7f + i * 0.05f, 30.0f), new Vector3(0.05f, 0.9f, 0.05f), new Vector3(70f, 0f, 5f), mShellDark, collider: false);

        // Missing skin panels on the north-east upper flank — dark recesses with the inner ribs exposed (abandonment).
        float[] mpX = { 7.2f, 7.6f, 7.0f };
        float[] mpY = { 10.3f, 7.5f, 6.6f };   // sampled to hug the descending NE skin flank (not float proud of it)
        float[] mpZ = { 22.5f, 24.0f, 25.5f };
        for (int i = 0; i < 3; i++)
        {
            Box(p, $"MissingPanel_{i}", new Vector3(mpX[i], mpY[i], mpZ[i]), new Vector3(1.1f, 1.3f, 0.3f), mShellDark, collider: false);
            for (int r = 0; r < 3; r++)
                Box(p, $"Rib_{i}_{r}", new Vector3(mpX[i] - 0.45f + r * 0.45f, mpY[i], mpZ[i] + 0.02f), new Vector3(0.06f, 1.2f, 0.06f), mShellDark, collider: false);
        }

        // A conduit hangs out of the tear, drooping toward the floor.
        BoxEuler(p, "HangingConduit", new Vector3(9.2f, 4.6f, 26.4f), new Vector3(0.08f, 4.2f, 0.08f), new Vector3(0f, 0f, 24f), mSteel, collider: false);

        // The cold daylight CRACK — origin at the north tear now, raking SW down onto the muster (REDIRECT).
        BoxEuler(p, "SkylightCrack", new Vector3(6.0f, 12.2f, 24.6f), new Vector3(0.5f, 0.2f, 3.0f), new Vector3(0f, 0f, 18f), mCrack, collider: false);

        // Bio-contamination = VERTICAL gravity streaks, clustered NORTH near the tear (REDIRECT: was tilted, mid-span).
        BoxEuler(p, "Stain_Run_A", new Vector3(10.6f, 4.6f, 22.5f), new Vector3(0.05f, 5.6f, 0.8f), new Vector3(0f, 0f, 0f), mGrime, collider: false);
        BoxEuler(p, "Stain_Run_B", new Vector3(10.0f, 4.0f, 24.6f), new Vector3(0.05f, 4.8f, 0.7f), new Vector3(0f, 0f, 0f), mGrime, collider: false);
        BoxEuler(p, "Stain_Run_C", new Vector3(9.4f, 3.6f, 25.8f), new Vector3(0.05f, 4.0f, 0.6f), new Vector3(0f, 0f, 0f), mGrime, collider: false);
    }

    // The shell skin: a Catmull-Rom cross-section (west base → eave → arch apex → long east descent → east base)
    // lofted along Z with the Ridge (apex height) + EastX (east extent) profiles. Smooth-normalled + double-sided
    // (mShell has _Cull=Off) so the swept surface reads as a true fluid curve and is never hollow from any angle.
    static void BuildShellMesh(Transform parent, Material mat)
    {
        const int zSteps = 72;   // ~0.37 m loft slices (REDIRECT: was 45 — kill faceting over the ~14 m span)
        const int arch = 12;     // Catmull-Rom samples per section segment (REDIRECT: was 6)
        var rows = new List<float>();
        var slices = new List<List<Vector2>>();
        for (int i = 0; i <= zSteps; i++)
        {
            float z = Depth * i / zSteps;
            float H = Sample(Ridge, z);
            float eX = Sample(EastX, z);
            float eaveY = Mathf.Clamp(H * 0.42f, 3.0f, 6.0f);
            slices.Add(Section(eaveY, H, eX, arch, z / Depth));
            rows.Add(z);
        }

        int cols = slices[0].Count;
        var verts = new List<Vector3>(rows.Count * cols);
        var uvs = new List<Vector2>(rows.Count * cols);
        for (int r = 0; r < rows.Count; r++)
        {
            var sec = slices[r];
            float u = 0f;
            for (int c = 0; c < cols; c++)
            {
                if (c > 0) u += Vector2.Distance(sec[c - 1], sec[c]);
                verts.Add(new Vector3(sec[c].x, sec[c].y, rows[r]));
                uvs.Add(new Vector2(u * 0.22f, rows[r] * 0.22f)); // ~1 concrete tile / 4.5 m
            }
        }

        var tris = new List<int>((rows.Count - 1) * (cols - 1) * 6);
        for (int r = 0; r < rows.Count - 1; r++)
            for (int c = 0; c < cols - 1; c++)
            {
                int a = r * cols + c, b = r * cols + c + 1, cc = (r + 1) * cols + c, d = (r + 1) * cols + c + 1;
                tris.Add(a); tris.Add(cc); tris.Add(b);
                tris.Add(b); tris.Add(cc); tris.Add(d);
            }

        var mesh = new Mesh { name = "HQ_ShellSkin" };
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        var go = new GameObject("ShellSkin");
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    // One cross-section (X-Y) at a Z slice: straight west wall A→P0, then a smooth Catmull-Rom arch P0→P4. REDIRECT:
    // the apex P2 lateral position LERPS 0.30→0.52·span by zT (south→north) — this converts the old straight EXTRUDE
    // into a true SWEEP (the ridge drifts east as it climbs north), the core fix for the symmetric-"lump" silhouette.
    static List<Vector2> Section(float eaveY, float H, float eX, int samples, float zT)
    {
        float span = eX - WestX;
        float rise = Mathf.Max(0.5f, H - eaveY);
        float p2x = Mathf.Lerp(0.30f, 0.52f, Mathf.Clamp01(zT));      // apex drifts east as the sweep climbs north
        Vector2 A = new(WestX, 0f);                                   // west base (meets floor)
        Vector2 P0 = new(WestX, eaveY);                               // west eave (top of the short west wall)
        Vector2 P1 = new(WestX + 0.10f * span, eaveY + 0.55f * rise); // steep west rise
        Vector2 P2 = new(WestX + p2x * span, H);                      // apex (ridge) — swept east-drift, not a fixed lean
        Vector2 P3 = new(eX - 0.16f * span, 0.50f * H);               // long east shoulder
        Vector2 P4 = new(eX, 0f);                                     // east base (meets floor)

        var pts = new List<Vector2> { A, P0 };
        Vector2[] cps = { P0, P0, P1, P2, P3, P4, P4 };
        for (int seg = 0; seg < 4; seg++)
            for (int s = 1; s <= samples; s++)
                pts.Add(CatmullRom(cps[seg], cps[seg + 1], cps[seg + 2], cps[seg + 3], (float)s / samples));
        return pts;
    }

    static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    // ----------------------------------------------------------------- the warm orthogonal NEST (SW corner)

    static void BuildNest(Transform root)
    {
        var p = Sub(root, "Nest");
        // Footprint x -0.8..5.8 (w 6.6), z -0.4..6.6 (d 7.0), roof 2.8. West + south are the shell; build the roof,
        // the east wall, the front (north) wall with a door gap at x 4.0..5.8 (the only way in/out). Cheap painted
        // cladding — the WARMTH is the tungsten light (§8), not the paint.
        Box(p, "Nest_Roof", new Vector3(2.5f, NestRoofY, 3.1f), new Vector3(6.6f, 0.2f, 7.0f), mNest);
        Box(p, "Nest_Wall_E", new Vector3(5.8f, NestRoofY * 0.5f, 3.1f), new Vector3(0.2f, NestRoofY, 7.0f), mNest);
        Box(p, "Nest_Wall_N", new Vector3(1.6f, NestRoofY * 0.5f, 6.6f), new Vector3(4.8f, NestRoofY, 0.2f), mNest);
    }

    // ----------------------------------------------------------------- interior (real AS_Office props)

    static void BuildInterior(Transform root)
    {
        var p = Sub(root, "Interior");

        // In the nest: the CRT job/settlement terminal (the only live signal), debt board, folding table + lamp, sofa.
        Prop(p, GA + "AS_OfficeComputer", "Computer_CRT", new Vector3(0.7f, 0f, 1.3f), 90f, new Vector3(1.4f, 1.05f, 0.9f));
        Prop(p, GA + "AS_OfficeDebtBoard", "DebtBoard", new Vector3(-0.7f, 1.3f, 3.3f), 90f, new Vector3(0.1f, 0.9f, 1.6f));
        Prop(p, GA + "AS_OfficeGasMaskSentinel", "Sentinel", new Vector3(0.6f, 0f, 5.6f), 35f, new Vector3(0.7f, 1.7f, 0.7f));
        Prop(p, GA + "AS_OfficeToolSet", "FileBoxes", new Vector3(0.8f, 0f, 4.7f), 20f, new Vector3(0.8f, 0.6f, 0.8f));
        Prop(p, GA + "AS_OfficeDesk", "FoldingTable", new Vector3(3.6f, 0f, 3.7f), 0f, new Vector3(1.2f, 0.75f, 0.8f));
        Prop(p, GA + "AS_LampDesk", "DeskLamp", new Vector3(3.95f, 0.75f, 3.7f), 0f, new Vector3(0.3f, 0.45f, 0.3f));
        Prop(p, GA + "AS_OfficeSofa", "Sofa", new Vector3(4.8f, 0f, 1.2f), 0f, new Vector3(1.8f, 0.85f, 0.8f));

        // Hall west wall: the half-empty gear wall + safety/garage corner (the broke office's thin supplies).
        Prop(p, GA + "AS_OfficeSupplyCabinet", "Gear_Supply", new Vector3(-0.4f, 0f, 9.4f), 90f, new Vector3(0.6f, 1.5f, 1.2f));
        Prop(p, GA + "AS_OfficeFilingCabinet", "Gear_Filing", new Vector3(-0.4f, 0f, 10.9f), 90f, new Vector3(0.6f, 1.25f, 0.9f));
        Prop(p, GA + "AS_OfficeToolRack", "Gear_ToolRack", new Vector3(-0.7f, 0.85f, 12.3f), 90f, new Vector3(0.15f, 1.3f, 1.1f));
        Prop(p, GA + "AS_OfficeSafetyBoard", "SafetyBoard", new Vector3(-0.7f, 1.15f, 8.1f), 90f, new Vector3(0.1f, 1.2f, 1.4f));
        Prop(p, GA + "AS_OfficeFireExtinguisher", "FireExt", new Vector3(-0.5f, 0f, 13.4f), 90f, new Vector3(0.5f, 0.55f, 0.5f));
        Prop(p, GA + "AS_GarageWorkshopCorner", "GarageCorner", new Vector3(0.2f, 0f, 14.6f), -130f, new Vector3(1.4f, 1.4f, 1.4f));

        // S1 padlocked side door on the hall west wall (peripheral, off-path — license-as-leash).
        Box(p, "S1_PadlockDoor_LOCKED", new Vector3(-0.94f, 1.05f, 17.2f), new Vector3(0.12f, 2.1f, 1.1f), mSteel);
        Box(p, "S1_Padlock", new Vector3(-0.76f, 1.1f, 17.2f), new Vector3(0.14f, 0.14f, 0.14f), mRed, collider: false);

        // Muster pad under the crack + 4 spawn markers in the nest (floor markings, no colliders).
        Box(p, "MusterPad", new Vector3(4.5f, 0.03f, 15.4f), new Vector3(3.2f, 0.06f, 2.4f), mAmber, collider: false);
        float[] sx = { 1.6f, 3.0f, 1.6f, 3.0f };
        float[] sz = { 2.2f, 2.2f, 4.0f, 4.0f };
        for (int i = 0; i < 4; i++)
            Box(p, $"SPAWN_{i + 1}", new Vector3(sx[i], 0.04f, sz[i]), new Vector3(0.6f, 0.06f, 0.6f), mAmber, collider: false);
    }

    // ----------------------------------------------------------------- the van, parked & boarded INSIDE the bay

    static void BuildVanBay(Transform root)
    {
        var p = Sub(root, "VanBay");
        // Interior loading bay z ~18..25; the van is the one cared-for object — board here, depart via overlay.
        Box(p, "BayDeck", new Vector3(4.5f, 0.04f, 21.4f), new Vector3(4.6f, 0.08f, 7.6f), mFloor, collider: false);
        Prop(p, GA + "AS_OfficeVan", "DispatchVan", new Vector3(4.5f, 0f, 20.9f), 0f, new Vector3(2.4f, 2.4f, 5.0f));
        // FUNCTIONAL board trigger: a solid (renderer-hidden) collider over the van + OfficeDepartureVan, so
        // PlayerInteraction's aim-SphereCast (range 2.5) can board it. RetireLegacy deactivates the old
        // HQ_OptionA board zone, so the Mars bay MUST supply its own — else you can't depart (无法和车交互/抵达地图).
        var board = Box(p, "Van_BoardZone", new Vector3(4.5f, 1.2f, 20.9f), new Vector3(2.9f, 2.4f, 5.4f), mGreen, collider: true);
        var bmr = board.GetComponent<MeshRenderer>(); if (bmr != null) bmr.enabled = false;
        board.AddComponent<OfficeDepartureVan>();
    }

    // ----------------------------------------------------------------- non-walkable Mars backdrop (through the door)

    static void BuildBackdrop(Transform root)
    {
        var p = Sub(root, "Backdrop_NonWalkable");
        // The dead Mars scrapyard SEEN through the door, z 27.5..40. Dark ground + real Tirgames junk silhouettes.
        Box(p, "Yard_Ground", new Vector3(4.5f, -0.25f, 33f), new Vector3(24f, 0.2f, 14f), mBackdrop, collider: false);
        BoxEuler(p, "Container", new Vector3(0.4f, 1.3f, 31.2f), new Vector3(3.0f, 2.6f, 2.2f), new Vector3(0f, 12f, 0f), mBackdrop);
        Box(p, "RuinLandmark", new Vector3(9.8f, 3.6f, 37f), new Vector3(2.6f, 7.2f, 2.6f), mBackdrop);

        // Real Tirgames debris / barrels / gas tank through the door (the dead-yard junk).
        FitPrefab(p, TIRP + "Debris01_2.prefab", "Yard_Debris_A", new Vector3(2.0f, 0f, 33f), 18f, new Vector3(2.2f, 1.3f, 2.2f));
        FitPrefab(p, TIRP + "Debris01_5.prefab", "Yard_Debris_B", new Vector3(7.2f, 0f, 35f), 110f, new Vector3(2.4f, 1.4f, 2.4f));
        FitPrefab(p, TIRP + "Barrel01b.prefab", "Yard_Barrel_A", new Vector3(-3.0f, 0f, 30.5f), 0f, new Vector3(0.6f, 0.9f, 0.6f));
        FitPrefab(p, TIRP + "Barrel01c.prefab", "Yard_Barrel_B", new Vector3(12.0f, 0f, 34f), 25f, new Vector3(0.6f, 0.9f, 0.6f));
        FitPrefab(p, TIRP + "GasBallone01_2.prefab", "Yard_GasTank", new Vector3(11.4f, 0f, 30f), -30f, new Vector3(0.6f, 1.2f, 0.6f));

        // A dead (unlit) floodlight pole silhouette.
        Box(p, "DeadPole_Shaft", new Vector3(7.6f, 2.6f, 33f), new Vector3(0.12f, 5.2f, 0.12f), mSteel);
        BoxEuler(p, "DeadPole_Head", new Vector3(8.0f, 5.1f, 33f), new Vector3(0.6f, 0.2f, 0.4f), new Vector3(0f, 0f, -20f), mSteel, collider: false);

        // Renderer-less collider across the threshold keeps the player IN (Option B).
        var blocker = Box(p, "ThresholdBlocker", new Vector3(4.5f, 2.6f, 27.7f), new Vector3(9f, 5.2f, 0.3f), mShell);
        var mr = blocker.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false;
    }

    // ----------------------------------------------------------------- mood + interior light pools

    static void ConfigureMood()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Rgb(0x2A, 0x2C, 0x30);     // cool dead Mars sky
        RenderSettings.ambientEquatorColor = Rgb(0x22, 0x24, 0x26);
        RenderSettings.ambientGroundColor = Rgb(0x14, 0x15, 0x16);  // dark so the light pools do the work
        // Stage B3: ambient pressed DOWN + fog pulled IN so the four §8 light pools are islands separated
        // by true dark, and the 13.6 m crest fades into gloom (sells the 1:4.86 nest compression).
        RenderSettings.ambientIntensity = 0.45f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.09f, 0.10f, 0.11f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 11f;
        RenderSettings.fogEndDistance = 50f;
    }

    static void BuildLights(Transform root)
    {
        var p = Sub(root, "Lights");

        // Cold directional KEY with soft shadows — grounds every surface so the concrete/steel materials read.
        var key = new GameObject("Key_ColdSky");
        key.transform.SetParent(p, false);
        key.transform.rotation = Quaternion.Euler(30f, -22f, 0f);            // REDIRECT: lower elevation = GRAZING along the sweep
        var kl = key.AddComponent<Light>();
        kl.type = LightType.Directional; kl.color = Rgb(0x86, 0x90, 0x9E); kl.intensity = 0.62f; kl.shadows = LightShadows.Soft;

        // Visible fluorescent fixtures (real Tirgames tubes) + pools; the hall tube sputters (the broke office).
        FitPrefab(p, TIRP + "LampCeiling01.prefab", "Tube_Nest", new Vector3(2.4f, NestRoofY - 0.12f, 3.2f), 0f, new Vector3(1.2f, 0.2f, 0.4f));
        FitPrefab(p, TIRP + "LampCeiling01.prefab", "Tube_Hall", new Vector3(2.0f, 5.4f, 12.0f), 0f, new Vector3(1.4f, 0.2f, 0.45f));
        var tube = AddLight(p, "Light_Fluorescent_Hall", new Vector3(2.0f, 5.2f, 12.0f), ColdIndustrial, 0.5f, 7f);
        tube.AddComponent<LightFlicker>().Configure(LightFlicker.Character.Sputter, 0.7f, 7f);

        // §8 interior pools — separated by required dark.
        AddLight(p, "Nest_Tungsten", new Vector3(1.8f, 2.5f, 3.0f), WarmTungsten, 1.4f, 6.8f);   // home warmth
        AddLight(p, "CRT_Phosphor", new Vector3(0.9f, 1.2f, 1.3f), CrtGreen, 0.9f, 2.6f);          // the live signal
        AddLight(p, "Crack_Daylight", new Vector3(6.0f, 12.2f, 24.6f), ColdDaylight, 2.8f, 22f,     // REDIRECT: from the north tear, raking SW onto the muster
            LightType.Spot, new Vector3(64f, -28f, 0f), 58f);
        AddLight(p, "Prow_Rim", new Vector3(7.0f, 9.0f, 30.0f), Rgb(0x9A, 0xA8, 0xBC), 1.4f, 14f,   // REDIRECT: 2nd cold shaft rims the broken prow
            LightType.Spot, new Vector3(40f, -150f, 0f), 60f);
        AddLight(p, "Bay_Sodium", new Vector3(6.4f, 3.6f, 21.0f), SodiumAmber, 1.7f, 8f);           // the van pool
        AddLight(p, "Door_Backglow", new Vector3(4.5f, 3.0f, 31f), Rgb(0x6E, 0x78, 0x82), 0.8f, 14f); // frames the door
    }

    static void BuildPost(Transform root)
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium1); grain.intensity.Override(0.18f); grain.response.Override(0.7f);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.15f); bloom.intensity.Override(0.42f); bloom.scatter.Override(0.45f);
        var color = profile.Add<ColorAdjustments>(true);
        color.saturation.Override(-12f); color.contrast.Override(8f);

        // Stage B4: vignette pulls the eye centre-frame; Municipal-Debt-Noir split tone (teal shadows /
        // sodium-amber highlights); a whisper of CA = the office's cheap dirty lens.
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.28f); vig.smoothness.Override(0.42f);
        var split = profile.Add<SplitToning>(true);
        split.shadows.Override(Rgb(0x2E, 0x4A, 0x4E)); split.highlights.Override(Rgb(0xC8, 0x9A, 0x50));
        split.balance.Override(-15f);
        var ca = profile.Add<ChromaticAberration>(true);
        ca.intensity.Override(0.06f);

        var go = new GameObject("MarsPostVolume");
        go.transform.SetParent(root, false);
        var v = go.AddComponent<Volume>();
        v.isGlobal = true; v.priority = 10f; v.profile = profile;
    }

    // ----------------------------------------------------------------- anchors / retire / helpers

    static void RepositionAnchors()
    {
        var spawn = GameObject.Find("PlayerSpawnPoint");
        if (spawn != null) spawn.transform.SetPositionAndRotation(new Vector3(4.5f, 0.1f, 2.4f), Quaternion.identity); // in the nest, facing +Z down the spine to the framed door
        var computer = GameObject.Find("MVP_OfficeComputer");
        if (computer != null) computer.transform.SetPositionAndRotation(new Vector3(1.2f, 1.0f, 1.2f), Quaternion.Euler(0f, 90f, 0f));
    }

    static int RetireLegacy()
    {
        int n = 0;
        var optA = GameObject.Find("HQ_OptionA");
        if (optA != null && optA.activeSelf) { optA.SetActive(false); n++; }
        foreach (GameObject go in AllObjects())
        {
            if (!go.activeSelf || Keep.Contains(go.name)) continue;
            // Include Terrain: the Option-A Earth-wilderness Terrain renders trees + grass INTERNALLY (no
            // child GameObjects), so a Renderer/Collider-only sweep misses it → the Earth treeline stayed
            // outside the pure-interior Mars shell. The Mars freight HQ has no outdoor trees by design.
            if (go.GetComponent<Renderer>() != null || go.GetComponent<Collider>() != null
                || go.GetComponent<Terrain>() != null) { go.SetActive(false); n++; }
        }
        // Belt + suspenders: the wilderness vegetation root (dead treeline / meadow scrub) — Mars HQ has none.
        var veg = GameObject.Find("Vegetation");
        if (veg != null && veg.activeSelf) { veg.SetActive(false); n++; }
        var van = GameObject.Find("MVP_RuntimeStyle_Office_ExteriorDispatch");
        if (van != null) foreach (var r in van.GetComponents<Renderer>()) r.enabled = false;
        return n;
    }

    static IEnumerable<GameObject> AllObjects()
    {
        foreach (var rootGo in SceneManager.GetActiveScene().GetRootGameObjects())
            foreach (var t in rootGo.GetComponentsInChildren<Transform>(true))
                yield return t.gameObject;
    }

    // ----------------------------------------------------------------- Stage A: baked shell panel textures

    const string TexDir = "Assets/_Project/Art/Textures";
    const string ShellAlbedoPath = TexDir + "/MarsShellPanels.png";
    const string ShellNormalPath = TexDir + "/MarsShellPanels_N.png";

    /// <summary>Bakes (idempotent, seeded) and applies the panel-seam albedo + normal onto the shell
    /// material. UV is ~1 tile / 4.5 m (BuildShellMesh), so a 2×2-panel sheet ≈ 2.3 m panels.</summary>
    static void ApplyShellSkinTextures(Material m)
    {
        BakeShellTextures();
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(ShellAlbedoPath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(ShellNormalPath);
        if (albedo != null)
        {
            m.SetTexture("_BaseMap", albedo);
            m.SetTextureScale("_BaseMap", Vector2.one);
            m.SetTextureOffset("_BaseMap", Vector2.zero);
        }
        if (normal != null)
        {
            m.SetTexture("_BumpMap", normal);
            m.SetFloat("_BumpScale", 1.0f);
            m.EnableKeyword("_NORMALMAP");
        }
    }

    static void BakeShellTextures()
    {
        const int S = 512;
        const int panels = 2;              // 2×2 panels per tile
        const int seamPx = 4;              // seam half-width in pixels
        int panelPx = S / panels;
        var rng = new System.Random(20260701);

        // Per-panel plate value (T1 sound bright / T2 weathered dark, baked variance).
        float[,] plate = new float[panels, panels];
        for (int py = 0; py < panels; py++)
            for (int px = 0; px < panels; px++)
                plate[px, py] = 0.78f + 0.24f * (float)rng.NextDouble();

        var albedoPx = new Color32[S * S];
        var height = new float[S, S];
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                int px = x / panelPx, py = y / panelPx;
                int lx = x % panelPx, ly = y % panelPx;
                int edge = Mathf.Min(Mathf.Min(lx, panelPx - 1 - lx), Mathf.Min(ly, panelPx - 1 - ly));

                float v = plate[px, py];
                // Gravity streaks: vertical dark runs, strongest just under the top seam of each panel.
                float streak = Fbm(x * 0.055f, py * 17.3f) * Mathf.Pow(1f - (float)ly / panelPx, 1.6f);
                v -= 0.16f * streak;
                // Mars dust drift settles pale-warm on the upper band of each panel.
                float dust = Fbm(x * 0.02f, y * 0.02f + px * 9.1f) * Mathf.Pow(1f - (float)ly / panelPx, 3f);
                // Fine surface noise so flat light never reads as paint.
                v += 0.045f * (Fbm(x * 0.11f, y * 0.11f) - 0.5f);

                float h = 1f;
                if (edge < seamPx) { v *= 0.42f + 0.13f * edge; h = 0.15f + 0.2f * edge; } // seam groove
                // Corner bolts (inset ~10 px): a dark ring + a height bump.
                float bx = lx < panelPx / 2 ? 10f : panelPx - 11f;
                float by = ly < panelPx / 2 ? 10f : panelPx - 11f;
                float bd = Mathf.Sqrt((lx - bx) * (lx - bx) + (ly - by) * (ly - by));
                if (bd < 3.5f) { v *= 0.55f; h = 1.25f; }

                v = Mathf.Clamp01(v);
                // Cool near-neutral cast — the material tint supplies the Mars blue; dust warms the top band.
                var c = new Color(v * 0.94f, v * 0.965f, v * 1.0f);
                c = Color.Lerp(c, new Color(0.72f, 0.62f, 0.55f), 0.16f * dust);
                albedoPx[y * S + x] = c;
                height[x, y] = h;
            }
        }

        var normalPx = new Color32[S * S];
        const float strength = 2.2f;
        for (int y = 0; y < S; y++)
        {
            for (int x = 0; x < S; x++)
            {
                float dx = height[(x + 1) % S, y] - height[(x - 1 + S) % S, y];
                float dy = height[x, (y + 1) % S] - height[x, (y - 1 + S) % S];
                var n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                normalPx[y * S + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f);
            }
        }

        System.IO.Directory.CreateDirectory(TexDir);
        WriteTexture(ShellAlbedoPath, S, albedoPx, normalMap: false);
        WriteTexture(ShellNormalPath, S, normalPx, normalMap: true);
    }

    static void WriteTexture(string path, int size, Color32[] pixels, bool normalMap)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter imp)
        {
            bool dirty = imp.wrapMode != TextureWrapMode.Repeat;
            imp.wrapMode = TextureWrapMode.Repeat;
            if (normalMap && imp.textureType != TextureImporterType.NormalMap)
            { imp.textureType = TextureImporterType.NormalMap; dirty = true; }
            if (dirty) imp.SaveAndReimport();
        }
    }

    // Cheap 2-octave value noise (deterministic, tileable enough at these frequencies).
    static float Fbm(float x, float y)
    {
        return 0.65f * ValueNoise(x, y) + 0.35f * ValueNoise(x * 2.7f + 11.3f, y * 2.7f + 7.9f);
    }

    static float ValueNoise(float x, float y)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float xf = x - xi, yf = y - yi;
        float a = Hash01(xi, yi), b = Hash01(xi + 1, yi), c = Hash01(xi, yi + 1), d = Hash01(xi + 1, yi + 1);
        float u = xf * xf * (3f - 2f * xf), v = yf * yf * (3f - 2f * yf);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    static float Hash01(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFFFF) / 16777215f;
        }
    }

    // ----------------------------------------------------------------- Stage B: atmosphere (shafts / dust / emissive)

    /// <summary>Stage B (PM 2026-06-24 "光效/氛围感要再做一轮"): visible dusty light shafts under the crack
    /// and door, drifting dust in the light pools, and the emissive "broke office life" (CRT phosphor
    /// glass, nest door strip) that the bloom pass picks out of the dark.</summary>
    static void BuildAtmosphere(Transform root)
    {
        var p = Sub(root, "Atmosphere");

        // B1 — god shafts (cheap crossed additive trapezoids; the LC derelict-chapel read).
        LightShaft(p, "Shaft_Crack", new Vector3(6.0f, 12.0f, 24.4f), new Vector3(4.5f, 0.05f, 15.6f),
            1.3f, 4.2f, ColdDaylight, 0.10f);
        LightShaft(p, "Shaft_Door", new Vector3(4.5f, DoorH - 0.2f, 26.8f), new Vector3(4.5f, 0.05f, 23.6f),
            DoorW * 0.9f, DoorW * 1.25f, Rgb(0x8E, 0x98, 0xA6), 0.06f);
        LightShaft(p, "Shaft_Prow", new Vector3(7.0f, 8.8f, 29.6f), new Vector3(8.6f, 0.05f, 26.4f),
            0.9f, 2.6f, Rgb(0x9A, 0xA8, 0xBC), 0.07f);

        // B2 — dust motes drifting inside the two big pools ("被遗弃的大空间" for pennies).
        Dust(p, "Dust_Crack", new Vector3(5.2f, 5.5f, 20f), new Vector3(4f, 10f, 9f), 14f);
        Dust(p, "Dust_Bay", new Vector3(5.4f, 2.0f, 21f), new Vector3(4f, 3.5f, 7f), 8f);

        // B5 — emissive life: the CRT's phosphor glass (in front of the terminal anchor, facing +X into
        // the nest) and a dim amber strip over the nest door — the only lit "signs" in the dead shell.
        EmissiveQuad(p, "CRT_Glass", new Vector3(1.34f, 1.05f, 1.2f), new Vector3(0.02f, 0.30f, 0.42f),
            CrtGreen, 2.2f);
        EmissiveQuad(p, "NestDoor_Strip", new Vector3(4.9f, 2.62f, 6.6f), new Vector3(1.5f, 0.07f, 0.05f),
            SodiumAmber, 1.6f);
    }

    /// <summary>Two crossed additive trapezoid quads from a bright top edge to a wide faded floor end.</summary>
    static void LightShaft(Transform parent, string name, Vector3 top, Vector3 bottom, float topW, float bottomW,
        Color c, float alpha)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = top;
        Vector3 axis = bottom - top;
        float len = axis.magnitude;
        go.transform.rotation = Quaternion.FromToRotation(Vector3.down, axis.normalized);

        var verts = new List<Vector3>();
        var uvs = new List<Vector2>();
        var tris = new List<int>();
        for (int q = 0; q < 2; q++)
        {
            Vector3 w = q == 0 ? Vector3.right : Vector3.forward;
            int i0 = verts.Count;
            verts.Add(-w * topW * 0.5f); uvs.Add(new Vector2(0f, 1f));
            verts.Add(w * topW * 0.5f); uvs.Add(new Vector2(1f, 1f));
            verts.Add(Vector3.down * len - w * bottomW * 0.5f); uvs.Add(new Vector2(0f, 0f));
            verts.Add(Vector3.down * len + w * bottomW * 0.5f); uvs.Add(new Vector2(1f, 0f));
            tris.AddRange(new[] { i0, i0 + 1, i0 + 2, i0 + 1, i0 + 3, i0 + 2 });
        }
        var mesh = new Mesh { name = name };
        mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
        mesh.RecalculateBounds();

        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = ShaftMaterial(c, alpha);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    static Material ShaftMaterial(Color c, float alpha)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetFloat("_Surface", 1f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);   // additive: light adds, never darkens
        m.SetInt("_ZWrite", 0);
        m.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        m.SetTexture("_BaseMap", ShaftGradientTexture());
        m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, alpha));
        return m;
    }

    static Texture2D shaftGradientTex;
    static Texture2D ShaftGradientTexture()
    {
        if (shaftGradientTex != null) return shaftGradientTex;
        const int S = 64;
        shaftGradientTex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                float a = Mathf.Sin(u * Mathf.PI);              // soft side edges
                a *= Mathf.Pow(v, 1.4f);                        // bright at the source (v=1), fading to the floor
                shaftGradientTex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        shaftGradientTex.Apply();
        return shaftGradientTex;
    }

    static void Dust(Transform parent, string name, Vector3 center, Vector3 box, float rate)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.05f);
        main.startColor = new Color(0.9f, 0.92f, 1f, 0.10f);
        main.maxParticles = 250;
        main.prewarm = true;
        main.loop = true;

        var emission = ps.emission;
        emission.rateOverTime = rate;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = box;
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.06f;
        noise.frequency = 0.12f;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        var m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        m.SetFloat("_Surface", 1f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        m.SetTexture("_BaseMap", DustDotTexture());
        m.SetColor("_BaseColor", Color.white);
        rend.sharedMaterial = m;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Texture2D dustDotTex;
    static Texture2D DustDotTexture()
    {
        if (dustDotTex != null) return dustDotTex;
        const int S = 32;
        dustDotTex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(S / 2f - 0.5f, S / 2f - 0.5f)) / (S / 2f);
                dustDotTex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
            }
        dustDotTex.Apply();
        return dustDotTex;
    }

    static void EmissiveQuad(Transform parent, string name, Vector3 pos, Vector3 size, Color c, float intensity)
    {
        var go = Box(parent, name, pos, size, null, collider: false);
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", Color.black);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", c * intensity);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
    }

    static void BuildPalette()
    {
        // Dead Mars shell. REDIRECT (2026-06-24): push it COLD PALE BLUE-GREY, deliberately OUTSIDE the Earth
        // concrete/green/wood/rust palette (Mars = alien) — the old near-white tint let the warm concrete bleed
        // through and read "gray-brown blob". A strong blue-biased, lower-value multiplier overpowers the warm albedo.
        mShell = Tint(TIR + "CommonConcreteWall02.mat", new Color(0.60f, 0.69f, 0.86f), 0.07f, doubleSided: true);
        // Stage A (PM 2026-06-24 "材质还差点意思"): swap the concrete tiling for a baked cold PANEL-SEAM sheet
        // (per-panel value variance + seam grooves via normal map) so the shell reads ASSEMBLED, not poured.
        ApplyShellSkinTextures(mShell);
        mShellDark = Flat(new Color(0.17f, 0.20f, 0.27f));                   // T3 exposed ribs / missing-panel recess / torn edges (dark blue-steel)
        mFloor = M(TIR + "CommonConcrete04.mat", new Color(0.16f, 0.16f, 0.18f));
        mSteel = Tint(TIR + "CommonMetal01.mat", new Color(0.50f, 0.56f, 0.64f), 0.22f);
        mGrime = Flat(new Color(0.09f, 0.10f, 0.12f));                       // cold contamination streaks on the shell
        // Warm nest cladding: cheap painted metal, warmed — but the WARMTH is the tungsten light (§8), not the paint.
        mNest = Tint(TIR + "CommonMetalPainted01.mat", new Color(1.0f, 0.92f, 0.80f), 0.10f);
        mBackdrop = Tint(TIR + "CommonConcrete05.mat", new Color(0.50f, 0.52f, 0.55f), 0.05f); // dead-yard silhouettes
        mCrack = Flat(new Color(0.80f, 0.86f, 0.82f));                       // cold daylight showing through the crack
        mGreen = Flat(new Color(0.42f, 0.85f, 0.40f));                       // van board zone
        mRed = Flat(new Color(0.66f, 0.18f, 0.16f));                         // padlock
        mAmber = Flat(new Color(0.78f, 0.62f, 0.30f));                       // muster / spawn floor markings
    }

    // Load a shared kit material; flat fallback if missing.
    static Material M(string path, Color fallback)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (asset != null) return asset;
        Debug.LogWarning($"[HQ Mars] Material not found, flat fallback: {path}");
        return Flat(fallback);
    }

    // Load a kit material into a fresh INSTANCE and re-tint it (the source .mat is never modified). doubleSided sets
    // URP Render Face = Both so a single-surface swept skin never reads hollow.
    static Material Tint(string path, Color mul, float smoothness, bool doubleSided = false)
    {
        var src = AssetDatabase.LoadAssetAtPath<Material>(path);
        Material m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (src == null) Debug.LogWarning($"[HQ Mars] Material missing, flat fallback: {path}");
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", mul);
        if (m.HasProperty("_Color")) m.SetColor("_Color", mul);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
        if (doubleSided && m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
        m.name = (src != null ? src.name : "Flat") + "_HQ";
        return m;
    }

    static Material Flat(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.12f);
        return m;
    }

    /// <summary>Instantiate an authored AS_Office prefab (import-normalized, real scale) at local pos + yaw; imported
    /// colliders disabled (shell boxes carry collision). Missing prefab -> labelled fallback box.</summary>
    static void Prop(Transform parent, string resPath, string name, Vector3 localPos, float yaw, Vector3 fallbackSize)
    {
        var prefab = Resources.Load<GameObject>(resPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[HQ Mars] Prop missing: {resPath} — fallback box '{name}'.");
            var box = Box(parent, name + " (FALLBACK)", localPos + Vector3.up * fallbackSize.y * 0.5f, fallbackSize, Flat(new Color(0.30f, 0.28f, 0.24f)), collider: false);
            box.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return;
        }
        var go = (GameObject)Object.Instantiate(prefab, parent);
        go.name = name;
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one;
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = false;
    }

    /// <summary>Instantiate a Tirgames prefab and AUTO-FIT it to a target box by measured renderer bounds, grounded at
    /// y=0 and centred on (pos.x,pos.z) with yaw. Returns false (no-op) if the prefab is missing so the caller can
    /// fall back. uniformByHeight scales uniformly to the target height.</summary>
    static bool FitPrefab(Transform parent, string assetPath, string name, Vector3 pos, float yaw, Vector3 target, bool uniformByHeight = false)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) { Debug.LogWarning($"[HQ Mars] Tirgames prefab missing: {assetPath} — fallback for '{name}'."); return false; }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.transform.localPosition = Vector3.zero;

        Bounds b = MeshBounds(go);
        if (b.size == Vector3.zero) { go.transform.localPosition = pos; return true; }

        Vector3 s = b.size;
        Vector3 scale = uniformByHeight
            ? Vector3.one * (target.y / Mathf.Max(1e-4f, s.y))
            : new Vector3(target.x / Mathf.Max(1e-4f, s.x), target.y / Mathf.Max(1e-4f, s.y), target.z / Mathf.Max(1e-4f, s.z));
        go.transform.localScale = scale;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        b = MeshBounds(go);
        Vector3 worldOffset = new(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
        go.transform.position += worldOffset;
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = false;
        return true;
    }

    static Bounds MeshBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static Transform Sub(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (!collider) { var c = go.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c); }
        return go;
    }

    static GameObject BoxEuler(Transform parent, string name, Vector3 pos, Vector3 size, Vector3 euler, Material mat, bool collider = true)
    {
        var go = Box(parent, name, pos, size, mat, collider);
        go.transform.localRotation = Quaternion.Euler(euler);
        return go;
    }

    static GameObject AddLight(Transform parent, string name, Vector3 pos, Color c, float intensity, float range,
        LightType type = LightType.Point, Vector3? euler = null, float spotAngle = 60f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
        var l = go.AddComponent<Light>();
        l.type = type;
        l.color = c;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        if (type == LightType.Spot) l.spotAngle = spotAngle;
        return go;
    }

    static float Sample(Vector2[] pts, float z)
    {
        if (z <= pts[0].x) return pts[0].y;
        for (int i = 1; i < pts.Length; i++)
            if (z <= pts[i].x)
            {
                Vector2 a = pts[i - 1], b = pts[i];
                return Mathf.Lerp(a.y, b.y, (z - a.x) / (b.x - a.x));
            }
        return pts[pts.Length - 1].y;
    }

    static Color Rgb(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f);
}
