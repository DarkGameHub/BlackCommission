using System.Collections.Generic;
using System.Linq;
using BlackCommission.Level;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the v3 abandoned-tower whitebox (design/levels/abandoned-tower-redesign-v3.md +
/// abandoned-tower-v3-connectivity.md). Unlike the old 36×20 builder, this one is DRIVEN BY THE
/// CONNECTIVITY GRAPH (<see cref="TowerTopologyV3.BuildCanonical"/>): every graph edge is realised
/// as a corridor/door with a matching <see cref="Connector"/> (id = edge id), so geometry and
/// connectivity can never drift. Room rectangles come from the coordinate table below; tweak those
/// for aesthetics — the corridors auto-route between linked rooms, so connectivity holds regardless.
///
/// The menu command deletes the old whitebox roots (the "delete the original models" step) and the
/// previous v3 root, then rebuilds from scratch. After running: bake NavMesh, and author a
/// TowerRoomCatalog/RoomDefs so the slots can be filled.
///
/// NOTE: authored blind (no Unity available at write time). Expect to nudge room coordinates in the
/// editor on first open; overlaps are logged as warnings. Connectivity is guaranteed by the graph.
/// </summary>
public static class TowerV3WhiteboxBuilder
{
    const string ScenePath = "Assets/Scene/AbandonedBuilding_Blockout.unity";
    const string RootName = "Tower_v3_Whitebox";
    static readonly string[] OldRootsToDelete = { "AB_FloorPlan_Blockout", "Tower_v3_Whitebox" };

    const float Floor2Y = 4.2f;
    const float FloorThickness = 0.08f;
    const float WallHeight = 3.2f;
    const float WallThickness = 0.28f;
    const float DoorWidth = 2.0f;
    const float CorridorWidth = 4.0f;
    const float CorridorOpeningWidth = 2.8f;
    const float WallOpeningEndKeep = 0.45f;
    const float DoorClearHeight = 2.25f;
    const float DoorFramePostWidth = 0.18f;
    const float DoorHeaderHeight = 0.24f;

    static Material floorMat, wallMat, corridorMat, blockerMat, stairMat, scaffoldMat, extMat;

    // ───────────────────────── node coordinate table (x,z bottom-left; w,d) ─────────────────────────
    // kind drives the RoomSlot role; size drives the slot size. Junctions/Van/Fire/Collapse get no slot.
    enum Kind { Room, Junction, Stair, Van, Fire, Collapse }

    struct Node
    {
        public string id; public int floor; public float x, z, w, d;
        public Kind kind; public RoomSizeClass size; public RoomSlotRole role;
        public Node(string id, int floor, float x, float z, float w, float d, Kind kind,
            RoomSizeClass size = RoomSizeClass.Medium, RoomSlotRole role = RoomSlotRole.Random)
        { this.id = id; this.floor = floor; this.x = x; this.z = z; this.w = w; this.d = d;
          this.kind = kind; this.size = size; this.role = role; }
    }

    static List<Node> BuildNodeTable()
    {
        var S = RoomSizeClass.Small; var M = RoomSizeClass.Medium; var L = RoomSizeClass.Large;
        var R = RoomSlotRole.Random;
        // Coordinates transcribed from tower_layout_v3.json (origin SW, x=east, z=north, (x,z)=room SW
        // corner in metres, d=depth=json "h"). Ids match TowerTopologyV3.BuildCanonical(). SHAFT (void)
        // and BRIDGE (decorative) are intentionally omitted — see that class's summary.
        return new List<Node>
        {
            // ---- Floor 1 (ground / arrival) ----
            new Node("VAN", 1, 14, -8, 12, 8, Kind.Van, M, RoomSlotRole.Van),
            new Node("LOBBY", 1, 12, 0, 12, 8, Kind.Room, L, RoomSlotRole.Fixed),
            new Node("WAREHOUSE", 1, 0, -8, 12, 8, Kind.Room, L, R),                 // moved off POWER entry lane
            new Node("POWER", 1, 0, 10, 4, 4, Kind.Room, S, RoomSlotRole.PowerGate), // Shifted +2z
            new Node("TEMP", 1, 4, 14, 4, 4, Kind.Room, S, R),                      // moved off POWER entry lane
            new Node("SECUR", 1, 8, 10, 4, 4, Kind.Room, S, R),                     // Shifted +2z
            new Node("SAMPLE", 1, 12, 10, 4, 4, Kind.Room, S, R),                    // Shifted +2z
            new Node("HALL", 1, 12, 16, 12, 8, Kind.Room, L, RoomSlotRole.Fixed),     // Shifted +4z
            new Node("WORKSHOP", 1, 24, 8, 8, 8, Kind.Room, M, R),
            new Node("DOCK", 1, 34, 16, 8, 8, Kind.Room, M, R),                      // Shifted +2x
            new Node("DORM", 1, 12, 24, 8, 8, Kind.Room, M, R),                      // Shifted +4z
            new Node("CANTEEN", 1, 12, 32, 8, 8, Kind.Room, M, R),                   // Shifted +4z
            new Node("FOREMAN", 1, 22, 36, 8, 8, Kind.Room, M, R),                   // Shifted +2x, +4z
            new Node("REBAR", 1, 34, 8, 8, 8, Kind.Room, M, R),                      // Shifted +2x
            new Node("PUMP", 1, 26, 0, 4, 4, Kind.Room, S, R),                       // moved off corridor lanes
            new Node("SHANTY", 1, 34, 24, 4, 4, Kind.Room, S, R),                    // Shifted +2x
            new Node("FIRE", 1, 30, 36, 8, 8, Kind.Fire),                            // Shifted +2x, +4z
            new Node("COLLAPSE", 1, 0, 24, 12, 16, Kind.Collapse),                  // Shifted +4z
            new Node("STAIRA1", 1, 26, 28, 4, 8, Kind.Stair, M, RoomSlotRole.Stair),  // Shifted +2x, +4z
            new Node("STAIRB1", 1, 0, 16, 4, 8, Kind.Stair, M, RoomSlotRole.Stair),   // Shifted +4z

            // ---- Floor 2 (show-flat / objective; stairs aligned with F1) ----
            new Node("TARGET", 2, 4, 24, 12, 8, Kind.Room, L, RoomSlotRole.Objective),
            new Node("SHOWFLAT", 2, 4, 16, 8, 8, Kind.Room, M, RoomSlotRole.Fixed),
            new Node("EXEC", 2, 16, 24, 8, 8, Kind.Room, M, R),
            new Node("MODEL", 2, 12, 16, 8, 8, Kind.Room, M, R),
            new Node("SALES", 2, 20, 16, 8, 8, Kind.Room, M, R),
            new Node("VIP", 2, 28, 12, 8, 8, Kind.Room, M, R),   // shifted +4x from json to clear SALES overlap
            new Node("EDGE", 2, 12, 4, 12, 8, Kind.Room, L, RoomSlotRole.Fixed),
            new Node("DANGER", 2, 24, 4, 4, 4, Kind.Room, S, R),
            new Node("MAINT", 2, 4, 12, 4, 4, Kind.Room, S, R),                      // moved off shaft-edge route
            new Node("MARKET", 2, 4, 8, 4, 4, Kind.Room, S, R),
            new Node("BALCONY", 2, 28, 4, 4, 4, Kind.Room, S, RoomSlotRole.Fixed),
            new Node("NEGOT", 2, 28, 20, 8, 8, Kind.Room, M, R),   // densify
            new Node("FIN", 2, 12, 0, 4, 4, Kind.Room, S, R),       // densify
            new Node("ARCHIVE", 2, 16, 0, 4, 4, Kind.Room, S, R),   // densify
            new Node("TANK", 2, 0, 8, 4, 4, Kind.Room, S, R),       // densify
            new Node("STAIRA2", 2, 24, 24, 4, 8, Kind.Stair, M, RoomSlotRole.Stair),
            new Node("STAIRB2", 2, 0, 12, 4, 8, Kind.Stair, M, RoomSlotRole.Stair),
        };
    }

    [MenuItem("Tools/Black Commission/MVP/Tower/Rebuild v3 Whitebox")]
    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureMaterials();

        // Comprehensive cleanup
        var allGos = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allGos)
        {
            if (go == null) continue;
            foreach (string old in OldRootsToDelete)
            {
                if (go.name == old && go.transform.parent == null)
                {
                    Object.DestroyImmediate(go);
                    break;
                }
            }
        }

        var root = new GameObject(RootName);
var nodes = BuildNodeTable();
        var byId = new Dictionary<string, Node>();
        foreach (Node n in nodes) byId[n.id] = n;

        WarnOnOverlaps(nodes);

        TopoGraph graph = TowerTopologyV3.BuildCanonical();
        foreach (string nid in graph.NodeFloor.Keys)
            if (!byId.ContainsKey(nid))
                Debug.LogWarning($"[TowerV3] Graph node '{nid}' has no coordinate in the table — it will have no geometry.");

        BuildFloorPlates(root.transform);

        // Pass 1: collect door gaps per node (so room walls are drawn with the right openings).
        var gaps = new Dictionary<string, List<(Edge edge, float center, float width)>>();
        void AddGap(string id, Edge e, float c, float w)
        {
            if (!gaps.TryGetValue(id, out var list)) { list = new List<(Edge, float, float)>(); gaps[id] = list; }
            list.Add((e, c, w));
        }

        var connectorsParent = new GameObject("Connectors"); connectorsParent.transform.SetParent(root.transform);
        var corridorInstructions = new List<(BlackCommission.Level.Edge edge, Node a, Node b)>();

        foreach (BlackCommission.Level.Edge e in graph.Edges)
        {
            if (!byId.TryGetValue(e.A, out Node a) || !byId.TryGetValue(e.B, out Node b)) continue;
            
            float width = CorridorOpeningWidth;
            if (e.Kind == EdgeKind.Door) width = DoorWidth;
            else if (e.Kind == EdgeKind.Stair || e.Kind == EdgeKind.ScaffoldDrop) width = 2.0f;

            var (ea, ca) = WallToward(a, Center(b));
            var (eb, cb) = WallToward(b, Center(a));
            AddGap(a.id, ea, ca, width);
            AddGap(b.id, eb, cb, width);

            if (e.Kind != EdgeKind.Stair && e.Kind != EdgeKind.ScaffoldDrop)
                corridorInstructions.Add((e, a, b));
        }

        // Pass 2: rooms (floor + walls with gaps + RoomSlot). Junctions/exits get just a floor pad.
        var floorsParent = new GameObject("Rooms"); floorsParent.transform.SetParent(root.transform);
        foreach (Node n in nodes)
        {
            gaps.TryGetValue(n.id, out var roomGaps);
            BuildNode(floorsParent.transform, n, roomGaps);
        }

        // Pass 3: realise each corridor/door + blocker + Connector component.
        foreach (var (e, a, b) in corridorInstructions)
            BuildConnector(connectorsParent.transform, e, a, b, nodes);

        // Stairs + scaffold drop (the inter-floor descents).
        BuildDescents(root.transform, byId, graph);

        // Exterior (simple): fence, forecourt, van pad, one-sided perimeter run, a few cover props.
        BuildExterior(root.transform, byId);

        // Runtime generator (drives topology toggles + content fill).
        var gen = new GameObject("TowerLayoutGenerator");
        gen.transform.SetParent(root.transform);
        gen.AddComponent<NetworkObject>();
        gen.AddComponent<TowerLayoutGenerator>();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"[TowerV3] Rebuilt v3 whitebox from the connectivity graph ({nodes.Count} nodes, " +
                  $"{graph.Edges.Count} edges). Deleted old roots. Next: bake NavMesh + author a TowerRoomCatalog.");
    }

    // ───────────────────────────── geometry ─────────────────────────────

    static void BuildFloorPlates(Transform root)
    {
        var parent = new GameObject("UnfinishedFloorPlates");
        parent.transform.SetParent(root);

        // F1: One large plate with a hole for COLLAPSE.
        AddFloorPlate(parent.transform, 1, -10, -12, 70, 36, "Plate_F1_South");
        AddFloorPlate(parent.transform, 1, 12, 24, 48, 30, "Plate_F1_North");
        AddFloorPlate(parent.transform, 1, -10, 24, 10, 30, "Plate_F1_WestEdge");

        // F2: Frame the holes for STAIRA2, STAIRB2, BALCONY, and SHAFT.
        AddFloorPlate(parent.transform, 2, -4, 20, 28, 20, "Plate_F2_NorthWest"); // West of STAIRA2
        AddFloorPlate(parent.transform, 2, 28, 20, 28, 20, "Plate_F2_NorthEast"); // East of STAIRA2
        AddFloorPlate(parent.transform, 2, 4, 4, 8, 16, "Plate_F2_WestMid");     // East of STAIRB2, West of SHAFT
        AddFloorPlate(parent.transform, 2, 28, 8, 28, 12, "Plate_F2_EastMid");   // East of SHAFT
        AddFloorPlate(parent.transform, 2, -4, -4, 32, 8, "Plate_F2_SouthWest"); // West of BALCONY
        AddFloorPlate(parent.transform, 2, 32, -4, 24, 12, "Plate_F2_SouthEast"); // East of BALCONY

        // Visual-only rims clarify intentional holes and unfinished slab edges.
        AddVoidRim(parent.transform, 2, 12, 12, 28, 16, "F2_ShaftVoid_Rim");
        AddVoidRim(parent.transform, 1, 0, 24, 12, 40, "F1_CollapseOpenSky_Rim");
        AddVoidRim(parent.transform, 2, 24, 4, 32, 8, "F2_BalconyDrop_Rim");
    }

    static void AddFloorPlate(Transform parent, int floor, float x, float z, float w, float d, string name)
    {
        float y = (floor == 2 ? Floor2Y : 0f) - FloorThickness * 0.5f - 0.005f;
        AddSlab(parent, floorMat, x + w * 0.5f, y, z + d * 0.5f, w, FloorThickness, d, name);
    }

    static void AddVoidRim(Transform parent, int floor, float x0, float z0, float x1, float z1, string name)
    {
        float y = (floor == 2 ? Floor2Y : 0f) + 0.12f;
        float cx = (x0 + x1) * 0.5f;
        float cz = (z0 + z1) * 0.5f;
        float w = x1 - x0;
        float d = z1 - z0;
        AddVisualSlab(parent, blockerMat, cx, y, z0, w, 0.24f, 0.14f, name + "_S");
        AddVisualSlab(parent, blockerMat, cx, y, z1, w, 0.24f, 0.14f, name + "_N");
        AddVisualSlab(parent, blockerMat, x0, y, cz, 0.14f, 0.24f, d, name + "_W");
        AddVisualSlab(parent, blockerMat, x1, y, cz, 0.14f, 0.24f, d, name + "_E");
    }

    static void BuildNode(Transform parent, Node n, List<(Edge edge, float center, float width)> roomGaps)
    {
        var go = new GameObject(n.id);
        go.transform.SetParent(parent);
        float y = n.floor == 2 ? Floor2Y : 0f;

        Material mat = n.kind switch
        {
            Kind.Collapse => blockerMat,
            Kind.Van => extMat,
            Kind.Stair => stairMat,
            _ => floorMat
        };
        // Walls (excluding junctions).
        if (n.kind != Kind.Junction)
        {
            var byEdge = new Dictionary<Edge, List<(float center, float width)>>();
            if (roomGaps != null)
                foreach (var g in roomGaps)
                {
                    if (!byEdge.TryGetValue(g.edge, out var list)) { list = new List<(float, float)>(); byEdge[g.edge] = list; }
                    list.Add((g.center, g.width));
                }
            AddWallLine(go.transform, y, "Wall_N", n.x, n.z + n.d, n.x + n.w, n.z + n.d, GapsFor(byEdge, Edge.North));
            AddWallLine(go.transform, y, "Wall_S", n.x, n.z, n.x + n.w, n.z, GapsFor(byEdge, Edge.South));
            AddWallLine(go.transform, y, "Wall_E", n.x + n.w, n.z, n.x + n.w, n.z + n.d, GapsFor(byEdge, Edge.East));
            AddWallLine(go.transform, y, "Wall_W", n.x, n.z, n.x, n.z + n.d, GapsFor(byEdge, Edge.West));
        }

        // RoomSlot marker for fillable nodes.
        if (n.kind == Kind.Room || n.kind == Kind.Van || n.kind == Kind.Stair)
        {
            // Floor slab for first floor or non-descent rooms.
            bool isDescentHole = (n.floor == 2 && (n.kind == Kind.Stair || n.id == "BALCONY"));
            if (!isDescentHole)
            {
                AddSlab(go.transform, mat, n.x + n.w * 0.5f, y - FloorThickness * 0.5f, n.z + n.d * 0.5f, n.w, FloorThickness, n.d, "Floor");
            }
var anchor = new GameObject(n.id + "_Slot");
            anchor.transform.SetParent(go.transform);
            anchor.transform.position = new Vector3(n.x + n.w * 0.5f, y, n.z + n.d * 0.5f);
            var slot = anchor.AddComponent<RoomSlot>();
            slot.size = n.size;
            slot.role = n.role;
            slot.floor = n.floor;
            slot.slotId = n.id;
        }
    }

    static void BuildConnector(Transform parent, BlackCommission.Level.Edge e, Node a, Node b, List<Node> allNodes)
    {
        var conn = new GameObject($"Connector_{e.Id}");
        conn.transform.SetParent(parent);
        float ya = a.floor == 2 ? Floor2Y : 0f;

        Vector2 aPort = PortPoint(a, Center(b));
        Vector2 bPort = PortPoint(b, Center(a));
        float width = e.Kind == EdgeKind.Door ? DoorWidth : CorridorWidth;

        List<Vector2> route = ResolveConnectorRoute(e.Id, a, b, aPort, bPort, width, allNodes, out var finalHits);

        if (finalHits.Count > 0)
        {
            Debug.LogWarning($"[TowerV3] Edge '{e.Id}' ({a.id}->{b.id}) route collides with: {string.Join(", ", finalHits)}");
        }

        var geometry = new GameObject("Geometry");
        geometry.transform.SetParent(conn.transform);
        for (int i = 0; i < route.Count - 1; i++)
            AddCorridorRun(geometry.transform, ya, route[i], route[i + 1], width);

        // Blocker (rubble) at the corridor midpoint — full height; carries a NavMeshObstacle so a
// closed connector reroutes agents. Starts inactive; the generator toggles it per seed.
        var blocker = new GameObject("Blocker");
        blocker.transform.SetParent(conn.transform);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Rubble";
        cube.transform.SetParent(blocker.transform);
        Vector2 blockerPos = PointAlongRoute(route, 0.5f);
        cube.transform.position = new Vector3(blockerPos.x, ya + WallHeight * 0.5f, blockerPos.y);
        cube.transform.localScale = new Vector3(width, WallHeight, width);
        cube.GetComponent<Renderer>().sharedMaterial = blockerMat;
        var obstacle = cube.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.size = Vector3.one;
        blocker.SetActive(false);

        var c = conn.AddComponent<Connector>();
        c.id = e.Id;
        c.aSlotId = e.A;
        c.bSlotId = e.B;
        c.kind = e.Kind;
        SetConnectorRefs(c, geometry, blocker);
    }

    static List<Vector2> ResolveConnectorRoute(string edgeId, Node a, Node b, Vector2 aPort, Vector2 bPort,
        float width, List<Node> allNodes, out List<string> finalHits)
    {
        var candidates = new List<List<Vector2>>
        {
            CompressRoute(new List<Vector2> { aPort, new Vector2(aPort.x, bPort.y), bPort }),
            CompressRoute(new List<Vector2> { aPort, new Vector2(bPort.x, aPort.y), bPort })
        };

        var gridRoute = FindClearGridRoute(a, b, aPort, bPort, width, allNodes);
        if (gridRoute != null && gridRoute.Count > 1)
            candidates.Add(gridRoute);

        List<Vector2> best = candidates[0];
        List<string> bestHits = GetIntersections(Segments(best), width, a.id, b.id, a.floor, allNodes);
        float bestScore = RouteScore(best, bestHits);
        for (int i = 1; i < candidates.Count; i++)
        {
            List<string> hits = GetIntersections(Segments(candidates[i]), width, a.id, b.id, a.floor, allNodes);
            float score = RouteScore(candidates[i], hits);
            if (score < bestScore)
            {
                best = candidates[i];
                bestHits = hits;
                bestScore = score;
            }
        }

        finalHits = bestHits;
        return best;
    }

    static List<Vector2> FindClearGridRoute(Node a, Node b, Vector2 aPort, Vector2 bPort,
        float width, List<Node> allNodes)
    {
        var xs = new SortedSet<float> { aPort.x, bPort.x };
        var zs = new SortedSet<float> { aPort.y, bPort.y };
        float clearance = width * 0.5f + 0.35f;

        foreach (Node n in allNodes)
        {
            if (n.floor != a.floor || n.id == a.id || n.id == b.id) continue;
            if (!BlocksCorridorRoute(n.kind)) continue;
            xs.Add(n.x - clearance);
            xs.Add(n.x + n.w + clearance);
            zs.Add(n.z - clearance);
            zs.Add(n.z + n.d + clearance);
        }

        var points = new List<Vector2>();
        foreach (float x in xs)
            foreach (float z in zs)
                points.Add(new Vector2(x, z));
        points.Add(aPort);
        points.Add(bPort);

        int start = points.Count - 2;
        int goal = points.Count - 1;
        int count = points.Count;
        var dist = new float[count];
        var prev = new int[count];
        var done = new bool[count];
        for (int i = 0; i < count; i++) { dist[i] = float.PositiveInfinity; prev[i] = -1; }
        dist[start] = 0f;

        for (int iter = 0; iter < count; iter++)
        {
            int cur = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                if (done[i] || dist[i] >= best) continue;
                cur = i;
                best = dist[i];
            }
            if (cur < 0 || cur == goal) break;
            done[cur] = true;

            for (int next = 0; next < count; next++)
            {
                if (done[next] || next == cur) continue;
                Vector2 p = points[cur], q = points[next];
                if (!SameAxis(p, q)) continue;
                var segment = new List<(Vector2 s, Vector2 e)> { (p, q) };
                if (GetIntersections(segment, width, a.id, b.id, a.floor, allNodes).Count > 0) continue;

                float alt = dist[cur] + Vector2.Distance(p, q);
                if (alt < dist[next])
                {
                    dist[next] = alt;
                    prev[next] = cur;
                }
            }
        }

        if (prev[goal] < 0) return null;
        var route = new List<Vector2>();
        for (int at = goal; at >= 0; at = prev[at])
        {
            route.Add(points[at]);
            if (at == start) break;
        }
        route.Reverse();
        return CompressRoute(route);
    }

    static float RouteScore(List<Vector2> route, List<string> hits)
    {
        float length = 0f;
        for (int i = 0; i < route.Count - 1; i++)
            length += Vector2.Distance(route[i], route[i + 1]);
        return hits.Count * 10000f + Turns(route) * 8f + length;
    }

    static int Turns(List<Vector2> route)
    {
        int turns = 0;
        for (int i = 1; i < route.Count - 1; i++)
        {
            bool prevHorizontal = Mathf.Abs(route[i].x - route[i - 1].x) > Mathf.Abs(route[i].y - route[i - 1].y);
            bool nextHorizontal = Mathf.Abs(route[i + 1].x - route[i].x) > Mathf.Abs(route[i + 1].y - route[i].y);
            if (prevHorizontal != nextHorizontal) turns++;
        }
        return turns;
    }

    static Vector2 PointAlongRoute(List<Vector2> route, float t)
    {
        float total = 0f;
        for (int i = 0; i < route.Count - 1; i++)
            total += Vector2.Distance(route[i], route[i + 1]);
        if (total <= 0.01f) return route[0];

        float target = total * Mathf.Clamp01(t);
        for (int i = 0; i < route.Count - 1; i++)
        {
            float len = Vector2.Distance(route[i], route[i + 1]);
            if (target <= len)
                return Vector2.Lerp(route[i], route[i + 1], target / Mathf.Max(len, 0.001f));
            target -= len;
        }
        return route[route.Count - 1];
    }

    static List<(Vector2 s, Vector2 e)> Segments(List<Vector2> route)
    {
        var result = new List<(Vector2, Vector2)>();
        for (int i = 0; i < route.Count - 1; i++)
            result.Add((route[i], route[i + 1]));
        return result;
    }

    static List<Vector2> CompressRoute(List<Vector2> route)
    {
        var result = new List<Vector2>();
        foreach (Vector2 p in route)
        {
            if (result.Count == 0 || Vector2.Distance(result[result.Count - 1], p) > 0.05f)
                result.Add(p);
        }
        for (int i = result.Count - 2; i > 0; i--)
        {
            if (SameAxis(result[i - 1], result[i]) && SameAxis(result[i], result[i + 1]) &&
                SameAxis(result[i - 1], result[i + 1]))
                result.RemoveAt(i);
        }
        return result;
    }

    static bool SameAxis(Vector2 a, Vector2 b) =>
        Mathf.Abs(a.x - b.x) < 0.01f || Mathf.Abs(a.y - b.y) < 0.01f;

    static bool BlocksCorridorRoute(Kind kind) =>
        kind == Kind.Room || kind == Kind.Stair || kind == Kind.Van || kind == Kind.Fire || kind == Kind.Collapse;

    static List<string> GetIntersections(List<(Vector2 s, Vector2 e)> segments, float width, string idA, string idB, int floor, List<Node> nodes)
    {
        var hits = new List<string>();
        float halfW = width * 0.5f;
        foreach (var seg in segments)
        {
            if (Vector2.Distance(seg.s, seg.e) < 0.1f) continue;

            bool vertical = Mathf.Abs(seg.s.x - seg.e.x) < 0.01f;
            bool horizontal = Mathf.Abs(seg.s.y - seg.e.y) < 0.01f;
            float minX = Mathf.Min(seg.s.x, seg.e.x) - (vertical ? halfW : 0);
            float maxX = Mathf.Max(seg.s.x, seg.e.x) + (vertical ? halfW : 0);
            float minZ = Mathf.Min(seg.s.y, seg.e.y) - (horizontal ? halfW : 0);
            float maxZ = Mathf.Max(seg.s.y, seg.e.y) + (horizontal ? halfW : 0);

            foreach (var n in nodes)
            {
                if (n.floor != floor || n.id == idA || n.id == idB) continue;
                if (!BlocksCorridorRoute(n.kind)) continue;
                bool overlap = maxX > n.x + 0.05f && minX < n.x + n.w - 0.05f &&
                               maxZ > n.z + 0.05f && minZ < n.z + n.d - 0.05f;
                if (overlap) hits.Add(n.id);
            }
        }
        return hits.Distinct().ToList();
    }

    // Connector.geometry/blocker are private [SerializeField]; set them via SerializedObject so the
    // editor build wires them without exposing public setters.
    static void SetConnectorRefs(Connector c, GameObject geometry, GameObject blocker)
    {
        var so = new SerializedObject(c);
        so.FindProperty("geometry").objectReferenceValue = geometry;
        so.FindProperty("blocker").objectReferenceValue = blocker;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BuildDescents(Transform root, Dictionary<string, Node> byId, TopoGraph graph)
    {
        var parent = new GameObject("Descents"); parent.transform.SetParent(root);
        foreach (BlackCommission.Level.Edge e in graph.Edges)
        {
            if (e.Kind != EdgeKind.Stair && e.Kind != EdgeKind.ScaffoldDrop) continue;
            if (!byId.TryGetValue(e.A, out Node a) || !byId.TryGetValue(e.B, out Node b)) continue;

            var conn = new GameObject($"Connector_{e.Id}"); conn.transform.SetParent(parent.transform);
            var geometry = new GameObject("Geometry"); geometry.transform.SetParent(conn.transform);

            Vector2 top = Center(a.floor == 2 ? a : b);     // upper node
            Vector2 bottom = Center(a.floor == 1 ? a : b);   // lower node

            // If nodes are stacked (same X,Z), create a ramp across the room depth
            if (e.Kind == EdgeKind.Stair && Vector2.Distance(top, bottom) < 0.1f)
            {
                Node stairNode = a.floor == 2 ? a : b;
                bottom = new Vector2(stairNode.x + stairNode.w * 0.5f, stairNode.z + 1.5f);
                top = new Vector2(stairNode.x + stairNode.w * 0.5f, stairNode.z + stairNode.d - 1.5f);
            }

            Material m = e.Kind == EdgeKind.ScaffoldDrop ? scaffoldMat : stairMat;

            if (e.Kind == EdgeKind.ScaffoldDrop)
                AddScaffoldDrop(geometry.transform, m, top, bottom);
            else
                AddRampFlight(geometry.transform, m, bottom, top, 16);

            var c = conn.AddComponent<Connector>();
            c.id = e.Id; c.aSlotId = e.A; c.bSlotId = e.B; c.kind = e.Kind;
            SetConnectorRefs(c, geometry, null); // descents are fixedOpen; no blocker
        }
    }

    static void BuildExterior(Transform root, Dictionary<string, Node> byId)
    {
        var ext = new GameObject("Exterior"); ext.transform.SetParent(root);
        // Site fence ring (loose box around the footprint). Whitebox curbs, non-precise.
        float x0 = -6, x1 = 56, z0 = -16, z1 = 48;
        AddSlab(ext.transform, extMat, (x0 + x1) * 0.5f, 0.6f, z0, x1 - x0, 1.2f, 0.4f, "Fence_S");
        AddSlab(ext.transform, extMat, (x0 + x1) * 0.5f, 0.6f, z1, x1 - x0, 1.2f, 0.4f, "Fence_N");
        AddSlab(ext.transform, extMat, x0, 0.6f, (z0 + z1) * 0.5f, 0.4f, 1.2f, z1 - z0, "Fence_W");
        AddSlab(ext.transform, extMat, x1, 0.6f, (z0 + z1) * 0.5f, 0.4f, 1.2f, z1 - z0, "Fence_E");

        // Forecourt + van pad (south, under the van node).
        if (byId.TryGetValue("VAN", out Node van))
            AddSlab(ext.transform, extMat, van.x + van.w * 0.5f, -FloorThickness, van.z + van.d * 0.5f,
                van.w + 6, FloorThickness, van.d + 4, "Forecourt");

        // One-sided perimeter run down the EAST side: fire exit (far north) back to the forecourt.
        AddSlab(ext.transform, extMat, 52, -FloorThickness, 16, 6, FloorThickness, 56, "PerimeterRun_East");

        // A few cover props (crane base, rebar/material stacks) to break sightlines on the run.
        AddSlab(ext.transform, blockerMat, 51, 1.5f, 8, 2, 3f, 2, "Prop_CraneBase");
        AddSlab(ext.transform, blockerMat, 53, 0.6f, 28, 1.5f, 1.2f, 4, "Prop_RebarStack");
        AddSlab(ext.transform, blockerMat, 50, 0.6f, 40, 3, 1.2f, 1.5f, "Prop_SpoilPile");
    }

    // ───────────────────────────── helpers ─────────────────────────────

    static Vector2 Center(Node n) => new Vector2(n.x + n.w * 0.5f, n.z + n.d * 0.5f);

    // Which wall of n faces target, and the gap center along that wall.
    static (Edge edge, float center) WallToward(Node n, Vector2 target)
    {
        Vector2 c = Center(n);
        float dx = target.x - c.x, dz = target.y - c.y;
        if (Mathf.Abs(dx) >= Mathf.Abs(dz))
            return (dx >= 0 ? Edge.East : Edge.West, target.y);
        return (dz >= 0 ? Edge.North : Edge.South, target.x);
    }

    // The point on n's boundary where a corridor toward target attaches.
    static Vector2 PortPoint(Node n, Vector2 target)
    {
        var (edge, center) = WallToward(n, target);
        return edge switch
        {
            Edge.East => new Vector2(n.x + n.w, center),
            Edge.West => new Vector2(n.x, center),
            Edge.North => new Vector2(center, n.z + n.d),
            _ => new Vector2(center, n.z) // South
        };
    }

    // A straight axis-aligned corridor floor slab between two points sharing one coordinate.
    static void AddCorridorRun(Transform parent, float y, Vector2 from, Vector2 to, float width)
    {
        float len = Vector2.Distance(from, to);
        if (len < 0.05f) return;
        Vector2 mid = (from + to) * 0.5f;
        bool horizontal = Mathf.Abs(to.x - from.x) >= Mathf.Abs(to.y - from.y);
        float w = horizontal ? len : width;
        float d = horizontal ? width : len;
        AddSlab(parent, corridorMat, mid.x, y - FloorThickness * 0.5f, mid.y, w, FloorThickness, d, "Run");
    }

    static void AddDoorFrame(Transform parent, float floorY, Node n, Vector2 port, string name)
    {
        if (n.kind != Kind.Room && n.kind != Kind.Stair) return;

        Edge edge = PortEdge(n, port);
        bool verticalWall = edge == Edge.East || edge == Edge.West;
        float wallDepth = WallThickness * 1.55f;
        float postY = floorY + DoorClearHeight * 0.5f;
        float headerY = floorY + DoorClearHeight + DoorHeaderHeight * 0.5f;
        float thresholdY = floorY + 0.025f;

        if (verticalWall)
        {
            AddVisualSlab(parent, wallMat, port.x, postY, port.y - DoorWidth * 0.5f - DoorFramePostWidth * 0.5f,
                wallDepth, DoorClearHeight, DoorFramePostWidth, name + "_FrameA");
            AddVisualSlab(parent, wallMat, port.x, postY, port.y + DoorWidth * 0.5f + DoorFramePostWidth * 0.5f,
                wallDepth, DoorClearHeight, DoorFramePostWidth, name + "_FrameB");
            AddVisualSlab(parent, wallMat, port.x, headerY, port.y,
                wallDepth, DoorHeaderHeight, DoorWidth + DoorFramePostWidth * 2f, name + "_Header");
            AddVisualSlab(parent, corridorMat, port.x, thresholdY, port.y,
                wallDepth, 0.05f, DoorWidth, name + "_Threshold");
        }
        else
        {
            AddVisualSlab(parent, wallMat, port.x - DoorWidth * 0.5f - DoorFramePostWidth * 0.5f, postY, port.y,
                DoorFramePostWidth, DoorClearHeight, wallDepth, name + "_FrameA");
            AddVisualSlab(parent, wallMat, port.x + DoorWidth * 0.5f + DoorFramePostWidth * 0.5f, postY, port.y,
                DoorFramePostWidth, DoorClearHeight, wallDepth, name + "_FrameB");
            AddVisualSlab(parent, wallMat, port.x, headerY, port.y,
                DoorWidth + DoorFramePostWidth * 2f, DoorHeaderHeight, wallDepth, name + "_Header");
            AddVisualSlab(parent, corridorMat, port.x, thresholdY, port.y,
                DoorWidth, 0.05f, wallDepth, name + "_Threshold");
        }
    }

    static Edge PortEdge(Node n, Vector2 port)
    {
        if (Mathf.Abs(port.x - (n.x + n.w)) < 0.05f) return Edge.East;
        if (Mathf.Abs(port.x - n.x) < 0.05f) return Edge.West;
        if (Mathf.Abs(port.y - (n.z + n.d)) < 0.05f) return Edge.North;
        return Edge.South;
    }

    static void AddRampFlight(Transform parent, Material mat, Vector2 bottom, Vector2 top, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            float t = (i + 0.5f) / steps;
            float x = Mathf.Lerp(bottom.x, top.x, t);
            float z = Mathf.Lerp(bottom.y, top.y, t);
            float yTop = Mathf.Lerp(0f, Floor2Y, (i + 1f) / steps);
            var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = $"Step_{i + 1:00}";
            step.transform.SetParent(parent);
            step.transform.position = new Vector3(x, yTop - 0.11f, z);
            step.transform.localScale = new Vector3(2.2f, 0.22f, 1.1f);
            step.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    static void AddScaffoldDrop(Transform parent, Material mat, Vector2 top, Vector2 bottom)
    {
        // Creates 3 landing platforms at different heights to facilitate a "drop" descent
        int platforms = 3;
        for (int i = 0; i < platforms; i++)
        {
            float t = (float)(i + 1) / (platforms + 1);
            float x = Mathf.Lerp(top.x, bottom.x, t);
            float z = Mathf.Lerp(top.y, bottom.y, t);
            float y = Mathf.Lerp(Floor2Y, 0f, t);

            AddSlab(parent, mat, x, y - FloorThickness * 0.5f, z, 3f, FloorThickness, 3f, $"Platform_{i + 1}");
            // Add a vertical "support" pole
            AddSlab(parent, mat, x + 1.2f, y * 0.5f, z + 1.2f, 0.2f, y, 0.2f, $"Pole_{i + 1}");
        }
    }

    static void AddSlab(Transform parent, Material mat, float cx, float cy, float cz, float sx, float sy, float sz, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(cx, cy, cz);
        go.transform.localScale = new Vector3(sx, sy, sz);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void AddVisualSlab(Transform parent, Material mat, float cx, float cy, float cz, float sx, float sy, float sz, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(cx, cy, cz);
        go.transform.localScale = new Vector3(sx, sy, sz);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        var collider = go.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
    }

    static Gap[] GapsFor(Dictionary<Edge, List<(float center, float width)>> byEdge, Edge edge)
    {
        if (!byEdge.TryGetValue(edge, out var list)) return new Gap[0];
        var gaps = new Gap[list.Count];
        for (int i = 0; i < list.Count; i++)
            gaps[i] = new Gap(list[i].center - list[i].width * 0.5f, list[i].center + list[i].width * 0.5f);
        return gaps;
    }

    static void AddWallLine(Transform parent, float floorY, string name, float x0, float z0, float x1, float z1, params Gap[] gaps)
    {
        bool horizontal = Mathf.Abs(z1 - z0) < 0.01f;
        float start = horizontal ? Mathf.Min(x0, x1) : Mathf.Min(z0, z1);
        float end = horizontal ? Mathf.Max(x0, x1) : Mathf.Max(z0, z1);
        var sorted = NormalizeWallGaps(gaps, start, end);

        float cursor = start; int seg = 0;
        foreach (var gap in sorted)
        {
            if (gap.min > cursor) AddWallSeg(parent, floorY, $"{name}_{++seg:00}", horizontal, x0, z0, cursor, gap.min);
            cursor = Mathf.Max(cursor, gap.max);
        }
        if (cursor < end) AddWallSeg(parent, floorY, $"{name}_{++seg:00}", horizontal, x0, z0, cursor, end);
    }

    static List<Gap> NormalizeWallGaps(IEnumerable<Gap> gaps, float start, float end)
    {
        var clamped = new List<Gap>();
        foreach (var raw in gaps)
        {
            float min = Mathf.Clamp(raw.min, start, end);
            float max = Mathf.Clamp(raw.max, start, end);
            if (max - min >= 0.05f)
                clamped.Add(new Gap(min, max));
        }

        clamped.Sort((a, b) => a.min.CompareTo(b.min));
        var merged = new List<Gap>();
        foreach (var gap in clamped)
        {
            if (merged.Count == 0 || gap.min > merged[merged.Count - 1].max)
            {
                merged.Add(gap);
                continue;
            }

            Gap last = merged[merged.Count - 1];
            merged[merged.Count - 1] = new Gap(last.min, Mathf.Max(last.max, gap.max));
        }

        float sideLength = end - start;
        float keep = 0.15f; // reduced from 0.45f to allow better alignment
        if (sideLength <= keep * 2f + 0.2f)
            return merged;

        float maxOpening = sideLength - keep * 2f;
        var capped = new List<Gap>();
        foreach (var gap in merged)
        {
            float width = Mathf.Min(gap.max - gap.min, maxOpening);
            float center = (gap.min + gap.max) * 0.5f;
            float minCenter = start + keep + width * 0.5f;
            float maxCenter = end - keep - width * 0.5f;
            center = Mathf.Clamp(center, minCenter, maxCenter);
            capped.Add(new Gap(center - width * 0.5f, center + width * 0.5f));
        }
        return capped;
    }

    static void AddWallSeg(Transform parent, float floorY, string name, bool horizontal, float baseX, float baseZ, float a, float b)
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

    static void WarnOnOverlaps(List<Node> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
            for (int j = i + 1; j < nodes.Count; j++)
            {
                Node a = nodes[i], b = nodes[j];
                if (a.floor != b.floor) continue;
                bool overlap = a.x < b.x + b.w && a.x + a.w > b.x && a.z < b.z + b.d && a.z + a.d > b.z;
                if (overlap)
                    Debug.LogWarning($"[TowerV3] Room rects overlap on floor {a.floor}: '{a.id}' and '{b.id}'. " +
                                     "Nudge coordinates in BuildNodeTable (connectivity is unaffected).");
            }
    }

    static void EnsureMaterials()
    {
        floorMat = Mat("V3_Floor", new Color(0.70f, 0.70f, 0.70f));
        wallMat = Mat("V3_Wall", new Color(0.92f, 0.92f, 0.88f));
        corridorMat = Mat("V3_Corridor", new Color(0.58f, 0.62f, 0.60f));
        blockerMat = Mat("V3_Rubble", new Color(0.35f, 0.30f, 0.26f));
        stairMat = Mat("V3_Stair", new Color(0.82f, 0.82f, 0.78f));
        scaffoldMat = Mat("V3_Scaffold", new Color(0.62f, 0.42f, 0.22f));
        extMat = Mat("V3_Exterior", new Color(0.40f, 0.42f, 0.45f));
    }

    static Material Mat(string name, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.name = name; mat.color = color; return mat;
    }

    enum Edge { North, South, East, West }

    readonly struct Gap
    {
        public readonly float min, max;
        public Gap(float min, float max) { this.min = min; this.max = max; }
    }
}
