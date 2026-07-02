using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using BlackCommission.Level;

/// <summary>
/// Mission map 3 — the Mars logistics hub (David-approved theme, 2026-07-02; design:
/// <c>design/levels/mars-logistics-01.md</c>). A seized freight transfer station on the
/// Martian surface: dust-storm exterior with a small drop-off pad, an airlock reception
/// plastered in seizure notices, a tall sodium-lit warehouse hall of rack aisles, a cold
/// storage annex (relic crates, monster nest) and an office mezzanine. Reuses the StageA
/// Mars panel-seam textures (<c>Art/Textures/MarsShellPanels{,_N}.png</c>).
///
/// Builds static geometry + LootAnchors + MonsterSeeds + the van exit point into the OPEN
/// scene. Run <c>MissionMapFinalizer</c> afterwards for the van visual + persisted NavMesh
/// + save. Idempotent: re-running replaces the previous build root.
///
/// Menu: <c>Tools ▸ Black Commission ▸ Map ▸ Build Mars Logistics (v1)</c>.
/// </summary>
public static class MarsLogisticsBuilder
{
    const string RootName = "MarsLogistics_v1";

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

    [MenuItem("Tools/Black Commission/Map/Build Mars Logistics (v1)")]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[MarsLogistics] Refusing to build in Play mode.");
            return;
        }
        MatCache.Clear();

        var old = GameObject.Find(RootName);
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject(RootName).transform;

        BuildExterior(Child(root, "Exterior"));
        BuildShell(Child(root, "Shell"));
        BuildAirlock(Child(root, "Airlock"));
        BuildHall(Child(root, "Hall"));
        BuildCold(Child(root, "ColdStorage"));
        BuildMezzanine(Child(root, "Mezzanine"));
        BuildLights(Child(root, "Lights"));
        BuildMissionAnchors(Child(root, "Mission"));
        ApplyAtmosphere();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("[MarsLogistics] Build complete. Run MissionMapFinalizer to bake navmesh + van, then save.");
    }

    // ── exterior: regolith apron, drop pad, storm backdrop ─────────────────────────
    static void BuildExterior(Transform p)
    {
        Box(p, "Regolith", new Vector3(21f, -0.55f, 10f), new Vector3(120f, 1.1f, 120f), Mat(Regolith));
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
        Box(p, "Storm_N", new Vector3(21f, 34f, 68f), new Vector3(130f, 70f, 0.5f), storm, collider: false);
        Box(p, "Storm_S", new Vector3(21f, 34f, -48f), new Vector3(130f, 70f, 0.5f), storm, collider: false);
        Box(p, "Storm_E", new Vector3(79f, 34f, 10f), new Vector3(0.5f, 70f, 130f), storm, collider: false);
        Box(p, "Storm_W", new Vector3(-37f, 34f, 10f), new Vector3(0.5f, 70f, 130f), storm, collider: false);
        Box(p, "Storm_Lid", new Vector3(21f, 66f, 10f), new Vector3(130f, 0.5f, 130f), storm, collider: false);

        // invisible perimeter blockers keep players inside the diegetic bowl
        Blocker(p, "Bound_N", new Vector3(21f, 3f, 55f), new Vector3(120f, 6f, 0.5f));
        Blocker(p, "Bound_S", new Vector3(21f, 3f, -30f), new Vector3(120f, 6f, 0.5f));
        Blocker(p, "Bound_E", new Vector3(68f, 3f, 10f), new Vector3(0.5f, 6f, 120f));
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
        WallX(p, wall, "Hall_N", 10f, 42f, 26f, 0f, 8f);
        WallZ(p, wall, "Hall_E", 6f, 26f, 42f, 0f, 8f);
        // hall/cold divider (h5 side): opening z15–17.5
        WallZ(p, wall, "Div_a", 6f, 15f, 10f, 0f, 5f);
        WallZ(p, wall, "Div_b", 17.5f, 26f, 10f, 0f, 5f);
        WallZ(p, wall, "Div_hdr", 15f, 17.5f, 10f, 2.6f, 5f);
        // wall band above the cold annex (hall is taller)
        WallZ(p, wall, "Div_band", 6f, 26f, 10f, 5f, 8f);

        // cold annex outer walls (h5)
        WallX(p, wall, "Cold_S", 0f, 10f, 6f, 0f, 5f);
        WallX(p, wall, "Cold_N", 0f, 10f, 26f, 0f, 5f);
        WallZ(p, wall, "Cold_W", 6f, 26f, 0f, 0f, 5f);

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

        // MonsterSeed — far NE aisle
        Seed(p, "MonsterSeed_ML_HALL", new Vector3(38f, 0.05f, 22f));
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

        // MonsterSeed — the nest, deepest corner
        Seed(p, "MonsterSeed_ML_COLD", new Vector3(2f, 0.05f, 23f));
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
        Box(p, "MezzRail_N", new Vector3(36f, 4.6f, 13.9f), new Vector3(12f, 0.9f, 0.08f), steel);
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
        dl.intensity = 0.45f;
        dl.shadows = LightShadows.Soft;

        // hall sodium pools (gaps between them stay genuinely dark — flashlight country)
        Point(p, "Sodium_A", new Vector3(14f, 6.5f, 11f), SodiumAmber, 3.6f, 14f);
        Point(p, "Sodium_B", new Vector3(26f, 6.5f, 15f), SodiumAmber, 3.6f, 14f);
        Point(p, "Sodium_C", new Vector3(36f, 6.5f, 20f), SodiumAmber, 3.3f, 13f);
        Point(p, "Sodium_D", new Vector3(18f, 6.5f, 21f), SodiumAmber, 3.0f, 12f);

        // airlock utility lamp
        Point(p, "AirLamp", new Vector3(21f, 2.7f, 3f), new Color(0.9f, 0.85f, 0.7f), 2.0f, 7f);

        // cold storage dying fluorescents
        Point(p, "Cold_A", new Vector3(5f, 4.2f, 12f), ColdCyan, 2.6f, 10f);
        Point(p, "Cold_B", new Vector3(4f, 4.2f, 22f), ColdCyan, 2.2f, 9f);
        Point(p, "Cold_C", new Vector3(8f, 4.2f, 17f), ColdCyan, 1.8f, 8f);

        // mezzanine: warm desk pool + phosphor green terminal glow
        Point(p, "MezzWarm", new Vector3(34f, 5.1f, 9f), new Color(1.0f, 0.8f, 0.5f), 2.0f, 6f);
        Point(p, "MezzCrt", new Vector3(38.5f, 4.9f, 11.5f), new Color(0.35f, 1.0f, 0.5f), 1.1f, 4f);

        // pad beacon so the return run reads from inside the door
        Point(p, "PadBeacon", new Vector3(21f, 3.2f, -8f), SodiumAmber, 1.8f, 10f);
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
        RenderSettings.ambientLight = new Color(0.34f, 0.27f, 0.20f);
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
