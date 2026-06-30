using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using BlackCommission.Level;

/// <summary>
/// Idempotent builder for the tower's first-series room content pool
/// (spec: design/levels/tower-room-pool-v1.md). Dressing direction (PM, 2026-06-18):
/// the whole map is "未完工毛坯 + 工业" — rooms keep their identities but are dressed from the
/// real Asset Store packs (TirgamesAssets/Factory + Sat Productions/Concrete Props Pack), with
/// residential furniture de-emphasised. A few whitebox furniture pieces (desk / notice board /
/// CRT / shelving) are kept where they carry meaning (the sales desks, the "已售" board).
///
/// For each authored room it assembles a content prefab, drops <see cref="LootAnchor"/> markers and
/// carving <see cref="NavMeshObstacle"/>s on blockers, then creates one <see cref="RoomDef"/> per room
/// and a single <see cref="TowerRoomCatalog"/> listing them all.
///
/// Menu: Tools > Black Commission > MVP > Modules > Build Tower Room Pool v1
/// Re-runnable: wipes the output folders first. Swapping art later is a prefab-ref change only.
/// NOTE: the pack materials may be Built-in pipeline — if they render magenta under URP, run
/// Edit > Rendering > Materials > Convert All Built-in Materials to URP once.
/// </summary>
public static class TowerRoomPoolBuilder
{
    const string FurnDir   = "Assets/_Project/Art/Maps/Shared/Furniture";
    const string FAC       = "Assets/TirgamesAssets/Factory/Prefabs";
    const string SAT       = "Assets/Sat Productions/B-01/Concrete Props Pack/Prefabs";
    const string SharedDir = "Assets/_Project/Art/Maps/Shared/Rooms";
    const string TowerDir  = "Assets/_Project/Art/Maps/Tower_EarthCoast_01/Rooms";
    const string CatalogPath = "Assets/Resources/Config/TowerRoomCatalog_v1.asset"; // Resources so the generator can fallback-load it

    static string Fac(string n) => $"{FAC}/{n}.prefab";
    static string Sat(string n) => $"{SAT}/{n}.prefab";

    static Vector2 Footprint(RoomSizeClass s) => s switch
    {
        RoomSizeClass.Small  => new Vector2(4f, 4f),
        RoomSizeClass.Medium => new Vector2(8f, 8f),
        _                    => new Vector2(12f, 8f),
    };

    enum Wall { N, S, E, W, C }

    class Prop
    {
        public string asset;                // full asset path (pack prefab) OR null
        public string furniture;            // Shared/Furniture key OR null
        public Vector3 size = Vector3.one;   // primitive size (also obstacle box fallback)
        public Wall wall = Wall.C;
        public float along, lift;
        public bool blocker;
        public DressingSurface? loot;
        public string label = "prop";
    }

    // Real pack asset.
    static Prop A(string assetPath, Wall wall, float along, string label,
                  DressingSurface? loot = null, bool blocker = false, float lift = 0f)
        => new Prop { asset = assetPath, wall = wall, along = along, label = label,
                      loot = loot, blocker = blocker, lift = lift };

    // Whitebox furniture (Shared/Furniture/Furniture_<key>.prefab).
    static Prop F(string key, Wall wall, float along, string label,
                  DressingSurface? loot = null, bool blocker = false, float lift = 0f)
        => new Prop { furniture = key, wall = wall, along = along, label = label,
                      loot = loot, blocker = blocker, lift = lift };

    // Primitive cube stand-in.
    static Prop Box(Vector3 size, Wall wall, float along, string label,
                    DressingSurface? loot = null, bool blocker = false)
        => new Prop { size = size, wall = wall, along = along, label = label,
                      loot = loot, blocker = blocker };

    class RoomSpec
    {
        public string id, display;
        public RoomSizeClass size;
        public bool shared, floor1Only, floor2Only, allowDuplicates;
        public int weight = 1;
        public Prop[] props;
    }

    static RoomSpec[] Rooms() => new[]
    {
        // ── Small (4x4) ──
        new RoomSpec { id="Room_BreakerCloset", display="配电间 Breaker Closet", size=RoomSizeClass.Small,
            shared=true, weight=2, allowDuplicates=true, props=new[]{
                A(Fac("PowerBox01_1"), Wall.N, -0.7f, "配电箱A", DressingSurface.Cabinet),
                A(Fac("PowerBox02_1"), Wall.N, 0.7f, "配电箱B"),
                A(Fac("MetalCabinet01_1"), Wall.W, 0f, "金属柜", DressingSurface.ShelfSlot),
                A(Fac("Debris01_1"), Wall.S, 0.5f, "碎屑", DressingSurface.Floor),
                A(Fac("DecalX1Y1"), Wall.C, 0f, "油渍"),
            }},
        new RoomSpec { id="Room_SecurityBooth", display="保安岗亭 Security Booth", size=RoomSizeClass.Small,
            shared=true, weight=1, allowDuplicates=false, props=new[]{
                F("Desk", Wall.N, 0f, "值班桌", DressingSurface.DeskSurface),
                F("CRT", Wall.N, 0f, "监控CRT", lift:0.9f),
                A(Fac("MetalCabinet01_2"), Wall.E, 0f, "钥匙柜", DressingSurface.Cabinet),
                F("NoticeBoard", Wall.W, 0f, "登记板", DressingSurface.Wall),
                A(Fac("Debris01_2"), Wall.S, 0.6f, "碎屑"),
            }},
        new RoomSpec { id="Room_ShowFlatBath", display="样板卫浴 Show-Flat Bathroom (毛坯)", size=RoomSizeClass.Small,
            shared=false, weight=1, allowDuplicates=false, props=new[]{
                A(Sat("B_Concrete_01"), Wall.N, 0f, "混凝土浴缸槽", DressingSurface.Floor),
                A(Sat("Pipes_long_01"), Wall.W, 0f, "外露水管"),
                F("NoticeBoard", Wall.E, 0f, "SPA广告牌", DressingSurface.Wall),
                A(Fac("DecalX1Y1"), Wall.C, 0f, "积水渍"),
            }},
        new RoomSpec { id="Room_JanitorStore", display="杂物储藏 Janitor Store", size=RoomSizeClass.Small,
            shared=true, weight=2, allowDuplicates=true, props=new[]{
                F("Shelving", Wall.N, 0f, "货架", DressingSurface.ShelfSlot),
                A(Fac("Barrel01a"), Wall.S, -0.6f, "油桶A", DressingSurface.CrateTop),
                A(Fac("Barrel01b"), Wall.S, 0.6f, "油桶B"),
                A(Fac("FireExtinguisher01_1"), Wall.E, 0f, "灭火器"),
                A(Fac("Debris01_3"), Wall.W, 0.5f, "碎屑", DressingSurface.Floor),
            }},

        // ── Medium (8x8) ──
        new RoomSpec { id="Room_WorkerBunkroom", display="工人宿舍 Worker Bunkroom (占屋)", size=RoomSizeClass.Medium,
            shared=true, weight=2, allowDuplicates=false, props=new[]{
                Box(new Vector3(2f,0.35f,0.9f), Wall.N, -1.5f, "地铺床垫", DressingSurface.Floor),
                A(Fac("MetalCabinet01_1"), Wall.W, 0f, "储物柜", DressingSurface.ShelfSlot),
                A(Sat("B_Concrete_02"), Wall.C, 1.5f, "当桌的混凝土块"),
                A(Fac("Debris01_4"), Wall.E, 1f, "碎屑堆", DressingSurface.Floor),
                A(Fac("Barrel01c"), Wall.S, 1.2f, "生火油桶"),
            }},
        new RoomSpec { id="Room_SiteCanteen", display="工地食堂 Site Canteen", size=RoomSizeClass.Medium,
            shared=true, weight=1, allowDuplicates=true, props=new[]{
                A(Sat("B_Concrete_03"), Wall.N, 0f, "打饭台(混凝土)", DressingSurface.DeskSurface, blocker:true),
                F("Desk", Wall.C, -1.5f, "长桌"),
                A(Fac("Barrel01d"), Wall.C, 0.5f, "当凳油桶"),
                F("NoticeBoard", Wall.W, 0f, "菜单板", DressingSurface.Wall),
                A(Fac("Debris01_5"), Wall.E, 1f, "碎屑", DressingSurface.Floor),
            }},
        new RoomSpec { id="Room_SalesBullpen", display="销售办公区 Sales Bullpen (谎言)", size=RoomSizeClass.Medium,
            shared=false, weight=2, allowDuplicates=false, props=new[]{
                F("Desk", Wall.C, -1f, "销售工位A", DressingSurface.DeskSurface, blocker:true),
                F("Desk", Wall.C, 1f, "销售工位B", DressingSurface.DeskSurface, blocker:true),
                F("CRT", Wall.C, -1f, "死终端", lift:0.9f),
                F("NoticeBoard", Wall.S, 0f, "「已售」户型墙", DressingSurface.Wall),
                A(Fac("Debris01_6"), Wall.N, 1f, "碎屑", DressingSurface.Floor),
            }},
        new RoomSpec { id="Room_ConstructionStores", display="施工材料间 Construction Stores", size=RoomSizeClass.Medium,
            shared=false, weight=1, allowDuplicates=true, props=new[]{
                A(Sat("B_Concrete_04"), Wall.C, 0f, "钢筋/料堆", DressingSurface.Floor, blocker:true),
                A(Sat("Blocks_01"), Wall.N, -1.2f, "砖块堆A", DressingSurface.CrateTop),
                A(Sat("Blocks_02"), Wall.N, 1.2f, "砖块堆B"),
                A(Sat("Barrier"), Wall.W, 0f, "隔离墩"),
                A(Sat("Pipes_Big_01"), Wall.E, 0f, "粗管道"),
                A(Fac("Barrel01a"), Wall.S, 1.5f, "油桶"),
            }},

        // ── Large (12x8) ──
        new RoomSpec { id="Room_ShowFlatFloor", display="毛坯样板层 Unfinished Show-Flat Floor", size=RoomSizeClass.Large,
            shared=false, weight=2, allowDuplicates=false, props=new[]{
                F("NoticeBoard", Wall.N, 0f, "「您的真实海岸」横幅", DressingSurface.Wall),
                A(Sat("B_Concrete_05"), Wall.C, -3.5f, "毛坯隔断墩"),
                A(Sat("B_Concrete_06"), Wall.C, 0f, "混凝土沙发形", DressingSurface.Floor, blocker:true),
                F("Desk", Wall.W, 0f, "样板控制台", DressingSurface.DeskSurface),
                A(Sat("A_Wall"), Wall.S, 3f, "树根撑裂的墙", DressingSurface.Floor),
                A(Fac("Debris01_8"), Wall.E, 1f, "碎屑"),
                A(Fac("DecalX3Y1"), Wall.C, 3f, "渗水渍"),
            }},
        new RoomSpec { id="Room_LoadingBay", display="地下卸货区 Loading Bay", size=RoomSizeClass.Large,
            shared=true, floor1Only=true, weight=1, allowDuplicates=false, props=new[]{
                A(Fac("LargeGates1_1"), Wall.N, 0f, "半开卷帘门"),
                A(Sat("B_Concrete_07"), Wall.C, -2.5f, "托盘料堆A", DressingSurface.CrateTop, blocker:true),
                A(Sat("Blocks_03"), Wall.C, 2.5f, "托盘料堆B", DressingSurface.CrateTop, blocker:true),
                A(Fac("Barrel01b"), Wall.W, 1.5f, "油桶", DressingSurface.Floor),
                A(Sat("Pipes_Big_01"), Wall.E, 0f, "粗管道"),
                A(Fac("Debris01_10"), Wall.S, 2f, "碎屑"),
                A(Fac("DecalX3Y1"), Wall.S, -2f, "积水"),
            }},
    };

    [MenuItem("Tools/Black Commission/MVP/Modules/Build Tower Room Pool v1")]
    public static void BuildAll()
    {
        EnsureCleanDir(SharedDir);
        EnsureCleanDir(TowerDir);

        var defs = new List<RoomDef>();
        int prefabCount = 0, anchorCount = 0, missing = 0;

        foreach (RoomSpec spec in Rooms())
        {
            string dir = spec.shared ? SharedDir : TowerDir;
            GameObject root = BuildRoomPrefab(spec, out int anchors, out int miss);
            string prefabPath = $"{dir}/{spec.id}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            prefabCount++; anchorCount += anchors; missing += miss;

            var def = ScriptableObject.CreateInstance<RoomDef>();
            def.roomName = spec.display;
            def.size = spec.size;
            def.contentPrefab = prefab;
            def.roleFilter = RoomSlotRole.Random;
            def.floor1Only = spec.floor1Only;
            def.floor2Only = spec.floor2Only;
            def.weight = spec.weight;
            def.allowDuplicates = spec.allowDuplicates;
            AssetDatabase.CreateAsset(def, $"{dir}/{spec.id}.asset");
            defs.Add(def);
        }

        var catalog = ScriptableObject.CreateInstance<TowerRoomCatalog>();
        catalog.rooms = defs;
        string catDir = System.IO.Path.GetDirectoryName(CatalogPath).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(catDir)) CreateFolders(catDir);
        AssetDatabase.DeleteAsset(CatalogPath);
        AssetDatabase.CreateAsset(catalog, CatalogPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[TowerRoomPoolBuilder] Built {prefabCount} room prefabs, {defs.Count} RoomDefs, " +
                  $"{anchorCount} loot anchors, 1 catalog → {CatalogPath}. " +
                  (missing > 0 ? $"⚠ {missing} prop(s) missing (box stand-ins used). " : "") +
                  "Assign the catalog to TowerLayoutGenerator.catalog in Tower_EarthCoast_01.");
    }

    // Preview-only: builds a scene where each room content prefab gets a floor + four low walls sized to
    // its footprint, laid in a 5-wide grid, so the PM can read each room's size + dressing. The walls/floor
    // are showcase scaffolding (NOT in the content prefabs — the tower shell provides them at integration).
    [MenuItem("Tools/Black Commission/MVP/Modules/Build Room Pool Showcase Scene")]
    public static void BuildShowcaseScene()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects,
            UnityEditor.SceneManagement.NewSceneMode.Single);

        int i = 0;
        foreach (RoomSpec spec in Rooms())
        {
            Vector2 fp = Footprint(spec.size);

            var rootGo = new GameObject($"Showcase_{spec.id}");
            rootGo.transform.position = new Vector3((i % 5) * 16f, 0f, (i / 5) * 16f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(rootGo.transform, false);
            floor.transform.localScale = new Vector3(fp.x, 0.1f, fp.y);
            floor.transform.localPosition = new Vector3(0f, -0.05f, 0f);

            // Height matched to the tower shell: WallHeight 3.2 m, DoorClearHeight 2.25 m
            // (TowerV8WhiteboxBuilder / ModularRoomBuilder). N / E / W solid; S wall has a centred doorway.
            const float H = 3.2f, doorClear = 2.25f;
            AddWall(rootGo, new Vector3(0f, H * 0.5f, fp.y * 0.5f),  new Vector3(fp.x, H, 0.1f));
            AddWall(rootGo, new Vector3(fp.x * 0.5f, H * 0.5f, 0f),  new Vector3(0.1f, H, fp.y));
            AddWall(rootGo, new Vector3(-fp.x * 0.5f, H * 0.5f, 0f), new Vector3(0.1f, H, fp.y));
            float gap = 1.4f, seg = (fp.x - gap) * 0.5f;
            if (seg > 0.05f)
            {
                AddWall(rootGo, new Vector3(-(gap + seg) * 0.5f, H * 0.5f, -fp.y * 0.5f), new Vector3(seg, H, 0.1f));
                AddWall(rootGo, new Vector3( (gap + seg) * 0.5f, H * 0.5f, -fp.y * 0.5f), new Vector3(seg, H, 0.1f));
                AddWall(rootGo, new Vector3(0f, (doorClear + H) * 0.5f, -fp.y * 0.5f), new Vector3(gap, H - doorClear, 0.1f)); // door header
            }
            // No furniture — PM hand-places it. Content prefabs remain under …/Rooms/ for reference.
            i++;
        }

        const string path = "Assets/_Project/Scenes/RoomPool_Showcase.unity";
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[RoomPool] Showcase scene built: {i} rooms (floor + walls per footprint) → {path}");
    }

    static void AddWall(GameObject parent, Vector3 localPos, Vector3 scale)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = "Wall";
        w.transform.SetParent(parent.transform, false);
        w.transform.localPosition = localPos;
        w.transform.localScale = scale;
    }

    static GameObject BuildRoomPrefab(RoomSpec spec, out int anchors, out int missing)
    {
        anchors = 0; missing = 0;
        var root = new GameObject(spec.id);   // at world (0,0,0)
        Vector2 fp = Footprint(spec.size);

        foreach (Prop p in spec.props)
        {
            GameObject go = Load(p, out _, out bool wasMissing);
            if (wasMissing) missing++;
            if (go == null) continue;
            go.name = p.label;
            go.transform.SetParent(root.transform, false);

            // Bounds-based placement (robust to arbitrary pivot/offset in imported prefabs):
            // centre the prop's rendered footprint on the target XZ and rest its BOTTOM on y=0.
            Bounds b = WorldBounds(go);
            Vector3 target = Place(fp, p, b.size);
            Vector3 cur = go.transform.position;
            go.transform.position = new Vector3(
                target.x - (b.center.x - cur.x),
                (cur.y - b.min.y) + p.lift,        // raise so the lowest point sits at the floor
                target.z - (b.center.z - cur.z));

            // Record the prop's wall + offset so RoomDoorClearance can slide it off a real door at
            // fill time. Centre props never reach a wall, so they carry no placement marker.
            if (p.wall != Wall.C)
            {
                var place = go.AddComponent<RoomPropPlacement>();
                place.wall = EdgeOf(p.wall);
                place.along = p.along;
            }

            if (p.blocker || p.loot.HasValue)
            {
                Bounds nb = WorldBounds(go);        // recompute after the move
                if (p.blocker)
                {
                    var obs = go.GetComponent<NavMeshObstacle>();
                    if (obs == null) obs = go.AddComponent<NavMeshObstacle>();
                    obs.shape = NavMeshObstacleShape.Box;
                    obs.carving = true;
                    obs.center = go.transform.InverseTransformPoint(nb.center);
                    obs.size = new Vector3(Mathf.Max(0.3f, nb.size.x), Mathf.Max(0.3f, nb.size.y), Mathf.Max(0.3f, nb.size.z));
                }
                if (p.loot.HasValue)
                {
                    // Parent the anchor UNDER the prop so a door-clearance nudge carries the loot with
                    // it (LootSpawner gathers anchors via FindObjectsByType, so nesting is fine).
                    var a = new GameObject($"LootAnchor_{p.label}");
                    a.transform.SetParent(go.transform, false);
                    a.transform.position = new Vector3(nb.center.x, nb.max.y + 0.05f, nb.center.z);
                    a.AddComponent<LootAnchor>().surface = p.loot.Value;
                    anchors++;
                }
            }
        }

        Bounds rb = WorldBounds(root);
        Debug.Log($"[RoomPool] {spec.id}: {root.transform.childCount} children, boundsSize=({rb.size.x:F1},{rb.size.y:F1},{rb.size.z:F1}), y∈[{rb.min.y:F2},{rb.max.y:F2}]");
        return root;
    }

    // Wall-hugging target XZ (Y resolved later by bounds). Inset clamps to 0 so oversized props just centre.
    static Vector3 Place(Vector2 fp, Prop p, Vector3 size)
    {
        float hx = Mathf.Max(0f, fp.x * 0.5f - 0.3f - size.x * 0.5f);
        float hz = Mathf.Max(0f, fp.y * 0.5f - 0.3f - size.z * 0.5f);
        return p.wall switch
        {
            Wall.N => new Vector3(p.along, 0f, hz),
            Wall.S => new Vector3(p.along, 0f, -hz),
            Wall.E => new Vector3(hx, 0f, p.along),
            Wall.W => new Vector3(-hx, 0f, p.along),
            _      => new Vector3(p.along, 0f, 0f),
        };
    }

    // Map a dressing wall to the plan-frame door edge (N/S/E/W). Centre = None.
    static DoorEdge EdgeOf(Wall w) => w switch
    {
        Wall.N => DoorEdge.N,
        Wall.S => DoorEdge.S,
        Wall.E => DoorEdge.E,
        Wall.W => DoorEdge.W,
        _      => DoorEdge.None,
    };

    // Combined world-space render bounds of a prop (all child renderers). Falls back to a small box.
    static Bounds WorldBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.3f);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    static GameObject Load(Prop p, out bool isPrimitive, out bool wasMissing)
    {
        isPrimitive = false; wasMissing = false;
        if (p.asset != null || p.furniture != null)
        {
            string path = p.asset ?? $"{FurnDir}/Furniture_{p.furniture}.prefab";
            var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (src != null) return (GameObject)PrefabUtility.InstantiatePrefab(src);
            Debug.LogWarning($"[TowerRoomPoolBuilder] Missing prefab '{path}' — box stand-in.");
            wasMissing = true;
        }
        isPrimitive = true;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bool genuinePrimitive = p.asset == null && p.furniture == null;
        go.transform.localScale = genuinePrimitive ? p.size : new Vector3(0.8f, 0.9f, 0.8f);
        return go;
    }

    static Vector3 ApproxBounds(GameObject go)
    {
        var r = go.GetComponentInChildren<Renderer>();
        return r != null ? r.bounds.size : new Vector3(0.8f, 0.9f, 0.8f);
    }

    static void EnsureCleanDir(string dir)
    {
        if (AssetDatabase.IsValidFolder(dir)) AssetDatabase.DeleteAsset(dir);
        CreateFolders(dir);
    }

    static void CreateFolders(string dir)
    {
        string[] parts = dir.Split('/');
        string acc = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{acc}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(acc, parts[i]);
            acc = next;
        }
    }
}
