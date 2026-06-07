using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebuilds the abandoned pre-sale tower whitebox (地球海岸壹号 · 烂尾预售楼) faithfully to the
/// locked floor plan in <c>design/levels/abandoned-tower-floorplan.md</c>.
///
/// Everything is authored on a strict 4 m grid inside a 36 × 20 m envelope per floor, in three
/// z-bands: South bays (z 0–8), Spine (z 8–12), North bays (z 12–20). The two stair cores sit at
/// the SAME x,z on both floors (mandatory vertical alignment). The central shaft void (x 14–22,
/// z 8–12) is an open hole in the F2 floor crossed only by the scaffold bridge — the signature
/// vertical gimmick. GameObject names match the design docs and the tokens that
/// <c>TowerSlotAnchorBuilder</c> reads (S/M/L size + PowerRoom/DeepTarget/Stair/StartVanArea role).
///
/// The menu command destroys the old <c>AB_FloorPlan_Blockout</c> root and regenerates from
/// scratch, so re-running always reflects the latest layout. After running, re-run
/// "Tower > 1. Add Slot Anchors To Blockout" and bake NavMesh.
/// </summary>
public static class AbandonedBuildingFloorPlanBuilder
{
    const string ScenePath = "Assets/Scene/AbandonedBuilding_Blockout.unity";
    const string RootName = "AB_FloorPlan_Blockout";

    // --- grid + geometry constants ---
    const float G = 4f;                 // grid unit (matches the _4m corridors)
    const float EnvW = 36f;             // envelope width  (x)
    const float EnvD = 20f;             // envelope depth  (z)
    const float Floor2Y = 4.2f;         // floor-to-floor height
    const float FloorThickness = 0.08f;
    const float WallHeight = 3.2f;      // clear interior height
    const float WallThickness = 0.28f;
    const float DoorWidth = 2.0f;
    const float DoorHeight = 2.35f;

    // Central shaft void (open hole in F2 floor / open ceiling on F1), crossed by the bridge.
    const float ShaftX0 = 14f, ShaftX1 = 22f, ShaftZ0 = 8f, ShaftZ1 = 12f;

    static Material floorMat;
    static Material wallMat;
    static Material doorMat;
    static Material stairMat;
    static Material scaffoldMat;

    [MenuItem("Tools/Black Commission/MVP/Rebuild Abandoned Building Floor Plan")]
    public static void RebuildFromMenu() => Rebuild();

    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureMaterials();

        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
            Object.DestroyImmediate(oldRoot);

        var root = new GameObject(RootName);
        BuildFloorOne(root.transform);
        BuildFloorTwo(root.transform);
        BuildStairFlights(root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[AbandonedBuilding] Whitebox floor plan rebuilt to the locked 36x20 grid " +
                  "(2 floors, central shaft + scaffold bridge, aligned stairs A/B). " +
                  "Re-run 'Tower > 1. Add Slot Anchors' and bake NavMesh.");
    }

    static void EnsureMaterials()
    {
        floorMat = MakeMat("AB_Blockout_Floor", new Color(0.72f, 0.72f, 0.72f));
        wallMat = MakeMat("AB_Blockout_Wall", new Color(0.94f, 0.94f, 0.9f));
        doorMat = MakeMat("AB_Blockout_DoorFrame", new Color(0.5f, 0.5f, 0.48f));
        stairMat = MakeMat("AB_Blockout_Stair", new Color(0.82f, 0.82f, 0.78f));
        scaffoldMat = MakeMat("AB_Blockout_Scaffold", new Color(0.62f, 0.42f, 0.22f));
    }

    static Material MakeMat(string name, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        return mat;
    }

    // ───────────────────────────────── Floor 1 — Ground / Arrival ─────────────────────────────────

    static void BuildFloorOne(Transform root)
    {
        var f = new GameObject("Floor_01");
        f.transform.SetParent(root);
        const float y = 0f;

        // Dispatch van forecourt, south of the envelope (z < 0). Spawn / return / partial settle.
        AddRoom(f.transform, y, "F1_S1_StartVanArea", 12, -8, 12, 8,
            Door.North(15));

        // South bays (z 0–8).
        AddRoom(f.transform, y, "F1_S3_PowerRoom", 4, 0, 4, 4,        // POWER GATE (S)
            Door.North(6));
        AddRoom(f.transform, y, "F1_M1_LobbySecurityPassage", 8, 0, 12, 8,   // entry hall (built L)
            Door.South(15), Door.North(10), Door.East(4));
        AddRoom(f.transform, y, "F1_S2_TemporaryOffice", 24, 0, 4, 4,  // power-room clue (S)
            Door.West(2), Door.North(26));
        AddRoom(f.transform, y, "F1_M2_EastAssistantWorkshop", 28, 0, 8, 8,  // consumables (M)
            Door.North(30), Door.West(4));

        // Spine (z 8–12): the central construction hall / hub. Shaft opens in its ceiling.
        AddRoom(f.transform, y, "F1_L1_CentralConstructionHall", 4, 8, 28, 4,
            Door.South(6), Door.South(10), Door.South(22), Door.South(26), Door.South(30),
            Door.North(10), Door.North(20), Door.North(26), Door.North(30),
            Door.West(10), Door.East(10));

        // North bays (z 12–20).
        AddRoom(f.transform, y, "F1_L2_WestMaterialWarehouse", 4, 12, 12, 8,  // consumables (L)
            Door.South(10));
        AddRoom(f.transform, y, "F1_M3_MainWorkerDorm", 16, 12, 8, 8,         // EVIDENCE (M)
            Door.South(20));
        AddRoom(f.transform, y, "F1_S4_NorthStorageRoom", 24, 12, 4, 4,       // filler/loot (S)
            Door.South(26));

        // Stair cores (vertically aligned with F2). Full ground-floor landing.
        AddStairRoom(f.transform, y, "F1_B_SideStair_ToFloor2", 0, 4, 4, 8, Door.East(10));
        AddStairRoom(f.transform, y, "F1_A_MainStair_ToFloor2", 32, 8, 4, 8, Door.West(10));

        // Connector / leftover floor so the whole 36×20 footprint is traversable (no pits).
        var conn = new GameObject("F1_Connectors");
        conn.transform.SetParent(f.transform);
        AddFloor(conn.transform, y, "F1_PowerConnector_4m", 4, 4, 4, 4);
        AddFloor(conn.transform, y, "F1_SEConnector_4m", 20, 0, 4, 8);
        AddFloor(conn.transform, y, "F1_TempConnector_4m", 24, 4, 4, 4);
        AddFloor(conn.transform, y, "F1_NEConnector_4m", 28, 12, 4, 8);
        AddFloor(conn.transform, y, "F1_NStorageBack_4m", 24, 16, 4, 4);
        AddFloor(conn.transform, y, "F1_NECorner_4m", 32, 16, 4, 4);
        AddFloor(conn.transform, y, "F1_SWCorner_4m", 0, 0, 4, 4);
        AddFloor(conn.transform, y, "F1_NWCorner_4m", 0, 12, 4, 8);

        AddEnvelope(f.transform, y, "F1", southGapMin: 12f, southGapMax: 24f);  // van gap on south
    }

    // ─────────────────────────── Floor 2 — Show-flat / Sales floor ───────────────────────────

    static void BuildFloorTwo(Transform root)
    {
        var f = new GameObject("Floor_02");
        f.transform.SetParent(root);
        const float y = Floor2Y;

        // South bays (z 0–8).
        AddRoom(f.transform, y, "F2_S4_MaintenanceRoom", 4, 0, 4, 4,          // shortcut (S)
            Door.North(6));
        AddRoom(f.transform, y, "F2_L3_FlowPlatformUnfinishedShaft", 8, 0, 12, 8,  // shaft edge (L)
            Door.North(10));
        AddRoom(f.transform, y, "F2_S5_DangerousShaftRoom", 20, 0, 4, 4,      // high-risk loot (S)
            Door.North(22));
        AddRoom(f.transform, y, "F2_M4_SalesOfficeRichLoot", 28, 0, 8, 8,     // rich loot (M)
            Door.North(30));

        // North bays (z 12–20).
        AddRoom(f.transform, y, "F2_L5_DeepTargetArea", 4, 12, 12, 8,         // 沙盘 OBJECTIVE + nest (L)
            Door.South(10), Door.East(16));
        AddRoom(f.transform, y, "F2_L4_SampleOfficeHalfFinishedArea", 16, 12, 8, 8,  // show-flat (built M)
            Door.South(23), Door.West(16));
        AddRoom(f.transform, y, "F2_S6_NorthUtil", 24, 12, 4, 4,             // filler (S)
            Door.South(26));

        // Spine floor strips around the open shaft void (the hole is left unfloored).
        var spine = new GameObject("F2_Spine");
        spine.transform.SetParent(f.transform);
        AddFloor(spine.transform, y, "F2_WestSpine_4m", 4, 8, 10, 4);   // x 4–14, z 8–12
        AddFloor(spine.transform, y, "F2_EastSpine_4m", 22, 8, 10, 4);  // x 22–32, z 8–12

        // Scaffold bridge: 2 m-wide plank across the void — the only direct E↔W route. Open hole
        // strips on both sides of the plank (z 8–9 and z 11–12) are the fall risk.
        AddBridge(spine.transform, y, "F2_M5_ScaffoldBridge", ShaftX0, 9f, ShaftX1 - ShaftX0, 2f);

        // Stair cores: top landing only on the spine side; the rest is the open stairwell.
        AddStairRoom(f.transform, y, "F2_B_SideStair_ToFloor1", 0, 4, 4, 8, Door.East(10),
            landingZ0: 8f, landingZ1: 11f);
        AddStairRoom(f.transform, y, "F2_A_MainStair_ToFloor1", 32, 8, 4, 8, Door.West(10),
            landingZ0: 8f, landingZ1: 11f);

        // Connector / leftover floor.
        var conn = new GameObject("F2_Connectors");
        conn.transform.SetParent(f.transform);
        AddFloor(conn.transform, y, "F2_MaintBack_4m", 4, 4, 4, 4);
        AddFloor(conn.transform, y, "F2_DangerBack_4m", 20, 4, 4, 4);
        AddFloor(conn.transform, y, "F2_EConnector_4m", 24, 0, 4, 8);
        AddFloor(conn.transform, y, "F2_NEConnector_4m", 28, 12, 4, 8);
        AddFloor(conn.transform, y, "F2_NUtilBack_4m", 24, 16, 4, 4);
        AddFloor(conn.transform, y, "F2_NECorner_4m", 32, 16, 4, 4);
        AddFloor(conn.transform, y, "F2_SWCorner_4m", 0, 0, 4, 4);
        AddFloor(conn.transform, y, "F2_NWCorner_4m", 0, 12, 4, 8);

        AddEnvelope(f.transform, y, "F2", southGapMin: 0f, southGapMax: 0f);  // F2 south is solid
    }

    // ───────────────────────────────── Stairs ─────────────────────────────────

    static void BuildStairFlights(Transform root)
    {
        var stairs = new GameObject("Stair_Links_A_and_B");
        stairs.transform.SetParent(root);
        // Climb toward the spine door (z decreasing) so you arrive at the F2 top landing (z 8–11).
        AddStairFlight(stairs.transform, "A_MainStair_Link", 34f, 15f, 34f, 9f, 16);
        AddStairFlight(stairs.transform, "B_SideStair_Link", 2f, 6f, 2f, 10f, 16);
    }

    static void AddStairFlight(Transform parent, string name, float x0, float z0, float x1, float z1, int steps)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);

        float angle = Mathf.Atan2(x1 - x0, z1 - z0) * Mathf.Rad2Deg;
        for (int i = 0; i < steps; i++)
        {
            float t = (i + 0.5f) / steps;
            float x = Mathf.Lerp(x0, x1, t);
            float z = Mathf.Lerp(z0, z1, t);
            float yTop = Mathf.Lerp(0f, Floor2Y, (i + 1f) / steps);

            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"{name}_Step_{i + 1:00}";
            step.transform.SetParent(root.transform);
            step.transform.position = new Vector3(x, yTop - 0.11f, z);
            step.transform.rotation = Quaternion.Euler(0f, angle, 0f);
            step.transform.localScale = new Vector3(3.2f, 0.22f, 1.1f);
            step.GetComponent<Renderer>().sharedMaterial = stairMat;
        }
    }

    // Stair core: walls + a single door + a floor that is either full (ground landing) or a
    // partial top landing (so the flight rises into an open stairwell on the upper floor).
    static void AddStairRoom(Transform parent, float floorY, string name, float x, float z,
        float w, float d, Door door, float? landingZ0 = null, float? landingZ1 = null)
    {
        var room = new GameObject(name);
        room.transform.SetParent(parent);

        float fz0 = landingZ0 ?? z;
        float fz1 = landingZ1 ?? (z + d);
        AddFloor(room.transform, floorY, "Floor", x, fz0, w, fz1 - fz0);

        var byEdge = GroupDoors(door);
        AddWallLine(room.transform, floorY, "Wall_North", x, z + d, x + w, z + d, GapsFor(byEdge, Edge.North));
        AddWallLine(room.transform, floorY, "Wall_South", x, z, x + w, z, GapsFor(byEdge, Edge.South));
        AddWallLine(room.transform, floorY, "Wall_East", x + w, z, x + w, z + d, GapsFor(byEdge, Edge.East));
        AddWallLine(room.transform, floorY, "Wall_West", x, z, x, z + d, GapsFor(byEdge, Edge.West));
        AddDoorHeader(room.transform, floorY, name, x, z, w, d, door);
    }

    // ───────────────────────────────── Rooms / walls / floors ─────────────────────────────────

    static void AddRoom(Transform parent, float floorY, string name, float x, float z, float w, float d, params Door[] doors)
    {
        var room = new GameObject(name);
        room.transform.SetParent(parent);
        AddFloor(room.transform, floorY, "Floor", x, z, w, d);

        var byEdge = GroupDoors(doors);
        AddWallLine(room.transform, floorY, "Wall_North", x, z + d, x + w, z + d, GapsFor(byEdge, Edge.North));
        AddWallLine(room.transform, floorY, "Wall_South", x, z, x + w, z, GapsFor(byEdge, Edge.South));
        AddWallLine(room.transform, floorY, "Wall_East", x + w, z, x + w, z + d, GapsFor(byEdge, Edge.East));
        AddWallLine(room.transform, floorY, "Wall_West", x, z, x, z + d, GapsFor(byEdge, Edge.West));

        foreach (var door in doors)
            AddDoorHeader(room.transform, floorY, name, x, z, w, d, door);
    }

    static Dictionary<Edge, List<Door>> GroupDoors(params Door[] doors)
    {
        var byEdge = new Dictionary<Edge, List<Door>>();
        foreach (var door in doors)
        {
            if (!byEdge.TryGetValue(door.edge, out var list))
            {
                list = new List<Door>();
                byEdge[door.edge] = list;
            }
            list.Add(door);
        }
        return byEdge;
    }

    static Gap[] GapsFor(Dictionary<Edge, List<Door>> doors, Edge edge)
    {
        if (!doors.TryGetValue(edge, out var list)) return new Gap[0];
        var gaps = new Gap[list.Count];
        for (int i = 0; i < list.Count; i++)
            gaps[i] = DoorGap(list[i].center, list[i].width);
        return gaps;
    }

    static Gap DoorGap(float center, float width = DoorWidth) => new Gap(center - width * 0.5f, center + width * 0.5f);

    static void AddFloor(Transform parent, float floorY, string name, float x, float z, float w, float d)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x + w * 0.5f, floorY - FloorThickness * 0.5f, z + d * 0.5f);
        go.transform.localScale = new Vector3(w, FloorThickness, d);
        go.GetComponent<Renderer>().sharedMaterial = floorMat;
    }

    // A thin walkable plank (the scaffold bridge) — distinct material so it reads as the crossing.
    static void AddBridge(Transform parent, float floorY, string name, float x, float z, float w, float d)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x + w * 0.5f, floorY - FloorThickness * 0.5f, z + d * 0.5f);
        go.transform.localScale = new Vector3(w, FloorThickness, d);
        go.GetComponent<Renderer>().sharedMaterial = scaffoldMat;
    }

    // Continuous outer envelope wall for one floor, with an optional gap on the south edge.
    static void AddEnvelope(Transform parent, float floorY, string floorTag, float southGapMin, float southGapMax)
    {
        var env = new GameObject($"{floorTag}_Envelope");
        env.transform.SetParent(parent);

        AddWallLine(env.transform, floorY, $"{floorTag}_Env_West", 0, 0, 0, EnvD);
        AddWallLine(env.transform, floorY, $"{floorTag}_Env_East", EnvW, 0, EnvW, EnvD);
        AddWallLine(env.transform, floorY, $"{floorTag}_Env_North", 0, EnvD, EnvW, EnvD);
        if (southGapMax > southGapMin)
            AddWallLine(env.transform, floorY, $"{floorTag}_Env_South", 0, 0, EnvW, 0,
                new Gap(southGapMin, southGapMax));
        else
            AddWallLine(env.transform, floorY, $"{floorTag}_Env_South", 0, 0, EnvW, 0);
    }

    static void AddWallLine(Transform parent, float floorY, string name, float x0, float z0, float x1, float z1, params Gap[] gaps)
    {
        bool horizontal = Mathf.Abs(z1 - z0) < 0.01f;
        float start = horizontal ? Mathf.Min(x0, x1) : Mathf.Min(z0, z1);
        float end = horizontal ? Mathf.Max(x0, x1) : Mathf.Max(z0, z1);
        var sorted = new List<Gap>(gaps);
        sorted.Sort((a, b) => a.min.CompareTo(b.min));

        float cursor = start;
        int segment = 0;
        foreach (var rawGap in sorted)
        {
            var gap = new Gap(Mathf.Clamp(rawGap.min, start, end), Mathf.Clamp(rawGap.max, start, end));
            if (gap.min > cursor)
                AddWallSegment(parent, floorY, $"{name}_{++segment:00}", horizontal, x0, z0, cursor, gap.min);
            cursor = Mathf.Max(cursor, gap.max);
        }

        if (cursor < end)
            AddWallSegment(parent, floorY, $"{name}_{++segment:00}", horizontal, x0, z0, cursor, end);
    }

    static void AddWallSegment(Transform parent, float floorY, string name, bool horizontal, float baseX, float baseZ, float a, float b)
    {
        if (b - a < 0.05f) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        if (horizontal)
        {
            go.transform.position = new Vector3((a + b) * 0.5f, floorY + WallHeight * 0.5f, baseZ);
            go.transform.localScale = new Vector3(b - a, WallHeight, WallThickness);
        }
        else
        {
            go.transform.position = new Vector3(baseX, floorY + WallHeight * 0.5f, (a + b) * 0.5f);
            go.transform.localScale = new Vector3(WallThickness, WallHeight, b - a);
        }
        go.GetComponent<Renderer>().sharedMaterial = wallMat;
    }

    static void AddDoorHeader(Transform parent, float floorY, string roomName, float x, float z, float w, float d, Door door)
    {
        bool horizontal = door.edge == Edge.North || door.edge == Edge.South;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"{roomName}_Door_{door.edge}_{door.center:0}";
        go.transform.SetParent(parent);

        float y = floorY + DoorHeight + 0.12f;
        if (horizontal)
        {
            float dz = door.edge == Edge.North ? z + d : z;
            go.transform.position = new Vector3(door.center, y, dz);
            go.transform.localScale = new Vector3(door.width, 0.24f, WallThickness * 1.35f);
        }
        else
        {
            float dx = door.edge == Edge.East ? x + w : x;
            go.transform.position = new Vector3(dx, y, door.center);
            go.transform.localScale = new Vector3(WallThickness * 1.35f, 0.24f, door.width);
        }
        go.GetComponent<Renderer>().sharedMaterial = doorMat;
    }

    enum Edge { North, South, East, West }

    readonly struct Gap
    {
        public readonly float min;
        public readonly float max;

        public Gap(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }

    readonly struct Door
    {
        public readonly Edge edge;
        public readonly float center;
        public readonly float width;

        Door(Edge edge, float center, float width)
        {
            this.edge = edge;
            this.center = center;
            this.width = width;
        }

        public static Door North(float x, float width = DoorWidth) => new Door(Edge.North, x, width);
        public static Door South(float x, float width = DoorWidth) => new Door(Edge.South, x, width);
        public static Door East(float z, float width = DoorWidth) => new Door(Edge.East, z, width);
        public static Door West(float z, float width = DoorWidth) => new Door(Edge.West, z, width);
    }
}
