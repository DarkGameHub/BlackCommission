using System.Collections.Generic;
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
        return new List<Node>
        {
            // ---- Floor 1 ----
            new Node("VAN", 1, 18, -10, 12, 8, Kind.Van, M, RoomSlotRole.Van),
            new Node("LOBBY", 1, 14, 0, 12, 8, Kind.Room, L, RoomSlotRole.Fixed),
            new Node("SECUR", 1, 8, 0, 4, 4, Kind.Room, S, R),
            new Node("PWR", 1, 2, 2, 4, 4, Kind.Room, S, RoomSlotRole.PowerGate),
            new Node("TEMP", 1, 2, 6, 4, 4, Kind.Room, S, R),
            new Node("STAIRB1", 1, 2, 12, 4, 8, Kind.Stair, M, RoomSlotRole.Stair),
            new Node("HALL", 1, 14, 12, 12, 8, Kind.Room, L, RoomSlotRole.Fixed),
            new Node("SAMPLE", 1, 8, 20, 4, 4, Kind.Room, S, R),
            new Node("WORKSHOP", 1, 2, 24, 8, 8, Kind.Room, M, R),
            new Node("WAREHOUSE", 1, 2, 34, 12, 8, Kind.Room, L, R),
            new Node("DORM", 1, 16, 24, 8, 8, Kind.Room, M, R),
            new Node("CANTEEN", 1, 26, 24, 8, 8, Kind.Room, M, R),
            new Node("FOREMAN", 1, 36, 24, 8, 8, Kind.Room, M, R),
            new Node("DOCK", 1, 40, 8, 8, 8, Kind.Room, M, R),
            new Node("COLLAPSE", 1, 16, 34, 8, 8, Kind.Collapse),
            new Node("FIRE", 1, 36, 34, 8, 4, Kind.Fire),
            new Node("STAIRA1", 1, 44, 20, 4, 8, Kind.Stair, M, RoomSlotRole.Stair),
            new Node("JSW", 1, 10, 8, 4, 4, Kind.Junction),
            new Node("JW", 1, 10, 14, 4, 4, Kind.Junction),
            new Node("JNW", 1, 16, 20, 4, 4, Kind.Junction),
            new Node("JN", 1, 26, 20, 4, 4, Kind.Junction),
            new Node("JNE", 1, 38, 20, 4, 4, Kind.Junction),

            // ---- Floor 2 (smaller, offset; stairs aligned with F1) ----
            new Node("SHOWFLAT", 2, 14, 12, 8, 8, Kind.Room, M, RoomSlotRole.Fixed),
            new Node("TARGET", 2, 14, 24, 12, 8, Kind.Room, L, RoomSlotRole.Objective),
            new Node("EXEC", 2, 28, 24, 8, 8, Kind.Room, M, R),
            new Node("MODEL", 2, 28, 16, 8, 8, Kind.Room, M, R),
            new Node("SALES", 2, 38, 16, 8, 8, Kind.Room, M, R),
            new Node("SHAFTEDGE", 2, 22, 6, 12, 8, Kind.Room, L, R),
            new Node("MAINT", 2, 22, 0, 4, 4, Kind.Room, S, R),
            new Node("DANGER", 2, 28, 0, 4, 4, Kind.Room, S, R),
            new Node("MARKET", 2, 40, 8, 4, 4, Kind.Room, S, R),
            new Node("VIP", 2, 38, 24, 8, 8, Kind.Room, M, R),
            new Node("BALCONY", 2, 44, 8, 4, 4, Kind.Room, S, RoomSlotRole.Fixed),
            new Node("STAIRB2", 2, 2, 12, 4, 8, Kind.Stair, M, RoomSlotRole.Stair),
            new Node("STAIRA2", 2, 44, 20, 4, 8, Kind.Stair, M, RoomSlotRole.Stair),
            new Node("BRIDGEJ", 2, 18, 8, 4, 4, Kind.Junction),
            new Node("JW2", 2, 12, 20, 4, 4, Kind.Junction),
            new Node("JE2", 2, 34, 20, 4, 4, Kind.Junction),
        };
    }

    [MenuItem("Tools/Black Commission/MVP/Tower/Rebuild v3 Whitebox")]
    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureMaterials();

        foreach (string old in OldRootsToDelete)
        {
            GameObject go = GameObject.Find(old);
            if (go != null) Object.DestroyImmediate(go);
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
            if (e.Kind == EdgeKind.Stair || e.Kind == EdgeKind.ScaffoldDrop) continue; // handled as flights/ramps

            float width = e.Kind == EdgeKind.Door ? DoorWidth : CorridorWidth;
            var (ea, ca) = WallToward(a, Center(b));
            var (eb, cb) = WallToward(b, Center(a));
            AddGap(a.id, ea, ca, width);
            AddGap(b.id, eb, cb, width);
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
            BuildConnector(connectorsParent.transform, e, a, b);

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
        AddSlab(go.transform, mat, n.x + n.w * 0.5f, y - FloorThickness * 0.5f, n.z + n.d * 0.5f, n.w, FloorThickness, n.d, "Floor");

        // Walls (rooms only; junctions/collapse/fire stay open pads). Collapse has no roof anyway.
        if (n.kind == Kind.Room || n.kind == Kind.Stair)
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

        // RoomSlot marker for fillable nodes. Lives on a child anchor at the room centre so the
        // parent (and its world-positioned geometry children) stay at the origin.
        if (n.kind == Kind.Room || n.kind == Kind.Van || n.kind == Kind.Stair)
        {
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

    static void BuildConnector(Transform parent, BlackCommission.Level.Edge e, Node a, Node b)
    {
        var conn = new GameObject($"Connector_{e.Id}");
        conn.transform.SetParent(parent);
        float ya = a.floor == 2 ? Floor2Y : 0f;

        Vector2 aPort = PortPoint(a, Center(b));
        Vector2 bPort = PortPoint(b, Center(a));
        Vector2 corner = new Vector2(aPort.x, bPort.y); // L-route: horizontal from A, vertical to B

        float width = e.Kind == EdgeKind.Door ? DoorWidth : CorridorWidth;

        var geometry = new GameObject("Geometry");
        geometry.transform.SetParent(conn.transform);
        AddCorridorRun(geometry.transform, ya, aPort, corner, width);
        AddCorridorRun(geometry.transform, ya, corner, bPort, width);

        // Blocker (rubble) at the corridor midpoint — full height; carries a NavMeshObstacle so a
        // closed connector reroutes agents. Starts inactive; the generator toggles it per seed.
        var blocker = new GameObject("Blocker");
        blocker.transform.SetParent(conn.transform);
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Rubble";
        cube.transform.SetParent(blocker.transform);
        cube.transform.position = new Vector3(corner.x, ya + WallHeight * 0.5f, corner.y);
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

            var conn = new GameObject($"Connector_{e.Id}"); conn.transform.SetParent(parent);
            var geometry = new GameObject("Geometry"); geometry.transform.SetParent(conn.transform);

            Vector2 top = Center(a.floor == 2 ? a : b);     // upper node
            Vector2 bottom = Center(a.floor == 1 ? a : b);   // lower node
            Material m = e.Kind == EdgeKind.ScaffoldDrop ? scaffoldMat : stairMat;
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
            return (dx >= 0 ? Edge.East : Edge.West, c.y);
        return (dz >= 0 ? Edge.North : Edge.South, c.x);
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

    static void AddSlab(Transform parent, Material mat, float cx, float cy, float cz, float sx, float sy, float sz, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(cx, cy, cz);
        go.transform.localScale = new Vector3(sx, sy, sz);
        go.GetComponent<Renderer>().sharedMaterial = mat;
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
        var sorted = new List<Gap>(gaps);
        sorted.Sort((a, b) => a.min.CompareTo(b.min));

        float cursor = start; int seg = 0;
        foreach (var raw in sorted)
        {
            var gap = new Gap(Mathf.Clamp(raw.min, start, end), Mathf.Clamp(raw.max, start, end));
            if (gap.min > cursor) AddWallSeg(parent, floorY, $"{name}_{++seg:00}", horizontal, x0, z0, cursor, gap.min);
            cursor = Mathf.Max(cursor, gap.max);
        }
        if (cursor < end) AddWallSeg(parent, floorY, $"{name}_{++seg:00}", horizontal, x0, z0, cursor, end);
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
