using System.Collections.Generic;
using System.Linq;
using BlackCommission.Level;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the V8 abandoned-tower whitebox from the PM-approved slab-partition plan
/// (<see cref="TowerPlanV8"/>, plan SVGs Tower_EarthCoast_01_F{1,2}_Plan_v8_proposal.svg,
/// spec design/art/spatial-language-spec.md). Replaces the v3/v5 graph-routed builder.
///
/// Realization model — NO routing code exists:
///   * Every 4 m cell belongs to exactly one slab (room / corridor / node / stair / void /
///     bridge / open plate); uncovered F1 cells become sealed poche blocks.
///   * Walls are emitted once per shared slab face; doors are holes punched at the exact
///     plan position (face mid + anti-enfilade offset). Junction doors (J#) are full-span
///     open merges with only a header beam.
///   * Section grammar per functionClass (the art-led spatial language): corridors get a
///     2.4 m perceived ceiling + cable tray + 4 m pilaster rhythm; nodes get floor markings;
///     dead rooms get 2.8 m ceilings; hubs stay open-topped (LOBBY/HALL) or get finished
///     island ceilings (SALES/SHOWFLAT); the plate is an open column field.
///   * Plan rule violations (V8-C1..C7) are a HARD ERROR: the build aborts.
///
/// Connector ids equal plan door ids, and the runtime graph is built from the same door
/// table (<see cref="TowerPlanV8.BuildCanonicalGraph"/>), so geometry and connectivity
/// can never drift.
/// </summary>
public static class TowerV8WhiteboxBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/Tower_EarthCoast_01.unity";
    const string RootName = "Tower_v8_Whitebox";
    static readonly string[] OldRootsToDelete = { "AB_FloorPlan_Blockout", "Tower_v3_Whitebox", RootName };
    // Legacy hand-placed F1 wall roots ("Wall_E_01 (n)") — PM ordered cleanup 2026-06-10.
    const string LegacyWallPrefix = "Wall_E_01";

    const float Floor2Y = 4.2f;
    const float FloorThickness = 0.08f;
    const float WallHeight = 3.2f;
    const float WallThickness = 0.28f;
    const float DoorClearHeight = 2.25f;
    const float DoorFramePostWidth = 0.18f;
    const float DoorHeaderHeight = 0.24f;

    // ---- section grammar (design/art/spatial-language-spec.md) ----
    const float CorridorCeilingY = 2.4f;   // perceived-low corridor top
    const float CableTrayY = 2.25f;        // tray hangs just under the corridor ceiling
    const float NodeCeilingY = 2.8f;
    const float RoomCeilingY = 2.8f;
    const float TargetCeilingY = 3.2f;     // L objective room: taller, display plinth below
    const float PilasterSpacing = 4f;

    static Material floorMat, wallMat, corridorMat, blockerMat, stairMat,
        scaffoldMat, extMat, finishMat, trayMat, woodMat, tealMat, paperMat, stampMat, ecoMat;

    static bool IsWalled(in PlanSlab s) =>
        s.Kind == SlabKind.Room || s.Kind == SlabKind.Corr || s.Kind == SlabKind.Stair ||
        s.Id == "COLLAPSE";

    static bool IsIslandFinish(string id) => id == "SALES" || id == "SHOWFLAT" || id == "VIP";

    [MenuItem("Tools/Black Commission/MVP/Tower/Rebuild v8 Whitebox (slab plan)")]
    public static void Rebuild()
    {
        // Plan rule violations are a hard error — never realize an invalid plan.
        List<string> planErrors = TowerPlanV8.ValidatePlan();
        if (planErrors.Count > 0)
        {
            Debug.LogError("[TowerV8] PLAN INVALID — build aborted:\n" + string.Join("\n", planErrors));
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureMaterials();

        int legacyWalls = 0;
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            bool legacy = go.name.StartsWith(LegacyWallPrefix);
            if (!legacy && !OldRootsToDelete.Contains(go.name)) continue;
            if (legacy) legacyWalls++;
            Object.DestroyImmediate(go);
        }
        if (legacyWalls > 0)
            Debug.Log($"[TowerV8] Deleted {legacyWalls} legacy '{LegacyWallPrefix}' wall roots.");

        var root = new GameObject(RootName);

        Transform floors = Group(root, "Floors");
        Transform walls = Group(root, "Walls");
        Transform ceilings = Group(root, "Ceilings_SectionGrammar");
        Transform poche = Group(root, "Poche_SealedShell");
        Transform connectors = Group(root, "Connectors");
        Transform descents = Group(root, "Descents");
        Transform plate = Group(root, "F2_OpenPlate");
        Transform lights = Group(root, "LightAnchors");
        Transform markers = Group(root, "Markers");
        Transform exterior = Group(root, "Exterior");

        BuildFloors(floors);
        BuildPoche(poche);
        int wallCount = BuildWalls(walls);
        BuildSectionGrammar(ceilings);
        BuildPlateDressing(plate);
        BuildAtriumAndBridge(plate);
        BuildConnectors(connectors);
        BuildDescents(descents);
        BuildRoomSlots(Group(root, "Rooms"));
        BuildLightAnchors(lights);
        BuildMarkers(markers);
        BuildIdentityDressing(Group(root, "IdentityDressing"));
        BuildPowerGate(Group(root, "PowerGate"));
        BuildMissionManager(Group(root, "Mission"));
        BuildExterior(exterior);
        ConfigureSceneLighting(scene);

        var gen = new GameObject("TowerLayoutGenerator");
        gen.transform.SetParent(root.transform);
        gen.AddComponent<NetworkObject>();
        gen.AddComponent<TowerLayoutGenerator>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[TowerV8] Rebuilt v8 whitebox from the slab plan: {TowerPlanV8.Slabs.Length} slabs, " +
                  $"{TowerPlanV8.Doors.Length} doors, {wallCount} wall faces, plan errors 0. " +
                  "Next: bake NavMesh, then run the 3-second readability pass per the spatial-language spec.");
    }

    // ───────────────────────────── floors ─────────────────────────────

    static void BuildFloors(Transform parent)
    {
        // F1: one slab per plan slab, material by function (corridor poured strip vs room).
        foreach (PlanSlab s in TowerPlanV8.Slabs)
        {
            if (s.Floor != 1) continue;
            Material m = s.Kind switch
            {
                SlabKind.Corr => corridorMat,
                SlabKind.Stair => stairMat,
                SlabKind.Void => blockerMat, // COLLAPSE rubble ground
                SlabKind.Van => extMat,
                _ => floorMat
            };
            AddSlab(parent, m, s.CenterX, -FloorThickness * 0.5f, s.CenterZ, s.W, FloorThickness, s.D,
                $"F1_Floor_{s.Id}");
        }
        // COLLAPSE rubble piles (slow ground, readable debris).
        AddSlab(parent, blockerMat, 4f, 0.35f, 32f, 3f, 0.7f, 2f, "F1_Collapse_Rubble_A");
        AddSlab(parent, blockerMat, 8f, 0.5f, 36f, 2f, 1.0f, 3f, "F1_Collapse_Rubble_B");
        AddSlab(parent, blockerMat, 3f, 0.25f, 38f, 2.5f, 0.5f, 1.5f, "F1_Collapse_Rebar_Fall");

        // F2: raw plate pieces. Holes: ATRIUM (12,16,12,8), STAIRB2 (0,16,4,8),
        // STAIRA2 (26,28,4,8), the collapsed corner sky shaft (0,28,12,12) and the
        // balcony drop hatch (40,16,4,2).
        var plates = new (float x, float z, float w, float d)[]
        {
            (0, 0, 44, 16),
            (4, 16, 8, 8), (24, 16, 16, 8), (40, 18, 4, 6),
            (0, 24, 44, 4),
            (12, 28, 14, 8), (30, 28, 14, 8),
            (12, 36, 32, 4),
        };
        int i = 0;
        foreach (var (x, z, w, d) in plates)
            AddSlab(parent, floorMat, x + w * 0.5f, Floor2Y - FloorThickness * 0.5f, z + d * 0.5f,
                w, FloorThickness, d, $"F2_Plate_{++i:00}");

        // Island finish-floor overlays (the precision-clean show-flat island vs raw plate).
        foreach (PlanSlab s in TowerPlanV8.Slabs)
        {
            if (s.Floor != 2 || s.Kind != SlabKind.Room) continue;
            Material m = IsIslandFinish(s.Id) ? finishMat : floorMat;
            AddVisualSlab(parent, m, s.CenterX, Floor2Y + 0.012f, s.CenterZ, s.W - 0.2f, 0.02f, s.D - 0.2f,
                $"F2_FinishFloor_{s.Id}");
        }
    }

    static void BuildPoche(Transform parent)
    {
        // Sealed unbuilt shell: every F1 grid cell not covered by a slab becomes a solid block.
        int count = 0;
        for (float cx = TowerPlanV8.OutlineX; cx < TowerPlanV8.OutlineX + TowerPlanV8.OutlineW; cx += TowerPlanV8.GridCell)
            for (float cz = TowerPlanV8.OutlineZ; cz < TowerPlanV8.OutlineZ + TowerPlanV8.OutlineD; cz += TowerPlanV8.GridCell)
            {
                bool covered = TowerPlanV8.Slabs.Any(s =>
                    s.Floor == 1 && s.Id != "VAN" &&
                    cx + TowerPlanV8.GridCell > s.X + 0.01f && cx < s.X + s.W - 0.01f &&
                    cz + TowerPlanV8.GridCell > s.Z + 0.01f && cz < s.Z + s.D - 0.01f);
                if (covered) continue;
                // Poche reaches the F2 slab — same fix as the F1 wall height (no air band).
                AddSlab(parent, blockerMat, cx + 2f, Floor2Y * 0.5f, cz + 2f, 4f, Floor2Y, 4f,
                    $"Poche_{cx:00}_{cz:00}");
                count++;
            }
        Debug.Log($"[TowerV8] Poche cells: {count} (plan expects 26).");
    }

    // ───────────────────────────── walls ─────────────────────────────

    readonly struct WallGap
    {
        public readonly float Center, Width;
        public readonly PlanDoor Door;
        public WallGap(float center, float width, PlanDoor door) { Center = center; Width = width; Door = door; }
    }

    static int BuildWalls(Transform parent)
    {
        int emitted = 0;
        foreach (int floor in new[] { 1, 2 })
        {
            var slabs = TowerPlanV8.Slabs.Where(s => s.Floor == floor).ToList();
            float y = floor == 2 ? Floor2Y : 0f;
            // F1 walls run all the way up to the F2 slab — a 3.2 m wall under a 4.2 m
            // floor left a 1 m open band between the floors (PM bug 2026-06-10).
            float wallH = floor == 1 ? Floor2Y : WallHeight;

            foreach (PlanSlab s in slabs)
            {
                if (!IsWalled(s)) continue;
                foreach (var face in FacesOf(s))
                {
                    // Neighbor intervals along this face (walled slabs emit once; bridge punches doors).
                    var intervals = new List<(float lo, float hi, PlanSlab n, bool hasNeighbor)>();
                    foreach (PlanSlab t in slabs)
                    {
                        if (t.Id == s.Id) continue;
                        if (t.Kind == SlabKind.Open || t.Kind == SlabKind.Van || t.Id == "ATRIUM") continue;
                        if (!TowerPlanV8.TryGetSharedFace(s, t, out PlanFace f)) continue;
                        if (f.Axis != face.axis || Mathf.Abs(f.At - face.at) > 0.01f) continue;
                        intervals.Add((f.Lo, f.Hi, t, true));
                    }
                    intervals.Sort((a, b) => a.lo.CompareTo(b.lo));

                    // Fill leftovers (poche / outline / plate-facing) with neighborless intervals.
                    var full = new List<(float lo, float hi, PlanSlab n, bool hasNeighbor)>();
                    float cursor = face.lo;
                    foreach (var iv in intervals)
                    {
                        if (iv.lo > cursor + 0.01f) full.Add((cursor, iv.lo, default, false));
                        full.Add(iv);
                        cursor = Mathf.Max(cursor, iv.hi);
                    }
                    if (cursor < face.hi - 0.01f) full.Add((cursor, face.hi, default, false));

                    foreach (var iv in full)
                    {
                        if (iv.hasNeighbor && IsWalled(iv.n) &&
                            string.CompareOrdinal(s.Id, iv.n.Id) > 0) continue; // other side emits

                        var doors = DoorsOnInterval(s, iv.n, iv.hasNeighbor, face.axis, face.at, iv.lo, iv.hi);
                        if (doors.Any(g => g.Door.Type == PlanDoorType.Junction))
                        {
                            // Full-span open merge: header beam only (junction grammar).
                            EmitHeaderBeam(parent, y, face.axis, face.at, iv.lo, iv.hi);
                            emitted++;
                            continue;
                        }

                        Material m = wallMat;
                        if (s.Id == "COLLAPSE" || (iv.hasNeighbor && iv.n.Id == "COLLAPSE")) m = blockerMat;
                        else if (IsIslandFinish(s.Id) || (iv.hasNeighbor && IsIslandFinish(iv.n.Id))) m = finishMat;

                        EmitWallWithGaps(parent, y, face.axis, face.at, iv.lo, iv.hi, doors, m,
                            $"Wall_{s.Id}_{face.name}", wallH);
                        emitted++;
                    }
                }
            }
        }
        return emitted;
    }

    static IEnumerable<(char axis, float at, float lo, float hi, string name)> FacesOf(PlanSlab s)
    {
        yield return ('x', s.X, s.Z, s.Z + s.D, "W");
        yield return ('x', s.X + s.W, s.Z, s.Z + s.D, "E");
        yield return ('z', s.Z, s.X, s.X + s.W, "S");
        yield return ('z', s.Z + s.D, s.X, s.X + s.W, "N");
    }

    static List<WallGap> DoorsOnInterval(PlanSlab s, PlanSlab n, bool hasNeighbor,
        char axis, float at, float lo, float hi)
    {
        var gaps = new List<WallGap>();
        foreach (PlanDoor d in TowerPlanV8.Doors)
        {
            bool pairMatch = hasNeighbor &&
                ((d.A == s.Id && d.B == n.Id) || (d.A == n.Id && d.B == s.Id));
            bool perimeterMatch = !hasNeighbor && d.IsPerimeter && (d.A == s.Id || d.B == s.Id);
            if (!pairMatch && !perimeterMatch) continue;
            if (!TowerPlanV8.TryGetDoorCenter(d, out char dAxis, out float dAt, out float pos))
            {
                if (d.Type == PlanDoorType.Junction) gaps.Add(new WallGap((lo + hi) * 0.5f, hi - lo, d));
                continue;
            }
            if (dAxis != axis || Mathf.Abs(dAt - at) > 0.01f) continue;
            if (pos < lo - 0.01f || pos > hi + 0.01f) continue;
            gaps.Add(new WallGap(pos, d.WidthM, d));
        }
        return gaps;
    }

    static void EmitWallWithGaps(Transform parent, float floorY, char axis, float at,
        float lo, float hi, List<WallGap> gaps, Material m, string name, float wallH)
    {
        gaps.Sort((a, b) => a.Center.CompareTo(b.Center));
        float cursor = lo;
        int seg = 0;
        foreach (WallGap g in gaps)
        {
            float gLo = g.Center - g.Width * 0.5f, gHi = g.Center + g.Width * 0.5f;
            if (gLo > cursor + 0.02f)
                EmitWallSeg(parent, floorY, axis, at, cursor, gLo, 0f, wallH, m, $"{name}_{++seg:00}");
            // Header over the doorway + frame posts (threshold lives on the Connector).
            EmitWallSeg(parent, floorY, axis, at, gLo, gHi, DoorClearHeight, wallH - DoorClearHeight, m,
                $"{name}_Header_{g.Door.Id}");
            EmitFramePosts(parent, floorY, axis, at, g);
            cursor = Mathf.Max(cursor, gHi);
        }
        if (cursor < hi - 0.02f)
            EmitWallSeg(parent, floorY, axis, at, cursor, hi, 0f, wallH, m, $"{name}_{++seg:00}");
    }

    static void EmitWallSeg(Transform parent, float floorY, char axis, float at,
        float lo, float hi, float yFrom, float height, Material m, string name)
    {
        if (hi - lo < 0.04f || height < 0.04f) return;
        float mid = (lo + hi) * 0.5f, len = hi - lo;
        float cy = floorY + yFrom + height * 0.5f;
        if (axis == 'x') AddSlab(parent, m, at, cy, mid, WallThickness, height, len, name);
        else AddSlab(parent, m, mid, cy, at, len, height, WallThickness, name);
    }

    static void EmitFramePosts(Transform parent, float floorY, char axis, float at, WallGap g)
    {
        float postY = floorY + DoorClearHeight * 0.5f;
        float depth = WallThickness * 1.55f;
        foreach (float side in new[] { -1f, 1f })
        {
            float p = g.Center + side * (g.Width * 0.5f + DoorFramePostWidth * 0.5f);
            if (axis == 'x')
                AddVisualSlab(parent, wallMat, at, postY, p, depth, DoorClearHeight, DoorFramePostWidth,
                    $"Frame_{g.Door.Id}{(side < 0 ? "A" : "B")}");
            else
                AddVisualSlab(parent, wallMat, p, postY, at, DoorFramePostWidth, DoorClearHeight, depth,
                    $"Frame_{g.Door.Id}{(side < 0 ? "A" : "B")}");
        }
    }

    static void EmitHeaderBeam(Transform parent, float floorY, char axis, float at, float lo, float hi)
    {
        // Junction merge: no wall, but a beam at corridor-ceiling height frames the threshold
        // (the "柱间收窄贯通" junction grammar).
        float mid = (lo + hi) * 0.5f, len = hi - lo;
        float cy = floorY + CorridorCeilingY + 0.15f;
        if (axis == 'x') AddVisualSlab(parent, trayMat, at, cy, mid, WallThickness * 1.4f, 0.3f, len, "JunctionBeam");
        else AddVisualSlab(parent, trayMat, mid, cy, at, len, 0.3f, WallThickness * 1.4f, "JunctionBeam");
    }

    // ─────────────────── section grammar (ceilings, trays, markings) ───────────────────

    static void BuildSectionGrammar(Transform parent)
    {
        foreach (PlanSlab s in TowerPlanV8.Slabs)
        {
            float y = s.Floor == 2 ? Floor2Y : 0f;
            switch (s.Function)
            {
                case SlabFunction.Corr:
                {
                    // Perceived-low top: ceiling plate + cable tray along the long axis + pilasters.
                    AddVisualSlab(parent, trayMat, s.CenterX, y + CorridorCeilingY, s.CenterZ,
                        s.W - 0.1f, 0.06f, s.D - 0.1f, $"CorrCeil_{s.Id}");
                    bool horiz = s.W >= s.D;
                    if (horiz)
                        AddVisualSlab(parent, trayMat, s.CenterX, y + CableTrayY, s.CenterZ,
                            s.W - 0.6f, 0.1f, 0.4f, $"CableTray_{s.Id}");
                    else
                        AddVisualSlab(parent, trayMat, s.CenterX, y + CableTrayY, s.CenterZ,
                            0.4f, 0.1f, s.D - 0.6f, $"CableTray_{s.Id}");
                    // Pilasters skip doorway clearances — the 4 m rhythm lands exactly on
                    // door centers in places (e.g. C4's z=20 pilaster vs D5), which read
                    // as a divider plank in the middle of the door (PM bug 2026-06-10).
                    for (float p = PilasterSpacing; p < (horiz ? s.W : s.D) - 0.1f; p += PilasterSpacing)
                    {
                        if (horiz)
                        {
                            if (!DoorNear('z', s.Z, s.X + p))
                                AddSlab(parent, wallMat, s.X + p, y + CorridorCeilingY * 0.5f, s.Z + 0.16f,
                                    0.3f, CorridorCeilingY, 0.3f, $"Pilaster_{s.Id}_S{p:00}");
                            if (!DoorNear('z', s.Z + s.D, s.X + p))
                                AddSlab(parent, wallMat, s.X + p, y + CorridorCeilingY * 0.5f, s.Z + s.D - 0.16f,
                                    0.3f, CorridorCeilingY, 0.3f, $"Pilaster_{s.Id}_N{p:00}");
                        }
                        else
                        {
                            if (!DoorNear('x', s.X, s.Z + p))
                                AddSlab(parent, wallMat, s.X + 0.16f, y + CorridorCeilingY * 0.5f, s.Z + p,
                                    0.3f, CorridorCeilingY, 0.3f, $"Pilaster_{s.Id}_W{p:00}");
                            if (!DoorNear('x', s.X + s.W, s.Z + p))
                                AddSlab(parent, wallMat, s.X + s.W - 0.16f, y + CorridorCeilingY * 0.5f, s.Z + p,
                                    0.3f, CorridorCeilingY, 0.3f, $"Pilaster_{s.Id}_E{p:00}");
                        }
                    }
                    break;
                }
                case SlabFunction.Node:
                {
                    // Junction node: taller ceiling than the corridors is the identity
                    // (yellow floor marks retired per PM 2026-06-10).
                    AddVisualSlab(parent, trayMat, s.CenterX, y + NodeCeilingY, s.CenterZ,
                        s.W - 0.1f, 0.06f, s.D - 0.1f, $"NodeCeil_{s.Id}");
                    break;
                }
                case SlabFunction.Dead when s.Kind == SlabKind.Room:
                {
                    if (s.Id == "DOCK" || s.Id == "BALCONY") break; // drop landing / exterior: open top
                    float ceil = s.Id == "TARGET" ? TargetCeilingY : RoomCeilingY;
                    Material m = IsIslandFinish(s.Id) ? finishMat : trayMat;
                    AddVisualSlab(parent, m, s.CenterX, (s.Floor == 2 ? Floor2Y : 0f) + ceil, s.CenterZ,
                        s.W - 0.15f, 0.06f, s.D - 0.15f, $"RoomCeil_{s.Id}");
                    break;
                }
                case SlabFunction.Hub when s.Floor == 2:
                {
                    // F2 island hubs read "finished" — intact suspended ceiling (identity = material).
                    AddVisualSlab(parent, finishMat, s.CenterX, Floor2Y + RoomCeilingY, s.CenterZ,
                        s.W - 0.15f, 0.06f, s.D - 0.15f, $"HubCeil_{s.Id}");
                    break;
                }
                // F1 hubs (LOBBY / HALL): no ceiling — full height / atrium above is the identity.
            }
        }

        // Corridor mid-break nodes (V8-C4): pilaster pair (doorway-clearance guarded).
        foreach (var (slabId, x, z, _) in TowerPlanV8.CorridorBreaks)
        {
            PlanSlab s = TowerPlanV8.ById[slabId];
            float y = s.Floor == 2 ? Floor2Y : 0f;
            if (!DoorNear('z', s.Z, x))
                AddSlab(parent, wallMat, x, y + CorridorCeilingY * 0.5f, s.Z + 0.25f, 0.6f, CorridorCeilingY, 0.5f, $"Break_{slabId}_S");
            if (!DoorNear('z', s.Z + s.D, x))
                AddSlab(parent, wallMat, x, y + CorridorCeilingY * 0.5f, s.Z + s.D - 0.25f, 0.6f, CorridorCeilingY, 0.5f, $"Break_{slabId}_N");
        }

        // LOBBY 裸龙骨 hint: two exposed grid beams under the 4.5 m line.
        AddVisualSlab(parent, trayMat, 18f, 3.0f, 2.5f, 11f, 0.08f, 0.12f, "LOBBY_KeelBeam_A");
        AddVisualSlab(parent, trayMat, 18f, 3.0f, 5.5f, 11f, 0.08f, 0.12f, "LOBBY_KeelBeam_B");
    }

    // ─────────────────── F2 plate / atrium / bridge dressing ───────────────────

    static void BuildPlateDressing(Transform parent)
    {
        // Frame columns: 8 m rhythm around the outline + sparse interior rows that stay out
        // of the V8-C6 sightline corridor (z 18..22).
        for (float x = 0; x <= 44; x += 8)
            foreach (float z in new[] { 0f, 40f })
                AddColumn(parent, x == 0 ? 0.3f : x >= 44 ? 43.7f : x, z == 0 ? 0.3f : 39.7f);
        for (float z = 8; z < 40; z += 8)
            foreach (float x in new[] { 0f, 44f })
                AddColumn(parent, x == 0 ? 0.3f : 43.7f, z);
        foreach (float z in new[] { 8f, 28f, 36f })
            for (float x = 8; x < 44; x += 8)
            {
                bool insideHole = (z >= 28 && x < 12) || (z >= 28 && x >= 26 && x < 30);
                bool insideRoom = TowerPlanV8.Slabs.Any(s => s.Floor == 2 && s.Kind == SlabKind.Room &&
                    x > s.X && x < s.X + s.W && z > s.Z && z < s.Z + s.D);
                if (!insideHole && !insideRoom) AddColumn(parent, x, z);
            }

        // Material stacks (cover; positions are plan data and keep the sightline corridor clear).
        int i = 0;
        foreach (var (x, z) in TowerPlanV8.PlateStacks)
            AddSlab(parent, blockerMat, x + 0.8f, Floor2Y + 0.6f, z + 0.6f, 1.6f, 1.2f, 1.2f,
                $"F2_Stack_{++i:00}");

        // Perimeter parapet (1.1 m, reads as 临边防护) + a taller invisible catch barrier.
        AddParapetRing(parent);

        // North edge: visibly broken guard stubs (坠落风险标识 flavor over the continuous
        // barrier). Yellow retired per PM 2026-06-10 — reads as plain broken concrete.
        foreach (float x in new[] { 16f, 25f, 36f })
            AddVisualSlab(parent, blockerMat, x, Floor2Y + 0.35f, 39.8f, 1.4f, 0.7f, 0.12f, "F2_BrokenGuard");
    }

    static void AddColumn(Transform parent, float x, float z)
    {
        AddSlab(parent, wallMat, x, Floor2Y + 1.6f, z, 0.4f, 3.2f, 0.6f, $"F2_Column_{x:00}_{z:00}");
        // Rebar cage stub on top (the "钢筋树林" silhouette).
        AddVisualSlab(parent, trayMat, x, Floor2Y + 3.5f, z, 0.12f, 0.6f, 0.12f, "F2_RebarStub");
    }

    static void AddParapetRing(Transform parent)
    {
        void Edge(float cx, float cz, float w, float d, string n)
        {
            AddSlab(parent, blockerMat, cx, Floor2Y + 0.55f, cz, w, 1.1f, d, $"F2_Parapet_{n}");
            var catcher = GameObject.CreatePrimitive(PrimitiveType.Cube);
            catcher.name = $"F2_EdgeBarrier_{n}";
            catcher.transform.SetParent(parent);
            catcher.transform.position = new Vector3(cx, Floor2Y + 1.6f, cz);
            catcher.transform.localScale = new Vector3(Mathf.Max(w, 0.2f), 3.2f, Mathf.Max(d, 0.2f));
            Object.DestroyImmediate(catcher.GetComponent<Renderer>());
        }
        Edge(22f, 0.1f, 44f, 0.2f, "S");
        Edge(22f, 39.9f, 44f, 0.2f, "N");
        Edge(0.1f, 20f, 0.2f, 40f, "W");
        Edge(43.9f, 20f, 0.2f, 40f, "E");
    }

    static void BuildAtriumAndBridge(Transform parent)
    {
        PlanSlab atrium = TowerPlanV8.ById["ATRIUM"];
        PlanSlab bridge = TowerPlanV8.ById["BRIDGE2"];

        // Scaffold bridge deck + double-pipe rails (the only crossing; fall = damage by design).
        AddSlab(parent, scaffoldMat, bridge.CenterX, Floor2Y - FloorThickness * 0.5f, bridge.CenterZ,
            bridge.W, FloorThickness, bridge.D, "Bridge_Deck");
        foreach (float z in new[] { bridge.Z + 0.1f, bridge.Z + bridge.D - 0.1f })
            foreach (float h in new[] { 0.5f, 1.0f })
                AddVisualSlab(parent, scaffoldMat, bridge.CenterX, Floor2Y + h, z,
                    bridge.W, 0.07f, 0.07f, "Bridge_Rail");

        // Atrium rim toe-guards on the open edges (bridge spans the middle band).
        AddVisualSlab(parent, blockerMat, atrium.CenterX, Floor2Y + 0.1f, atrium.Z + 0.07f,
            atrium.W, 0.2f, 0.14f, "Atrium_Rim_S");
        AddVisualSlab(parent, blockerMat, atrium.CenterX, Floor2Y + 0.1f, atrium.Z + atrium.D - 0.07f,
            atrium.W, 0.2f, 0.14f, "Atrium_Rim_N");
        foreach (float z in new[] { atrium.Z + 1f, atrium.Z + atrium.D - 1f })
            AddVisualSlab(parent, blockerMat, atrium.X + 0.07f, Floor2Y + 0.1f, z, 0.14f, 0.2f, 2f, "Atrium_Rim_W");
    }

    // ───────────────────────────── connectors / descents ─────────────────────────────

    static void BuildConnectors(Transform parent)
    {
        foreach (PlanDoor d in TowerPlanV8.Doors)
        {
            var conn = new GameObject($"Connector_{d.Id}");
            conn.transform.SetParent(parent);

            var geometry = new GameObject("Geometry");
            geometry.transform.SetParent(conn.transform);
            GameObject blocker = null;

            if (TowerPlanV8.TryGetDoorCenter(d, out char axis, out float at, out float pos))
            {
                int floor = TowerPlanV8.ById.TryGetValue(d.A, out PlanSlab a) && a.Floor == 2 ? 2
                    : TowerPlanV8.ById.TryGetValue(d.B, out PlanSlab b) && b.Floor == 2 ? 2 : 1;
                if (d.Id == "D30" || d.Id == "D31") floor = 2;
                float y = floor == 2 ? Floor2Y : 0f;
                Vector3 p = axis == 'x' ? new Vector3(at, y, pos) : new Vector3(pos, y, at);
                conn.transform.position = p;

                // Threshold strip (toggles off when closed; the rubble plug takes its place).
                float w = Mathf.Max(d.WidthM, 0.6f);
                if (axis == 'x')
                    AddVisualSlab(geometry.transform, corridorMat, at, y + 0.025f, pos,
                        WallThickness * 1.55f, 0.05f, w, "Threshold");
                else
                    AddVisualSlab(geometry.transform, corridorMat, pos, y + 0.025f, at,
                        w, 0.05f, WallThickness * 1.55f, "Threshold");

                if (d.Type == PlanDoorType.Toggle)
                {
                    blocker = new GameObject("Blocker");
                    blocker.transform.SetParent(conn.transform);
                    var plug = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plug.name = "RubblePlug";
                    plug.transform.SetParent(blocker.transform);
                    plug.transform.position = axis == 'x'
                        ? new Vector3(at, y + DoorClearHeight * 0.5f, pos)
                        : new Vector3(pos, y + DoorClearHeight * 0.5f, at);
                    plug.transform.localScale = axis == 'x'
                        ? new Vector3(0.8f, DoorClearHeight, d.WidthM)
                        : new Vector3(d.WidthM, DoorClearHeight, 0.8f);
                    plug.GetComponent<Renderer>().sharedMaterial = blockerMat;
                    var obstacle = plug.AddComponent<UnityEngine.AI.NavMeshObstacle>();
                    obstacle.carving = true;
                    obstacle.size = Vector3.one;
                    blocker.SetActive(false);
                }
            }

            var c = conn.AddComponent<Connector>();
            c.id = d.Id;
            c.aSlotId = d.A;
            c.bSlotId = d.B;
            c.kind = d.Type == PlanDoorType.Junction || d.WidthM >= 2.8f ? EdgeKind.Corridor : EdgeKind.Door;
            SetConnectorRefs(c, geometry, blocker);
        }
    }

    static void BuildDescents(Transform parent)
    {
        foreach (var (id, upper, lower, _) in TowerPlanV8.Descents)
        {
            var conn = new GameObject($"Connector_{id}");
            conn.transform.SetParent(parent);
            var geometry = new GameObject("Geometry");
            geometry.transform.SetParent(conn.transform);

            if (id == "E-DROP")
            {
                // Balcony hatch -> straight one-way drop into the DOCK (PM 2026-06-10:
                // scaffold platforms deleted; fall = damage + completeness loss by design).
                // Connector stays so the route remains in the graph. Landing mark only.
                AddVisualSlab(geometry.transform, scaffoldMat, 42f, 0.012f, 17f, 1.8f, 0.02f, 1.8f, "Drop_LandingMark");
            }
            else if (id == "E-STAIRA")
            {
                // Dog-leg scissor stair (design/levels/tower-stair-redesign-v2.md + UX
                // notes): two 2 m z-runs side by side with a central spine wall, half
                // landing at the north end, top landing at the south end feeding D35.
                // Entering D10 (south, center x=28) the up-flight rises directly ahead.
                Transform g = geometry.transform;
                AddRampFlight(g, stairMat, new Vector2(27f, 29.5f), new Vector2(27f, 34f), 12, 2f, 0f, 2.1f);
                AddSlab(g, stairMat, 28f, 2.0f, 35f, 4f, 0.2f, 2f, "A_MID");
                AddRampFlight(g, stairMat, new Vector2(29f, 34f), new Vector2(29f, 29.5f), 12, 2f, 2.1f, Floor2Y);
                AddSlab(g, stairMat, 28f, Floor2Y - 0.1f, 28.75f, 4f, 0.2f, 1.5f, "A_TOP");
                AddSlab(g, wallMat, 28f, 2.1f, 31.75f, 0.24f, 4.2f, 4.5f, "A_Spine");
            }
            else // E-STAIRB
            {
                // Dog-leg across the 4 m shaft width (31° is the x-axis cap, spec §2):
                // south lane (z17..19) climbs west straight off the D5 threshold, west
                // landing x0..2/z19..23, north lane (z21..23) climbs east back to the
                // top platform at the D30 debt shutter (x2.5..4, z18.6..23).
                Transform g = geometry.transform;
                // run1 starts at x=3.3, NOT at the threshold: starting at 3.72 put the
                // first steps inside the D5 doorway, so the climber met the door header
                // (y2.25) with only ~1.7m of headroom (validator-confirmed PM bug).
                AddRampFlight(g, stairMat, new Vector2(3.3f, 18f), new Vector2(0.28f, 18f), 12, 2f, 0f, 2.1f);
                AddSlab(g, stairMat, 1f, 2.0f, 21f, 2f, 0.2f, 4f, "B_MID");
                // run2 stops at x=3.0 so the top platform never overhangs the climb —
                // the old x3.72 run rose INTO B_TOP's underside (PM head-bonk bug).
                AddRampFlight(g, stairMat, new Vector2(0.28f, 22f), new Vector2(3.0f, 22f), 12, 2f, 2.1f, Floor2Y);
                AddSlab(g, stairMat, 3.5f, Floor2Y - 0.1f, 22.05f, 1.0f, 0.2f, 2.9f, "B_TOP"); // x3..4, covers D30 @z20.6..23.4
            }

            var c = conn.AddComponent<Connector>();
            c.id = id;
            c.aSlotId = upper;
            c.bSlotId = lower;
            c.kind = id == "E-DROP" ? EdgeKind.ScaffoldDrop : EdgeKind.Stair;
            SetConnectorRefs(c, geometry, null); // descents are fixed-open; no blocker
        }
    }

    // ───────────────────────────── slots / lights / markers / exterior ─────────────────────────────

    static void BuildRoomSlots(Transform parent)
    {
        foreach (PlanSlab s in TowerPlanV8.Slabs)
        {
            bool fillable = s.Kind == SlabKind.Room || s.Kind == SlabKind.Stair || s.Kind == SlabKind.Van;
            if (!fillable) continue;

            var go = new GameObject(s.Id);
            go.transform.SetParent(parent);
            float y = s.Floor == 2 ? Floor2Y : 0f;

            var anchor = new GameObject(s.Id + "_Slot");
            anchor.transform.SetParent(go.transform);
            anchor.transform.position = new Vector3(s.CenterX, y, s.CenterZ);
            var slot = anchor.AddComponent<RoomSlot>();
            slot.size = s.Size switch
            {
                PlanSize.S => RoomSizeClass.Small,
                PlanSize.L => RoomSizeClass.Large,
                _ => RoomSizeClass.Medium
            };
            slot.role = s.Id switch
            {
                "VAN" => RoomSlotRole.Van,
                "POWER" => RoomSlotRole.PowerGate,
                "TARGET" => RoomSlotRole.Objective,
                _ when s.Kind == SlabKind.Stair => RoomSlotRole.Stair,
                // Hubs are transit space: zero random loot (affordance rule from the spec).
                _ when s.Function == SlabFunction.Hub => RoomSlotRole.Fixed,
                "BALCONY" => RoomSlotRole.Fixed,
                _ => RoomSlotRole.Random
            };
            slot.floor = s.Floor;
            slot.slotId = s.Id;
        }
    }

    static void BuildLightAnchors(Transform parent)
    {
        foreach (var (id, floor, x, z, hex, label) in TowerPlanV8.LightAnchors)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            var go = new GameObject($"{id}_{label}");
            go.transform.SetParent(parent);

            float baseY = floor == 2 ? Floor2Y : 0f;
            float y = baseY + 2.3f;
            float range = 8f, intensity = 1.6f;
            switch (id)
            {
                case "LA-SKY": y = baseY + 7.5f; range = 18f; intensity = 1.3f; break;   // skylight down the collapse shaft
                case "LA-BEACON": y = baseY + 2.6f; range = 30f; intensity = 2.6f; break; // whole-map lighthouse
                case "LA-P01": y = baseY + 2.2f; range = 6f; intensity = 2.0f; break;
                case "LA-ECO": y = baseY + 1.5f; range = 9f; intensity = 1.4f; break;
                case "LA-DUTY": range = 10f; break;
                case "LA-SODIUM": y = baseY + 4.0f; break;        // high on the flight side wall
                case "LA-SODIUM2": y = baseY + 2.5f; range = 6f; intensity = 1.2f; break; // door spill
            }
            go.transform.position = new Vector3(x, y, z);
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }
    }

    static void BuildMarkers(Transform parent)
    {
        // Seed-randomized monster starts (感染监理) — consumed later by the spawner.
        foreach (var (id, x, z) in TowerPlanV8.MonsterSeeds)
        {
            var go = new GameObject($"MonsterSeed_{id}");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, Floor2Y, z);
        }
        // Target plinth (生态柱 display base) inside the objective room.
        AddSlab(parent, blockerMat, 38f, Floor2Y + 0.5f, 12f, 2f, 1.0f, 2f, "TARGET_EcoColumnPlinth");
        BuildEcoColumn(parent);
        // SALES: sand-table set dressing, centred to block the D32->D33 sightline (anti-enfilade prop).
        AddSlab(parent, woodMat, 28f, Floor2Y + 0.55f, 20f, 3.2f, 1.1f, 2.2f, "SALES_SandTable_Dressing");
    }

    /// <summary>
    /// BC identity pass (docs/design/lofi-readability-notes.md §4): the cheapest
    /// LC-vs-BC differentiator is the civic-paperwork semantic layer — rolled debt
    /// shutters in worn civic teal over the F2 stair doorways, plus 催缴/封条 paper
    /// notices (aged paper + stamp red) beside the doors that carry the debt story.
    /// </summary>
    static void BuildIdentityDressing(Transform parent)
    {
        // Rolled shutter boxes (欠款卷帘①/②) above the two F2 stair doorways.
        foreach (string doorId in new[] { "D30", "D35" })
        {
            PlanDoor d = TowerPlanV8.Doors.First(x => x.Id == doorId);
            if (!TowerPlanV8.TryGetDoorCenter(d, out char ax, out float at, out float pos)) continue;
            float y = Floor2Y + DoorClearHeight + 0.26f;
            if (ax == 'x')
                AddVisualSlab(parent, tealMat, at, y, pos, 0.45f, 0.45f, d.WidthM + 0.5f, $"ShutterBox_{doorId}");
            else
                AddVisualSlab(parent, tealMat, pos, y, at, d.WidthM + 0.5f, 0.45f, 0.45f, $"ShutterBox_{doorId}");
        }

        // Paper notices: (door, offset along the face from the door centre, which side of
        // the wall (+1 = +axis normal), floor base y). Stamp red block on the lower third.
        var notices = new (string door, float along, float side, float baseY)[]
        {
            ("D-VAN", 1.9f, -1f, 0f),       // 进楼封条 beside the van entrance, exterior side
            ("D4",    1.6f,  1f, 0f),       // P-01 配电房催缴单, corridor side
            ("D30",  -2.1f,  1f, Floor2Y),  // 欠款卷帘① notice, plate side
            ("D34",   1.9f, -1f, Floor2Y),  // 生态柱展厅封条, showflat side
        };
        foreach (var n in notices)
        {
            PlanDoor d = TowerPlanV8.Doors.First(x => x.Id == n.door);
            if (!TowerPlanV8.TryGetDoorCenter(d, out char ax, out float at, out float pos)) continue;
            float along = pos + n.along;
            float wallOut = at + n.side * 0.22f;
            float yPaper = n.baseY + 1.55f;
            if (ax == 'x')
            {
                AddVisualSlab(parent, paperMat, wallOut, yPaper, along, 0.04f, 0.6f, 0.45f, $"Notice_{n.door}");
                AddVisualSlab(parent, stampMat, wallOut + n.side * 0.025f, yPaper - 0.16f, along, 0.02f, 0.16f, 0.16f, $"NoticeStamp_{n.door}");
            }
            else
            {
                AddVisualSlab(parent, paperMat, along, yPaper, wallOut, 0.45f, 0.6f, 0.04f, $"Notice_{n.door}");
                AddVisualSlab(parent, stampMat, along, yPaper - 0.16f, wallOut + n.side * 0.025f, 0.16f, 0.16f, 0.02f, $"NoticeStamp_{n.door}");
            }
        }
    }

    /// <summary>
    /// Step ④ power-gate wiring (design/levels/abandoned-tower-earth-coast-01.md):
    /// the P-01 breaker prop in POWER drives <see cref="PowerGateBreaker"/> — holding it
    /// 3 s (host-validated) drops the closed debt shutters on D30/D35 (the only F2 doors)
    /// and turns on the stair work lights. Shutters carry colliders + carving obstacles,
    /// so both players and NavMesh agents are gated until power is restored.
    /// </summary>
    static void BuildPowerGate(Transform parent)
    {
        // Breaker cabinet on the POWER room north wall (P-01 red light hangs above it).
        var breakerGo = new GameObject("P01_BreakerCabinet");
        breakerGo.transform.SetParent(parent);
        breakerGo.transform.position = new Vector3(2f, 0f, 15.4f);
        AddSlab(breakerGo.transform, tealMat, 2f, 1.25f, 15.55f, 0.9f, 1.5f, 0.3f, "Cabinet_Body");
        AddVisualSlab(breakerGo.transform, trayMat, 2f, 1.45f, 15.36f, 0.5f, 0.6f, 0.08f, "Cabinet_Panel");
        AddVisualSlab(breakerGo.transform, trayMat, 2f, 1.0f, 15.3f, 0.12f, 0.45f, 0.12f, "Cabinet_Lever");

        // Closed shutter planks filling the two F2 stair doorways until power returns.
        var shutters = new List<GameObject>();
        foreach (string doorId in new[] { "D30", "D35" })
        {
            PlanDoor d = TowerPlanV8.Doors.First(x => x.Id == doorId);
            if (!TowerPlanV8.TryGetDoorCenter(d, out char ax, out float at, out float pos)) continue;
            var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = $"PowerShutter_{doorId}";
            plank.transform.SetParent(parent);
            plank.transform.position = ax == 'x'
                ? new Vector3(at, Floor2Y + DoorClearHeight * 0.5f, pos)
                : new Vector3(pos, Floor2Y + DoorClearHeight * 0.5f, at);
            plank.transform.localScale = ax == 'x'
                ? new Vector3(0.34f, DoorClearHeight, d.WidthM)
                : new Vector3(d.WidthM, DoorClearHeight, 0.34f);
            plank.GetComponent<Renderer>().sharedMaterial = tealMat;
            var obstacle = plank.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.carving = true;
            shutters.Add(plank);
        }

        // Stair work lights, OFF until power is restored (the lighting swap).
        var powerLights = new GameObject("F2_PowerLights");
        powerLights.transform.SetParent(parent);
        foreach (var (nm, x, z) in new[] { ("WorkLight_StairA", 28f, 32f), ("WorkLight_StairB", 2f, 20f) })
        {
            var lp = new GameObject(nm);
            lp.transform.SetParent(powerLights.transform);
            lp.transform.position = new Vector3(x, Floor2Y + 2.6f, z);
            var light = lp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.83f, 0.55f); // warm work light per art bible
            light.range = 9f;
            light.intensity = 1.7f;
            light.shadows = LightShadows.None;
        }
        powerLights.SetActive(false);

        breakerGo.AddComponent<NetworkObject>();
        var gate = breakerGo.AddComponent<PowerGateBreaker>();
        var so = new SerializedObject(gate);
        so.FindProperty("enableWhenRestored").arraySize = 1;
        so.FindProperty("enableWhenRestored").GetArrayElementAtIndex(0).objectReferenceValue = powerLights;
        var dis = so.FindProperty("disableWhenRestored");
        dis.arraySize = shutters.Count;
        for (int i = 0; i < shutters.Count; i++)
            dis.GetArrayElementAtIndex(i).objectReferenceValue = shutters[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 「真实海岸」生态柱 — the carriable mission objective (heavy two-hand carry,
    /// design/levels/abandoned-tower-earth-coast-01.md). Sealed glass column on the
    /// TARGET plinth; dropping it costs completeness (Carriable.dropDamageThreshold).
    /// </summary>
    static void BuildEcoColumn(Transform parent)
    {
        var col = new GameObject("EcoColumn_Objective");
        col.transform.SetParent(parent);
        // Plinth top is Floor2Y+1.0; capsule half-height 0.85 + resting epsilon.
        col.transform.position = new Vector3(38f, Floor2Y + 1.0f + 0.86f, 12f);

        var glass = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glass.name = "Glass";
        glass.transform.SetParent(col.transform, false);
        glass.transform.localScale = new Vector3(0.55f, 0.62f, 0.55f); // 1.24 m tall body
        Object.DestroyImmediate(glass.GetComponent<Collider>());
        glass.GetComponent<Renderer>().sharedMaterial = ecoMat;

        foreach (var (nm, y) in new[] { ("Cap_Top", 0.7f), ("Cap_Base", -0.7f) })
        {
            var cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cap.name = nm;
            cap.transform.SetParent(col.transform, false);
            cap.transform.localPosition = new Vector3(0f, y, 0f);
            cap.transform.localScale = new Vector3(0.62f, 0.08f, 0.62f);
            Object.DestroyImmediate(cap.GetComponent<Collider>());
            cap.GetComponent<Renderer>().sharedMaterial = trayMat;
        }

        var capsule = col.AddComponent<CapsuleCollider>();
        capsule.height = 1.7f;
        capsule.radius = 0.32f;

        var rb = col.AddComponent<Rigidbody>();
        rb.mass = 45f; // heavy two-hand carry weight class

        col.AddComponent<NetworkObject>();
        var carriable = col.AddComponent<EcoColumnCarriable>(); // forwards hard impacts to the mission manager
        var so = new SerializedObject(carriable);
        so.FindProperty("isHeavy").boolValue = true;
        so.FindProperty("dropDamageThreshold").floatValue = 3.5f; // fragile: drops cost completeness
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Tower mission wiring (design/quick-specs/tower-mission-manager-2026-06-10.md):
    /// host-authoritative manager + van cargo zone (set the column DOWN inside to count
    /// as aboard) + depart lever at the van. References wired via SerializedObject.
    /// </summary>
    static void BuildMissionManager(Transform parent)
    {
        PlanSlab van = TowerPlanV8.ById["VAN"];

        // Cargo zone: rear half of the van pad, building side.
        var zoneGo = new GameObject("VAN_CargoZone");
        zoneGo.transform.SetParent(parent);
        zoneGo.transform.position = new Vector3(van.CenterX, 1.25f, van.Z + van.D - 2f);
        var zone = zoneGo.AddComponent<BoxCollider>();
        zone.isTrigger = true;
        zone.size = new Vector3(8f, 2.5f, 4f);

        // Depart lever post beside the cargo zone (civic teal, child collider for interact).
        var leverGo = new GameObject("VAN_DepartLever");
        leverGo.transform.SetParent(parent);
        leverGo.transform.position = new Vector3(van.X + 1.2f, 0f, van.Z + van.D - 1f);
        AddSlab(leverGo.transform, tealMat, van.X + 1.2f, 0.6f, van.Z + van.D - 1f, 0.3f, 1.2f, 0.3f, "Lever_Post");
        AddVisualSlab(leverGo.transform, stampMat, van.X + 1.2f, 1.28f, van.Z + van.D - 1f, 0.12f, 0.35f, 0.12f, "Lever_Handle");

        var managerGo = new GameObject("TowerMissionManager");
        managerGo.transform.SetParent(parent);
        managerGo.AddComponent<NetworkObject>();
        var manager = managerGo.AddComponent<TowerMissionManager>();

        var eco = Object.FindFirstObjectByType<EcoColumnCarriable>(FindObjectsInactive.Include);
        var so = new SerializedObject(manager);
        so.FindProperty("ecoColumn").objectReferenceValue = eco;
        so.FindProperty("cargoZone").objectReferenceValue = zone;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (eco == null)
            Debug.LogWarning("[TowerV8] Mission manager built but no EcoColumnCarriable found to wire.");

        var lever = leverGo.AddComponent<TowerVanDepartLever>();
        var leverSo = new SerializedObject(lever);
        leverSo.FindProperty("manager").objectReferenceValue = manager;
        leverSo.ApplyModifiedPropertiesWithoutUndo();

        // Van exit point: board/sit + lockers + return decision (MvpHud van panel).
        var exitGo = new GameObject("VAN_ExitPoint");
        exitGo.transform.SetParent(parent);
        exitGo.transform.position = new Vector3(van.CenterX - 2.5f, 1.0f, van.Z + van.D - 1.5f);
        var exitCol = exitGo.AddComponent<BoxCollider>();
        exitCol.size = new Vector3(2.5f, 2f, 2.5f);
        exitGo.AddComponent<NetworkObject>();
        exitGo.AddComponent<MissionVanExitPoint>();
        // HQ-flow entry: PlayerController.GetSceneSafePosition finds this by name.
        var spawn = new GameObject("PlayerSpawnPoint");
        spawn.transform.SetParent(parent);
        spawn.transform.position = new Vector3(van.CenterX, 0.1f, van.Z + 2f);
    }

    /// <summary>
    /// Tower mood lighting (art bible: lo-fi silhouette-driven darkness). The scene's
    /// default white sun flattened everything (PM: "光线还没有"); now it's a dim cold
    /// overcast key with soft shadows — F2's open plate gets gray sky light, F1 goes
    /// dark under the slab and reads by the sodium/red/teal light anchors + flashlights.
    /// Concrete fog gives depth without hiding the LA-BEACON read across the map.
    /// </summary>
    static void ConfigureSceneLighting(Scene scene)
    {
        Light sun = null;
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == "Directional Light" && go.TryGetComponent(out Light l)) { sun = l; break; }
        if (sun == null)
        {
            var sunGo = new GameObject("Directional Light");
            sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.color = new Color(0.722f, 0.769f, 0.808f);   // overcast steel-gray sky
        sun.intensity = 0.32f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.94f;
        sun.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.078f, 0.090f, 0.106f);  // civic blue-black haze
        RenderSettings.fogDensity = 0.026f;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.200f, 0.227f, 0.259f);
        RenderSettings.ambientEquatorColor = new Color(0.125f, 0.141f, 0.157f);
        RenderSettings.ambientGroundColor = new Color(0.071f, 0.078f, 0.086f);
        RenderSettings.ambientIntensity = 1.0f;
    }

    static void BuildExterior(Transform parent)
    {
        float x0 = -6, x1 = 50, z0 = -16, z1 = 46;
        AddSlab(parent, extMat, (x0 + x1) * 0.5f, 0.6f, z0, x1 - x0, 1.2f, 0.4f, "Fence_S");
        AddSlab(parent, extMat, (x0 + x1) * 0.5f, 0.6f, z1, x1 - x0, 1.2f, 0.4f, "Fence_N");
        AddSlab(parent, extMat, x0, 0.6f, (z0 + z1) * 0.5f, 0.4f, 1.2f, z1 - z0, "Fence_W");
        AddSlab(parent, extMat, x1, 0.6f, (z0 + z1) * 0.5f, 0.4f, 1.2f, z1 - z0, "Fence_E");

        PlanSlab van = TowerPlanV8.ById["VAN"];
        AddSlab(parent, extMat, van.CenterX, -FloorThickness, van.CenterZ,
            van.W + 6, FloorThickness, van.D + 4, "Forecourt");

        // East perimeter run: fire exit (north outline) back to the forecourt, with cover.
        AddSlab(parent, extMat, 46.5f, -FloorThickness, 16f, 5f, FloorThickness, 56f, "PerimeterRun_East");
        AddSlab(parent, extMat, 33.5f, -FloorThickness, 42.5f, 22f, FloorThickness, 5f, "PerimeterRun_North");
        AddSlab(parent, blockerMat, 46f, 1.5f, 6f, 2f, 3f, 2f, "Prop_CraneBase");
        AddSlab(parent, blockerMat, 47.5f, 0.6f, 26f, 1.5f, 1.2f, 4f, "Prop_RebarStack");
        AddSlab(parent, blockerMat, 44f, 0.6f, 42f, 3f, 1.2f, 1.5f, "Prop_SpoilPile");
    }

    // ───────────────────────────── primitives ─────────────────────────────

    static Transform Group(GameObject root, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform);
        return go.transform;
    }

    static void AddRampFlight(Transform parent, Material mat, Vector2 bottom, Vector2 top, int steps,
        float width = 2.2f, float yFrom = 0f, float yTo = Floor2Y)
    {
        // Runs may go along x or z; the step boxes orient to the run direction.
        bool alongZ = Mathf.Abs(top.y - bottom.y) >= Mathf.Abs(top.x - bottom.x);
        float depth = Vector2.Distance(bottom, top) / steps + 0.45f;
        for (int i = 0; i < steps; i++)
        {
            float t = (i + 0.5f) / steps;
            float x = Mathf.Lerp(bottom.x, top.x, t);
            float z = Mathf.Lerp(bottom.y, top.y, t);
            float yTop = Mathf.Lerp(yFrom, yTo, (i + 1f) / steps);
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"Step_{i + 1:00}";
            step.transform.SetParent(parent);
            step.transform.position = new Vector3(x, yTop - 0.11f, z);
            step.transform.localScale = alongZ
                ? new Vector3(width, 0.22f, depth)
                : new Vector3(depth, 0.22f, width);
            step.GetComponent<Renderer>().sharedMaterial = mat;
            ApplyWorldTiling(step, mat);
            // Steps are visual-only: the CharacterController snapping up each discrete
            // 0.175 m tread reads as heavy stutter (PM 2026-06-10). Walking happens on
            // the invisible ramp collider below; the agent/NavMesh use it too.
            Object.DestroyImmediate(step.GetComponent<Collider>());
        }

        // One smooth invisible ramp collider per flight, its top plane lifted half a
        // tread (rise/steps/2) so no step box pokes through the walk surface.
        float rise = yTo - yFrom;
        float lift = Mathf.Abs(rise) / steps * 0.5f;
        Vector3 a = new(bottom.x, yFrom + lift, bottom.y);
        Vector3 b = new(top.x, yTo + lift, top.y);
        const float rampThickness = 0.25f;
        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp_WalkSurface";
        ramp.transform.SetParent(parent);
        ramp.transform.rotation = Quaternion.LookRotation(b - a);
        ramp.transform.position = (a + b) * 0.5f - ramp.transform.up * (rampThickness * 0.5f);
        ramp.transform.localScale = new Vector3(width, rampThickness, Vector3.Distance(a, b));
        Object.DestroyImmediate(ramp.GetComponent<Renderer>());
    }

    static void AddSlab(Transform parent, Material mat, float cx, float cy, float cz,
        float sx, float sy, float sz, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(cx, cy, cz);
        go.transform.localScale = new Vector3(sx, sy, sz);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        ApplyWorldTiling(go, mat);
    }

    static void AddVisualSlab(Transform parent, Material mat, float cx, float cy, float cz,
        float sx, float sy, float sz, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(cx, cy, cz);
        go.transform.localScale = new Vector3(sx, sy, sz);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        ApplyWorldTiling(go, mat);
        var collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
    }

    // Connector.geometry/blocker are private [SerializeField]; wire them via SerializedObject.
    static void SetConnectorRefs(Connector c, GameObject geometry, GameObject blocker)
    {
        var so = new SerializedObject(c);
        so.FindProperty("geometry").objectReferenceValue = geometry;
        so.FindProperty("blocker").objectReferenceValue = blocker;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    const string MatFolder = "Assets/_Project/Art/Maps/Tower_EarthCoast_01/Materials/Whitebox";

    static void EnsureMaterials()
    {
        // Lo-fi industrial horror palette (docs/art/black-commission-style-lock-v2.md):
        // ambientCG albedo maps clamped to 256px (visible texel grain is the feature),
        // no normal/AO/metalness micro detail, high roughness, art-bible color tints.
        // Markings stay flat — paint is genuinely flat.
        floorMat    = MatAsset("V8_Concrete_Slab",      "#8A8A8A", 0f,    0.20f, "Concrete048");
        wallMat     = MatAsset("V8_Concrete_WallRaw",   "#A0A0A0", 0f,    0.18f, "Concrete034");
        corridorMat = MatAsset("V8_Concrete_Poured",    "#7E837C", 0f,    0.20f, "Concrete048");
        // V8_Marking_AgedYellow retired (PM 2026-06-10): yellow posts/floor marks deleted;
        // hazard marking returns later as decals if needed. Asset kept on disk.
        blockerMat  = MatAsset("V8_Rubble",             "#9A8F84", 0f,    0.12f, "Gravel043");
        stairMat    = MatAsset("V8_Concrete_Stair",     "#90928A", 0f,    0.20f, "Concrete034");
        scaffoldMat = MatAsset("V8_Steel_Rust",         "#A06A45", 0.35f, 0.22f, "MetalWalkway014");
        extMat      = MatAsset("V8_Asphalt_Muddy",      "#8F8F88", 0f,    0.12f, "Asphalt031");
        finishMat   = MatAsset("V8_Finish_OffWhite",    "#D8D4C8", 0f,    0.30f, "Tiles133D");
        trayMat     = MatAsset("V8_Steel_Dark",         "#4A4845", 0.45f, 0.25f, "Metal063");
        woodMat     = MatAsset("V8_Wood_Formwork",      "#B09275", 0f,    0.20f, "Planks037A");
        // BC identity accents (AGENTS.md Municipal Debt Noir): worn civic-teal painted
        // metal, aged paper, stamp red (paper/signage ONLY per the art bible). Flat —
        // paint and paper are genuinely flat surfaces (style-lock v2 §4).
        tealMat     = MatAsset("V8_Civic_TealPaint",    "#3F5F5C", 0.2f,  0.25f);
        paperMat    = MatAsset("V8_Paper_Aged",         "#D6CCAE", 0f,    0.15f);
        stampMat    = MatAsset("V8_Stamp_Red",          "#C23A2B", 0f,    0.20f);
        // 生态柱 sealed-glass objective: the map's ONLY teal-green hue (#7FD4C0 = objective
        // semantics per the spatial-language spec) and the one smoothness exception —
        // sealed glass holding live Earth biology. Emissive so it self-identifies in the dark.
        ecoMat      = MatAsset("V8_EcoColumn_Glass",    "#7FD4C0", 0f,    0.5f);
        ecoMat.EnableKeyword("_EMISSION");
        ecoMat.SetColor("_EmissionColor", new Color(0.498f, 0.831f, 0.753f) * 0.9f);
        EditorUtility.SetDirty(ecoMat);
    }

    static Material MatAsset(string name, string hex, float metallic, float smoothness, string acgId = null)
    {
        string path = $"{MatFolder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            System.IO.Directory.CreateDirectory(MatFolder);
            AssetDatabase.Refresh();
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }
        ColorUtility.TryParseHtmlString(hex, out Color c);
        mat.color = c;
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", acgId == null ? null : LoadLoFiTexture(acgId));
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>Loads an ambientCG Color map clamped to 256px (style-lock v2 §4 lo-fi rule).</summary>
    static Texture2D LoadLoFiTexture(string acgId)
    {
        string[] guids = AssetDatabase.FindAssets(acgId + "_1K-JPG_Color");
        if (guids.Length == 0)
        {
            Debug.LogWarning($"[TowerV8] Missing ambientCG Color map for '{acgId}' — material stays flat.");
            return null;
        }
        string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        if (AssetImporter.GetAtPath(assetPath) is TextureImporter imp &&
            (imp.maxTextureSize != 256 || imp.filterMode != FilterMode.Point || imp.mipmapEnabled))
        {
            // Lo-fi rules (style-lock v2 + docs/art/lofi-blur-diagnosis.md): 256px,
            // Point filter, no mips — bilinear/mips smear the texel grain into mush.
            imp.maxTextureSize = 256;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    /// <summary>
    /// True when a plan door's clear opening (plus margin) covers position <paramref name="along"/>
    /// on the wall plane at <paramref name="facePlane"/> — used to keep pilasters/columns
    /// out of doorways.
    /// </summary>
    static bool DoorNear(char axis, float facePlane, float along, float clearance = 0.55f)
    {
        foreach (PlanDoor d in TowerPlanV8.Doors)
        {
            if (!TowerPlanV8.TryGetDoorCenter(d, out char a, out float at, out float pos)) continue;
            if (a != axis || Mathf.Abs(at - facePlane) > 0.45f) continue;
            if (Mathf.Abs(pos - along) < d.WidthM * 0.5f + clearance) return true;
        }
        return false;
    }

    /// <summary>
    /// World-anchored tiling (~2 m per repeat, style-lock v2 §4) via a per-renderer
    /// MaterialPropertyBlock so one material serves every slab size without stretching.
    /// </summary>
    static void ApplyWorldTiling(GameObject go, Material mat)
    {
        if (mat == null || !mat.HasProperty("_BaseMap") || mat.GetTexture("_BaseMap") == null) return;
        Vector3 s = go.transform.localScale;
        float[] d = { Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z) };
        System.Array.Sort(d);
        const float metersPerTile = 2f;
        var r = go.GetComponent<Renderer>();
        var mpb = new MaterialPropertyBlock();
        r.GetPropertyBlock(mpb);
        mpb.SetVector("_BaseMap_ST", new Vector4(
            Mathf.Max(d[2] / metersPerTile, 0.25f), Mathf.Max(d[1] / metersPerTile, 0.25f), 0f, 0f));
        r.SetPropertyBlock(mpb);
    }
}
