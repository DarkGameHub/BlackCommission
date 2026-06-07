using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AbandonedBuildingFloorPlanBuilder
{
    const string ScenePath = "Assets/Scene/AbandonedBuilding_Blockout.unity";
    const string RootName = "AB_FloorPlan_Blockout";
    const float Floor2Y = 4.2f;
    const float FloorThickness = 0.08f;
    const float WallHeight = 3.2f;
    const float WallThickness = 0.28f;
    const float DoorWidth = 2.0f;
    const float DoorHeight = 2.35f;

    static Material floorMat;
    static Material wallMat;
    static Material doorMat;
    static Material stairMat;

    [MenuItem("Tools/Black Commission/MVP/Rebuild Abandoned Building Floor Plan")]
    public static void RebuildFromMenu()
    {
        Rebuild();
    }

    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureMaterials();

        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
            Object.DestroyImmediate(oldRoot);

        var root = new GameObject(RootName);
        AddFloorOne(root.transform);
        AddFloorTwo(root.transform);
        AddStairLinks(root.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[AbandonedBuilding] Whitebox floor plan rebuilt from the two-floor reference layout.");
    }

    static void EnsureMaterials()
    {
        floorMat = MakeMat("AB_Blockout_Floor", new Color(0.72f, 0.72f, 0.72f));
        wallMat = MakeMat("AB_Blockout_Wall", new Color(0.94f, 0.94f, 0.9f));
        doorMat = MakeMat("AB_Blockout_DoorFrame", new Color(0.5f, 0.5f, 0.48f));
        stairMat = MakeMat("AB_Blockout_Stair", new Color(0.82f, 0.82f, 0.78f));
    }

    static Material MakeMat(string name, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.name = name;
        mat.color = color;
        return mat;
    }

    static void AddFloorOne(Transform root)
    {
        var f = new GameObject("Floor_01");
        f.transform.SetParent(root);
        const float y = 0f;

        // Rooms from the first-floor plan.
        AddRoom(f.transform, y, "F1_S2_TemporaryOffice", 0, 24, 8, 8,
            Door.South(6));
        AddRoom(f.transform, y, "F1_M2_EastAssistantWorkshop", 16, 24, 16, 8,
            Door.South(18), Door.South(30));
        AddRoom(f.transform, y, "F1_S3_PowerRoom", 40, 24, 8, 8,
            Door.South(46));
        AddRoom(f.transform, y, "F1_M3_MainWorkerDorm", 56, 24, 16, 8,
            Door.South(60), Door.East(22));

        AddRoom(f.transform, y, "F1_L2_WestMaterialWarehouse", 0, 8, 24, 16,
            Door.East(12), Door.South(18));
        AddRoom(f.transform, y, "F1_L1_CentralConstructionHall", 28, 8, 36, 16,
            Door.West(14), Door.North(42), Door.South(48), Door.East(14));

        AddRoom(f.transform, y, "F1_S1_StartVanArea", 16, 0, 24, 8,
            Door.West(4), Door.North(26), Door.East(4));
        AddRoom(f.transform, y, "F1_M1_LobbySecurityPassage", 40, 0, 16, 8,
            Door.West(4), Door.North(48), Door.East(4));

        AddRoom(f.transform, y, "F1_A_MainStair_ToFloor2", 72, 16, 8, 16,
            Door.West(22));
        AddRoom(f.transform, y, "F1_B_SideStair_ToFloor2", 72, 0, 8, 16,
            Door.West(8));

        // Corridor floors only; their edges are defined by room and exterior walls.
        AddFloor(f.transform, y, "F1_NorthCorridor_4m", 8, 20, 64, 4);
        AddFloor(f.transform, y, "F1_WestConnector_4m", 24, 8, 4, 16);
        AddFloor(f.transform, y, "F1_EastMainCorridor_4m", 64, 0, 8, 32);
        AddFloor(f.transform, y, "F1_StairLobbyConnector_4m", 56, 0, 8, 8);

        // Exterior corridor containment walls matching the long public route in the plan.
        AddWallLine(f.transform, y, "F1_NorthCorridor_SouthWall", 8, 20, 72, 20,
            DoorGap(18), DoorGap(30), DoorGap(42), DoorGap(46), DoorGap(60));
        AddWallLine(f.transform, y, "F1_NorthCorridor_NorthWall", 8, 24, 72, 24,
            DoorGap(18), DoorGap(30), DoorGap(46), DoorGap(60));
        AddWallLine(f.transform, y, "F1_EastCorridor_WestWall", 64, 0, 64, 32,
            DoorGap(4), DoorGap(14), DoorGap(22));
        AddWallLine(f.transform, y, "F1_EastCorridor_EastWall", 72, 0, 72, 32,
            DoorGap(8), DoorGap(22));
    }

    static void AddFloorTwo(Transform root)
    {
        var f = new GameObject("Floor_02");
        f.transform.SetParent(root);
        const float y = Floor2Y;

        AddRoom(f.transform, y, "F2_L4_SampleOfficeHalfFinishedArea", 0, 16, 24, 16,
            Door.East(20), Door.East(16));
        AddRoom(f.transform, y, "F2_M5_ScaffoldBridge", 24, 24, 32, 8,
            Door.South(30), Door.South(52));
        AddRoom(f.transform, y, "F2_L3_FlowPlatformUnfinishedShaft", 24, 12, 32, 12,
            Door.North(30), Door.North(52), Door.South(32), Door.South(48), Door.East(18));
        AddRoom(f.transform, y, "F2_M4_SalesOfficeRichLoot", 56, 16, 16, 8,
            Door.West(18), Door.East(20), Door.South(60));

        AddRoom(f.transform, y, "F2_S4_MaintenanceRoom", 0, 0, 8, 8,
            Door.North(4));
        AddRoom(f.transform, y, "F2_S5_DangerousShaftRoom", 16, 0, 16, 8,
            Door.North(20), Door.East(4));
        AddRoom(f.transform, y, "F2_L5_DeepTargetArea", 40, 0, 32, 8,
            Door.North(48), Door.East(4));

        AddRoom(f.transform, y, "F2_A_MainStair_ToFloor1", 72, 16, 8, 16,
            Door.West(24));
        AddRoom(f.transform, y, "F2_B_SideStair_ToFloor1", 72, 0, 8, 16,
            Door.West(8));

        AddFloor(f.transform, y, "F2_TopCorridor_4m", 24, 24, 48, 4);
        AddFloor(f.transform, y, "F2_MainReturnCorridor_4m", 24, 8, 48, 4);
        AddFloor(f.transform, y, "F2_LeftLowerConnector_4m", 8, 0, 8, 8);
        AddFloor(f.transform, y, "F2_EastStairCorridor_4m", 72, 0, 8, 32);

        AddWallLine(f.transform, y, "F2_MainCorridor_NorthWall", 24, 12, 72, 12,
            DoorGap(32), DoorGap(48), DoorGap(60));
        AddWallLine(f.transform, y, "F2_MainCorridor_SouthWall", 24, 8, 72, 8,
            DoorGap(20), DoorGap(48), DoorGap(68));
        AddWallLine(f.transform, y, "F2_TopCorridor_NorthWall", 24, 28, 72, 28,
            DoorGap(30), DoorGap(52));
        AddWallLine(f.transform, y, "F2_TopCorridor_SouthWall", 24, 24, 72, 24,
            DoorGap(30), DoorGap(52), DoorGap(64));
        AddWallLine(f.transform, y, "F2_EastCorridor_WestWall", 72, 0, 72, 32,
            DoorGap(4), DoorGap(8), DoorGap(20), DoorGap(24));
    }

    static void AddStairLinks(Transform root)
    {
        var stairs = new GameObject("Stair_Links_A_and_B");
        stairs.transform.SetParent(root);
        AddStairFlight(stairs.transform, "A_MainStair_Link", 74f, 18f, 78f, 30f);
        AddStairFlight(stairs.transform, "B_SideStair_Link", 78f, 14f, 74f, 2f);
    }

    static void AddStairFlight(Transform parent, string name, float x0, float z0, float x1, float z1)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);

        const int steps = 10;
        for (int i = 0; i < steps; i++)
        {
            float t = (i + 0.5f) / steps;
            float x = Mathf.Lerp(x0, x1, t);
            float z = Mathf.Lerp(z0, z1, t);
            float y = Mathf.Lerp(0f, Floor2Y, (i + 1f) / steps);
            float angle = Mathf.Atan2(x1 - x0, z1 - z0) * Mathf.Rad2Deg;

            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"{name}_Step_{i + 1:00}";
            step.transform.SetParent(root.transform);
            step.transform.position = new Vector3(x, y - 0.08f, z);
            step.transform.rotation = Quaternion.Euler(0f, angle, 0f);
            step.transform.localScale = new Vector3(3.2f, 0.22f, 1.1f);
            step.GetComponent<Renderer>().sharedMaterial = stairMat;
        }
    }

    static void AddRoom(Transform parent, float floorY, string name, float x, float z, float w, float d, params Door[] doors)
    {
        var room = new GameObject(name);
        room.transform.SetParent(parent);
        AddFloor(room.transform, floorY, "Floor", x, z, w, d);

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

        AddWallLine(room.transform, floorY, "Wall_North", x, z + d, x + w, z + d, GapsFor(byEdge, Edge.North));
        AddWallLine(room.transform, floorY, "Wall_South", x, z, x + w, z, GapsFor(byEdge, Edge.South));
        AddWallLine(room.transform, floorY, "Wall_East", x + w, z, x + w, z + d, GapsFor(byEdge, Edge.East));
        AddWallLine(room.transform, floorY, "Wall_West", x, z, x, z + d, GapsFor(byEdge, Edge.West));

        foreach (var door in doors)
            AddDoorHeader(room.transform, floorY, name, x, z, w, d, door);
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
