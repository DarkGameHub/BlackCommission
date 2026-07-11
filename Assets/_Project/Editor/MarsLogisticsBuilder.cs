using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BlackCommission.Level;

/// <summary>
/// Mission map 3 — the Mars logistics hub (David-approved theme, 2026-07-02; design:
/// <c>design/levels/mars-logistics-01.md</c>). A seized freight transfer station on the
/// Martian surface: dust-storm exterior with a small drop-off pad, then a sequence of
/// receiving, processing, office, quarantine and sealed-storage rooms. Low connector
/// corridors and offset doors prevent long through-sightlines. Reuses the StageA
/// Mars panel-seam textures (<c>Art/Textures/MarsShellPanels{,_N}.png</c>).
///
/// Builds static geometry + LootAnchors + MonsterSeeds + the van exit point into the OPEN
/// scene. Run <c>MissionMapFinalizer</c> afterwards for the van visual + persisted NavMesh
/// + save. Idempotent: re-running replaces the previous build root.
///
/// Menu: <c>Tools ▸ Black Commission ▸ Map ▸ Build Mars Logistics (v3 Rooms)</c>.
/// </summary>
public static class MarsLogisticsBuilder
{
    const string RootName = "MarsLogistics_v3_Rooms";
    const string ScenePath = "Assets/_Project/Scenes/Mars_Logistics_01.unity";

    // ── palette (Municipal Debt Noir on Mars: butterscotch storm, sodium amber, seizure red) ──
    static readonly Color Regolith = new Color(0.30f, 0.17f, 0.11f);
    static readonly Color StormSky = new Color(0.42f, 0.30f, 0.19f);
    static readonly Color FogColor = new Color(0.45f, 0.32f, 0.20f);
    static readonly Color PanelTint = new Color(0.72f, 0.62f, 0.55f);
    static readonly Color FloorGray = new Color(0.26f, 0.25f, 0.24f);
    static readonly Color RackSteel = new Color(0.16f, 0.17f, 0.19f);
    static readonly Color ShelfWood = new Color(0.32f, 0.26f, 0.18f);
    static readonly Color CrateOlive = new Color(0.28f, 0.30f, 0.22f);
    static readonly Color CrateBrown = new Color(0.34f, 0.27f, 0.19f);
    static readonly Color ColdWall = new Color(0.42f, 0.48f, 0.50f);
    static readonly Color Paper = new Color(0.78f, 0.72f, 0.58f);
    static readonly Color StampRed = new Color(0.62f, 0.12f, 0.10f);
    static readonly Color SodiumAmber = new Color(1.0f, 0.62f, 0.25f);
    static readonly Color ColdCyan = new Color(0.65f, 0.85f, 0.85f);

    static readonly Dictionary<Color, Material> MatCache = new Dictionary<Color, Material>();

    [MenuItem("Tools/Black Commission/Map/Build Mars Logistics (v3 Rooms)")]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[MarsLogistics] Refusing to build in Play mode.");
            return;
        }
        MatCache.Clear();

        foreach (string legacyRoot in new[] { "MarsLogistics_v1", "MarsLogistics_v2", RootName })
        {
            var old = GameObject.Find(legacyRoot);
            if (old != null) Object.DestroyImmediate(old);
        }

        var root = new GameObject(RootName).transform;

        BuildExterior(Child(root, "Exterior"));
        BuildCompartmentRoute(Child(root, "CompartmentRoute"));
        BuildCompartmentLights(Child(root, "Lights"));
        BuildMissionAnchors(Child(root, "Mission"));
        ApplyAtmosphere();

        // The room chain uses a lower count than the former open warehouse so each find reads.
        var profile = new GameObject("ScavengeMapProfile").AddComponent<ScavengeMapProfile>();
        profile.transform.SetParent(root, false);
        profile.itemsMin = 18;
        profile.itemsMax = 24;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[MarsLogistics] v3 room route built. Run MissionMapFinalizer to bake navmesh + van, then save.");
    }

    [MenuItem("Tools/Black Commission/Map/Rebuild Mars Logistics Scene (v3 Rooms) %#9")]
    public static void RebuildSceneV3()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[MarsLogistics] Exit Play mode before rebuilding the saved scene.");
            return;
        }

        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        Build();
        MissionMapFinalizer.FinalizeActiveScene();
        Selection.activeGameObject = GameObject.Find(RootName);
        if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.FrameSelected();
    }

    // ── exterior: regolith apron, drop pad, storm backdrop ─────────────────────────
    /// <summary>Room-by-room route with offset doors and low connector corridors.</summary>
    static void BuildCompartmentRoute(Transform p)
    {
        Material wall = PanelMat();
        Material floor = Mat(FloorGray);
        Material roof = Mat(new Color(0.16f, 0.17f, 0.18f));
        Material threshold = Mat(new Color(0.50f, 0.38f, 0.12f));

        RoomShell(p, "Airlock", 17f, 25f, 0f, 6f, 3.2f, 21f, 22f, 2f, wall, floor, roof);
        RoomShell(p, "Receiving", 14f, 28f, 6f, 14f, 4.5f, 22f, 17f, 2f, wall, floor, roof);
        CorridorShell(p, "CorridorA", 15f, 19f, 14f, 20f, wall, floor, roof);
        RoomShell(p, "Sorting", 15f, 29f, 20f, 28f, 4.8f, 17f, 28f, 2f, wall, floor, roof);
        CorridorShell(p, "CorridorB", 27f, 31f, 28f, 34f, wall, floor, roof);
        RoomShell(p, "FreightOffice", 19f, 31f, 34f, 42f, 3.2f, 29f, 20f, 2f, wall, floor, roof);
        CorridorShell(p, "CorridorC", 18f, 22f, 42f, 48f, wall, floor, roof);
        RoomShell(p, "Quarantine", 17f, 31f, 48f, 56f, 4f, 20f, 30f, 2f, wall, floor, roof);
        CorridorShell(p, "CorridorD", 28f, 32f, 56f, 62f, wall, floor, roof);
        RoomShell(p, "SealedBay", 15f, 33f, 62f, 72f, 5.5f, 30f, float.NaN, 2.8f, wall, floor, roof);

        foreach (Vector3 t in new[]
        {
            new Vector3(22f, .07f, 6f), new Vector3(17f, .07f, 14f),
            new Vector3(17f, .07f, 20f), new Vector3(28f, .07f, 28f),
            new Vector3(29f, .07f, 34f), new Vector3(20f, .07f, 42f),
            new Vector3(20f, .07f, 48f), new Vector3(30f, .07f, 56f),
            new Vector3(30f, .07f, 62f)
        }) Box(p, "Threshold", t, new Vector3(2f, .03f, .35f), threshold, collider: false);

        Box(p, "ReceivingCounter", new Vector3(25.5f, .55f, 9f), new Vector3(3.2f, 1.1f, .8f), Mat(ShelfWood));
        Loot(p, "LA_entry_counter", new Vector3(25.5f, 1.15f, 9f), DressingSurface.DeskSurface);
        Crate(p, "EntryCrate", new Vector3(16f, .5f, 11.5f), 1f, CrateBrown, 8f);
        Loot(p, "LA_entry_crate", new Vector3(16f, 1.05f, 11.5f), DressingSurface.CrateTop);
        TapeX(p, new Vector3(24.7f, 1.45f, 5.82f));

        RackRun(p, 17f, 21f, 23f);
        RackRun(p, 23f, 27f, 26f);
        Loot(p, "LA_sort_0", new Vector3(18f, .78f, 23f), DressingSurface.ShelfSlot);
        Loot(p, "LA_sort_1", new Vector3(20f, 1.78f, 23f), DressingSurface.ShelfSlot);
        Loot(p, "LA_sort_2", new Vector3(24f, .78f, 26f), DressingSurface.ShelfSlot);
        Loot(p, "LA_sort_3", new Vector3(26f, 1.78f, 26f), DressingSurface.ShelfSlot);
        Seed(p, "MonsterSeed_ML_MID", new Vector3(26f, .05f, 22f));

        Desk(p, "FreightDesk_A", new Vector3(22f, .08f, 37f), 0f);
        Desk(p, "FreightDesk_B", new Vector3(28f, .08f, 39.5f), 180f);
        Loot(p, "LA_office_0", new Vector3(22f, .9f, 37f), DressingSurface.DeskSurface);
        Loot(p, "LA_office_1", new Vector3(28f, .9f, 39.5f), DressingSurface.DeskSurface);
        Box(p, "ManifestCabinet", new Vector3(20f, .8f, 40.5f), new Vector3(.7f, 1.6f, .6f), Mat(RackSteel));
        Loot(p, "LA_office_2", new Vector3(20f, 1.65f, 40.5f), DressingSurface.Cabinet);

        foreach (float x in new[] { 19f, 23f, 27f })
        {
            Crate(p, $"Quarantine_{x:0}", new Vector3(x, .65f, 53.5f), 1.3f, CrateOlive, x * 3f);
            Loot(p, $"LA_quarantine_{x:0}", new Vector3(x, 1.35f, 53.5f), DressingSurface.CrateTop);
        }
        TapeX(p, new Vector3(29f, 1.45f, 55.82f));
        Seed(p, "MonsterSeed_ML_QUARANTINE_WARDEN", new Vector3(28f, .05f, 50f));

        Box(p, "SealedContainer", new Vector3(24f, 1.35f, 68f), new Vector3(6.2f, 2.7f, 2.8f),
            Mat(new Color(0.22f, 0.27f, 0.25f)));
        TapeX(p, new Vector3(24f, 1.4f, 66.55f));
        foreach (Vector3 q in new[]
        {
            new Vector3(17f, .6f, 64f), new Vector3(31f, .6f, 70f), new Vector3(18f, .6f, 70f)
        })
        {
            Crate(p, "DeepCrate", q, 1.2f, CrateBrown, q.x * 9f);
            Loot(p, "LA_deep", q + Vector3.up * .65f, DressingSurface.CrateTop);
        }
        Loot(p, "LA_deep_floor_0", new Vector3(20f, .12f, 66f), DressingSurface.Floor);
        Loot(p, "LA_deep_floor_1", new Vector3(29f, .12f, 65f), DressingSurface.Floor);
        Seed(p, "MonsterSeed_ML_DEEP_IDOL", new Vector3(24f, .05f, 70f));
    }

    static void RoomShell(Transform p, string name, float x0, float x1, float z0, float z1,
        float height, float southDoorX, float northDoorX, float doorWidth,
        Material wall, Material floor, Material roof)
    {
        float cx = (x0 + x1) * .5f;
        float cz = (z0 + z1) * .5f;
        Box(p, name + "_Floor", new Vector3(cx, -.08f, cz), new Vector3(x1 - x0, .25f, z1 - z0), floor);
        Box(p, name + "_Roof", new Vector3(cx, height + .05f, cz),
            new Vector3(x1 - x0 + .3f, .2f, z1 - z0 + .3f), roof);
        DoorWallX(p, wall, name + "_South", x0, x1, z0, height, southDoorX, doorWidth);
        DoorWallX(p, wall, name + "_North", x0, x1, z1, height, northDoorX, doorWidth);
        WallZ(p, wall, name + "_West", z0, z1, x0, 0f, height);
        WallZ(p, wall, name + "_East", z0, z1, x1, 0f, height);
    }

    static void CorridorShell(Transform p, string name, float x0, float x1, float z0, float z1,
        Material wall, Material floor, Material roof)
    {
        float cx = (x0 + x1) * .5f;
        float cz = (z0 + z1) * .5f;
        Box(p, name + "_Floor", new Vector3(cx, -.08f, cz), new Vector3(x1 - x0, .25f, z1 - z0), floor);
        Box(p, name + "_Roof", new Vector3(cx, 2.45f, cz), new Vector3(x1 - x0 + .3f, .2f, z1 - z0 + .3f), roof);
        WallZ(p, wall, name + "_West", z0, z1, x0, 0f, 2.4f);
        WallZ(p, wall, name + "_East", z0, z1, x1, 0f, 2.4f);
        Box(p, name + "_CableTray", new Vector3(x0 + .35f, 2.15f, cz),
            new Vector3(.25f, .18f, z1 - z0), Mat(RackSteel), collider: false);
    }

    static void DoorWallX(Transform p, Material wall, string name, float x0, float x1,
        float z, float height, float doorCenter, float doorWidth)
    {
        if (float.IsNaN(doorCenter))
        {
            WallX(p, wall, name, x0, x1, z, 0f, height);
            return;
        }
        float d0 = Mathf.Max(x0, doorCenter - doorWidth * .5f);
        float d1 = Mathf.Min(x1, doorCenter + doorWidth * .5f);
        if (d0 > x0) WallX(p, wall, name + "_L", x0, d0, z, 0f, height);
        if (d1 < x1) WallX(p, wall, name + "_R", d1, x1, z, 0f, height);
        WallX(p, wall, name + "_Lintel", d0, d1, z, 2.4f, height);
    }

    static void BuildCompartmentLights(Transform p)
    {
        var sun = new GameObject("StormSun");
        sun.transform.SetParent(p, false);
        sun.transform.rotation = Quaternion.Euler(38f, 155f, 0f);
        var dl = sun.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = new Color(1f, .62f, .35f);
        dl.intensity = .45f;
        dl.shadows = LightShadows.Soft;
        Point(p, "PadBeacon", new Vector3(21f, 3.2f, -8f), SodiumAmber, 2.4f, 11f);
        Point(p, "AirlockUtility", new Vector3(21f, 2.7f, 3f), new Color(.9f, .85f, .7f), 2.4f, 7f);
        Point(p, "ReceivingAmber", new Vector3(24f, 3.8f, 10f), SodiumAmber, 3.2f, 9f);
        Point(p, "SortWorklight", new Vector3(20f, 4f, 24f), new Color(.78f, .82f, .74f), 3f, 9f);
        Point(p, "OfficeCrt", new Vector3(27f, 2.5f, 38f), new Color(.32f, .9f, .48f), 2f, 7f);
        Point(p, "QuarantineCold", new Vector3(23f, 3.2f, 52f), ColdCyan, 3f, 9f);
        Point(p, "DeepSealRed", new Vector3(24f, 4.2f, 68f), StampRed, 3.6f, 10f);
    }

    static void BuildExterior(Transform p)
    {
        // regolith in four pieces, leaving a shaft slot (x−9..0.5, z7.8..10.7) so the
        // sublevel descent tube doesn't run through solid collider (navmesh dies inside it)
        Material rego = Mat(Regolith);
        Box(p, "Regolith_W", new Vector3(-24f, -0.55f, 10f), new Vector3(30f, 1.1f, 120f), rego);
        Box(p, "Regolith_E", new Vector3(40.75f, -0.55f, 10f), new Vector3(80.5f, 1.1f, 120f), rego);
        Box(p, "Regolith_ShaftN", new Vector3(-4.25f, -0.55f, 40.35f), new Vector3(9.5f, 1.1f, 59.3f), rego);
        Box(p, "Regolith_ShaftS", new Vector3(-4.25f, -0.55f, -21.1f), new Vector3(9.5f, 1.1f, 57.8f), rego);
        Box(p, "DropPad", new Vector3(21f, 0.03f, -8f), new Vector3(10f, 0.06f, 8f), Mat(FloorGray));
        // faded hazard stripes on the pad (decor)
        for (int i = -2; i <= 2; i++)
        {
            var s = Box(p, $"PadStripe_{i}", new Vector3(21f + i * 1.7f, 0.07f, -8f),
                new Vector3(0.3f, 0.02f, 7.4f), Mat(new Color(0.5f, 0.42f, 0.12f)), collider: false);
            s.transform.localRotation = Quaternion.Euler(0f, 24f, 0f);
        }

        // distant seized-facility silhouettes, swallowed by the storm fog
        Material sil = Mat(new Color(0.24f, 0.17f, 0.12f));
        float[][] gantries = { new[] { -18f, 48f }, new[] { 8f, 55f }, new[] { 48f, 52f }, new[] { 66f, 30f }, new[] { -22f, 6f } };
        foreach (var g in gantries)
        {
            float gx = g[0], gz = g[1];
            Box(p, $"GantryLegA_{gx:0}", new Vector3(gx - 3f, 9f, gz), new Vector3(1.2f, 18f, 1.2f), sil, collider: false);
            Box(p, $"GantryLegB_{gx:0}", new Vector3(gx + 3f, 9f, gz), new Vector3(1.2f, 18f, 1.2f), sil, collider: false);
            Box(p, $"GantryBeam_{gx:0}", new Vector3(gx, 17f, gz), new Vector3(9f, 1.6f, 1.6f), sil, collider: false);
        }

        // enclosing storm walls + lid: guarantee every sightline ends in butterscotch, whatever
        // the player camera's clear flags are (no skybox dependency)
        Material storm = Mat(StormSky);
        Box(p, "Storm_N", new Vector3(21f, 34f, 88f), new Vector3(130f, 70f, 0.5f), storm, collider: false);
        Box(p, "Storm_S", new Vector3(21f, 34f, -48f), new Vector3(130f, 70f, 0.5f), storm, collider: false);
        Box(p, "Storm_E", new Vector3(79f, 34f, 10f), new Vector3(0.5f, 70f, 130f), storm, collider: false);
        Box(p, "Storm_W", new Vector3(-37f, 34f, 10f), new Vector3(0.5f, 70f, 130f), storm, collider: false);
        Box(p, "Storm_Lid", new Vector3(21f, 66f, 10f), new Vector3(130f, 0.5f, 130f), storm, collider: false);

        // invisible perimeter blockers keep players inside the diegetic bowl
        Blocker(p, "Bound_N", new Vector3(21f, 3f, 82f), new Vector3(120f, 6f, 0.5f));
        Blocker(p, "Bound_S", new Vector3(21f, 3f, -30f), new Vector3(120f, 6f, 0.5f));
        Blocker(p, "Bound_E", new Vector3(78f, 3f, 10f), new Vector3(0.5f, 6f, 120f)); // v2: past the yard
        Blocker(p, "Bound_W", new Vector3(-26f, 3f, 10f), new Vector3(0.5f, 6f, 120f));
    }

    // ── building shell (hall x10..42 z6..26 h8 · cold x0..10 z6..26 h5 · airlock x16..26 z0..6 h3.2) ──
    static void BuildShell(Transform p)
    {
        Material wall = PanelMat();
        Material floor = Mat(FloorGray);
        Material roof = Mat(new Color(0.20f, 0.18f, 0.17f));

        // floors (tops at y≈0.05)
        Box(p, "Floor_Hall", new Vector3(26f, -0.075f, 16f), new Vector3(32f, 0.25f, 20f), floor);
        Box(p, "Floor_Cold", new Vector3(5f, -0.075f, 16f), new Vector3(10f, 0.25f, 20f), floor);
        Box(p, "Floor_Airlock", new Vector3(21f, -0.075f, 3f), new Vector3(10f, 0.25f, 6f), floor);

        // hall walls (h8): south wall has the airlock gap (x19.5–22.5 handled by airlock) and the
        // BROKEN ROLLER shortcut x33–37 (open to 2.2, jammed shutter slab above)
        WallX(p, wall, "Hall_S_a", 10f, 19.5f, 6f, 0f, 8f);
        WallX(p, wall, "Hall_S_b", 22.5f, 33f, 6f, 0f, 8f);
        WallX(p, wall, "Hall_S_c", 37f, 42f, 6f, 0f, 8f);
        WallX(p, wall, "Hall_S_rollerHdr", 33f, 37f, 6f, 2.2f, 8f); // jammed shutter above the gap
        // north wall (v2): x20–23 roller OPEN to the loading dock; x32–35 reads as a second
        // roller jammed SHUT (header + stuck slab with collider — no passage)
        WallX(p, wall, "Hall_N_a", 10f, 20f, 26f, 0f, 8f);
        WallX(p, wall, "Hall_N_hdrA", 20f, 23f, 26f, 2.2f, 8f);
        WallX(p, wall, "Hall_N_b", 23f, 32f, 26f, 0f, 8f);
        WallX(p, wall, "Hall_N_hdrB", 32f, 35f, 26f, 2.2f, 8f);
        WallX(p, wall, "Hall_N_c", 35f, 42f, 26f, 0f, 8f);
        var jam = Box(p, "Hall_N_jammedShutter", new Vector3(33.5f, 1.1f, 26f), new Vector3(2.9f, 2.24f, 0.16f), Mat(RackSteel));
        jam.transform.localRotation = Quaternion.Euler(0f, 0f, 1.5f);
        // east wall (v2): z14–17 roller OPEN to the container yard
        WallZ(p, wall, "Hall_E_a", 6f, 14f, 42f, 0f, 8f);
        WallZ(p, wall, "Hall_E_hdr", 14f, 17f, 42f, 2.6f, 8f);
        WallZ(p, wall, "Hall_E_b", 17f, 26f, 42f, 0f, 8f);
        // hall/cold divider (h5 side): opening z15–17.5
        WallZ(p, wall, "Div_a", 6f, 15f, 10f, 0f, 5f);
        WallZ(p, wall, "Div_b", 17.5f, 26f, 10f, 0f, 5f);
        WallZ(p, wall, "Div_hdr", 15f, 17.5f, 10f, 2.6f, 5f);
        // wall band above the cold annex (hall is taller)
        WallZ(p, wall, "Div_band", 6f, 26f, 10f, 5f, 8f);

        // cold annex outer walls (h5)
        WallX(p, wall, "Cold_S", 0f, 10f, 6f, 0f, 5f);
        WallX(p, wall, "Cold_N", 0f, 10f, 26f, 0f, 5f);
        // west wall (v2): z8–10.5 doorway down to the quarantine sublevel
        WallZ(p, wall, "Cold_W_a", 6f, 8f, 0f, 0f, 5f);
        WallZ(p, wall, "Cold_W_hdr", 8f, 10.5f, 0f, 2.4f, 5f);
        WallZ(p, wall, "Cold_W_b", 10.5f, 26f, 0f, 0f, 5f);

        // airlock walls (h3.2): outer door x19.5–22.5 in z0; inner door same x in z6 (cut from hall wall)
        WallX(p, wall, "Air_S_a", 16f, 19.5f, 0f, 0f, 3.2f);
        WallX(p, wall, "Air_S_b", 22.5f, 26f, 0f, 0f, 3.2f);
        WallX(p, wall, "Air_S_hdr", 19.5f, 22.5f, 0f, 2.6f, 3.2f);
        WallX(p, wall, "Air_InnerHdr", 19.5f, 22.5f, 6f, 2.6f, 8f); // header of the inner doorway (hall side full height)
        WallZ(p, wall, "Air_W", 0f, 6f, 16f, 0f, 3.2f);
        WallZ(p, wall, "Air_E", 0f, 6f, 26f, 0f, 3.2f);

        // roofs
        Box(p, "Roof_Hall", new Vector3(26f, 8.1f, 16f), new Vector3(32.6f, 0.2f, 20.6f), roof);
        Box(p, "Roof_Cold", new Vector3(5f, 5.1f, 16f), new Vector3(10.6f, 0.2f, 20.6f), roof);
        Box(p, "Roof_Air", new Vector3(21f, 3.3f, 3f), new Vector3(10.6f, 0.2f, 6.6f), roof);
    }

    static void BuildAirlock(Transform p)
    {
        // seizure language at the entrance: notice quads + red stamp + tape X strips
        Material paper = Mat(Paper);
        Material red = Mat(StampRed);
        for (int i = 0; i < 3; i++)
        {
            var n = Box(p, $"Notice_{i}", new Vector3(17.2f + i * 0.9f, 1.6f + (i % 2) * 0.25f, 5.83f),
                new Vector3(0.62f, 0.85f, 0.02f), paper, collider: false);
            n.transform.localRotation = Quaternion.Euler(0f, (i - 1) * 4f, (i % 2 == 0 ? 2f : -3f));
            Box(p, $"NoticeStamp_{i}", new Vector3(17.2f + i * 0.9f, 1.35f + (i % 2) * 0.25f, 5.815f),
                new Vector3(0.3f, 0.18f, 0.02f), red, collider: false);
        }
        // check-in counter, long abandoned
        Box(p, "CheckinDesk", new Vector3(24f, 0.55f, 3.4f), new Vector3(2.4f, 1.1f, 0.9f), Mat(ShelfWood));
        Loot(p, "LA_air_desk", new Vector3(24f, 1.18f, 3.4f), DressingSurface.DeskSurface);
        // tape X on both sides of the inner doorway
        TapeX(p, new Vector3(18.6f, 1.5f, 5.9f));
        TapeX(p, new Vector3(23.4f, 1.5f, 5.9f));
    }

    static void BuildHall(Transform p)
    {
        // four double-sided rack rows (aisle gap at x20–22.5), shelves at 0.7 / 1.7
        float[] rows = { 10f, 13.5f, 17f, 20.5f };
        foreach (float z in rows)
        {
            RackRun(p, 13f, 20f, z);
            RackRun(p, 22.5f, 29f, z);
        }

        // shelf loot anchors (alternating low/high, skipping the tipped-rack zone)
        float[] lootX = { 14.5f, 18f, 23.5f, 27f };
        int k = 0;
        foreach (float z in rows)
            foreach (float x in lootX)
            {
                if (z == 17f && x < 19f) continue; // tipped rack replaces these
                Loot(p, $"LA_hall_{k}", new Vector3(x, (k % 2 == 0 ? 0.78f : 1.78f), z), DressingSurface.ShelfSlot);
                k++;
            }

        // the tipped rack + spilled crates (blocks a straight west aisle run)
        var tip = Box(p, "TippedRack", new Vector3(16.5f, 0.9f, 16.2f), new Vector3(4.2f, 0.25f, 1.1f), Mat(RackSteel));
        tip.transform.localRotation = Quaternion.Euler(0f, 8f, 68f);
        Crate(p, "Spill_A", new Vector3(15.3f, 0.36f, 15.4f), 0.72f, CrateBrown, 14f);
        Crate(p, "Spill_B", new Vector3(17.6f, 0.30f, 16.9f), 0.6f, CrateOlive, 40f);
        Crate(p, "Spill_C", new Vector3(16.2f, 0.30f, 17.4f), 0.6f, CrateBrown, 71f);
        Loot(p, "LA_spill_0", new Vector3(15.9f, 0.12f, 16.6f), DressingSurface.Floor);
        Loot(p, "LA_spill_1", new Vector3(17.1f, 0.12f, 15.8f), DressingSurface.Floor);
        Loot(p, "LA_spill_2", new Vector3(14.6f, 0.12f, 17.1f), DressingSurface.Floor);

        // stray pallet stacks along the north lane
        Crate(p, "NorthStack_A", new Vector3(13f, 0.5f, 24.2f), 1.0f, CrateOlive, 5f);
        Crate(p, "NorthStack_A2", new Vector3(13f, 1.4f, 24.2f), 0.8f, CrateBrown, 32f);
        Crate(p, "NorthStack_B", new Vector3(30f, 0.5f, 24.5f), 1.0f, CrateBrown, 84f);
        Loot(p, "LA_north_0", new Vector3(13f, 1.92f, 24.2f), DressingSurface.CrateTop);
        Loot(p, "LA_north_1", new Vector3(30f, 1.12f, 24.5f), DressingSurface.CrateTop);

        // overhead crane rail (pure silhouette against the roof)
        Box(p, "CraneRail", new Vector3(26f, 7.2f, 16f), new Vector3(30f, 0.35f, 0.5f), Mat(RackSteel), collider: false);
        Box(p, "CraneTrolley", new Vector3(22f, 6.7f, 16f), new Vector3(1.6f, 0.7f, 1.1f), Mat(RackSteel), collider: false);

        // MonsterSeed — far NE aisle. "IDOL" routes to the Civic Idol: the hall's long
        // sightlines + shelf aisles are the freeze-when-watched arena (glance = frozen,
        // aisle blocks the line of sight = it walks).
        Seed(p, "MonsterSeed_ML_HALL_IDOL", new Vector3(38f, 0.05f, 22f));
    }

    static void BuildCold(Transform p)
    {
        Material coldW = Mat(ColdWall);
        // frost skin panels on the inside walls (decor)
        Box(p, "FrostSkin_W", new Vector3(0.18f, 2.4f, 16f), new Vector3(0.06f, 4.6f, 19.4f), coldW, collider: false);
        Box(p, "FrostSkin_N", new Vector3(5f, 2.4f, 25.82f), new Vector3(9.4f, 4.6f, 0.06f), coldW, collider: false);

        // heritage crate blocks (relic-tier loot on top), two bays with a corridor between
        for (int bay = 0; bay < 2; bay++)
        {
            float bz = 10f + bay * 9f;
            for (int i = 0; i < 3; i++)
            {
                float bx = 2.2f + i * 2.6f;
                Crate(p, $"Heritage_{bay}_{i}", new Vector3(bx, 0.55f, bz), 1.1f, CrateOlive, i * 29f + bay * 13f);
                Crate(p, $"Heritage_{bay}_{i}_top", new Vector3(bx, 1.5f, bz), 0.85f, CrateBrown, i * 47f);
                if ((i + bay) % 2 == 0)
                    Loot(p, $"LA_cold_{bay}_{i}", new Vector3(bx, 1.97f, bz), DressingSurface.CrateTop);
            }
        }
        Loot(p, "LA_cold_floor", new Vector3(7.5f, 0.12f, 21f), DressingSurface.Floor);

        // seizure language on the cold door frame
        TapeX(p, new Vector3(10.1f, 1.5f, 14.6f));
        Box(p, "ColdNotice", new Vector3(10.12f, 1.7f, 18.1f), new Vector3(0.02f, 0.8f, 0.6f), Mat(Paper), collider: false);
        Box(p, "ColdNoticeStamp", new Vector3(10.13f, 1.45f, 18.1f), new Vector3(0.02f, 0.18f, 0.3f), Mat(StampRed), collider: false);

        // MonsterSeed — the nest, deepest corner. "WARDEN" routes to the FileWarden:
        // the archive custodian guards the heritage crates (MonsterSpawnBootstrap keyword).
        Seed(p, "MonsterSeed_ML_COLD_WARDEN", new Vector3(2f, 0.05f, 23f));
    }

    static void BuildMezzanine(Transform p)
    {
        Material steel = Mat(RackSteel);
        Material floor = Mat(new Color(0.30f, 0.30f, 0.32f));

        // deck x30..42 z6..14 at y3.6
        Box(p, "MezzDeck", new Vector3(36f, 3.6f, 10f), new Vector3(12f, 0.15f, 8f), floor);
        // support posts
        for (int i = 0; i < 3; i++)
            Box(p, $"MezzPost_{i}", new Vector3(31f + i * 5f, 1.8f, 13.6f), new Vector3(0.3f, 3.6f, 0.3f), steel);
        // railing along the open north edge and west stair edge
        // x30–40 railed; x40–42 opens onto the upper catwalk loop (v2)
        Box(p, "MezzRail_N", new Vector3(35f, 4.6f, 13.9f), new Vector3(10f, 0.9f, 0.08f), steel);
        Box(p, "MezzRail_W", new Vector3(30.05f, 4.6f, 11.2f), new Vector3(0.08f, 0.9f, 5.4f), steel);

        // straight stair ramp up the west side: from (24,0,7.6) to (30,3.6,7.6) ≈ 31°
        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "MezzRamp";
        ramp.transform.SetParent(p, false);
        Vector3 a = new Vector3(24f, 0.05f, 7.6f), b = new Vector3(30f, 3.6f, 7.6f);
        Vector3 mid = (a + b) * 0.5f;
        float len = Vector3.Distance(a, b);
        ramp.transform.localPosition = mid;
        ramp.transform.localRotation = Quaternion.FromToRotation(Vector3.right, (b - a).normalized);
        ramp.transform.localScale = new Vector3(len, 0.15f, 1.6f);
        ramp.GetComponent<MeshRenderer>().sharedMaterial = steel;
        // low kerbs so players don't slide off the ramp sideways
        Box(p, "RampKerb_N", new Vector3(27f, 2f, 8.45f), new Vector3(7f, 4.2f, 0.08f), steel, collider: true, visible: false);

        // paperwork office: two desks, filing cabinet, warm loot
        Desk(p, "MezzDesk_A", new Vector3(34f, 3.68f, 9f), 0f);
        Desk(p, "MezzDesk_B", new Vector3(38.5f, 3.68f, 11.5f), 90f);
        Loot(p, "LA_mezz_0", new Vector3(34f, 4.5f, 9f), DressingSurface.DeskSurface);
        Loot(p, "LA_mezz_1", new Vector3(38.5f, 4.5f, 11.5f), DressingSurface.DeskSurface);
        Box(p, "MezzCabinet", new Vector3(41.3f, 4.35f, 7.2f), new Vector3(0.6f, 1.5f, 0.5f), Mat(RackSteel));
        Loot(p, "LA_mezz_2", new Vector3(41.3f, 5.18f, 7.2f), DressingSurface.Cabinet);
        // scattered manifests (paper decor)
        for (int i = 0; i < 4; i++)
        {
            var sheet = Box(p, $"Manifest_{i}", new Vector3(33f + i * 1.7f, 3.69f, 10.5f + (i % 2) * 1.2f),
                new Vector3(0.3f, 0.01f, 0.42f), Mat(Paper), collider: false);
            sheet.transform.localRotation = Quaternion.Euler(0f, i * 37f, 0f);
        }

        // MonsterSeed — mezzanine prowler
        Seed(p, "MonsterSeed_ML_MEZZ", new Vector3(36f, 3.75f, 10f));
    }

    static void BuildLights(Transform p)
    {
        // storm sun
        var sun = new GameObject("StormSun");
        sun.transform.SetParent(p, false);
        sun.transform.rotation = Quaternion.Euler(38f, 155f, 0f);
        var dl = sun.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = new Color(1.0f, 0.62f, 0.35f);
        dl.intensity = 0.55f;
        dl.shadows = LightShadows.Soft;

        // hall sodium pools (R3 brighten pass, David 2026-07-02 "内部偏暗": pools up ~40%,
        // gaps still dimmer than pools so the light-language survives, floor no longer black)
        Point(p, "Sodium_A", new Vector3(14f, 6.5f, 11f), SodiumAmber, 5.0f, 16f);
        Point(p, "Sodium_B", new Vector3(26f, 6.5f, 15f), SodiumAmber, 5.0f, 16f);
        Point(p, "Sodium_C", new Vector3(36f, 6.5f, 20f), SodiumAmber, 4.6f, 15f);
        Point(p, "Sodium_D", new Vector3(18f, 6.5f, 21f), SodiumAmber, 4.2f, 14f);

        // airlock utility lamp
        Point(p, "AirLamp", new Vector3(21f, 2.7f, 3f), new Color(0.9f, 0.85f, 0.7f), 2.8f, 8f);

        // cold storage dying fluorescents
        Point(p, "Cold_A", new Vector3(5f, 4.2f, 12f), ColdCyan, 3.6f, 11f);
        Point(p, "Cold_B", new Vector3(4f, 4.2f, 22f), ColdCyan, 3.1f, 10f);
        Point(p, "Cold_C", new Vector3(8f, 4.2f, 17f), ColdCyan, 2.5f, 9f);

        // mezzanine: warm desk pool + phosphor green terminal glow
        Point(p, "MezzWarm", new Vector3(34f, 5.1f, 9f), new Color(1.0f, 0.8f, 0.5f), 2.8f, 7f);
        Point(p, "MezzCrt", new Vector3(38.5f, 4.9f, 11.5f), new Color(0.35f, 1.0f, 0.5f), 1.4f, 4.5f);

        // pad beacon so the return run reads from inside the door
        Point(p, "PadBeacon", new Vector3(21f, 3.2f, -8f), SodiumAmber, 2.4f, 11f);

        // v2 sections
        Point(p, "Yard_Sodium", new Vector3(58f, 6.2f, 12f), SodiumAmber, 3.9f, 16f);
        Point(p, "Dock_Sodium_A", new Vector3(20f, 5.2f, 32f), SodiumAmber, 3.6f, 14f);
        Point(p, "Dock_Sodium_B", new Vector3(34f, 5.2f, 35f), SodiumAmber, 3.3f, 13f);
    }

    static void BuildMissionAnchors(Transform p)
    {
        // van boarding trigger on the pad (MissionMapFinalizer grounds the van visual beside it)
        var exit = new GameObject("VAN_ExitPoint");
        exit.transform.SetParent(p, false);
        exit.transform.position = new Vector3(21f, 1f, -8f);
        var box = exit.AddComponent<BoxCollider>();
        box.size = new Vector3(2.5f, 2f, 2.5f);
        box.isTrigger = true;
        exit.AddComponent<Unity.Netcode.NetworkObject>();
        exit.AddComponent<MissionVanExitPoint>();

        var spawn = new GameObject("PlayerSpawnPoint");
        spawn.transform.SetParent(p, false);
        spawn.transform.position = new Vector3(17.5f, 0.1f, -10.5f);
    }

    static void ApplyAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = FogColor;
        RenderSettings.fogStartDistance = 16f;
        RenderSettings.fogEndDistance = 62f;
        RenderSettings.skybox = null; // the storm box owns every sightline
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.34f, 0.26f); // R3 brighten: floor of visibility up ~25%
    }

    // ── v2 sections ─────────────────────────────────────────────────────────────

    /// <summary>Open-air container yard east of the hall (x44–74, z0–26): stacked rows with
    /// walk-through lanes, three enterable containers holding loot. Storm-exposed.</summary>
    static void BuildYard(Transform p)
    {
        Color[] cans = { CrateOlive, CrateBrown, new Color(0.24f, 0.28f, 0.33f) };
        var openSpots = new (float x, float z)[] { (47f, 7f), (59.4f, 14f), (65.6f, 21f) };

        foreach (float z in new[] { 7f, 14f, 21f })
            for (float x = 47f; x <= 71f; x += 6.2f)
            {
                bool isOpen = false;
                foreach (var o in openSpots) if (Mathf.Abs(o.x - x) < 0.1f && Mathf.Abs(o.z - z) < 0.1f) isOpen = true;
                int h = Mathf.Abs((int)(x * 13 + z * 7));
                if (isOpen) { OpenContainer(p, new Vector3(x, 0f, z), $"OpenCan_{x:0}_{z:0}"); continue; }
                if ((h & 7) == 0) continue; // lane break
                Color c = cans[h % 3];
                Box(p, $"Can_{x:0}_{z:0}", new Vector3(x, 1.3f, z), new Vector3(6f, 2.6f, 2.6f), Mat(c));
                if ((h & 3) == 0)
                    Box(p, $"CanTop_{x:0}_{z:0}", new Vector3(x + 0.15f, 3.9f, z), new Vector3(6f, 2.6f, 2.6f), Mat(cans[(h + 1) % 3]));
            }

        // lane loot between the stacks
        Loot(p, "LA_yard_0", new Vector3(50f, 0.12f, 10.5f), DressingSurface.Floor);
        Loot(p, "LA_yard_1", new Vector3(62f, 0.12f, 17.5f), DressingSurface.Floor);
        Loot(p, "LA_yard_2", new Vector3(56f, 0.12f, 3.8f), DressingSurface.Floor);
        Loot(p, "LA_yard_3", new Vector3(68.5f, 0.12f, 10.5f), DressingSurface.Floor);

        // dead yard floodlight + one live sodium pole
        Box(p, "YardPole", new Vector3(58f, 3.2f, 12f), new Vector3(0.18f, 6.4f, 0.18f), Mat(RackSteel));
        Seed(p, "MonsterSeed_ML_YARD", new Vector3(58f, 0.05f, 13f));
    }

    /// <summary>One enterable container: five slabs, open end toward -X, two loot anchors inside.</summary>
    static void OpenContainer(Transform p, Vector3 basePos, string name)
    {
        Material shell = Mat(new Color(0.30f, 0.24f, 0.16f));
        float x = basePos.x, z = basePos.z;
        Box(p, name + "_floor", new Vector3(x, 0.08f, z), new Vector3(6f, 0.16f, 2.6f), shell);
        Box(p, name + "_roof", new Vector3(x, 2.52f, z), new Vector3(6f, 0.16f, 2.6f), shell);
        Box(p, name + "_sideN", new Vector3(x, 1.3f, z + 1.22f), new Vector3(6f, 2.6f, 0.16f), shell);
        Box(p, name + "_sideS", new Vector3(x, 1.3f, z - 1.22f), new Vector3(6f, 2.6f, 0.16f), shell);
        Box(p, name + "_back", new Vector3(x + 2.92f, 1.3f, z), new Vector3(0.16f, 2.6f, 2.6f), shell);
        var door = Box(p, name + "_doorAjar", new Vector3(x - 3.3f, 1.28f, z + 1.05f), new Vector3(0.1f, 2.4f, 1.2f), shell);
        door.transform.localRotation = Quaternion.Euler(0f, -35f, 0f);
        Loot(p, name + "_LA0", new Vector3(x + 0.6f, 0.28f, z + 0.4f), DressingSurface.Floor);
        Loot(p, name + "_LA1", new Vector3(x + 2f, 0.28f, z - 0.4f), DressingSurface.Floor);
    }

    /// <summary>Loading dock north of the hall (x14–42, z26–38, h6): conveyor line, two
    /// trailers (one open-backed with loot), pallet clutter. Entry via the open roller x20–23.</summary>
    static void BuildDock(Transform p)
    {
        Material wall = PanelMat();
        Material floor = Mat(FloorGray);
        Material steel = Mat(RackSteel);

        Box(p, "Dock_Floor", new Vector3(28f, -0.075f, 32f), new Vector3(28f, 0.25f, 12f), floor);
        Box(p, "Dock_Roof", new Vector3(28f, 6.1f, 32f), new Vector3(28.6f, 0.2f, 12.6f), Mat(new Color(0.20f, 0.18f, 0.17f)));
        WallZ(p, wall, "Dock_W", 26f, 38f, 14f, 0f, 6f);
        WallZ(p, wall, "Dock_E", 26f, 38f, 42f, 0f, 6f);
        WallX(p, wall, "Dock_N", 14f, 42f, 38f, 0f, 6f);
        // two exterior rollers on the north wall, both jammed shut (visual seams)
        Box(p, "Dock_RollerA", new Vector3(20f, 1.6f, 37.9f), new Vector3(3.4f, 3.2f, 0.1f), steel, collider: false);
        Box(p, "Dock_RollerB", new Vector3(34f, 1.6f, 37.9f), new Vector3(3.4f, 3.2f, 0.1f), steel, collider: false);

        // conveyor line across the room
        Box(p, "Conveyor", new Vector3(28f, 0.72f, 31.2f), new Vector3(22f, 0.14f, 0.9f), steel);
        for (float x = 18f; x <= 38f; x += 2.2f)
        {
            var leg = Box(p, $"ConvLeg_{x:0}", new Vector3(x, 0.33f, 31.2f), new Vector3(0.12f, 0.66f, 0.7f), steel);
        }
        Loot(p, "LA_dock_conv0", new Vector3(22f, 0.9f, 31.2f), DressingSurface.DeskSurface);
        Loot(p, "LA_dock_conv1", new Vector3(33f, 0.9f, 31.2f), DressingSurface.DeskSurface);

        // closed trailer + open-backed trailer with loot inside
        Box(p, "TrailerClosed", new Vector3(19f, 1.35f, 35f), new Vector3(7f, 2.5f, 2.4f), Mat(CrateOlive));
        Material tr = Mat(new Color(0.33f, 0.30f, 0.24f));
        Box(p, "TrailerOpen_floor", new Vector3(31f, 0.35f, 35f), new Vector3(7f, 0.2f, 2.4f), tr);
        Box(p, "TrailerOpen_roof", new Vector3(31f, 2.55f, 35f), new Vector3(7f, 0.2f, 2.4f), tr);
        Box(p, "TrailerOpen_sideN", new Vector3(31f, 1.45f, 36.1f), new Vector3(7f, 2f, 0.14f), tr);
        Box(p, "TrailerOpen_sideS", new Vector3(31f, 1.45f, 33.9f), new Vector3(7f, 2f, 0.14f), tr);
        Box(p, "TrailerOpen_back", new Vector3(34.43f, 1.45f, 35f), new Vector3(0.14f, 2f, 2.4f), tr);
        // step up into the open trailer bed
        Box(p, "TrailerStep", new Vector3(27f, 0.16f, 35f), new Vector3(1.2f, 0.32f, 1.6f), steel);
        Loot(p, "LA_dock_tr0", new Vector3(29.5f, 0.6f, 35.3f), DressingSurface.Floor);
        Loot(p, "LA_dock_tr1", new Vector3(32.5f, 0.6f, 34.7f), DressingSurface.Floor);

        // pallet clutter
        Crate(p, "DockPallet_A", new Vector3(16f, 0.5f, 28.5f), 1.0f, CrateBrown, 20f);
        Crate(p, "DockPallet_B", new Vector3(39.5f, 0.5f, 29f), 1.0f, CrateOlive, 55f);
        Crate(p, "DockPallet_B2", new Vector3(39.5f, 1.4f, 29f), 0.8f, CrateBrown, 80f);
        Loot(p, "LA_dock_p0", new Vector3(16f, 1.12f, 28.5f), DressingSurface.CrateTop);
        Loot(p, "LA_dock_p1", new Vector3(39.5f, 1.92f, 29f), DressingSurface.CrateTop);
        Loot(p, "LA_dock_floor", new Vector3(24f, 0.12f, 34f), DressingSurface.Floor);

        Seed(p, "MonsterSeed_ML_DOCK", new Vector3(36f, 0.05f, 33f));
    }

    /// <summary>Quarantine sublevel under the west regolith (y −4): an enclosed ramp from the
    /// cold annex descends to a corridor and three rooms — exam, relic archive (warden nest),
    /// incinerator. The relic-dense, darkest end of the map.</summary>
    static void BuildSublevel(Transform p)
    {
        Material wall = PanelMat();
        Material floor = Mat(new Color(0.22f, 0.21f, 0.20f));
        Material steel = Mat(RackSteel);

        // descent tube from the Cold_W doorway (x0, z8–10.5) down to x−8 (26.6°)
        Tube(p, new Vector3(-0.2f, 0.02f, 9.25f), new Vector3(-8f, -3.95f, 9.25f), 2.4f, 2.6f, wall, "SubDescent");
        // landing slab bridging tube mouth ↔ corridor: the voxelizer left a one-cell seam at
        // the junction (z≈10.5) that islanded the whole sublevel — a physical overlap stitches it
        Box(p, "Sub_Landing", new Vector3(-9.2f, -3.93f, 10.2f), new Vector3(2.2f, 0.1f, 5.4f), floor);
        // …and the seams SURVIVED the slab (polys on both sides, no weld) — NavMeshLinks are the
        // sanctioned bridge. The tube mouth pinches BOTH sides: one link per seam (z≈8 and z≈10.5).
        foreach (float lz in new[] { 7.95f, 10.5f })
        {
            var linkGo = new GameObject($"Sub_SeamLink_{lz:0}");
            linkGo.transform.SetParent(p, false);
            linkGo.transform.position = new Vector3(-9.2f, -3.85f, lz);
            var seam = linkGo.AddComponent<Unity.AI.Navigation.NavMeshLink>();
            seam.startPoint = new Vector3(0f, 0f, -1.1f);
            seam.endPoint = new Vector3(0f, 0f, 1.1f);
            seam.width = 2f;
            seam.bidirectional = true;
        }

        // shell: floor/ceiling x−22..−8, z4..26
        Box(p, "Sub_Floor", new Vector3(-15f, -4.075f, 15f), new Vector3(14f, 0.25f, 22f), floor);
        Box(p, "Sub_Ceil", new Vector3(-15f, -0.9f, 15f), new Vector3(14.6f, 0.2f, 22.6f), Mat(new Color(0.16f, 0.15f, 0.14f)));
        WallX(p, wall, "Sub_S", -22f, -8f, 4f, -4f, -1f);
        WallX(p, wall, "Sub_N", -22f, -8f, 26f, -4f, -1f);
        WallZ(p, wall, "Sub_W", 4f, 26f, -22f, -4f, -1f);
        // east wall with the tube mouth (z8–10.5)
        WallZ(p, wall, "Sub_E_a", 4f, 8f, -8f, -4f, -1f);
        WallZ(p, wall, "Sub_E_hdr", 8f, 10.5f, -8f, -1.4f, -1f);
        WallZ(p, wall, "Sub_E_b", 10.5f, 26f, -8f, -4f, -1f);

        // corridor x−10.4..−8; room wall x=−10.4 with three doorways
        WallZ(p, wall, "Sub_RoomWall_a", 4f, 6f, -10.4f, -4f, -1f);
        WallZ(p, wall, "Sub_RW_hdr1", 6f, 8f, -10.4f, -1.4f, -1f);   // 2.55m clear — 2.2 got voxel-rounded below the 2.0 agent
        WallZ(p, wall, "Sub_RoomWall_b", 8f, 13.5f, -10.4f, -4f, -1f);
        WallZ(p, wall, "Sub_RW_hdr2", 13.5f, 15.5f, -10.4f, -1.4f, -1f);
        WallZ(p, wall, "Sub_RoomWall_c", 15.5f, 20.5f, -10.4f, -4f, -1f);
        WallZ(p, wall, "Sub_RW_hdr3", 20.5f, 22.5f, -10.4f, -1.4f, -1f);
        WallZ(p, wall, "Sub_RoomWall_d", 22.5f, 26f, -10.4f, -4f, -1f);
        // room dividers
        WallX(p, wall, "Sub_Div1", -22f, -10.4f, 11f, -4f, -1f);
        WallX(p, wall, "Sub_Div2", -22f, -10.4f, 18f, -4f, -1f);

        // R1 exam room (z4–11): two gurneys
        Box(p, "Sub_Gurney_A", new Vector3(-17f, -3.6f, 7f), new Vector3(1.9f, 0.7f, 0.8f), steel);
        Box(p, "Sub_Gurney_B", new Vector3(-13.5f, -3.6f, 9f), new Vector3(1.9f, 0.7f, 0.8f), steel);
        Loot(p, "LA_sub_0", new Vector3(-17f, -3.18f, 7f), DressingSurface.DeskSurface);
        Loot(p, "LA_sub_1", new Vector3(-13.5f, -3.18f, 9f), DressingSurface.DeskSurface);

        // R2 relic archive (z11–18): two short shelf runs — the warden's nest
        foreach (float z in new[] { 13.2f, 16f })
        {
            Box(p, $"SubShelf_lo_{z:0}", new Vector3(-16f, -3.3f, z), new Vector3(9f, 0.08f, 1.1f), Mat(ShelfWood));
            Box(p, $"SubShelf_hi_{z:0}", new Vector3(-16f, -2.3f, z), new Vector3(9f, 0.08f, 1.1f), Mat(ShelfWood));
            for (float x = -20f; x <= -12f; x += 4f)
                Box(p, $"SubPost_{x:0}_{z:0}", new Vector3(x, -2.9f, z), new Vector3(0.16f, 2.1f, 1.05f), steel);
        }
        Loot(p, "LA_sub_2", new Vector3(-19f, -3.22f, 13.2f), DressingSurface.ShelfSlot);
        Loot(p, "LA_sub_3", new Vector3(-14f, -2.22f, 13.2f), DressingSurface.ShelfSlot);
        Loot(p, "LA_sub_4", new Vector3(-17f, -3.22f, 16f), DressingSurface.ShelfSlot);
        Loot(p, "LA_sub_5", new Vector3(-13f, -2.22f, 16f), DressingSurface.ShelfSlot);
        Seed(p, "MonsterSeed_ML_SUB_WARDEN", new Vector3(-15f, -3.9f, 14.5f));

        // R3 incinerator (z18–26)
        Box(p, "Sub_Furnace", new Vector3(-16f, -2.9f, 22.5f), new Vector3(2.2f, 2.2f, 2.2f), steel);
        Box(p, "Sub_FurnaceDoor", new Vector3(-16f, -3.3f, 21.35f), new Vector3(1f, 1f, 0.08f), Mat(new Color(0.35f, 0.12f, 0.08f)), collider: false);
        Loot(p, "LA_sub_6", new Vector3(-19.5f, -3.83f, 24f), DressingSurface.Floor);
        Loot(p, "LA_sub_7", new Vector3(-12f, -3.83f, 20f), DressingSurface.Floor);
        // corridor stray
        Loot(p, "LA_sub_8", new Vector3(-9.2f, -3.83f, 23f), DressingSurface.Floor);

        // seizure language at the descent mouth
        TapeX(p, new Vector3(0.12f, 1.45f, 7.6f));

        // lights (R3 brighten): corridor + exam + archive cyans, red glow in R3
        Point(p, "SubLamp_Corr", new Vector3(-9.2f, -1.5f, 10f), ColdCyan, 2.0f, 8f);
        Point(p, "SubLamp_Exam", new Vector3(-15f, -1.5f, 7.5f), ColdCyan, 2.2f, 8f);
        Point(p, "SubLamp_Arch", new Vector3(-16f, -1.5f, 14.5f), ColdCyan, 2.4f, 9f);
        Point(p, "SubLamp_Furn", new Vector3(-16f, -2.6f, 21f), new Color(0.9f, 0.25f, 0.12f), 2.4f, 8f);
    }

    /// <summary>Upper catwalk loop (y 3.6): mezz NE corner → east wall run → north wall run →
    /// ramp back down at the hall's NW. Overlooks every rack aisle; four loot anchors.</summary>
    static void BuildCatwalk(Transform p)
    {
        Material steel = Mat(RackSteel);
        Material deckM = Mat(new Color(0.30f, 0.30f, 0.32f));

        // decks OVERLAP their neighbours (mezz / each other / the ramp) — edge-to-edge
        // adjacency left navmesh seams and the loop baked as an island
        Box(p, "CatE_Deck", new Vector3(41.2f, 3.6f, 19.05f), new Vector3(1.6f, 0.15f, 11.1f), deckM);  // z13.5–24.6, laps the mezz deck
        Box(p, "CatN_Deck", new Vector3(25.95f, 3.6f, 25.2f), new Vector3(31.1f, 0.15f, 1.6f), deckM);  // x10.4–41.5, laps ramp + east run
        // inner railings (outer edges hug walls)
        Box(p, "CatE_Rail", new Vector3(40.44f, 4.35f, 19.3f), new Vector3(0.08f, 1.5f, 10.6f), steel);
        Box(p, "CatN_Rail", new Vector3(27.9f, 4.35f, 24.44f), new Vector3(26.8f, 1.5f, 0.08f), steel);
        // supports
        foreach (float x in new[] { 17f, 26f, 35f })
            Box(p, $"CatPost_{x:0}", new Vector3(x, 1.8f, 25.6f), new Vector3(0.25f, 3.6f, 0.25f), steel);

        // ramp down along the west divider (clear of the rack rows which start at x13)
        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Cat_Ramp";
        ramp.transform.SetParent(p, false);
        Vector3 a = new Vector3(11.2f, 3.6f, 24.4f), b = new Vector3(11.2f, 0.05f, 17.4f);
        ramp.transform.localPosition = (a + b) * 0.5f;
        ramp.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, (b - a).normalized);
        ramp.transform.localScale = new Vector3(1.6f, 0.14f, Vector3.Distance(a, b));
        ramp.GetComponent<MeshRenderer>().sharedMaterial = steel;
        Box(p, "CatRamp_Rail", new Vector3(12.05f, 2.6f, 20.9f), new Vector3(0.08f, 3.4f, 7.4f), steel);

        Loot(p, "LA_cat_0", new Vector3(16f, 3.72f, 25.2f), DressingSurface.Floor);
        Loot(p, "LA_cat_1", new Vector3(24f, 3.72f, 25.2f), DressingSurface.Floor);
        Loot(p, "LA_cat_2", new Vector3(33f, 3.72f, 25.2f), DressingSurface.Floor);
        Loot(p, "LA_cat_3", new Vector3(41.2f, 3.72f, 16.5f), DressingSurface.Floor);
    }

    /// <summary>Enclosed sloped tube between two points sharing a z: walkable floor, side
    /// walls, roof. The sublevel descent.</summary>
    static void Tube(Transform p, Vector3 a, Vector3 b, float width, float height, Material m, string name)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        Quaternion rot = Quaternion.LookRotation(d / len, Vector3.up);
        Vector3 up = rot * Vector3.up;
        Vector3 right = rot * Vector3.right;
        Vector3 mid = (a + b) * 0.5f;
        RotBox(p, m, mid - up * 0.08f, rot, new Vector3(width, 0.16f, len + 0.6f), name + "_Floor");
        RotBox(p, m, mid + right * (width * 0.5f) + up * (height * 0.5f), rot, new Vector3(0.16f, height, len), name + "_WallR");
        RotBox(p, m, mid - right * (width * 0.5f) + up * (height * 0.5f), rot, new Vector3(0.16f, height, len), name + "_WallL");
        // roof pulled back toward the TOP end: an overhang past the mouth squeezed the landing
        // strip below agent height and cut the sublevel corridor out of the navmesh
        Vector3 dirN = d / len;
        RotBox(p, m, mid + up * height - dirN * 0.7f, rot, new Vector3(width, 0.16f, len - 0.8f), name + "_Roof");
    }

    static void RotBox(Transform p, Material m, Vector3 pos, Quaternion rot, Vector3 scale, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = m;
    }

    // ── pieces ──────────────────────────────────────────────────────────────────
    /// <summary>A double-shelf rack segment run between x0..x1 at row z.</summary>
    static void RackRun(Transform p, float x0, float x1, float z)
    {
        Material steel = Mat(RackSteel);
        Material shelf = Mat(ShelfWood);
        for (float x = x0; x <= x1 + 0.01f; x += 3.5f)
            Box(p, $"RackPost_{x:0}_{z:0}", new Vector3(x, 1.1f, z), new Vector3(0.18f, 2.2f, 1.1f), steel);
        float cx = (x0 + x1) * 0.5f, w = x1 - x0;
        Box(p, $"Shelf_lo_{cx:0}_{z:0}", new Vector3(cx, 0.7f, z), new Vector3(w, 0.08f, 1.15f), shelf);
        Box(p, $"Shelf_hi_{cx:0}_{z:0}", new Vector3(cx, 1.7f, z), new Vector3(w, 0.08f, 1.15f), shelf);
        // a few boxes living on the shelves (decor, collider-free)
        for (float x = x0 + 1.2f; x < x1; x += 2.9f)
        {
            int h = Mathf.Abs((int)(x * 7 + z * 13));
            if ((h & 3) == 0) continue;
            Crate(p, $"ShelfBox_{x:0}_{z:0}", new Vector3(x, (h & 1) == 0 ? 1.02f : 2.02f, z),
                0.55f + (h % 3) * 0.08f, (h & 1) == 0 ? CrateBrown : CrateOlive, h % 90, collider: false);
        }
    }

    static void Crate(Transform p, string name, Vector3 pos, float size, Color c, float yaw, bool collider = true)
    {
        var go = Box(p, name, pos, new Vector3(size, size, size), Mat(c), collider);
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    static void Desk(Transform p, string name, Vector3 pos, float yaw)
    {
        var prefab = Resources.Load<GameObject>("GeneratedArt/AS_OfficeDesk");
        if (prefab != null)
        {
            var go = (GameObject)Object.Instantiate(prefab, p);
            go.name = name;
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return;
        }
        Box(p, name, pos + Vector3.up * 0.38f, new Vector3(1.6f, 0.76f, 0.8f), Mat(ShelfWood));
    }

    static void TapeX(Transform p, Vector3 pos)
    {
        Material red = Mat(StampRed);
        var a = Box(p, "TapeX_a", pos, new Vector3(0.09f, 1.35f, 0.02f), red, collider: false);
        a.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        var b = Box(p, "TapeX_b", pos, new Vector3(0.09f, 1.35f, 0.02f), red, collider: false);
        b.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
    }

    static void Loot(Transform p, string name, Vector3 pos, DressingSurface surface)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.position = pos;
        go.AddComponent<LootAnchor>().surface = surface;
    }

    static void Seed(Transform p, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.position = pos;
    }

    static void Point(Transform p, string name, Vector3 pos, Color c, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
    }

    static void Blocker(Transform p, string name, Vector3 pos, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.position = pos;
        var c = go.AddComponent<BoxCollider>();
        c.size = size;
    }

    // wall along X (constant z) between x0..x1, from yLo to yHi
    static void WallX(Transform p, Material m, string name, float x0, float x1, float z, float yLo, float yHi)
        => Box(p, name, new Vector3((x0 + x1) * 0.5f, (yLo + yHi) * 0.5f, z),
               new Vector3(x1 - x0, yHi - yLo, 0.3f), m);

    // wall along Z (constant x) between z0..z1
    static void WallZ(Transform p, Material m, string name, float z0, float z1, float x, float yLo, float yHi)
        => Box(p, name, new Vector3(x, (yLo + yHi) * 0.5f, (z0 + z1) * 0.5f),
               new Vector3(0.3f, yHi - yLo, z1 - z0), m);

    static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat,
        bool collider = true, bool visible = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (!visible) Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
        if (!collider) { var c = go.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c); }
        return go;
    }

    static Transform Child(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static Material Mat(Color c)
    {
        if (MatCache.TryGetValue(c, out var cached) && cached != null) return cached;
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.10f);
        MatCache[c] = m;
        return m;
    }

    /// <summary>Shell panel material reusing the StageA Mars seam bake (albedo + normal).</summary>
    static Material PanelMat()
    {
        var m = Mat(PanelTint);
        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/MarsShellPanels.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/MarsShellPanels_N.png");
        if (albedo != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", albedo);
        if (normal != null && m.HasProperty("_BumpMap")) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
        if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", new Vector2(3f, 2f));
        return m;
    }
}
