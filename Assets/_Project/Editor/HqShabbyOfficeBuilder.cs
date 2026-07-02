using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Builder for the APPROVED HQ direction「破旧事务所 + 车库湾」(plan v6 rev2, PM David 2026-07-01 —
/// the Mars-freight direction is RETIRED). Source of truth: design/hq/HQ_ShabbyOffice_Plan_v2.png
/// (data: tools/hq_shabby_office_plan_v1.py), revised after UX + game-design review.
///
///   • OFFICE HALL 8×7 m, ceiling 2.9 m (aged municipal paint, lino floor): SW rest corner
///     (sofa + folding table with the BESTIARY notebook + 4 spawn pads), west wall CRT terminal
///     desk + DEBT BOARD (the return-settle sightline through the curtain), north GEAR WALL
///     (supply cabinet with storage trigger / filing / tool rack), box clutter pinching aisles.
///   • The street FRONT DOOR is COURT-SEALED (boards + stamp-red tape + seizure notice) and a
///     stamped-paperwork PINBOARD hangs beside it — the civic-paperwork pillar's physical anchor.
///     The crew enters/leaves ONLY through the garage. S1 padlocked side door = license-as-leash.
///   • GARAGE BAY 7.2×8.2 m, ceiling 4.3 m (tin addition, pokes 1.2 m north): the DispatchVan
///     nose-to-door, board zone at the REAR, muster pad by the strip curtain, workbench/barrels/
///     tires/powerbox/oil stains. The 4.0 m roll-up FRAMES a dead-street backdrop (never entered).
///   • LIGHT LANGUAGE (pinned in review): tungsten pool (rest corner) / CRT phosphor green /
///     sodium (garage) / cold daylight (dirty south windows) + a sputtering fluorescent tube.
///     Atmosphere pass reused from the Mars tech: window/door god-shafts, drifting dust,
///     emissive CRT glass, LC grain/bloom/vignette/split-tone post.
///
/// Self-contained + ADDITIVE: does not touch other builders; deactivates (never deletes)
/// HQ_MarsWhitebox / HQ_OptionA + stray legacy geometry; repositions PlayerSpawnPoint +
/// MVP_OfficeComputer; preserves NetworkManager/UI/menu-camera objects. Does NOT auto-save.
/// Every prefab load falls back to a labelled box so the build never breaks.
///
/// Menu: Tools ▸ Black Commission ▸ Map ▸ Build HQ Shabby Office (v6)
///       Tools ▸ Black Commission ▸ Map ▸ Remove HQ Shabby Office (restore Mars)
/// </summary>
public static class HqShabbyOfficeBuilder
{
    const string RootName = "HQ_ShabbyOffice";
    const string SceneName = "HQ";

    // ---- plan v6 rev2 metres. Office x[0,8] z[0,7] h2.9; garage x[8,15.2] z[0,8.2] h4.3. ----
    const float OfficeH = 2.9f;
    const float GarageH = 4.3f;
    const float WallT = 0.3f;
    const float RollupW = 4.0f;   // x 9.6..13.6 on the garage north wall
    const float RollupH = 3.4f;
    // MEASURED (unity-model-fit, 2026-07-01): AS_OfficeVan is 5.14 long on its LOCAL X (2.19 wide,
    // 2.00 tall, floor pivot). Screenshot-verified: yaw 270 points the CAB north to the roll-up
    // (yaw 90 parked it nose-south into the boarding zone).
    const float VanYaw = 270f;

    const string TIR = "Assets/TirgamesAssets/Factory/Models/Materials/";
    const string TIRP = "Assets/TirgamesAssets/Factory/Prefabs/";
    const string GA = "GeneratedArt/";

    static Material mWall, mGarWall, mFloor, mGarFloor, mCeil, mSteel, mWood, mSeal, mAmber,
        mPaper, mGlass, mCurtain, mDark, mGreen;

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

    [MenuItem("Tools/Black Commission/Map/Build HQ Shabby Office (v6)")]
    public static void Build()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[HQ Shabby] Refusing to build during Play mode — the result would be discarded " +
                           "on exit (and half-applied to the running session). Exit Play and rebuild.");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName)
            Debug.LogWarning($"[HQ Shabby] Active scene is '{scene.name}', not '{SceneName}' — building anyway.");

        var existing = GameObject.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing);
        int retired = RetireLegacy();

        BuildPalette();
        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        BuildEnvelope(root.transform);
        BuildOffice(root.transform);
        BuildGarage(root.transform);
        BuildBackdrop(root.transform);
        ConfigureMood();
        BuildLights(root.transform);
        BuildPost(root.transform);
        BuildAtmosphere(root.transform);
        RepositionAnchors();

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        SceneView.FrameLastActiveSceneView();
        Debug.Log($"[HQ Shabby] Built the shabby office + garage bay (plan v6 rev2) into '{scene.name}' " +
                  $"(retired {retired} legacy objects). WALK IT: wake on the rest-corner bedrolls, read the debt " +
                  "board, take the job at the CRT, restock at the gear wall, push through the strip curtain and " +
                  "board the van's rear in the sodium bay — the roll-up frames the dead street. The front door is " +
                  "court-sealed. No auto-save (Ctrl+S if approved). Undo: Tools ▸ Black Commission ▸ Map ▸ Remove " +
                  "HQ Shabby Office (restore Mars).");
    }

    [MenuItem("Tools/Black Commission/Map/Remove HQ Shabby Office (restore Mars)")]
    public static void Remove()
    {
        var root = GameObject.Find(RootName);
        if (root != null) Object.DestroyImmediate(root);
        // Minimal undo: bring the previous (Mars) build back; deeper restore = git checkout HQ.unity.
        foreach (GameObject go in AllObjects())
            if (go.name == "HQ_MarsWhitebox" && !go.activeSelf) go.SetActive(true);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[HQ Shabby] Removed; HQ_MarsWhitebox reactivated. Exact baseline: `git checkout -- Assets/_Project/Scenes/HQ.unity`.");
    }

    // ----------------------------------------------------------------- envelope

    static void BuildEnvelope(Transform root)
    {
        var p = Sub(root, "Envelope");

        // Floors (tops at y=0) and roofs.
        Box(p, "Floor_Office", new Vector3(4.0f, -0.05f, 3.5f), new Vector3(8.0f, 0.1f, 7.0f), mFloor);
        Box(p, "Floor_Garage", new Vector3(11.6f, -0.05f, 4.1f), new Vector3(7.2f, 0.1f, 8.2f), mGarFloor);
        Box(p, "Roof_Office", new Vector3(3.925f, OfficeH + 0.1f, 3.5f), new Vector3(8.45f, 0.2f, 7.6f), mCeil);
        Box(p, "Roof_Garage", new Vector3(11.675f, GarageH + 0.1f, 4.1f), new Vector3(7.65f, 0.2f, 8.8f), mGarWall);

        // West wall (office) — S1 padlock door is face dressing, wall stays solid.
        Box(p, "Wall_W", new Vector3(-0.15f, OfficeH * 0.5f, 3.5f), new Vector3(WallT, OfficeH, 7.6f), mWall);
        Box(p, "S1_Door", new Vector3(0.06f, 1.05f, 1.3f), new Vector3(0.1f, 2.1f, 1.0f), mSteel, collider: false);
        Box(p, "S1_Padlock", new Vector3(0.12f, 1.1f, 1.72f), new Vector3(0.12f, 0.12f, 0.12f), mSeal, collider: false);

        // Office north wall + party wall (curtain opening z 2.6..5.0, h 2.4) + office roofline.
        Box(p, "Wall_N_Office", new Vector3(3.925f, OfficeH * 0.5f, 7.15f), new Vector3(8.45f, OfficeH, WallT), mWall);
        Box(p, "Wall_Party_S", new Vector3(8.0f, GarageH * 0.5f, 1.15f), new Vector3(WallT, GarageH, 2.9f), mWall);
        Box(p, "Wall_Party_N", new Vector3(8.0f, GarageH * 0.5f, 6.75f), new Vector3(WallT, GarageH, 3.5f), mWall);
        Box(p, "Wall_Party_Lintel", new Vector3(8.0f, 3.35f, 3.8f), new Vector3(WallT, 1.9f, 2.4f), mWall);
        BuildStripCurtain(p);

        // South wall — office: sealed door dressing + two dirty windows (sill 1.1, head 2.2).
        Box(p, "Wall_S_OffA", new Vector3(2.15f, OfficeH * 0.5f, -0.15f), new Vector3(4.9f, OfficeH, WallT), mWall);
        Box(p, "Win1_Sill", new Vector3(5.2f, 0.55f, -0.15f), new Vector3(1.2f, 1.1f, WallT), mWall);
        Box(p, "Win1_Head", new Vector3(5.2f, 2.55f, -0.15f), new Vector3(1.2f, 0.7f, WallT), mWall);
        Box(p, "Wall_S_OffB", new Vector3(6.0f, OfficeH * 0.5f, -0.15f), new Vector3(0.4f, OfficeH, WallT), mWall);
        Box(p, "Win2_Sill", new Vector3(6.8f, 0.55f, -0.15f), new Vector3(1.2f, 1.1f, WallT), mWall);
        Box(p, "Win2_Head", new Vector3(6.8f, 2.55f, -0.15f), new Vector3(1.2f, 0.7f, WallT), mWall);
        Box(p, "Wall_S_OffC", new Vector3(7.7f, OfficeH * 0.5f, -0.15f), new Vector3(0.6f, OfficeH, WallT), mWall);
        Box(p, "Win1_Glass", new Vector3(5.2f, 1.65f, -0.15f), new Vector3(1.2f, 1.1f, 0.08f), mGlass);
        Box(p, "Win2_Glass", new Vector3(6.8f, 1.65f, -0.15f), new Vector3(1.2f, 1.1f, 0.08f), mGlass);

        // Court-sealed front door (x 2.0..3.1): slab + boards + stamp-red tape X + notice.
        Box(p, "SealedDoor", new Vector3(2.55f, 1.05f, 0.06f), new Vector3(1.1f, 2.1f, 0.1f), mWood, collider: false);
        BoxEuler(p, "SealedBoard_A", new Vector3(2.55f, 1.25f, 0.13f), new Vector3(1.5f, 0.18f, 0.04f), new Vector3(0f, 0f, 22f), mWood, collider: false);
        BoxEuler(p, "SealedBoard_B", new Vector3(2.55f, 1.05f, 0.15f), new Vector3(1.5f, 0.18f, 0.04f), new Vector3(0f, 0f, -22f), mWood, collider: false);
        BoxEuler(p, "SealTape_A", new Vector3(2.55f, 1.55f, 0.17f), new Vector3(1.3f, 0.09f, 0.02f), new Vector3(0f, 0f, 14f), mSeal, collider: false);
        BoxEuler(p, "SealTape_B", new Vector3(2.55f, 1.55f, 0.18f), new Vector3(1.3f, 0.09f, 0.02f), new Vector3(0f, 0f, -14f), mSeal, collider: false);
        Box(p, "SeizureNotice", new Vector3(2.55f, 1.85f, 0.13f), new Vector3(0.32f, 0.42f, 0.02f), mPaper, collider: false);
        Box(p, "SeizureStamp", new Vector3(2.62f, 1.72f, 0.145f), new Vector3(0.12f, 0.12f, 0.01f), mSeal, collider: false);

        // South wall — garage (solid tin) + east wall + north wall with the roll-up opening.
        Box(p, "Wall_S_Garage", new Vector3(11.75f, GarageH * 0.5f, -0.15f), new Vector3(7.5f, GarageH, WallT), mGarWall);
        Box(p, "Wall_E", new Vector3(15.35f, GarageH * 0.5f, 4.1f), new Vector3(WallT, GarageH, 8.8f), mGarWall);
        Box(p, "Wall_N_GarW", new Vector3(8.65f, GarageH * 0.5f, 8.35f), new Vector3(1.9f, GarageH, WallT), mGarWall);
        Box(p, "Wall_N_GarE", new Vector3(14.55f, GarageH * 0.5f, 8.35f), new Vector3(1.9f, GarageH, WallT), mGarWall);
        Box(p, "Wall_N_GarLintel", new Vector3(11.6f, (RollupH + GarageH) * 0.5f, 8.35f), new Vector3(RollupW, GarageH - RollupH, WallT), mGarWall);
        // Retracted roll-up coil under the lintel (the door is OPEN — it frames the street).
        Box(p, "Rollup_Coil", new Vector3(11.6f, RollupH + 0.18f, 8.28f), new Vector3(RollupW + 0.2f, 0.36f, 0.36f), mSteel, collider: false);
        // Renderer-less collider across the threshold keeps players IN (framed view, never a path).
        var blocker = Box(p, "ThresholdBlocker", new Vector3(11.6f, 2.2f, 8.35f), new Vector3(RollupW, GarageH, 0.2f), mDark);
        var mr = blocker.GetComponent<MeshRenderer>(); if (mr != null) mr.enabled = false;
    }

    static void BuildStripCurtain(Transform p)
    {
        var c = Sub(p, "StripCurtain");
        for (int i = 0; i < 9; i++)
        {
            float z = 2.72f + 0.26f * i;
            Box(c, $"Strip_{i}", new Vector3(8.0f, 1.15f, z), new Vector3(0.06f, 2.3f, 0.24f), mCurtain, collider: false);
        }
    }

    // ----------------------------------------------------------------- office hall

    // All Y values + footprints derive from MEASURED prefab bounds (unity-model-fit, 2026-07-01):
    // AS_OfficeComputer = a COMPLETE desk unit 1.69×1.05×0.89 (floor pivot — no desk under it!);
    // AS_OfficeSofa = an L corner sofa 1.78×0.85×1.88; AS_OfficeDesk (folding table) top = 0.75;
    // AS_OfficeToolSet = a 2.3×2.06 monster (NOT a carton — replaced with primitive carton stacks).
    static void BuildOffice(Transform root)
    {
        var p = Sub(root, "Office");

        // Rest corner: the L sofa hugs the SW corner; folding table beside it carries the lamp +
        // bestiary notebook (both resting on the measured 0.75 m top); bedrolls under the windows.
        Prop(p, GA + "AS_OfficeSofa", "Sofa", new Vector3(0.95f, 0f, 1.0f), 0f, new Vector3(1.78f, 0.85f, 1.88f));
        Prop(p, GA + "AS_OfficeDesk", "FoldingTable", new Vector3(2.0f, 0f, 2.5f), 0f, new Vector3(1.11f, 0.75f, 0.57f));
        Prop(p, GA + "AS_LampDesk", "TableLamp", new Vector3(2.35f, 0.75f, 2.6f), 0f, new Vector3(0.32f, 0.45f, 0.32f));
        BuildBestiaryNotebook(p);
        float[] sx = { 4.2f, 5.0f, 4.2f, 5.0f };
        float[] sz = { 0.75f, 0.75f, 1.65f, 1.65f };
        for (int i = 0; i < 4; i++)
            Box(p, $"Bedroll_{i + 1}", new Vector3(sx[i], 0.03f, sz[i]), new Vector3(0.7f, 0.06f, 0.8f), mAmber, collider: false);

        // West wall: the CRT desk UNIT sits on the floor (screen faces +X); debt board at eye height.
        Prop(p, GA + "AS_OfficeComputer", "Computer_CRT", new Vector3(0.6f, 0f, 4.4f), 90f, new Vector3(0.89f, 1.05f, 1.69f));
        Prop(p, GA + "AS_OfficeDebtBoard", "DebtBoard", new Vector3(0.2f, 1.1f, 5.4f), 90f, new Vector3(0.07f, 0.9f, 1.55f));

        // Sentinel guards the sealed door's east side (the office's grim joke).
        Prop(p, GA + "AS_OfficeGasMaskSentinel", "Sentinel", new Vector3(3.35f, 0f, 0.55f), 195f, new Vector3(0.66f, 1.7f, 0.48f));

        // Gear wall (north): supply cabinet + storage trigger, filing cabinet, tool rack.
        Prop(p, GA + "AS_OfficeSupplyCabinet", "GearSupply", new Vector3(3.4f, 0f, 6.55f), 180f, new Vector3(1.78f, 1.5f, 0.81f));
        var cab = Box(p, "GearSupply_StorageTrigger", new Vector3(3.4f, 1.0f, 6.0f), new Vector3(1.3f, 1.7f, 0.5f), mGreen);
        var cmr = cab.GetComponent<MeshRenderer>(); if (cmr != null) cmr.enabled = false;
        cab.GetComponent<BoxCollider>().isTrigger = true;
        cab.AddComponent<OfficeCabinetStorage>();
        Prop(p, GA + "AS_OfficeFilingCabinet", "GearFiling", new Vector3(4.9f, 0f, 6.55f), 180f, new Vector3(0.77f, 1.25f, 0.44f));
        Prop(p, GA + "AS_OfficeToolRack", "GearToolRack", new Vector3(6.5f, 0.9f, 6.62f), 180f, new Vector3(2.13f, 1.3f, 0.34f));

        // Civic-paperwork pinboard (1.88 wide — the only wall run that fits it is above the sofa,
        // two steps from the sealed door): stamped notices, overdue slips, stamp-red chips.
        Prop(p, GA + "AS_OfficeSafetyBoard", "PaperworkBoard", new Vector3(1.0f, 1.45f, 0.08f), 0f, new Vector3(1.88f, 1.2f, 0.11f));
        Box(p, "Paperwork_StampA", new Vector3(0.7f, 1.7f, 0.17f), new Vector3(0.1f, 0.1f, 0.01f), mSeal, collider: false);
        Box(p, "Paperwork_StampB", new Vector3(1.35f, 1.35f, 0.17f), new Vector3(0.1f, 0.1f, 0.01f), mSeal, collider: false);

        // Clutter pinching the aisles: primitive carton stacks (the ToolSet prefab measures
        // 2.3×2.06 m — a room-blocker, not a carton) + fitted Tirgames debris.
        CartonStack(p, "BoxStack_A", new Vector3(6.9f, 0f, 1.1f), 15f);
        CartonStack(p, "BoxStack_B", new Vector3(3.3f, 0f, 5.3f), -25f);
        FitPrefab(p, TIRP + "Debris01_4.prefab", "Clutter_E", new Vector3(7.3f, 0f, 5.9f), 40f, new Vector3(0.8f, 0.5f, 1.4f));
        Prop(p, GA + "AS_OfficeFireExtinguisher", "FireExt", new Vector3(7.76f, 1.1f, 5.9f), -90f, new Vector3(0.16f, 0.55f, 0.78f));
    }

    // Three stacked cardboard cartons (aged-paper tones) — reads "broke office" at carton scale.
    static void CartonStack(Transform p, string name, Vector3 pos, float yaw)
    {
        var g = Sub(p, name);
        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var carton = Flat(new Color(0.60f, 0.50f, 0.36f));
        var cartonDark = Flat(new Color(0.50f, 0.41f, 0.30f));
        Box(g, "Carton_Base", new Vector3(0f, 0.25f, 0f), new Vector3(0.85f, 0.5f, 0.65f), carton);
        Box(g, "Carton_Mid", new Vector3(0.08f, 0.72f, 0.03f), new Vector3(0.7f, 0.44f, 0.6f), cartonDark);
        BoxEuler(g, "Carton_Top", new Vector3(-0.05f, 1.12f, -0.02f), new Vector3(0.5f, 0.36f, 0.5f), new Vector3(0f, 24f, 0f), carton, collider: false);
    }

    // The monster bestiary notebook lives ON the folding table (UX review: idle-read for the
    // non-host crew; OfficeMonsterBestiary is IInteractable and needs an aimable collider).
    static void BuildBestiaryNotebook(Transform p)
    {
        var cover = Box(p, "BestiaryNotebook", new Vector3(1.7f, 0.775f, 2.4f), new Vector3(0.34f, 0.05f, 0.26f),
            Flat(new Color(0.28f, 0.31f, 0.25f)));
        Box(cover.transform, "Pages", new Vector3(0f, 0.55f, 0f), new Vector3(0.92f, 0.5f, 0.92f),
            Flat(new Color(0.72f, 0.68f, 0.56f)), collider: false);
        cover.AddComponent<OfficeMonsterBestiary>();
    }

    // ----------------------------------------------------------------- garage bay

    static void BuildGarage(Transform root)
    {
        var p = Sub(root, "Garage");

        Prop(p, GA + "AS_OfficeVan", "DispatchVan", new Vector3(11.6f, 0f, 4.3f), VanYaw, new Vector3(2.4f, 2.4f, 5.0f));
        // Board trigger over the van REAR (solid, renderer hidden — PlayerInteraction aims at it).
        var board = Box(p, "Van_BoardZone", new Vector3(11.6f, 1.2f, 2.3f), new Vector3(2.9f, 2.4f, 2.6f), mGreen);
        var bmr = board.GetComponent<MeshRenderer>(); if (bmr != null) bmr.enabled = false;
        board.AddComponent<OfficeDepartureVan>();

        // Floor language: muster pad by the curtain, boarding pad at the rear (plan v2 depth 1.5).
        Box(p, "MusterPad", new Vector3(9.1f, 0.02f, 3.8f), new Vector3(1.6f, 0.04f, 2.0f), mAmber, collider: false);
        Box(p, "BoardingPad", new Vector3(11.6f, 0.02f, 1.05f), new Vector3(2.6f, 0.04f, 1.5f), mPaper, collider: false);

        // Clutter: workbench corner (E wall), barrels (SE), tires, powerbox on the party wall.
        Prop(p, GA + "AS_GarageWorkshopCorner", "Workbench", new Vector3(14.4f, 0f, 5.6f), -90f, new Vector3(1.2f, 1.4f, 2.2f));
        FitPrefab(p, TIRP + "Barrel01b.prefab", "Barrel_A", new Vector3(14.5f, 0f, 0.9f), 0f, new Vector3(0.6f, 0.9f, 0.6f));
        FitPrefab(p, TIRP + "Barrel01c.prefab", "Barrel_B", new Vector3(13.9f, 0f, 1.3f), 30f, new Vector3(0.6f, 0.9f, 0.6f));
        if (!FitPrefab(p, TIRP + "Tires01.prefab", "TireStack", new Vector3(14.6f, 0f, 3.0f), 0f, new Vector3(0.8f, 0.7f, 0.8f)))
            Box(p, "TireStack (FALLBACK)", new Vector3(14.6f, 0.35f, 3.0f), new Vector3(0.8f, 0.7f, 0.8f), Flat(new Color(0.10f, 0.10f, 0.11f)));
        FitPrefab(p, TIRP + "PowerBox01_1.prefab", "PowerBox", new Vector3(8.4f, 0f, 6.9f), 90f, new Vector3(0.5f, 1.1f, 0.7f));
        // Oil stains under/behind the van (Tirgames decal — URP-converted 2026-07-01).
        FitPrefab(p, TIRP + "DecalX1Y1.prefab", "OilStain_A", new Vector3(11.3f, 0.01f, 3.0f), 10f, new Vector3(1.6f, 0.02f, 1.6f));
        FitPrefab(p, TIRP + "DecalX1Y1.prefab", "OilStain_B", new Vector3(12.1f, 0.01f, 5.6f), 65f, new Vector3(1.2f, 0.02f, 1.2f));
    }

    // ----------------------------------------------------------------- dead-street backdrop (framed, never entered)

    static void BuildBackdrop(Transform root)
    {
        var p = Sub(root, "Backdrop_NonWalkable");
        Box(p, "Street_Ground", new Vector3(11.6f, -0.08f, 10.8f), new Vector3(16f, 0.1f, 5.0f), mDark, collider: false);
        // Chain-link fence line (posts + two rails — silhouette detail, no real mesh needed).
        for (int i = 0; i < 6; i++)
            Box(p, $"FencePost_{i}", new Vector3(5.6f + i * 2.4f, 0.9f, 11.8f), new Vector3(0.08f, 1.8f, 0.08f), mSteel, collider: false);
        Box(p, "FenceRail_Top", new Vector3(11.6f, 1.75f, 11.8f), new Vector3(12.2f, 0.05f, 0.05f), mSteel, collider: false);
        Box(p, "FenceRail_Mid", new Vector3(11.6f, 0.9f, 11.8f), new Vector3(12.2f, 0.04f, 0.04f), mSteel, collider: false);
        // Dumpster + debris + a dead streetlight; the opposite derelict block closes the view.
        Box(p, "Dumpster", new Vector3(9.0f, 0.65f, 10.4f), new Vector3(1.8f, 1.3f, 1.1f), mDark);
        FitPrefab(p, TIRP + "Debris01_2.prefab", "Street_Debris", new Vector3(13.6f, 0f, 10.2f), 105f, new Vector3(2.0f, 1.1f, 2.0f));
        Box(p, "DeadLamp_Pole", new Vector3(10.4f, 2.6f, 10.9f), new Vector3(0.12f, 5.2f, 0.12f), mSteel, collider: false);
        BoxEuler(p, "DeadLamp_Head", new Vector3(10.8f, 5.1f, 10.9f), new Vector3(0.7f, 0.18f, 0.3f), new Vector3(0f, 0f, -18f), mSteel, collider: false);
        Box(p, "OppositeBlock", new Vector3(11.6f, 3.2f, 13.2f), new Vector3(15f, 6.4f, 0.6f), mDark, collider: false);
    }

    // ----------------------------------------------------------------- mood / lights / post / atmosphere

    static void ConfigureMood()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Rgb(0x2E, 0x2C, 0x27);     // warm-dim (this is a HOME, not the Mars tomb)
        RenderSettings.ambientEquatorColor = Rgb(0x24, 0x22, 0x1E);
        RenderSettings.ambientGroundColor = Rgb(0x15, 0x14, 0x12);
        RenderSettings.ambientIntensity = 0.5f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.08f, 0.08f, 0.07f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 9f;
        RenderSettings.fogEndDistance = 34f;
    }

    static void BuildLights(Transform root)
    {
        var p = Sub(root, "Lights");

        // The four review-pinned pools, separated by real dark (no directional — full interior).
        AddLight(p, "Pool_Tungsten", new Vector3(1.5f, 2.4f, 1.2f), WarmTungsten, 1.3f, 5.5f);   // rest corner
        AddLight(p, "Pool_TableLamp", new Vector3(2.4f, 1.1f, 2.65f), WarmTungsten, 0.6f, 2.2f);
        AddLight(p, "Pool_CrtGreen", new Vector3(1.3f, 1.0f, 4.4f), CrtGreen, 0.9f, 2.4f);        // the live signal
        AddLight(p, "Pool_Sodium", new Vector3(10.0f, 3.9f, 2.6f), SodiumAmber, 1.7f, 8f);        // garage bay
        AddLight(p, "Win_Cold_A", new Vector3(5.2f, 1.8f, 0.5f), ColdDaylight, 0.8f, 3.6f);       // dirty windows
        AddLight(p, "Win_Cold_B", new Vector3(6.8f, 1.8f, 0.5f), ColdDaylight, 0.8f, 3.6f);
        AddLight(p, "Street_Glow", new Vector3(11.6f, 3.2f, 10.4f), Rgb(0x6E, 0x78, 0x82), 0.9f, 12f); // frames the door

        // The sputtering fluorescent tube over the office middle (the broke office's heartbeat).
        FitPrefab(p, TIRP + "LampCeiling01.prefab", "Tube_Office", new Vector3(4.5f, OfficeH - 0.12f, 3.8f), 0f, new Vector3(1.3f, 0.2f, 0.45f));
        var tube = AddLight(p, "Light_Fluorescent", new Vector3(4.5f, OfficeH - 0.3f, 3.8f), ColdIndustrial, 0.5f, 6.5f);
        tube.AddComponent<LightFlicker>().Configure(LightFlicker.Character.Sputter, 0.7f, 7f);
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
        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.28f); vig.smoothness.Override(0.42f);
        var split = profile.Add<SplitToning>(true);
        split.shadows.Override(Rgb(0x2E, 0x4A, 0x4E)); split.highlights.Override(Rgb(0xC8, 0x9A, 0x50));
        split.balance.Override(-15f);
        var ca = profile.Add<ChromaticAberration>(true);
        ca.intensity.Override(0.06f);

        var go = new GameObject("ShabbyPostVolume");
        go.transform.SetParent(root, false);
        var v = go.AddComponent<Volume>();
        v.isGlobal = true; v.priority = 10f; v.profile = profile;
    }

    static void BuildAtmosphere(Transform root)
    {
        var p = Sub(root, "Atmosphere");

        // God shafts: the two dirty windows rake cold light onto the office floor; the open
        // roll-up pushes a broad street glow into the bay.
        LightShaft(p, "Shaft_Win1", new Vector3(5.2f, 2.15f, 0.15f), new Vector3(5.2f, 0.05f, 2.1f), 1.15f, 1.9f, ColdDaylight, 0.09f);
        LightShaft(p, "Shaft_Win2", new Vector3(6.8f, 2.15f, 0.15f), new Vector3(6.8f, 0.05f, 2.1f), 1.15f, 1.9f, ColdDaylight, 0.09f);
        LightShaft(p, "Shaft_Rollup", new Vector3(11.6f, RollupH - 0.2f, 8.2f), new Vector3(11.6f, 0.05f, 5.4f), RollupW * 0.9f, RollupW * 1.2f, Rgb(0x8E, 0x98, 0xA6), 0.05f);

        // Drifting dust in the window light + the sodium pool.
        Dust(p, "Dust_Office", new Vector3(6.0f, 1.4f, 1.4f), new Vector3(3.4f, 2.4f, 2.6f), 9f);
        Dust(p, "Dust_Garage", new Vector3(10.8f, 2.0f, 3.2f), new Vector3(4.4f, 3.4f, 5.2f), 8f);

        // Emissive life: the CRT unit's own screen is lit (measured model); just the amber strip
        // over the curtain marks the way out.
        EmissiveQuad(p, "Curtain_Strip", new Vector3(7.82f, 2.5f, 3.8f), new Vector3(0.05f, 0.07f, 1.2f), SodiumAmber, 1.6f);
    }

    // ----------------------------------------------------------------- anchors / retire

    static void RepositionAnchors()
    {
        var spawn = GameObject.Find("PlayerSpawnPoint");
        if (spawn != null) spawn.transform.SetPositionAndRotation(new Vector3(4.6f, 0.1f, 1.2f), Quaternion.identity);
        // Terminal anchor at the CRT unit's screen face (unit spans x 0.155..1.045 at yaw 90),
        // forward = +X into the room (GetTerminalCameraPose + CrtMenuStage derive from this).
        var computer = GameObject.Find("MVP_OfficeComputer");
        if (computer != null) computer.transform.SetPositionAndRotation(new Vector3(1.0f, 0.95f, 4.4f), Quaternion.Euler(0f, 90f, 0f));
    }

    static int RetireLegacy()
    {
        int n = 0;
        foreach (string legacyRoot in new[] { "HQ_MarsWhitebox", "HQ_OptionA" })
        {
            var go = GameObject.Find(legacyRoot);
            if (go != null && go.activeSelf) { go.SetActive(false); n++; }
        }
        foreach (GameObject go in AllObjects())
        {
            if (!go.activeSelf || Keep.Contains(go.name)) continue;
            if (go.GetComponent<Renderer>() != null || go.GetComponent<Collider>() != null
                || go.GetComponent<Terrain>() != null) { go.SetActive(false); n++; }
        }
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

    // ----------------------------------------------------------------- palette + shared helpers

    static void BuildPalette()
    {
        mWall = Tint(TIR + "CommonConcreteWall02.mat", new Color(0.72f, 0.69f, 0.61f), 0.06f);   // aged municipal paint
        mGarWall = Tint(TIR + "CommonMetal01.mat", new Color(0.52f, 0.55f, 0.56f), 0.20f);        // tin addition
        mFloor = Tint(TIR + "CommonConcrete04.mat", new Color(0.44f, 0.46f, 0.41f), 0.18f);       // worn lino green-grey
        mGarFloor = M(TIR + "CommonConcrete04.mat", new Color(0.16f, 0.16f, 0.18f));              // raw slab
        mCeil = Flat(new Color(0.55f, 0.54f, 0.50f));
        mSteel = Tint(TIR + "CommonMetal01.mat", new Color(0.45f, 0.48f, 0.52f), 0.25f);
        mWood = Flat(new Color(0.42f, 0.33f, 0.22f));
        mSeal = Flat(new Color(0.66f, 0.18f, 0.16f));                                             // stamp red
        mAmber = Flat(new Color(0.78f, 0.62f, 0.30f));
        mPaper = Flat(new Color(0.80f, 0.75f, 0.62f));                                            // aged paper
        mGlass = GlassMaterial(new Color(0.72f, 0.78f, 0.75f, 0.45f));
        mCurtain = GlassMaterial(new Color(0.62f, 0.70f, 0.68f, 0.35f));
        mDark = Flat(new Color(0.15f, 0.16f, 0.15f));
        mGreen = Flat(new Color(0.42f, 0.85f, 0.40f));
    }

    static Material GlassMaterial(Color rgba)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetFloat("_Surface", 1f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        m.SetColor("_BaseColor", rgba);
        m.SetFloat("_Smoothness", 0.55f);
        ValidateUrpMaterial(m);
        return m;
    }

    /// <summary>URP ignores hand-set _Surface/_Blend floats until its editor-side
    /// ShaderUtils.UpdateMaterial reapplies the blend state — without this, every runtime-configured
    /// transparent material renders OPAQUE WHITE (screenshot-verified 2026-07-01). The type is
    /// internal, so this goes through reflection; a miss just logs (material stays opaque).</summary>
    static void ValidateUrpMaterial(Material m)
    {
        var su = System.Type.GetType("Unity.Rendering.Universal.ShaderUtils, Unity.RenderPipelines.Universal.Editor");
        var updateType = su?.GetNestedType("MaterialUpdateType",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (su == null || updateType == null)
        {
            Debug.LogWarning("[HQ Shabby] URP ShaderUtils not found — transparent materials may render opaque.");
            return;
        }
        System.Reflection.MethodInfo method = null;
        foreach (var mi in su.GetMethods(System.Reflection.BindingFlags.Public |
                     System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
            if (mi.Name == "UpdateMaterial" && mi.GetParameters().Length == 3
                && mi.GetParameters()[2].ParameterType.Name == "ShaderID") method = mi;
        if (method == null) return;
        object modified = System.Enum.Parse(updateType, "ModifiedMaterial");
        method.Invoke(null, new object[] { m, modified, method.GetParameters()[2].DefaultValue });
    }

    static Material M(string path, Color fallback)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (asset != null) return asset;
        Debug.LogWarning($"[HQ Shabby] Material not found, flat fallback: {path}");
        return Flat(fallback);
    }

    static Material Tint(string path, Color mul, float smoothness)
    {
        var src = AssetDatabase.LoadAssetAtPath<Material>(path);
        Material m = src != null ? new Material(src) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (src == null) Debug.LogWarning($"[HQ Shabby] Material missing, flat fallback: {path}");
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", mul);
        if (m.HasProperty("_Color")) m.SetColor("_Color", mul);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
        m.name = (src != null ? src.name : "Flat") + "_Shabby";
        return m;
    }

    static Material Flat(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
        return m;
    }

    static void Prop(Transform parent, string resPath, string name, Vector3 localPos, float yaw, Vector3 fallbackSize)
    {
        var prefab = Resources.Load<GameObject>(resPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[HQ Shabby] Prop missing: {resPath} — fallback box '{name}'.");
            var box = Box(parent, name + " (FALLBACK)", localPos + Vector3.up * fallbackSize.y * 0.5f, fallbackSize,
                Flat(new Color(0.30f, 0.28f, 0.24f)), collider: false);
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

    static bool FitPrefab(Transform parent, string assetPath, string name, Vector3 pos, float yaw, Vector3 target)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) { Debug.LogWarning($"[HQ Shabby] Prefab missing: {assetPath} — fallback for '{name}'."); return false; }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.name = name;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.transform.localPosition = Vector3.zero;

        Bounds b = MeshBounds(go);
        if (b.size == Vector3.zero) { go.transform.localPosition = pos; return true; }

        Vector3 s = b.size;
        go.transform.localScale = new Vector3(
            target.x / Mathf.Max(1e-4f, s.x), target.y / Mathf.Max(1e-4f, s.y), target.z / Mathf.Max(1e-4f, s.z));
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        b = MeshBounds(go);
        go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
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
        if (mat != null) go.GetComponent<MeshRenderer>().sharedMaterial = mat;
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

    static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

    // --- atmosphere helpers (shared recipe with HqMarsFreightWhitebox Stage B) ---

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
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    static Material ShaftMaterial(Color c, float alpha)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetFloat("_Surface", 1f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.One);
        m.SetInt("_ZWrite", 0);
        m.SetFloat("_Cull", (float)CullMode.Off);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        m.SetTexture("_BaseMap", ShaftGradientTexture());
        m.SetColor("_BaseColor", new Color(c.r, c.g, c.b, alpha));
        ValidateUrpMaterial(m);
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
                float a = Mathf.Sin(u * Mathf.PI) * Mathf.Pow(v, 1.4f);
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
        m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = 3000;
        m.SetTexture("_BaseMap", DustDotTexture());
        m.SetColor("_BaseColor", Color.white);
        ValidateUrpMaterial(m);
        rend.sharedMaterial = m;
        rend.shadowCastingMode = ShadowCastingMode.Off;
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
                float a = Mathf.Clamp01(1f - d);
                dustDotTex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
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
}
