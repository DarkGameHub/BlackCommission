using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// One-click FIRST-PERSON playtest of the locked HQ design ("Option A — Long-Axis Z-Spine").
/// A throwaway "go in and walk it" rig, completely SEPARATE from the shipping HQ. It does NOT call,
/// touch, or overwrite <c>MvpSceneStyleDirector</c> / <c>HqOfficePropRestorer</c> (the real runtime HQ):
/// it builds its own root <c>PLAYTEST_HQ</c>, OFFSET to <see cref="BuildOrigin"/> (x+300) so it can
/// never overlap the real HQ, and it NEVER saves the scene. Press Play (▶) to walk it; close Play and
/// the build is gone.
///
/// DESIGN — "Option A", the floor plan PM David locked (2026-06-21):
///   ONE leased industrial unit, a gentle WEDGE (9 m wide, 17 m on the +X wall, 19.5 m on the -X wall).
///   The far wall is CANTED; the roll-up departure door sits in it, dead ahead of the spawn line, so the
///   team stares at the closed door the whole prep (axial reveal). Stations sit on OPPOSITE walls to
///   force a diagonal Z-spine walk:
///       spawn line (z≈2) → debt/takeover board + CRT (-X wall) → gear wall (+X wall) → muster pad →
///       empty run-up → roll-up door → (reveal) van + fogged city outside.
///   The S1 padlocked side door stays in peripheral view on the -X wall (license-as-leash, off-path).
///
/// "NOT just blocks" (PM 2026-06-21 "一定要视觉上就很好，而不是就几个方块"):
///   • Props are the REAL authored office meshes from Resources/GeneratedArt (AS_Office* — computer,
///     gear cabinets, debt board, sofa, lamps, extinguisher, gas-mask sentinel…). They are import-
///     NORMALIZED (base at y=0, centred, real-world height — see OfficePropImporter), so a clean
///     Instantiate at scale 1 drops them at correct size, grounded. Missing prefab → labelled fallback box.
///   • The shell (floor / walls / roof / roll-up) stays skinned boxes — that is how the real runtime HQ
///     does its shell too — wearing the authored Municipal-Debt-Noir materials (V8_* tower whitebox set).
///   • Render look mirrors the HQ office: concrete fog, Trilight ambient, cold-fluorescent / warm-tungsten
///     / sodium-amber light pools with shadows OFF, one sputtering "can't afford a new tube" fluorescent
///     (<see cref="LightFlicker"/>), and the LC post stack (grain / bloom / grade). Post is enabled on the
///     playtest camera (URP default is OFF), and the camera clears to the fog colour (no bright skybox).
///   • The civic blue-black LC OUTLINE is a global URP renderer feature. If the scene looks un-outlined,
///     run once: Tools ▸ Black Commission ▸ Art ▸ Setup LC Retro Rendering.
///
/// ITERATION NOTE: every prop's facing is a single yaw in <see cref="PropYaw"/> below. They are best-guess
/// (the FBX import lays props on their back, then normalizes) — if a prop faces the wrong way in Play,
/// nudge its entry there and re-run the menu. Sizes/positions come from the locked floor plan.
///
/// Menu: Tools ▸ Black Commission ▸ Map ▸ Playtest HQ (Walk It)
/// </summary>
public static class HqPlaytestMenu
{
    const string RootName = "PLAYTEST_HQ";

    // Built far from world origin so it can never overlap the real (runtime) HQ if that is also present.
    static readonly Vector3 BuildOrigin = new Vector3(300f, 0f, 0f);

    // --- Unit footprint (local, metres). Locked Option A wedge. Origin = near-left (-X/-Z) corner. ---
    // Shell polygon: (0,0) (9,0) (9,17) (0,19.5). +X = right, +Z = forward (toward the roll-up door).
    const float UnitW = 9f;       // X span
    const float DepthShort = 17f; // +X wall length (z)
    const float DepthLong = 19.5f;// -X wall length (z)
    const float WallH = 3.6f;
    const float Wall = 0.2f;

    // Roll-up opening (4 m) centred on the canted far wall.
    const float DoorW = 4f;
    const float DoorH = 3.2f;

    // --- Authored material libraries (reused, not re-created). ---
    const string V8 = "Assets/_Project/Art/Maps/Tower_EarthCoast_01/Materials/Whitebox/";

    // --- Real prop prefabs (import-normalized: base at y=0, centred, real height). ---
    const string R = "GeneratedArt/"; // Resources.Load path prefix

    // --- Per-prop facing (yaw, degrees). Best-guess; tweak + re-run if a prop faces wrong. ---
    static class PropYaw
    {
        public const float Computer = 90f;   // CRT on -X wall, screen toward +X (into room)
        public const float DebtBoard = 90f;  // -X wall, faces +X
        public const float SafetyBoard = -90f; // +X wall, faces -X
        public const float Gear = -90f;      // +X wall cabinets, faces -X
        public const float Sofa = 0f;        // back (-Z) wall, faces +Z
        public const float Sentinel = 35f;   // back -X corner watcher
        public const float Table = 0f;
        public const float Extinguisher = -90f;
        public const float Garage = -130f;
        public const float Van = 0f;         // outside; length along Z, nose toward the door
    }

    // --- Municipal Debt Noir fallback palette (only used if an authored asset is missing) ---
    static readonly Color cFloor   = new Color(0.16f, 0.16f, 0.18f);
    static readonly Color cWall    = new Color(0.34f, 0.35f, 0.36f);
    static readonly Color cRoof    = new Color(0.22f, 0.22f, 0.24f);
    static readonly Color cPanel   = new Color(0.20f, 0.20f, 0.22f);
    static readonly Color cPadlock = new Color(0.62f, 0.12f, 0.12f);
    static readonly Color cShutter = new Color(0.34f, 0.26f, 0.16f);
    static readonly Color cGround  = new Color(0.12f, 0.12f, 0.13f);
    static readonly Color cSil     = new Color(0.05f, 0.06f, 0.08f);
    static readonly Color cSpawn   = new Color(0.62f, 0.55f, 0.20f);
    static readonly Color cProp    = new Color(0.30f, 0.28f, 0.24f);

    // --- Office light colours (copied from HqOfficeLightingPass) ---
    static readonly Color ColdIndustrial = Rgb(0xD9, 0xE2, 0xDD);
    static readonly Color WarmTungsten   = Rgb(0xFF, 0xBB, 0x73);
    static readonly Color DispatchGreen  = new Color(0.20f, 0.95f, 0.65f);
    static readonly Color FogConcrete    = Rgb(0x2E, 0x30, 0x2D);

    [MenuItem("Tools/Black Commission/Map/Playtest HQ (Walk It)")]
    public static void Setup()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject(RootName);
        root.transform.position = BuildOrigin;

        BuildShell(root.transform);
        BuildInterior(root.transform);
        BuildOutside(root.transform);

        ConfigureRenderSettings();
        EnsureLighting(root.transform);
        BuildPostVolume(root.transform);

        var spawn = new Vector3(4.0f, 1.2f, 2.6f); // on the spawn line, facing the spine + closed door (+Z)
        SpawnPlayer(root.transform, spawn);

        Debug.Log("[PlaytestHQ] READY (Option A wedge, real prefabs). Press Play (▶), WASD + mouse to walk. " +
                  "You spawn on the team line looking down the spine: CRT + debt board on your LEFT (-X), gear " +
                  "wall on your RIGHT (+X), the roll-up departure door dead AHEAD in the canted far wall (CLOSED " +
                  "= prep). To preview the departure REVEAL (van + fogged city), un-tick " +
                  "'PLAYTEST_HQ/Shell/RollUpDoor_CLOSED' in the Inspector. Controls: Shift sprint · Ctrl crouch · " +
                  "Space jump · F flythrough · Esc cursor. If un-outlined, run Tools ▸ Black Commission ▸ Art ▸ " +
                  "Setup LC Retro Rendering once. Throwaway build — never saves, does not touch the real HQ. " +
                  "Any prop facing wrong? Tweak HqPlaytestMenu.PropYaw and re-run the menu.");

        Selection.activeGameObject = root;
        SceneView.FrameLastActiveSceneView();
    }

    // ---------------------------------------------------------------- shell (floor / walls / roof / door)

    static void BuildShell(Transform root)
    {
        var p = Sub(root, "Shell");

        var floorMat = M(V8 + "V8_Concrete_Poured.mat", cFloor);
        var roofMat  = M(V8 + "V8_Steel_Dark.mat", cRoof);
        var wallMat  = M(V8 + "V8_Concrete_WallRaw.mat", cWall);
        float cy = WallH * 0.5f;

        // Floor + roof: oversized rectangles (walls hide the canted edge; cheaper + reads identical inside).
        float midZ = DepthLong * 0.5f;
        Box(p, "Floor", new Vector3(UnitW * 0.5f, -0.1f, midZ), new Vector3(UnitW + 0.6f, 0.2f, DepthLong + 0.8f), floorMat);
        Box(p, "Roof",  new Vector3(UnitW * 0.5f, WallH + 0.1f, midZ), new Vector3(UnitW + 0.6f, 0.2f, DepthLong + 0.8f), roofMat);

        // Three straight walls.
        Box(p, "Wall_-X(left)",  new Vector3(-Wall * 0.5f, cy, DepthLong * 0.5f), new Vector3(Wall, WallH, DepthLong), wallMat);
        Box(p, "Wall_+X(right)", new Vector3(UnitW + Wall * 0.5f, cy, DepthShort * 0.5f), new Vector3(Wall, WallH, DepthShort), wallMat);
        Box(p, "Wall_-Z(back)",  new Vector3(UnitW * 0.5f, cy, -Wall * 0.5f), new Vector3(UnitW + Wall, WallH, Wall), wallMat);

        // Canted far wall connecting (9,17) -> (0,19.5), with the centred 4 m roll-up opening.
        Vector2 a = new Vector2(UnitW, DepthShort);
        Vector2 b = new Vector2(0f, DepthLong);
        Vector2 mid = (a + b) * 0.5f;
        Vector2 dir = (b - a).normalized;
        float len = (b - a).magnitude;
        float yaw = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg; // local +X -> (dir.x,0,dir.y)
        float half = len * 0.5f;
        float openHalf = DoorW * 0.5f;
        float segLen = half - openHalf;

        Vector3 OnWall(float t, float y) => new Vector3(mid.x + dir.x * t, y, mid.y + dir.y * t);

        BoxRot(p, "Wall_Far_L", OnWall(-(half + openHalf) * 0.5f, cy), new Vector3(segLen, WallH, Wall), yaw, wallMat);
        BoxRot(p, "Wall_Far_R", OnWall((half + openHalf) * 0.5f, cy), new Vector3(segLen, WallH, Wall), yaw, wallMat);
        BoxRot(p, "Wall_Far_Lintel", OnWall(0f, (DoorH + WallH) * 0.5f), new Vector3(DoorW, WallH - DoorH, Wall), yaw, wallMat);

        // The roll-up door itself — CLOSED. Un-tick this object to preview the reveal.
        BoxRot(p, "RollUpDoor_CLOSED", OnWall(0f, DoorH * 0.5f), new Vector3(DoorW, DoorH, 0.14f), yaw,
            M(V8 + "V8_Steel_Rust.mat", cShutter));
    }

    // ---------------------------------------------------------------- interior (props on the Z-spine)

    static void BuildInterior(Transform root)
    {
        var p = Sub(root, "Interior");

        // --- LEFT (-X) wall: debt/takeover board, CRT computer, file boxes, sentinel ---
        Prop(p, R + "AS_OfficeComputer", "Computer_CRT", new Vector3(1.0f, 0f, 6.0f), PropYaw.Computer, new Vector3(1.80f, 1.35f, 1.16f));
        Prop(p, R + "AS_OfficeDebtBoard", "DebtBoard", new Vector3(0.22f, 1.25f, 3.6f), PropYaw.DebtBoard, new Vector3(0.1f, 0.9f, 1.6f));
        Prop(p, R + "AS_OfficeToolSet", "FileBoxes", new Vector3(1.0f, 0f, 4.7f), 20f, new Vector3(0.8f, 0.6f, 0.8f));
        Prop(p, R + "AS_OfficeGasMaskSentinel", "Sentinel", new Vector3(1.0f, 0f, 2.2f), PropYaw.Sentinel, new Vector3(0.7f, 1.7f, 0.7f));

        // --- RIGHT (+X) wall: gear wall (the config you face + scan) ---
        Prop(p, R + "AS_OfficeSupplyCabinet", "Gear_Supply", new Vector3(8.45f, 0f, 12.4f), PropYaw.Gear, new Vector3(0.6f, 1.5f, 1.2f));
        Prop(p, R + "AS_OfficeFilingCabinet", "Gear_Filing", new Vector3(8.55f, 0f, 10.8f), PropYaw.Gear, new Vector3(0.6f, 1.25f, 0.9f));
        Prop(p, R + "AS_OfficeToolRack", "Gear_ToolRack", new Vector3(8.78f, 0.85f, 13.9f), PropYaw.Gear, new Vector3(0.15f, 1.3f, 1.1f));
        Prop(p, R + "AS_OfficeSafetyBoard", "SafetyBoard", new Vector3(8.8f, 1.15f, 9.5f), PropYaw.SafetyBoard, new Vector3(0.1f, 1.2f, 1.4f));
        Prop(p, R + "AS_OfficeFireExtinguisher", "FireExt", new Vector3(8.5f, 0f, 16.0f), PropYaw.Extinguisher, new Vector3(0.5f, 0.55f, 0.5f));
        Prop(p, R + "AS_GarageWorkshopCorner", "GarageCorner", new Vector3(7.6f, 0f, 15.2f), PropYaw.Garage, new Vector3(1.4f, 1.4f, 1.4f));

        // --- centre spine: folding table (with a desk lamp on it), sofa against the back wall ---
        Prop(p, R + "AS_OfficeDesk", "FoldingTable", new Vector3(2.2f, 0f, 10.6f), PropYaw.Table, new Vector3(1.2f, 0.75f, 0.8f));
        Prop(p, R + "AS_LampDesk", "DeskLamp", new Vector3(2.55f, 0.75f, 10.6f), 0f, new Vector3(0.3f, 0.45f, 0.3f));
        Prop(p, R + "AS_OfficeSofa", "Sofa", new Vector3(6.9f, 0f, 1.1f), PropYaw.Sofa, new Vector3(1.8f, 0.85f, 0.8f));

        // --- ceiling fluorescents (paired with the point lights in EnsureLighting) ---
        Prop(p, R + "AS_LampFluorescent", "Fluor_CRT", new Vector3(1.9f, 3.42f, 6.0f), 0f, new Vector3(1.2f, 0.15f, 0.3f));
        Prop(p, R + "AS_LampFluorescent", "Fluor_Gear", new Vector3(7.8f, 3.42f, 12.3f), 0f, new Vector3(1.2f, 0.15f, 0.3f));

        // --- S1 padlocked side door on the -X wall (peripheral, off-path, license-as-leash) ---
        Box(p, "S1_PadlockDoor_LOCKED", new Vector3(0.16f, 1.05f, 15.9f), new Vector3(0.12f, 2.1f, 1.1f), M(V8 + "V8_Steel_Dark.mat", cPanel));
        Box(p, "S1_Padlock", new Vector3(0.34f, 1.1f, 15.9f), new Vector3(0.14f, 0.14f, 0.14f), M(V8 + "V8_Stamp_Red.mat", cPadlock), collider: false);

        // --- muster pad + spawn anchors (floor markings, no colliders) ---
        var markMat = M(V8 + "V8_Marking_AgedYellow.mat", cSpawn);
        Box(p, "MusterPad", new Vector3(4.5f, 0.02f, 16.3f), new Vector3(3.0f, 0.04f, 2.2f), markMat, collider: false);
        SpawnMarker(p, "SPAWN_1", new Vector3(2.2f, 0.03f, 2.2f), markMat);
        SpawnMarker(p, "SPAWN_2", new Vector3(4.0f, 0.03f, 2.2f), markMat);
        SpawnMarker(p, "SPAWN_3", new Vector3(5.6f, 0.03f, 2.2f), markMat);
        SpawnMarker(p, "SPAWN_4", new Vector3(7.0f, 0.03f, 2.2f), markMat);
    }

    // ---------------------------------------------------------------- outside (van / silhouette)

    static void BuildOutside(Transform root)
    {
        var p = Sub(root, "Outside");

        Box(p, "OutsideGround", new Vector3(UnitW * 0.5f, -0.1f, 24f), new Vector3(26f, 0.2f, 16f), M(V8 + "V8_Asphalt_Muddy.mat", cGround));

        // The dispatch van, parked just outside, framed by (not blocking) the 4 m opening.
        Prop(p, R + "AS_OfficeVan", "DispatchVan", new Vector3(4.5f, 0f, 22.2f), PropYaw.Van, new Vector3(2.2f, 2.0f, 5.0f), cProp);

        // Cheap flat city silhouette in the distance — fog does the work. (x, height, width)
        var silMat = M(V8 + "V8_Steel_Dark.mat", cSil);
        var sil = new[]
        {
            new Vector3(-7f, 6.0f, 3.0f), new Vector3(-3f, 9.0f, 3.5f), new Vector3(0.5f, 5.0f, 2.5f),
            new Vector3(4.5f, 8.0f, 4.0f), new Vector3(8.5f, 6.5f, 3.0f), new Vector3(12f, 10f, 3.5f),
            new Vector3(16f, 5.5f, 3.0f),
        };
        for (int i = 0; i < sil.Length; i++)
            Box(p, $"Skyline_{i}", new Vector3(sil[i].x, sil[i].y * 0.5f, 31f), new Vector3(sil[i].z, sil[i].y, 0.6f), silMat, collider: false);
    }

    // ---------------------------------------------------------------- render settings / lighting / post

    /// <summary>
    /// HQ-office render look (see HqOfficeLightingPass), with one deliberate change: LINEAR fog instead
    /// of the office's thin ExponentialSquared. This scene has a distant exterior silhouette that must
    /// haze while the interior stays clear; linear start/end gives that precise indoor-clear /
    /// distance-fogged read.
    /// </summary>
    static void ConfigureRenderSettings()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = FogConcrete;
        RenderSettings.fogStartDistance = 14f;  // interior (≤19.5 m but viewed close) stays clear
        RenderSettings.fogEndDistance = 36f;     // city silhouette (~31 m) reads as fogged

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Rgb(0x4B, 0x4C, 0x48);
        RenderSettings.ambientEquatorColor = Rgb(0x3D, 0x3F, 0x3A);
        RenderSettings.ambientGroundColor = Rgb(0x26, 0x28, 0x25);
        RenderSettings.ambientIntensity = 0.92f;
    }

    static void EnsureLighting(Transform parent)
    {
        // Dim key for the exterior van + silhouette (the office interior is mostly ambient + pools).
        var sun = new GameObject("Playtest Key");
        sun.transform.SetParent(parent, false);
        sun.transform.rotation = Quaternion.Euler(46f, -24f, 0f);
        var l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 0.45f;
        l.color = new Color(0.86f, 0.84f, 0.80f);
        l.shadows = LightShadows.None; // office kills all shadows (PM 2026-06-13)

        // Cold fluorescents (the gear tube sputters — the "can't afford a new tube" piece).
        var gearTube = PointLight(parent, "Light_Fluorescent_Gear", new Vector3(7.8f, 2.9f, 12.3f), ColdIndustrial, 0.55f, 6.0f);
        gearTube.AddComponent<LightFlicker>().Configure(LightFlicker.Character.Sputter, 0.7f, 7f);
        PointLight(parent, "Light_Fluorescent_CRT", new Vector3(1.9f, 2.9f, 6.0f), ColdIndustrial, 0.5f, 6.0f);
        PointLight(parent, "Light_Fluorescent_Padlock", new Vector3(1.0f, 2.8f, 15.9f), ColdIndustrial, 0.38f, 5.0f);

        // Dispatch-green CRT screen glow (the signature company colour).
        PointLight(parent, "Light_CRT_Screen", new Vector3(1.5f, 1.2f, 6.3f), DispatchGreen, 0.5f, 1.8f);

        // Warm tungsten desk lamp on the folding table.
        PointLight(parent, "Light_DeskLamp", new Vector3(2.55f, 1.2f, 10.6f), WarmTungsten, 0.34f, 2.2f);

        // Sodium-amber pool at the threshold — the money shot when the roll-up rises.
        PointLight(parent, "Light_Threshold", new Vector3(4.5f, 3.3f, 17.2f), WarmTungsten, 0.9f, 13f);
    }

    /// <summary>The same LC post stack the office builds in-memory (grain / bloom / grade), parented
    /// under the throwaway root so it is never serialized into the scene.</summary>
    static void BuildPostVolume(Transform root)
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Medium1);
        grain.intensity.Override(0.20f);
        grain.response.Override(0.7f);

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.05f);
        bloom.intensity.Override(0.40f);
        bloom.scatter.Override(0.6f);

        var color = profile.Add<ColorAdjustments>(true);
        color.saturation.Override(-10f);
        color.contrast.Override(7f);

        var go = new GameObject("LC_PostVolume (Playtest)");
        go.transform.SetParent(root, false);
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.profile = profile;
    }

    static void SpawnPlayer(Transform parent, Vector3 localSpawn)
    {
        // Only the playtest camera should render / hear — disable any other cameras + listeners.
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
        foreach (var a in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) a.enabled = false;

        var player = new GameObject("PLAYTEST_PLAYER");
        player.transform.SetParent(parent, false);
        player.transform.localPosition = localSpawn;

        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.stepOffset = 0.5f;
        cc.slopeLimit = 75f;
        cc.center = new Vector3(0f, 1f, 0f);

        var camGo = new GameObject("PlaytestCam");
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.7f, 0f); // eye height
        var cam = camGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.05f;
        // Clear to the fog colour so there is no bright default skybox behind the silhouette.
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = FogConcrete;
        camGo.AddComponent<AudioListener>();

        // URP post-processing defaults to OFF per camera — enable it or grain/bloom/grade do nothing.
        var urpData = cam.GetUniversalAdditionalCameraData();
        if (urpData != null)
        {
            urpData.renderPostProcessing = true;
            urpData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        // Reuse the generic first-person walk rig from the Map 2 playtest (it is not Map2-specific).
        player.AddComponent<Map2PlaytestController>();
    }

    // ---------------------------------------------------------------- helpers

    static Transform Sub(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    /// <summary>
    /// Instantiate a real authored prop prefab (import-normalized: base at y=0, centred, real height) at a
    /// LOCAL position + yaw, scale 1. Imported colliders are disabled (look-test; shell boxes carry the
    /// collision). Missing prefab → labelled flat fallback box of <paramref name="fallbackSize"/> so the
    /// build never breaks and the gap is obvious.
    /// </summary>
    static GameObject Prop(Transform parent, string resPath, string name, Vector3 localPos, float yaw,
        Vector3 fallbackSize, Color? fallbackColor = null)
    {
        var prefab = Resources.Load<GameObject>(resPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[PlaytestHQ] Prop prefab missing: {resPath} — using fallback box '{name}'.");
            var box = Box(parent, name + " (FALLBACK)", localPos + Vector3.up * fallbackSize.y * 0.5f, fallbackSize,
                FlatMat(fallbackColor ?? cProp), collider: false);
            box.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return box;
        }

        var go = (GameObject)Object.Instantiate(prefab, parent);
        go.name = name;
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.transform.localScale = Vector3.one;
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = false;
        return go;
    }

    static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }
        return go;
    }

    // A box rotated about Y (for the canted far wall + roll-up). Scale is applied in local space, then yaw.
    static GameObject BoxRot(Transform parent, string name, Vector3 center, Vector3 size, float yaw, Material mat, bool collider = true)
    {
        var go = Box(parent, name, center, size, mat, collider);
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }

    static void SpawnMarker(Transform parent, string name, Vector3 center, Material mat)
    {
        Box(parent, name, center, new Vector3(0.6f, 0.04f, 0.6f), mat, collider: false);
    }

    static GameObject PointLight(Transform parent, string name, Vector3 pos, Color c, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        return go;
    }

    // Load an authored material; fall back to a tinted flat material if it is missing.
    static Material M(string assetPath, Color fallback)
    {
        var asset = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (asset != null) return asset;
        Debug.LogWarning($"[PlaytestHQ] Material not found, using flat fallback: {assetPath}");
        return FlatMat(fallback);
    }

    static Material FlatMat(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        return m;
    }

    static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);
}
