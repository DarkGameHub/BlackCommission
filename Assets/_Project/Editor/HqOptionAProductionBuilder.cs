using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// PRODUCTION builder for the locked HQ floor plan "Option A — Long-Axis Z-Spine" (PM David, 2026-06-21),
/// built straight into the shipping <c>HQ.unity</c> (PM 2026-06-22: wants a directly production-ready HQ, not a
/// throwaway walk-test, using the in-project Tirgames Factory + Concrete kits — no downloads).
///
/// What it does (Phase 1 = OFFICE INTERIOR only; garage/dispatch-yard/vegetation = Phase 2):
///   • Builds the Option A wedge unit (9 m wide; +X wall 17 m, -X wall 19.5 m; canted far wall with a centred
///     4 m roll-up departure door) under a single root <c>HQ_OptionA</c> at world origin.
///   • SHELL = exact-dimension boxes wearing the real Tirgames Factory concrete/steel materials (production
///     surface, precise geometry — no guessed modular-wall tiling), with HERO MESHES from the Tirgames kit
///     auto-fitted by measured bounds (NO eyeballed transforms): <c>LargeGates1</c> as the roll-up door,
///     <c>Factory1Column</c> at the corners, <c>LampCeiling01</c> tubes. Every Tirgames load falls back to a
///     tinted box if the asset is missing, so the build never breaks.
///   • INTERIOR = the real authored <c>AS_Office*</c> prefabs on the Z-spine (positions reuse the plan-tuned
///     <c>HqPlaytestMenu</c> layout): CRT/computer + debt board (-X), gear wall (+X), folding table, sofa,
///     sentinel, fire extinguisher, muster pad + 4 spawn markers, S1 padlocked side door.
///   • LIGHT/POST = the HQ office look (concrete linear fog, Trilight ambient, cold-fluorescent / warm-tungsten
///     / sodium-amber pools with shadows OFF, one sputtering tube, LC grain/bloom/grade).
///   • Retires the OLD office WITHOUT deleting anything: deactivates the legacy shell/colliders/props
///     (<c>Shell*</c> / <c>BlenderHQ_*</c> / <c>HQ*Collider</c> / <c>Office_*</c> / <c>MVP_RestoredOfficeProps</c>
///     …). Deactivating <c>MVP_RestoredOfficeProps</c> also neutralizes <c>HqOfficePropRestorer</c> (its
///     SceneObjectExists checks see the inactive props and skip re-adding).
///   • PRESERVES the live dispatch loop: leaves NetworkManager / UI / cameras / MissionVanExitPoint /
///     MVP_RuntimeStyle_Office_ExteriorDispatch alone, and REPOSITIONS <c>PlayerSpawnPoint</c> and
///     <c>MVP_OfficeComputer</c> into the wedge so spawning + task selection still work.
///
/// Does NOT auto-save — it marks the scene dirty so you can walk it first, then Ctrl+S (or the menu logs how).
/// Re-runnable (idempotent). Reversible: <c>Tools ▸ Black Commission ▸ Map ▸ Restore Old HQ Office (undo
/// Option A)</c> reactivates the legacy office and removes the wedge.
///
/// Menu: Tools ▸ Black Commission ▸ Map ▸ Build Production HQ (Option A)
/// </summary>
public static class HqOptionAProductionBuilder
{
    const string RootName = "HQ_OptionA";
    const string SceneName = "HQ";

    // --- Unit footprint (world metres). Locked Option A wedge. Origin = near-left (-X/-Z) corner. ---
    // Shell polygon: (0,0) (9,0) (9,17) (0,19.5). +X = right, +Z = forward (toward the roll-up door).
    const float UnitW = 9f;        // X span
    const float DepthShort = 17f;  // +X wall length (z)
    const float DepthLong = 19.5f; // -X wall length (z)
    const float WallH = 3.6f;
    const float Wall = 0.2f;
    const float DoorW = 4f;        // roll-up opening
    const float DoorH = 3.2f;
    const float HqScale = 1.2f;     // enlarge the building + furniture ~20% vs the (map-locked 2 m) player — PM 2026-06-23

    // --- Tirgames Factory material library (the production surfaces; reused, not re-created). ---
    const string TIR = "Assets/TirgamesAssets/Factory/Models/Materials/";
    const string TIRP = "Assets/TirgamesAssets/Factory/Prefabs/";
    const string R = "GeneratedArt/"; // Resources.Load prefix for the authored office props

    // --- Per-prop facing (yaw, deg). Same plan-tuned best-guess as HqPlaytestMenu; tweak + re-run if wrong. ---
    static class Yaw
    {
        public const float Computer = 90f, DebtBoard = 90f, SafetyBoard = -90f, Gear = -90f, Sofa = 0f,
            Sentinel = 35f, Table = 0f, Extinguisher = -90f, Garage = -130f;
    }

    // --- Office light colours (from HqOfficeLightingPass / HqPlaytestMenu) ---
    static readonly Color ColdIndustrial = Rgb(0xD9, 0xE2, 0xDD);
    static readonly Color WarmTungsten = Rgb(0xFF, 0xBB, 0x73);
    static readonly Color DispatchGreen = Rgb(0x6C, 0xFF, 0x5F); // Art Bible §4 CRT phosphor green (was teal 0.20/0.95/0.65)
    static readonly Color FogConcrete = Rgb(0x2E, 0x30, 0x2D);

    // --- Municipal Debt Noir fallback palette (only if an authored asset/material is missing) ---
    static readonly Color cFloor = new(0.16f, 0.16f, 0.18f);
    static readonly Color cWall = new(0.34f, 0.35f, 0.36f);
    static readonly Color cRoof = new(0.22f, 0.22f, 0.24f);
    static readonly Color cShutter = new(0.34f, 0.26f, 0.16f);
    static readonly Color cPanel = new(0.20f, 0.20f, 0.22f);
    static readonly Color cPadlock = new(0.62f, 0.12f, 0.12f);
    static readonly Color cSpawn = new(0.62f, 0.55f, 0.20f);
    static readonly Color cProp = new(0.30f, 0.28f, 0.24f);

    // Legacy office objects to DEACTIVATE (retire, not delete). Prefixes + exact names from the HQ.unity
    // inventory. The KEEP set (logic/UI/anchors) is never touched.
    static readonly string[] RetirePrefixes =
    {
        "Shell", "BlenderHQ_", "Office_", "HQVoidFence", "HQOffice", "HQGarage", "HQSealed", "HQExterior",
        "ExteriorDispatchYard", "GarageExit",
    };
    static readonly HashSet<string> RetireExact = new()
    {
        "MVP_RestoredOfficeProps", "Wall_Left", "Wall_Right", "Wall_Front", "Wall_Back", "Floor", "Ceiling",
        "Desk", "Shelf", "Shelf_Top", "OldFan", "Sign_ZeroAccident", "Whiteboard", "DoorFrame",
        "Text_BlenderHQ_ComputerLabel", "DentedBayDoor",
    };
    // Never deactivate these even if a prefix would match (gameplay / networking / UI / cameras / anchors).
    static readonly HashSet<string> Keep = new()
    {
        "NetworkManager", "ConnectionManager", "DisconnectHandler", "MVP_OfficeComputer", "MVP_HUD", "HQUI",
        "SettlementUI", "MainMenu_UGUI", "HQMenuCamera", "HQSpawnManager", "PlayerSpawnPoint",
        "MVP_RuntimeStyle_Office_ExteriorDispatch", RootName,
    };

    [MenuItem("Tools/Black Commission/Map/Build Production HQ (Option A)")]
    public static void Build()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != SceneName)
        {
            if (!EditorUtility.DisplayDialog("Build Production HQ (Option A)",
                    $"Active scene is '{scene.name}', not '{SceneName}'. Open HQ.unity first.\n\nBuild anyway into " +
                    "the active scene?", "Build anyway", "Cancel"))
                return;
        }

        var existing = GameObject.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing);
        int retired = RetireLegacyOffice();
        var root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        BuildShell(root.transform);
        BuildInterior(root.transform);
        BuildDressing(root.transform);
        BuildYard(root.transform);
        BuildWilderness(root.transform);
        BuildVegetation(root.transform);
        ConfigureRenderSettings();
        EnsureLighting(root.transform);
        BuildPostVolume(root.transform);
        RepositionAnchors();

        // Enlarge the BUILDING + furniture ~20% vs the (mission-map-locked 2 m) player — PM 2026-06-23. Scale only
        // the building parents, NOT the Wilderness/Terrain (Unity Terrain ignores transform scale → it would desync
        // from a scaled building). The flat terrain pad (HqFlatMask) is sized to cover the scaled footprint.
        foreach (var n in new[] { "Shell", "Interior", "Dressing", "Yard" })
        {
            var t = root.transform.Find(n);
            if (t != null) t.localScale = Vector3.one * HqScale;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        SceneView.FrameLastActiveSceneView();

        Debug.Log($"[HQ Option A] Built the production wedge into '{scene.name}' (retired {retired} legacy office " +
                  "objects, dispatch loop preserved). WALK IT: press Play (▶) — you spawn on the team line looking " +
                  "down the spine (CRT + debt board LEFT, gear wall RIGHT, roll-up door dead AHEAD). Then SAVE: " +
                  "Ctrl+S (this menu does not auto-save). To revert: Tools ▸ Black Commission ▸ Map ▸ Restore Old " +
                  "HQ Office (undo Option A).");
    }

    [MenuItem("Tools/Black Commission/Map/Restore Old HQ Office (undo Option A)")]
    public static void RestoreOld()
    {
        var optionA = GameObject.Find(RootName);
        if (optionA != null) Object.DestroyImmediate(optionA);

        int restored = 0;
        foreach (GameObject go in AllObjects())
        {
            if (!go.activeSelf && (go.GetComponent<Renderer>() != null || go.GetComponent<Collider>() != null))
            {
                go.SetActive(true);
                restored++;
            }
        }
        var van = GameObject.Find("MVP_RuntimeStyle_Office_ExteriorDispatch");
        if (van != null)
            foreach (var r in van.GetComponents<Renderer>())
                r.enabled = true;
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[HQ Option A] Reverted: removed the wedge, reactivated {restored} legacy objects + van mesh. " +
                  "For an exact baseline, `git checkout -- Assets/_Project/Scenes/HQ.unity`.");
    }

    // ----------------------------------------------------------------- retire / preserve

    // Hide ALL legacy visible/solid geometry (walls, furniture, colliders) by deactivating any ACTIVE object
    // that carries a Renderer or Collider and is NOT a kept logic/UI/camera/van object. NAME-AGNOSTIC on
    // purpose — the earlier name-prefix retire missed props the PM had hand-placed, leaving furniture "outdoors".
    // HQ_OptionA is destroyed BEFORE this runs, so the new wedge is never touched. (Lights/UI have no Renderer
    // or Collider, so they survive.)
    static int RetireLegacyOffice()
    {
        int n = 0;
        foreach (GameObject go in AllObjects())
        {
            if (!go.activeSelf || Keep.Contains(go.name)) continue;
            if (go.GetComponent<Renderer>() != null || go.GetComponent<Collider>() != null)
            {
                go.SetActive(false);
                n++;
            }
        }
        // The dispatch object stays ACTIVE (it carries the HQ menu Camera) — just hide its own legacy office
        // mesh so it doesn't show through the new wedge.
        var van = GameObject.Find("MVP_RuntimeStyle_Office_ExteriorDispatch");
        if (van != null)
            foreach (var r in van.GetComponents<Renderer>())
                r.enabled = false;

        // Re-home boarding to the VISIBLE yard van (PM 2026-06-23): strip the OLD invisible OfficeDepartureVan
        // interactable(s) and disable the old dispatch colliders so no ghost target remains. BuildYard then
        // adds exactly one OfficeDepartureVan on the van's VanBoardZone. The menu Camera is untouched.
        foreach (var dv in Object.FindObjectsByType<OfficeDepartureVan>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(dv);
        if (van != null)
            foreach (var col in van.GetComponentsInChildren<Collider>())
                col.enabled = false;
        return n;
    }

    static bool ShouldRetire(string name)
    {
        if (Keep.Contains(name)) return false;
        if (RetireExact.Contains(name)) return true;
        foreach (var p in RetirePrefixes)
            if (name.StartsWith(p)) return true;
        return false;
    }

    // Reposition the preserved gameplay anchors into the Option A wedge so spawn + task-select still work.
    static void RepositionAnchors()
    {
        var spawn = GameObject.Find("PlayerSpawnPoint");
        if (spawn != null) spawn.transform.SetPositionAndRotation(new Vector3(4.5f, 0.1f, 2.4f) * HqScale,
            Quaternion.Euler(0f, 0f, 0f)); // on the team line, facing +Z down the spine toward the door (scaled with the building)

        // The CRT/task computer interactable sits on the -X wall at the start of the spine (matches the AS_OfficeComputer prop).
        var computer = GameObject.Find("MVP_OfficeComputer");
        if (computer != null) computer.transform.SetPositionAndRotation(new Vector3(1.2f, 1.0f, 6.0f) * HqScale,
            Quaternion.Euler(0f, 90f, 0f));
    }

    // ----------------------------------------------------------------- shell

    static void BuildShell(Transform root)
    {
        var p = Sub(root, "Shell");
        // 混凝土公务站房 palette (PM 2026-06-23): polished concrete floor (was the FactoryPropsGround dirt texture —
        // the worst offender), poured concrete walls, corrugated steel roof.
        var floorMat = M(TIR + "CommonConcrete04.mat", cFloor);
        var roofMat = M(TIR + "CommonSteelRoof01.mat", cRoof);
        var wallMat = M(TIR + "CommonConcreteWall02.mat", cWall);
        float cy = WallH * 0.5f;
        float midZ = DepthLong * 0.5f;

        Box(p, "Floor", new Vector3(UnitW * 0.5f, -0.1f, midZ), new Vector3(UnitW + 0.6f, 0.2f, DepthLong + 0.8f), floorMat);
        Box(p, "Roof", new Vector3(UnitW * 0.5f, WallH + 0.1f, midZ), new Vector3(UnitW + 0.6f, 0.2f, DepthLong + 0.8f), roofMat);
        Box(p, "Wall_-X(left)", new Vector3(-Wall * 0.5f, cy, DepthLong * 0.5f), new Vector3(Wall, WallH, DepthLong), wallMat);
        Box(p, "Wall_+X(right)", new Vector3(UnitW + Wall * 0.5f, cy, DepthShort * 0.5f), new Vector3(Wall, WallH, DepthShort), wallMat);
        Box(p, "Wall_-Z(back)", new Vector3(UnitW * 0.5f, cy, -Wall * 0.5f), new Vector3(UnitW + Wall, WallH, Wall), wallMat);

        // Canted far wall (9,17) -> (0,19.5) with the centred 4 m roll-up opening.
        Vector2 a = new(UnitW, DepthShort), b = new(0f, DepthLong);
        Vector2 mid = (a + b) * 0.5f, dir = (b - a).normalized;
        float len = (b - a).magnitude;
        float yaw = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;
        float half = len * 0.5f, openHalf = DoorW * 0.5f, segLen = half - openHalf;
        Vector3 OnWall(float t, float y) => new(mid.x + dir.x * t, y, mid.y + dir.y * t);

        BoxRot(p, "Wall_Far_L", OnWall(-(half + openHalf) * 0.5f, cy), new Vector3(segLen, WallH, Wall), yaw, wallMat);
        BoxRot(p, "Wall_Far_R", OnWall((half + openHalf) * 0.5f, cy), new Vector3(segLen, WallH, Wall), yaw, wallMat);
        BoxRot(p, "Wall_Far_Lintel", OnWall(0f, (DoorH + WallH) * 0.5f), new Vector3(DoorW, WallH - DoorH, Wall), yaw, wallMat);

        // Roll-up departure door on a clean identity-scaled WRAPPER that carries the yaw, the sealing/aim
        // collider, and the reveal — so the panel mesh + collider roll up together. (FitPrefab strips the
        // LargeGates mesh colliders, so the wrapper supplies one; the box fallback's own collider is off.)
        // Manual [E] reveal (PM 2026-06-23): the team rolls the bay door up themselves before they board.
        var door = new GameObject("RollUpDoor");
        door.transform.SetParent(p, false);
        door.transform.SetPositionAndRotation(OnWall(0f, 0f), Quaternion.Euler(0f, yaw, 0f));

        // Children carry yaw=0 — the wrapper already provides it (else FitPrefab's local yaw would double it).
        if (!FitPrefab(door.transform, TIRP + "LargeGates1_2.prefab", "RollUpDoor_CLOSED", OnWall(0f, 0f), 0f,
                new Vector3(DoorW, DoorH, 0.3f)))
            BoxRot(door.transform, "RollUpDoor_CLOSED", new Vector3(0f, DoorH * 0.5f, 0f),
                new Vector3(DoorW, DoorH, 0.14f), 0f, M(TIR + "LargeGates1.mat", cShutter), collider: false);

        var doorCol = door.AddComponent<BoxCollider>();
        doorCol.center = new Vector3(0f, DoorH * 0.5f, 0f);
        doorCol.size = new Vector3(DoorW, DoorH, 0.3f);
        door.AddComponent<HqRollUpDoorReveal>().Configure(HqRollUpDoorReveal.Trigger.Manual, DoorH, 1.6f);

        // Corner columns (real Tirgames mesh, fit to wall height) for production read at the four corners.
        float[] cxs = { 0.25f, UnitW - 0.25f };
        float[] czs = { 0.4f, DepthShort - 0.6f };
        int ci = 0;
        foreach (var cx in cxs)
            foreach (var cz in czs)
                FitPrefab(p, TIRP + "Factory1Column02.prefab", $"Column_{ci++}", new Vector3(cx, 0f, cz), 0f,
                    new Vector3(0.6f, WallH, 0.6f), uniformByHeight: true);
    }

    // ----------------------------------------------------------------- interior (Z-spine, real AS_Office props)

    static void BuildInterior(Transform root)
    {
        var p = Sub(root, "Interior");

        // LEFT (-X): debt/takeover board, CRT computer, file boxes, sentinel
        Prop(p, R + "AS_OfficeComputer", "Computer_CRT", new Vector3(1.0f, 0f, 6.0f), Yaw.Computer, new Vector3(1.4f, 1.05f, 0.9f));
        Prop(p, R + "AS_OfficeDebtBoard", "DebtBoard", new Vector3(0.22f, 1.25f, 3.6f), Yaw.DebtBoard, new Vector3(0.1f, 0.9f, 1.6f));
        Prop(p, R + "AS_OfficeToolSet", "FileBoxes", new Vector3(1.0f, 0f, 4.7f), 20f, new Vector3(0.8f, 0.6f, 0.8f));
        Prop(p, R + "AS_OfficeGasMaskSentinel", "Sentinel", new Vector3(1.0f, 0f, 2.2f), Yaw.Sentinel, new Vector3(0.7f, 1.7f, 0.7f));

        // RIGHT (+X): gear wall
        Prop(p, R + "AS_OfficeSupplyCabinet", "Gear_Supply", new Vector3(8.45f, 0f, 12.4f), Yaw.Gear, new Vector3(0.6f, 1.5f, 1.2f));
        Prop(p, R + "AS_OfficeFilingCabinet", "Gear_Filing", new Vector3(8.55f, 0f, 10.8f), Yaw.Gear, new Vector3(0.6f, 1.25f, 0.9f));
        Prop(p, R + "AS_OfficeToolRack", "Gear_ToolRack", new Vector3(8.78f, 0.85f, 13.9f), Yaw.Gear, new Vector3(0.15f, 1.3f, 1.1f));
        Prop(p, R + "AS_OfficeSafetyBoard", "SafetyBoard", new Vector3(8.8f, 1.15f, 9.5f), Yaw.SafetyBoard, new Vector3(0.1f, 1.2f, 1.4f));
        Prop(p, R + "AS_OfficeFireExtinguisher", "FireExt", new Vector3(8.5f, 0f, 16.0f), Yaw.Extinguisher, new Vector3(0.5f, 0.55f, 0.5f));
        Prop(p, R + "AS_GarageWorkshopCorner", "GarageCorner", new Vector3(7.6f, 0f, 15.2f), Yaw.Garage, new Vector3(1.4f, 1.4f, 1.4f));

        // centre spine: folding table (+ desk lamp), sofa against the back wall
        Prop(p, R + "AS_OfficeDesk", "FoldingTable", new Vector3(2.2f, 0f, 10.6f), Yaw.Table, new Vector3(1.2f, 0.75f, 0.8f));
        Prop(p, R + "AS_LampDesk", "DeskLamp", new Vector3(2.55f, 0.75f, 10.6f), 0f, new Vector3(0.3f, 0.45f, 0.3f));
        Prop(p, R + "AS_OfficeSofa", "Sofa", new Vector3(6.9f, 0f, 1.1f), Yaw.Sofa, new Vector3(1.8f, 0.85f, 0.8f));

        // S1 padlocked side door on the -X wall (peripheral, off-path, license-as-leash)
        Box(p, "S1_PadlockDoor_LOCKED", new Vector3(0.16f, 1.05f, 15.9f), new Vector3(0.12f, 2.1f, 1.1f), M(TIR + "DoorIndustrial01.mat", cPanel));
        Box(p, "S1_Padlock", new Vector3(0.34f, 1.1f, 15.9f), new Vector3(0.14f, 0.14f, 0.14f), Flat(cPadlock), collider: false);

        // muster pad + 4 spawn anchors (floor markings, no colliders)
        var markMat = M(TIR + "CommonMetalPainted01.mat", cSpawn);
        Box(p, "MusterPad", new Vector3(4.5f, 0.02f, 16.3f), new Vector3(3.0f, 0.04f, 2.2f), markMat, collider: false);
        Box(p, "SPAWN_1", new Vector3(2.2f, 0.03f, 2.2f), new Vector3(0.6f, 0.04f, 0.6f), markMat, collider: false);
        Box(p, "SPAWN_2", new Vector3(4.0f, 0.03f, 2.2f), new Vector3(0.6f, 0.04f, 0.6f), markMat, collider: false);
        Box(p, "SPAWN_3", new Vector3(5.6f, 0.03f, 2.2f), new Vector3(0.6f, 0.04f, 0.6f), markMat, collider: false);
        Box(p, "SPAWN_4", new Vector3(7.0f, 0.03f, 2.2f), new Vector3(0.6f, 0.04f, 0.6f), markMat, collider: false);
    }

    // ----------------------------------------------------------------- industrial dressing (Tirgames meshes)

    static void BuildDressing(Transform root)
    {
        var p = Sub(root, "Dressing");
        // On the flanks / corners so the central spine stays clear. Each auto-grounds; box fallback if missing.
        FitPrefab(p, TIRP + "PowerBox01_1.prefab", "PowerBox", new Vector3(0.55f, 0f, 9.0f), 90f, new Vector3(0.5f, 1.1f, 0.4f));
        FitPrefab(p, TIRP + "MetalCabinet01_1.prefab", "MetalCabinet", new Vector3(8.5f, 0f, 5.5f), -90f, new Vector3(0.9f, 1.9f, 0.6f));
        FitPrefab(p, TIRP + "Barrel01b.prefab", "Barrels_A", new Vector3(8.2f, 0f, 17.4f), 0f, new Vector3(0.6f, 0.9f, 0.6f));
        FitPrefab(p, TIRP + "Barrel01c.prefab", "Barrels_B", new Vector3(7.5f, 0f, 17.6f), 35f, new Vector3(0.6f, 0.9f, 0.6f));
    }

    // ----------------------------------------------------------------- dispatch yard (per the HQ map)

    /// <summary>
    /// Outdoor DISPATCH YARD matching design/mockups/hq-dispatch-yard-plan-v1.png (room ~25x30 m: fenced
    /// perimeter, exit gate at the far end, the van parked by the roll-up door, hazard markings). Sits north of
    /// the wedge's roll-up door (+Z). Vegetation + nicer wire fence = later polish; this nails the map's SPACE
    /// + key features so it stops reading as a void pad.
    /// </summary>
    static void BuildYard(Transform root)
    {
        var p = Sub(root, "Yard");
        float cx = UnitW * 0.5f;          // 4.5 - yard centre x, aligned to the building + door
        const float YW = 24f, YD = 26f;   // yard footprint approx the map's ~25x30 m
        const float z0 = 18f;             // starts just outside the roll-up door
        float zN = z0 + YD;               // north (exit) edge
        float zMid = z0 + YD * 0.5f;
        float xW = cx - YW * 0.5f, xE = cx + YW * 0.5f;

        var groundMat = M(TIR + "CommonConcrete05.mat", new Color(0.11f, 0.10f, 0.09f)); // near-black asphalt (was reading as a light test pad)
        var wallMat = M(TIR + "CommonConcreteWall01.mat", cWall);
        var markMat = M(TIR + "CommonMetalPainted01.mat", cSpawn);

        // Asphalt yard.
        Box(p, "YardGround", new Vector3(cx, -0.05f, zMid), new Vector3(YW, 0.1f, YD), groundMat);

        // Low concrete perimeter - E/W full length + N split for the exit gate; the building closes the south.
        const float wH = 1.3f, wT = 0.3f, gateW = 6f;
        float segW = (YW - gateW) * 0.5f;
        Box(p, "Yard_WallW", new Vector3(xW, wH * 0.5f, zMid), new Vector3(wT, wH, YD), wallMat);
        Box(p, "Yard_WallE", new Vector3(xE, wH * 0.5f, zMid), new Vector3(wT, wH, YD), wallMat);
        Box(p, "Yard_WallN_L", new Vector3(cx - (gateW + segW) * 0.5f, wH * 0.5f, zN), new Vector3(segW, wH, wT), wallMat);
        Box(p, "Yard_WallN_R", new Vector3(cx + (gateW + segW) * 0.5f, wH * 0.5f, zN), new Vector3(segW, wH, wT), wallMat);

        // Exit gate (real Tirgames LargeGates) in the north gap; box fallback if the prefab is missing.
        if (!FitPrefab(p, TIRP + "LargeGates1_1.prefab", "Yard_ExitGate", new Vector3(cx, 0f, zN), 0f, new Vector3(gateW, 3.0f, 0.3f)))
            Box(p, "Yard_ExitGate", new Vector3(cx, 1.5f, zN), new Vector3(gateW, 3.0f, 0.2f), M(TIR + "LargeGates1.mat", cShutter));

        // Dispatch van parked just outside the roll-up door, nose toward the door (the team boards it here).
        Prop(p, R + "AS_OfficeVan", "DispatchVan", new Vector3(cx, 0f, z0 + 4f), 0f, new Vector3(2.2f, 2.0f, 5.0f));
        Box(p, "Yard_VanBay", new Vector3(cx, 0.02f, z0 + 4f), new Vector3(3.2f, 0.04f, 6f), markMat, collider: false);

        // BOARD ZONE: the VISIBLE van is what you aim at + press E to board (PM 2026-06-23). Prop strips the
        // van's imported colliders, so a measured BoxCollider on a child gives it a solid, aim-able body that
        // carries the re-homed OfficeDepartureVan. The old invisible dispatch interactable is stripped in
        // RetireLegacyOffice, so this is the only boarding target. Van is placed at scale 1 / yaw 0, so the
        // world AABB maps to the local collider with just a position offset (no rotation/scale distortion).
        var vanGo = p.Find("DispatchVan");
        if (vanGo != null)
        {
            Bounds vb = MeshBounds(vanGo.gameObject);
            if (vb.size != Vector3.zero)
            {
                var zone = new GameObject("VanBoardZone");
                zone.transform.SetParent(vanGo, false);
                var bc = zone.AddComponent<BoxCollider>();
                bc.center = vanGo.InverseTransformPoint(vb.center);
                bc.size = vb.size;
                zone.AddComponent<OfficeDepartureVan>();
            }
        }

        // Asymmetric sodium lights (art-direction): gate threshold (departure framing) + van bay (ritual staging)
        // + a dim west flank (deepens the east shadow). Sodium = #D9A850 / #C89040 only.
        PointLight(p, "Yard_Sodium_Gate", new Vector3(4.5f, 7.0f, 43.5f), Rgb(0xD9, 0xA8, 0x50), 1.0f, 16f);
        PointLight(p, "Yard_Sodium_VanBay", new Vector3(4.5f, 5.5f, 22.5f), Rgb(0xD9, 0xA8, 0x50), 0.85f, 12f);
        PointLight(p, "Yard_Sodium_Flank", new Vector3(-5.5f, 4.5f, 30.0f), Rgb(0xC8, 0x90, 0x40), 0.5f, 9f);

        // Industrial grit on the flanks (clear of the van lane x≈2–7): power box, gas tanks, barrels, debris.
        FitPrefab(p, TIRP + "PowerBox02_1.prefab", "Yard_PowerBox", new Vector3(xW + 1.4f, 0f, z0 + 2.5f), 90f, new Vector3(0.6f, 1.3f, 0.5f));
        FitPrefab(p, TIRP + "GasBallone01_2.prefab", "Yard_GasTank_A", new Vector3(8.8f, 0f, 22.0f), -30f, new Vector3(0.6f, 1.2f, 0.6f));
        FitPrefab(p, TIRP + "GasBallone01_2.prefab", "Yard_GasTank_B", new Vector3(8.3f, 0f, 23.2f), -60f, new Vector3(0.6f, 1.2f, 0.6f));
        FitPrefab(p, TIRP + "Barrel01b.prefab", "Yard_Barrel_A", new Vector3(-6.0f, 0f, 38f), 0f, new Vector3(0.6f, 0.9f, 0.6f));
        FitPrefab(p, TIRP + "Barrel01c.prefab", "Yard_Barrel_B", new Vector3(-5.4f, 0f, 39f), 25f, new Vector3(0.6f, 0.9f, 0.6f));
        FitPrefab(p, TIRP + "Barrel01b.prefab", "Yard_Barrel_C", new Vector3(16.0f, 0f, 28f), 15f, new Vector3(0.6f, 0.9f, 0.6f));
        FitPrefab(p, TIRP + "Debris01_2.prefab", "Yard_Debris_A", new Vector3(-6.5f, 0f, 22f), 15f, new Vector3(0.7f, 0.45f, 0.7f));
        FitPrefab(p, TIRP + "Debris01_5.prefab", "Yard_Debris_B", new Vector3(15.5f, 0f, 40f), 110f, new Vector3(0.7f, 0.45f, 0.7f));
        FitPrefab(p, TIRP + "Debris01_8.prefab", "Yard_Debris_C", new Vector3(2.5f, 0f, 43.5f), 5f, new Vector3(0.7f, 0.45f, 0.7f));

        // Dead floodlight poles (broken — no Light component).
        DeadPole(p, -6.0f, 30.0f);
        DeadPole(p, 15.0f, 36.0f);

        // Cracked asphalt streaks (collider-free decals on the yard surface).
        var crackMat = M(TIR + "CommonConcrete05.mat", new Color(0.08f, 0.08f, 0.09f));
        float[] ckX = { -2f, 1f, 7f, 13f }, ckZ = { 26f, 34f, 25f, 29f }, ckYaw = { 12f, 20f, -15f, 28f };
        for (int i = 0; i < ckX.Length; i++)
            BoxRot(p, "Crack_" + i, new Vector3(ckX[i], -0.04f, ckZ[i]), new Vector3(0.15f, 0.02f, 4.0f + i * 0.4f), ckYaw[i], crackMat, collider: false);
    }

    // ----------------------------------------------------------------- wilderness backdrop (the office sits in the wild)

    /// <summary>
    /// Non-playable WILDERNESS wrapping the HQ (PM 2026-06-23: the commission office sits out in the wild, not in a
    /// derelict urban district). A real undulating Unity Terrain (grass heightmap + GPU-instanced detail grass — the
    /// same tech as the mission map's <c>MapSiteBuilder.BuildTerrain</c>, replicated here so the HQ builder stays
    /// self-contained) sits FLAT under the building + yard + the van's dirt track and rolls out into the fog
    /// elsewhere. A worn dirt track runs north from the yard gate (the van's way out); boulders give silhouette; a
    /// faint cool moon fill lifts the wild without flattening the office's warm pools. The dead treeline + scrub are
    /// seeded in <see cref="BuildVegetation"/>. FoliageSet/grass-layer absent ⇒ a flat grass-mesh fallback so the
    /// build never breaks. (NOTE: not scaled by HqScale — Unity Terrain ignores transform scale.)
    /// </summary>
    static void BuildWilderness(Transform root)
    {
        var p = Sub(root, "Wilderness");
        var fset = AssetDatabase.LoadAssetAtPath<BlackCommission.Level.FoliageSet>("Assets/Resources/FoliageSet.asset");

        if (fset != null && fset.grassLayer != null)
            BuildHqTerrain(p, fset);
        else
        {
            var grass = M("Assets/Resources/GrassGround.mat", new Color(0.17f, 0.19f, 0.14f)); // cDead dead-olive, not green
            Box(p, "WildGround", new Vector3(WildCx, -0.06f, WildCz), new Vector3(WildW, 0.12f, WildD), grass);
        }

        // The van's dirt track north out of the yard gate into the wild (the flat pad keeps it level).
        var dirtMat = M(TIR + "CommonConcrete05.mat", new Color(0.21f, 0.17f, 0.11f)); // lighter brown-tan, distinct from the near-black yard asphalt
        Box(p, "DirtTrack", new Vector3(4.5f, 0.015f, 96f), new Vector3(6.0f, 0.05f, 104f), dirtMat, collider: false);

        // Distant boulders for silhouette/scale through the fog (whitebox-safe — no prefab dependency), grounded on
        // the rolling terrain and sunk ~0.5 m so they read as outcrops, not floating blocks.
        var rockMat = M(TIR + "CommonConcrete04.mat", new Color(0.21f, 0.21f, 0.20f));
        BoxRot(p, "Boulder_A", new Vector3(-40f, HqGroundY(-40f, 72f) + 1.1f, 72f), new Vector3(7f, 3.2f, 6f), 24f, rockMat);
        BoxRot(p, "Boulder_B", new Vector3(48f, HqGroundY(48f, 58f) + 0.7f, 58f), new Vector3(5.5f, 2.4f, 5f), -36f, rockMat);
        BoxRot(p, "Boulder_C", new Vector3(30f, HqGroundY(30f, -36f) + 0.6f, -36f), new Vector3(6f, 2.2f, 5.5f), 12f, rockMat);

        // Faint cool moon fill so the wild reads at night without washing out the office's warm interior pools.
        PointLight(p, "Wild_MoonFill", new Vector3(4.5f, 28f, 44f), Rgb(0x5A, 0x68, 0x80), 0.35f, 130f);
    }

    // Terrain footprint (world): wide open country; the office + yard sit near the south end.
    const float WildCx = 4.5f, WildCz = 30f, WildW = 220f, WildD = 220f;
    const float TBASE = -4f, HRANGE = 8f; // terrain base/vertical span; HqGroundY ∈ ≈[-1.9,1.9] sits inside

    // Replicated from MapSiteBuilder.BuildTerrain (self-contained so HQ doesn't depend on mission-map code): a Unity
    // Terrain whose SURFACE equals HqGroundY everywhere, one grass TerrainLayer painted full, GPU-instanced detail
    // grass cleared on the building/yard/track pads.
    static void BuildHqTerrain(Transform parent, BlackCommission.Level.FoliageSet fset)
    {
        float minX = WildCx - WildW * 0.5f, minZ = WildCz - WildD * 0.5f;
        const int Hres = 257;

        var td = new TerrainData { heightmapResolution = Hres };
        td.size = new Vector3(WildW, HRANGE, WildD);

        var heights = new float[Hres, Hres];
        for (int i = 0; i < Hres; i++)
        {
            float z = minZ + (i / (float)(Hres - 1)) * WildD;
            for (int j = 0; j < Hres; j++)
            {
                float x = minX + (j / (float)(Hres - 1)) * WildW;
                heights[i, j] = Mathf.Clamp01((HqGroundY(x, z) - TBASE) / HRANGE);
            }
        }
        td.SetHeights(0, 0, heights);

        td.terrainLayers = new[] { fset.grassLayer };
        const int Ares = 64;
        td.alphamapResolution = Ares;
        var alpha = new float[Ares, Ares, 1];
        for (int i = 0; i < Ares; i++) for (int j = 0; j < Ares; j++) alpha[i, j, 0] = 1f;
        td.SetAlphamaps(0, 0, alpha);

        if (fset.grassDetailPrefab != null || fset.grassDetail != null)
        {
            const int Dres = 512;
            td.SetDetailResolution(Dres, 16);
            DetailPrototype proto = fset.grassDetailPrefab != null
                ? new DetailPrototype { usePrototypeMesh = true, prototype = fset.grassDetailPrefab, useInstancing = true,
                    renderMode = DetailRenderMode.VertexLit, healthyColor = new Color(0.62f, 0.56f, 0.38f), dryColor = new Color(0.45f, 0.40f, 0.24f),
                    minWidth = 0.7f, maxWidth = 1.4f, minHeight = 0.6f, maxHeight = 1.3f, noiseSpread = 0.35f }
                : new DetailPrototype { prototypeTexture = fset.grassDetail, usePrototypeMesh = false,
                    renderMode = DetailRenderMode.GrassBillboard, healthyColor = new Color(0.34f, 0.34f, 0.22f),
                    dryColor = new Color(0.30f, 0.28f, 0.17f), minWidth = 0.6f, maxWidth = 1.3f, minHeight = 0.4f,
                    maxHeight = 1.0f, noiseSpread = 0.35f };
            td.detailPrototypes = new[] { proto };
            var details = new int[Dres, Dres];
            for (int i = 0; i < Dres; i++)
            {
                float z = minZ + ((i + 0.5f) / Dres) * WildD;
                for (int j = 0; j < Dres; j++)
                {
                    float x = minX + ((j + 0.5f) / Dres) * WildW;
                    details[i, j] = HqGrassDensity(x, z);
                }
            }
            td.SetDetailLayer(0, 0, 0, details);
            td.wavingGrassStrength = 0.25f; td.wavingGrassAmount = 0.3f; td.wavingGrassSpeed = 0.4f;
            td.wavingGrassTint = new Color(0.40f, 0.38f, 0.25f); // dead-olive, not yellow-green
        }

        // Persist TerrainData as an asset so the SAVED scene keeps a valid reference (a runtime-only TerrainData is
        // lost on reload). Re-runnable: replace any existing one.
        const string tdPath = "Assets/_Project/Scenes/HQ_TerrainData.asset";
        AssetDatabase.DeleteAsset(tdPath);
        AssetDatabase.CreateAsset(td, tdPath);

        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terrain";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(minX, TBASE, minZ);
        var terrain = go.GetComponent<Terrain>();
        if (fset.terrainMaterial != null) terrain.materialTemplate = fset.terrainMaterial;
        terrain.drawInstanced = true;
        terrain.detailObjectDistance = 140f;
        terrain.detailObjectDensity = 1f;
        AssetDatabase.SaveAssets();
    }

    // Terrain surface height: flat (0) under the building + yard + the van's dirt track; gentle Perlin rolls in the wild.
    static float HqGroundY(float x, float z)
    {
        float flat = HqFlatMask(x, z);
        float u = (Mathf.PerlinNoise(x * 0.018f + 11.7f, z * 0.018f + 4.3f) - 0.5f) * 3.0f
                + (Mathf.PerlinNoise(x * 0.060f + 2.1f, z * 0.060f + 9.8f) - 0.5f) * 0.8f;
        // Recess the flat pad 0.12 m BELOW the slab tops (Shell Floor + YardGround, both y=0) so the concrete
        // occludes the terrain instead of Z-fighting it coplanar (the "两个面重合" the PM flagged). Wild keeps the roll.
        return Mathf.Lerp(u, -0.12f, flat);
    }

    // 1 inside the building/yard pad and the van's track; SmoothStep to 0 in the wild. Pad bounds cover the SCALED
    // footprint (Shell/Interior/Dressing/Yard get HqScale, so the flat pad is widened to match).
    static float HqFlatMask(float x, float z)
    {
        float pad = SmoothBand(x, -11f, 22f, 10f) * SmoothBand(z, -4f, 56f, 10f);
        float track = SmoothBand(x, 1.5f, 9f, 6f) * SmoothBand(z, 50f, 150f, 10f);
        return Mathf.Max(pad, track);
    }

    // 1 inside [lo,hi]; SmoothStep down to 0 over `fade` metres outside.
    static float SmoothBand(float v, float lo, float hi, float fade)
    {
        if (v >= lo && v <= hi) return 1f;
        float d = v < lo ? lo - v : v - hi;
        return Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(d / fade));
    }

    // Detail-grass density per world cell: lush in the wild, 0 on the building/yard/track pads.
    static int HqGrassDensity(float x, float z)
    {
        if (HqFlatMask(x, z) > 0.5f) return 0;
        int h = Mathf.Abs(Mathf.RoundToInt(x * 5.3f) * 73856093 ^ Mathf.RoundToInt(z * 5.7f) * 19349663);
        return 14 + (h % 8); // 14..21
    }

    // One derelict warehouse. Non-hero = a solid concrete/brick block + steel roof cap (backdrop massing the fog
    // eats). HERO (near, yard-facing) also gets a detailed -Z facade so it reads as a real derelict building.
    static void Warehouse(Transform parent, Vector3 basePos, Vector3 size, Material wall, Material roof, bool hero = false)
    {
        var go = Sub(parent, "Warehouse");
        go.localPosition = basePos;
        Box(go, "Body", new Vector3(0f, size.y * 0.5f, 0f), size, wall);
        Box(go, "Roof", new Vector3(0f, size.y + 0.25f, 0f), new Vector3(size.x * 1.04f, 0.5f, size.z * 1.04f), roof);
        if (!hero) return;

        // HERO PASS — detail the yard-facing (-Z) face: broken windows, loading door, columns, roof vents, rust.
        float faceZ = -size.z * 0.5f - 0.08f;
        Box(go, "Windows", new Vector3(0f, size.y * 0.70f, faceZ), new Vector3(size.x * 0.6f, size.y * 0.28f, 0.12f),
            M(TIR + "WindowsBroken01.mat", new Color(0.18f, 0.18f, 0.16f)), collider: false);
        if (!FitPrefab(go, TIRP + "DoorIndustrial01_1.prefab", "LoadingDoor", new Vector3(0f, 0f, faceZ - 0.05f), 180f, new Vector3(4.0f, 3.2f, 0.3f)))
            Box(go, "LoadingDoor", new Vector3(0f, 1.6f, faceZ - 0.05f), new Vector3(4.0f, 3.2f, 0.14f), M(TIR + "DoorIndustrial01.mat", cShutter));
        float colX = size.x * 0.5f - 0.3f;
        FitPrefab(go, TIRP + "Factory1Column02.prefab", "CornerCol_L", new Vector3(-colX, 0f, faceZ + 0.1f), 0f, new Vector3(0.55f, size.y, 0.55f), uniformByHeight: true);
        FitPrefab(go, TIRP + "Factory1Column02.prefab", "CornerCol_R", new Vector3(colX, 0f, faceZ + 0.1f), 0f, new Vector3(0.55f, size.y, 0.55f), uniformByHeight: true);
        var roofPropMat = M(TIR + "CommonMetalPainted01.mat", new Color(0.20f, 0.22f, 0.18f));
        Box(go, "Vent_A", new Vector3(size.x * 0.2f, size.y + 0.3f, -size.z * 0.1f), new Vector3(1.2f, 0.6f, 1.2f), roofPropMat, collider: false);
        Box(go, "Vent_B", new Vector3(-size.x * 0.25f, size.y + 0.3f, size.z * 0.15f), new Vector3(0.9f, 0.5f, 0.9f), roofPropMat, collider: false);
        var dirtMat = M(TIR + "DecalsDirt01.mat", new Color(0.14f, 0.12f, 0.10f));
        float[] streakX = { -size.x * 0.28f, 0f, size.x * 0.22f };
        foreach (var sx in streakX)
            Box(go, "RustStreak", new Vector3(sx, size.y * 0.35f, faceZ - 0.02f), new Vector3(0.18f, size.y * 0.45f, 0.02f), dirtMat, collider: false);
    }

    // Shared warehouse layout (x, z centre ; w, h, d ; material 0/1 ; hero=detailed). Placed by BuildEnvironment;
    // read by BuildVegetation as exclusion zones. The 3 near-yard north blocks are hero.
    static readonly (float x, float z, float w, float h, float d, int m, bool hero)[] WarehouseBlocks =
    {
        (-14, 62, 20, 12, 24, 0, true), (10, 68, 26, 17, 30, 1, true), (34, 60, 16, 10, 20, 0, true), (-34, 70, 18, 13, 22, 1, false), (54, 66, 22, 11, 26, 0, false),
        (46, 12, 20, 14, 24, 1, false), (52, 34, 18, 10, 22, 0, false), (68, 22, 24, 16, 30, 1, false), (44, -16, 16, 9, 20, 0, false),
        (-34, 8, 20, 12, 24, 1, false), (-44, 32, 18, 11, 22, 0, false), (-62, 18, 22, 15, 28, 1, false), (-34, -14, 16, 10, 20, 0, false),
        (-8, -28, 20, 12, 24, 0, false), (16, -32, 18, 13, 22, 1, false), (-30, -38, 16, 10, 20, 0, false), (40, -28, 22, 14, 26, 1, false),
        (0, 96, 30, 22, 24, 0, false), (-72, 60, 26, 20, 24, 1, false), (82, 52, 28, 24, 26, 0, false),
    };

    // ----------------------------------------------------------------- vegetation (dead/derelict; reuses FoliageSet)

    /// <summary>
    /// Dead/derelict vegetation across the HQ exterior (PM 2026-06-22 "no vegetation, just blocks"; per
    /// design/art/hq-exterior-art-direction.md). Reuses the authored Assets/Resources/FoliageSet.asset (pines as
    /// skeletal dead trees, ferns as dry scrub) + the MapSiteBuilder dead-olive treatment. Seeded + deterministic;
    /// stays out of the van corridor + building footprints; falls back to procedural stalks if the set is absent.
    /// </summary>
    static void BuildVegetation(Transform root)
    {
        var p = Sub(root, "Vegetation");
        var fset = AssetDatabase.LoadAssetAtPath<BlackCommission.Level.FoliageSet>("Assets/Resources/FoliageSet.asset");
        if (fset != null)
        {
            if (fset.treeMaterial != null) fset.treeMaterial.enableInstancing = true;
            if (fset.bushMaterial != null) fset.bushMaterial.enableInstancing = true;
        }
        var trees = fset != null ? fset.trees : null;
        var bushes = fset != null ? fset.bushes : null;
        var treeMat = fset != null ? fset.treeMaterial : null;
        var bushMat = fset != null ? fset.bushMaterial : null;
        float treeH = fset != null ? fset.treeHeight : 6f;
        float bushH = fset != null ? fset.bushHeight : 0.9f;
        float trunkR = fset != null ? fset.trunkRadius : 0.3f;

        var rng = new System.Random(0xBCA11);
        float Rnd(float a, float b) => a + (float)rng.NextDouble() * (b - a);
        void Tree(float x, float z, float sc) { if (!VegBlocked(x, z)) PlaceVeg(p, trees, treeMat, treeH, trunkR, x, z, Rnd(0f, 360f), sc, true, HqGroundY(x, z)); }
        void Bush(float x, float z, float sc) { if (!VegBlocked(x, z)) PlaceVeg(p, bushes, bushMat, bushH, 0f, x, z, Rnd(0f, 360f), sc, false, HqGroundY(x, z)); }

        // A — dead TREE LINE ringing the site in the fog (dense): the wall of the wild around the lone office.
        for (float a = 0f; a < 360f; a += 5f)
        {
            float rad = a * Mathf.Deg2Rad;
            float r = Rnd(70f, 92f);
            Tree(WildCx + Mathf.Cos(rad) * r, WildCz + Mathf.Sin(rad) * r * 0.92f, Rnd(0.95f, 1.35f));
        }
        // B — mid-field scattered dead pines (sparse, irregular) filling the meadow between office and treeline
        for (int i = 0; i < 55; i++) Tree(Rnd(-64f, 72f), Rnd(-48f, 112f), Rnd(0.8f, 1.2f));
        // C — dry scrub / ferns clumped across the meadow (VegBlocked keeps both off the building/yard/track pads)
        for (int i = 0; i < 90; i++) Bush(Rnd(-62f, 70f), Rnd(-46f, 110f), Rnd(0.6f, 1.1f));
    }

    // Keep the central van corridor + every warehouse footprint clear of scatter.
    static bool VegBlocked(float x, float z)
    {
        return HqFlatMask(x, z) > 0.25f; // keep scatter off the building + yard + the van's dirt track (the flat pads)
    }

    static int PickVegIndex(float x, float z, int n)
    {
        if (n <= 1) return 0;
        int h = Mathf.Abs(Mathf.RoundToInt(x * 23.1f) * 46327811 ^ Mathf.RoundToInt(z * 11.7f) * 83492791);
        return h % n;
    }

    // Instantiate one FoliageSet model — dead-tinted, bounds-fit to a target height, grounded, colliders stripped;
    // trees get a thin trunk capsule. Falls back to a procedural dead stalk if the set/pack is missing.
    static void PlaceVeg(Transform parent, GameObject[] models, Material mat, float baseH, float trunkR,
        float x, float z, float yaw, float scale, bool isTree, float groundY = 0f)
    {
        float targetH = baseH * (0.7f + 0.6f * scale);
        GameObject go = null;
        if (models != null && models.Length > 0)
        {
            var prefab = models[PickVegIndex(x, z, models.Length)];
            if (prefab != null) go = (GameObject)Object.Instantiate(prefab, parent);
        }
        if (go != null)
        {
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;
            if (mat != null)
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var ms = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                    for (int i = 0; i < ms.Length; i++) ms[i] = mat;
                    r.sharedMaterials = ms;
                }
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                Bounds b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                go.transform.localScale = Vector3.one * (targetH / Mathf.Max(b.size.y, 0.01f));
                b = rs[0].bounds;
                for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
                go.transform.position = new Vector3(x, go.transform.position.y - b.min.y + groundY, z);
            }
            else go.transform.position = new Vector3(x, groundY, z);
            foreach (var c in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, targetH * 0.4f + groundY, z);
            go.transform.localScale = new Vector3(0.2f, targetH * 0.4f, 0.2f);
            go.GetComponent<Renderer>().sharedMaterial = Flat(new Color(0.18f, 0.16f, 0.12f));
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        go.name = (isTree ? "VegTree_" : "VegBush_") + $"{x:F0}_{z:F0}";
        if (isTree && trunkR > 0f)
        {
            var col = new GameObject("VegTrunk");
            col.transform.SetParent(parent, false);
            col.transform.position = new Vector3(x, groundY, z);
            var cap = col.AddComponent<CapsuleCollider>();
            cap.radius = trunkR; cap.height = targetH; cap.center = new Vector3(0f, targetH * 0.5f, 0f);
        }
    }

    // A dead (unlit) floodlight pole — broken; carries no Light.
    static void DeadPole(Transform parent, float x, float z)
    {
        var pole = Sub(parent, "DeadPole");
        var shaftMat = M(TIR + "CommonMetal01.mat", new Color(0.18f, 0.18f, 0.19f));
        var headMat = M(TIR + "CommonMetalPainted01.mat", new Color(0.22f, 0.22f, 0.20f));
        Box(pole, "Shaft", new Vector3(x, 2.75f, z), new Vector3(0.1f, 5.5f, 0.1f), shaftMat);
        BoxRot(pole, "Head", new Vector3(x + 0.4f, 5.5f, z), new Vector3(0.6f, 0.2f, 0.4f), -20f, headMat, collider: false);
    }

    // ----------------------------------------------------------------- render / lighting / post (office look)

    static void ConfigureRenderSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Rgb(0x22, 0x22, 0x1C); // darker than ambient sky so the wild dissolves into night, not haze (art-director 2026-06-24)
        RenderSettings.fogStartDistance = 22f; // just past the scaled building so the office interior never fogs
        RenderSettings.fogEndDistance = 90f; // the dead treeline (~70-92 m) dissolves into fog instead of floating in clear air
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Rgb(0x32, 0x33, 0x30); // darker so the light pools do the work, not flat ambient fill
        RenderSettings.ambientEquatorColor = Rgb(0x28, 0x2A, 0x26);
        RenderSettings.ambientGroundColor = Rgb(0x18, 0x1A, 0x17);
        RenderSettings.ambientIntensity = 0.60f; // was 0.78 — let the noir light pools establish focal hierarchy
    }

    static void EnsureLighting(Transform parent)
    {
        var key = AddLight(parent, "HQ_OptionA Key", LightType.Directional, new Color(0.86f, 0.84f, 0.80f), 0.4f, 0f);
        key.transform.rotation = Quaternion.Euler(46f, -24f, 0f);
        key.GetComponent<Light>().shadows = LightShadows.Soft; // CRITICAL grounding — every light was shadow-less (art-director 2026-06-24)

        // Ceiling fluorescent MESHES (real Tirgames tubes) + their point-light pools.
        FitPrefab(parent, TIRP + "LampCeiling01.prefab", "Tube_CRT", new Vector3(1.9f, WallH - 0.18f, 6.0f), 0f, new Vector3(1.2f, 0.2f, 0.4f));
        FitPrefab(parent, TIRP + "LampCeiling01.prefab", "Tube_Gear", new Vector3(7.8f, WallH - 0.18f, 12.3f), 0f, new Vector3(1.2f, 0.2f, 0.4f));
        FitPrefab(parent, TIRP + "LampCeiling01.prefab", "Tube_Spine", new Vector3(4.5f, WallH - 0.18f, 10.5f), 0f, new Vector3(1.2f, 0.2f, 0.4f));

        var gearTube = PointLight(parent, "Light_Fluorescent_Gear", new Vector3(7.8f, 2.9f, 12.3f), ColdIndustrial, 0.55f, 6.0f);
        var flick = gearTube.AddComponent<LightFlicker>();
        flick.Configure(LightFlicker.Character.Sputter, 0.7f, 7f);
        var crtFlux = PointLight(parent, "Light_Fluorescent_CRT", new Vector3(1.9f, 2.9f, 6.0f), ColdIndustrial, 0.3f, 4.5f);
        crtFlux.GetComponent<Light>().shadows = LightShadows.Soft; // ground the hero CRT/desk zone (art-director 2026-06-24)
        PointLight(parent, "Light_Fluorescent_Spine", new Vector3(4.5f, 2.9f, 10.5f), WarmTungsten, 0.5f, 5.0f); // warm tungsten over the spine/desks (was cold)
        PointLight(parent, "Light_CRT_Screen", new Vector3(1.5f, 1.2f, 6.3f), DispatchGreen, 0.35f, 1.2f); // tighter phosphor spill, no teal bleed onto the debt board
        PointLight(parent, "Light_DeskLamp", new Vector3(2.55f, 1.2f, 10.6f), WarmTungsten, 0.7f, 3.0f); // brighter warm desk pool (was 0.34)
        PointLight(parent, "Light_Threshold", new Vector3(4.5f, 3.3f, 17.2f), WarmTungsten, 1.1f, 7f); // tighter, brighter door pool (range was 13 — spilled inside)
    }

    static void BuildPostVolume(Transform root)
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium1); grain.intensity.Override(0.20f); grain.response.Override(0.7f);
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.2f); bloom.intensity.Override(0.28f); bloom.scatter.Override(0.4f); // higher threshold + tighter scatter, no false halos on mid lights (art-director 2026-06-24)
        var color = profile.Add<ColorAdjustments>(true);
        color.saturation.Override(-10f); color.contrast.Override(7f);

        var go = new GameObject("LC_PostVolume (Option A)");
        go.transform.SetParent(root, false);
        var v = go.AddComponent<Volume>();
        v.isGlobal = true; v.priority = 10f; v.profile = profile;
    }

    // ----------------------------------------------------------------- helpers

    static Transform Sub(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) { var c = go.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c); }
        return go;
    }

    static GameObject BoxRot(Transform parent, string name, Vector3 center, Vector3 size, float yaw, Material mat, bool collider = true)
    {
        var go = Box(parent, name, center, size, mat, collider);
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }

    /// <summary>Instantiate an authored office prefab (import-normalized) at local pos + yaw, scale 1; imported
    /// colliders disabled (shell boxes carry collision). Missing prefab -> labelled fallback box.</summary>
    static void Prop(Transform parent, string resPath, string name, Vector3 localPos, float yaw, Vector3 fallbackSize)
    {
        var prefab = Resources.Load<GameObject>(resPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[HQ Option A] Prop missing: {resPath} — fallback box '{name}'.");
            var box = Box(parent, name + " (FALLBACK)", localPos + Vector3.up * fallbackSize.y * 0.5f, fallbackSize, Flat(cProp), collider: false);
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

    /// <summary>Instantiate a Tirgames prefab and AUTO-FIT it to a target box by measured renderer bounds (no
    /// eyeballed transforms), grounded at y=0 and centred at (pos.x,pos.z) with yaw. Returns false (no-op) if the
    /// prefab is missing so the caller can fall back. uniformByHeight scales uniformly to the target height.</summary>
    static bool FitPrefab(Transform parent, string assetPath, string name, Vector3 pos, float yaw, Vector3 target, bool uniformByHeight = false)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) { Debug.LogWarning($"[HQ Option A] Tirgames prefab missing: {assetPath} — using fallback for '{name}'."); return false; }

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

        // Re-measure after scale+rotate, then ground (min.y -> pos.y) and centre on (pos.x, pos.z).
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

    static GameObject PointLight(Transform parent, string name, Vector3 pos, Color c, float intensity, float range)
    {
        var go = AddLight(parent, name, LightType.Point, c, intensity, range);
        go.transform.localPosition = pos;
        return go;
    }

    static GameObject AddLight(Transform parent, string name, LightType type, Color c, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var l = go.AddComponent<Light>();
        l.type = type; l.color = c; l.intensity = intensity;
        if (range > 0f) l.range = range;
        l.shadows = LightShadows.None;
        return go;
    }

    static Material M(string assetPath, Color fallback)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (asset != null) return asset;
        Debug.LogWarning($"[HQ Option A] Material not found, flat fallback: {assetPath}");
        return Flat(fallback);
    }

    static Material Flat(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.25f); // avoid a near-mirror gloss blowout on fallback boxes
        return m;
    }

    static IEnumerable<GameObject> AllObjects() =>
        Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    static Color Rgb(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f);
}
