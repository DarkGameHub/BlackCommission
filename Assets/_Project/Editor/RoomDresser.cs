using System.IO;
using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using BlackCommission.Level;

/// <summary>
/// Bakes data-driven furniture (from RoomDressingSet ScriptableObjects) into the shared
/// room module prefabs, following the same editor dress-pass convention as
/// HqOfficePropRestorer. Spec: design/quick-specs/room-dresser-set-dressing-2026-06-16.md.
///
/// Menu: Tools > Black Commission > MVP > Modules > Dress Room Modules
/// Run AFTER 'Build Whitebox Furniture' (needs the furniture prefabs) and after
/// 'Build All Shared Modules' (needs the module shells).
///
/// Idempotent: clears the prior "Dressing" group before rebuilding it.
/// On first run it creates default RoomDressingSet assets under Shared/Dressing/;
/// thereafter it uses (and respects edits to) those assets.
/// </summary>
public static class RoomDresser
{
    const string ModuleDir = "Assets/_Project/Art/Maps/Shared/Modules";
    const string DressDir  = "Assets/_Project/Art/Maps/Shared/Dressing";
    const string FurnDir   = "Assets/_Project/Art/Maps/Shared/Furniture";

    [MenuItem("Tools/Black Commission/MVP/Modules/Dress Room Modules")]
    public static void DressAll()
    {
        if (!AssetDatabase.IsValidFolder(FurnDir) ||
            Directory.GetFiles(FurnDir, "*.prefab").Length == 0)
        {
            Debug.LogError("[RoomDresser] No furniture prefabs found. " +
                "Run 'Tools > BC > MVP > Modules > Build Whitebox Furniture' first.");
            return;
        }

        EnsureDir();

        int dressed = 0;
        dressed += DressModule("Room_Office_4x8",  DefaultOfficeSet);
        dressed += DressModule("Room_Utility_4x4", DefaultUtilitySet);
        dressed += DressModule("Room_Large_8x8",   DefaultLargeSet);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RoomDresser] Dressed {dressed}/3 room modules → sets in {DressDir}/");
    }

    // ── Per-module dress ────────────────────────────────────────────────────────────

    static int DressModule(string module, System.Func<RoomDressingSet> makeDefault)
    {
        string modulePath = $"{ModuleDir}/{module}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(modulePath) == null)
        {
            Debug.LogWarning($"[RoomDresser] Module prefab missing: {modulePath} — skipped. " +
                "Run 'Build All Shared Modules' first.");
            return 0;
        }

        // Get-or-create the dressing set (created defaults respect later designer edits).
        string setPath = $"{DressDir}/Dressing_{module}.asset";
        var set = AssetDatabase.LoadAssetAtPath<RoomDressingSet>(setPath);
        if (set == null)
        {
            set = makeDefault();
            set.targetModule = module;
            AssetDatabase.CreateAsset(set, setPath);
            AssetDatabase.SaveAssets();
        }

        var root = PrefabUtility.LoadPrefabContents(modulePath);
        try
        {
            var old = root.transform.Find("Dressing");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var group = new GameObject("Dressing");
            group.transform.SetParent(root.transform, false);

            int idx = 0;
            foreach (var p in set.placements)
            {
                idx++;
                if (p.spawnProbability < 1f && Deterministic(module, idx) >= p.spawnProbability)
                    continue;
                if (p.prefab == null)
                {
                    Debug.LogWarning($"[RoomDresser] {module}: placement '{p.label}' has no prefab — skipped.");
                    continue;
                }

                var inst = (GameObject)Object.Instantiate(p.prefab, group.transform);
                inst.name = p.label;
                inst.transform.localPosition    = p.localPosition;
                inst.transform.localEulerAngles = p.localEuler;
                inst.transform.localScale       = p.localScale == Vector3.zero ? Vector3.one : p.localScale;

                if (p.centralBlocker)
                {
                    var ob = inst.AddComponent<NavMeshObstacle>();
                    ob.shape   = NavMeshObstacleShape.Box;
                    ob.carving = true;
                }

                if (p.lootAnchor && set.lootAnchorsEnabled)
                {
                    var anchor = new GameObject($"LootAnchor_{p.label}");
                    anchor.transform.SetParent(group.transform, false);
                    anchor.transform.localPosition = p.localPosition + Vector3.up * 0.02f;
                    anchor.AddComponent<LootAnchor>().surface = p.lootSurface;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, modulePath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return 1;
    }

    /// Stable per-(module,index) value in [0,1) so optional placements bake reproducibly.
    static float Deterministic(string module, int index)
    {
        unchecked
        {
            int h = 17;
            foreach (char c in module) h = h * 31 + c;
            h = h * 31 + index;
            return (float)new System.Random(h).NextDouble();
        }
    }

    // ── Default dressing sets (seed data; editable in the .asset afterwards) ─────────
    // Module local space: floor spans x:[0,w], z:[0,d], floor top at y≈0. Items hug
    // walls / perimeter and avoid door openings (centred on each open wall, 2m wide).

    static RoomDressingSet DefaultOfficeSet() // 4 (x) × 8 (z), doors on S + E
    {
        var s = New();
        Add (s, "Desk",        F("Furniture_Desk"),        new Vector3(2.0f, 0f,    7.2f),  new Vector3(0, 180, 0));
        Add (s, "Chair",       F("Furniture_Chair"),       new Vector3(2.0f, 0f,    6.4f),  Vector3.zero);
        Loot(s, "CRT",         F("Furniture_CRT"),         new Vector3(1.6f, 0.77f, 7.2f),  new Vector3(0, 180, 0), DressingSurface.DeskSurface);
        Loot(s, "Cabinet",     F("Furniture_Cabinet"),     new Vector3(0.4f, 0f,    6.0f),  new Vector3(0, 90, 0),  DressingSurface.Cabinet);
        Add (s, "Shelving",    F("Furniture_Shelving"),    new Vector3(0.3f, 0f,    3.0f),  new Vector3(0, 90, 0));
        Add (s, "NoticeBoard", F("Furniture_NoticeBoard"), new Vector3(2.0f, 1.5f,  7.85f), Vector3.zero);
        Loot(s, "Crate",       F("Furniture_Crate"),       new Vector3(0.5f, 0f,    7.4f),  Vector3.zero,           DressingSurface.CrateTop);
        return s;
    }

    static RoomDressingSet DefaultUtilitySet() // 4 × 4, door on S
    {
        var s = New();
        Loot(s, "Shelving",        F("Furniture_Shelving"),        new Vector3(2.0f, 0f,   3.7f), Vector3.zero,          DressingSurface.ShelfSlot);
        Add (s, "ElectricalPanel", F("Furniture_ElectricalPanel"), new Vector3(3.8f, 1.4f, 2.0f), new Vector3(0, -90, 0));
        Add (s, "Cabinet",         F("Furniture_Cabinet"),         new Vector3(0.4f, 0f,   3.5f), new Vector3(0, 90, 0));
        Loot(s, "Crate",           F("Furniture_Crate"),           new Vector3(0.5f, 0f,   0.6f), Vector3.zero,          DressingSurface.CrateTop);
        Add (s, "PipeRun",         F("Furniture_PipeRun"),         new Vector3(2.0f, 2.6f, 3.85f), Vector3.zero);
        return s;
    }

    static RoomDressingSet DefaultLargeSet() // 8 × 8, doors on S + W
    {
        var s = New();
        Loot(s, "Shelving_N1", F("Furniture_Shelving"), new Vector3(2.0f, 0f,   7.7f), Vector3.zero,         DressingSurface.ShelfSlot);
        Loot(s, "Shelving_N2", F("Furniture_Shelving"), new Vector3(6.0f, 0f,   7.7f), Vector3.zero,         DressingSurface.ShelfSlot);
        Loot(s, "Shelving_E1", F("Furniture_Shelving"), new Vector3(7.7f, 0f,   2.0f), new Vector3(0, 90, 0), DressingSurface.ShelfSlot);
        Loot(s, "Shelving_E2", F("Furniture_Shelving"), new Vector3(7.7f, 0f,   6.0f), new Vector3(0, 90, 0), DressingSurface.ShelfSlot);
        var crate = F("Furniture_Crate");
        Loot(s, "Crate_C1", crate, new Vector3(5.0f, 0f,   5.5f), Vector3.zero, DressingSurface.CrateTop);
        Add (s, "Crate_C2", crate, new Vector3(5.6f, 0f,   5.5f), Vector3.zero);
        Add (s, "Crate_C3", crate, new Vector3(5.0f, 0.6f, 5.5f), Vector3.zero);
        Add (s, "PipeRun_N", F("Furniture_PipeRun"), new Vector3(4.0f, 2.8f, 7.85f), Vector3.zero);
        return s;
    }

    // ── Set-building helpers ────────────────────────────────────────────────────────

    static RoomDressingSet New()
    {
        var s = ScriptableObject.CreateInstance<RoomDressingSet>();
        s.lootAnchorsEnabled = true;
        return s;
    }

    static GameObject F(string name) =>
        AssetDatabase.LoadAssetAtPath<GameObject>($"{FurnDir}/{name}.prefab");

    static void Add(RoomDressingSet s, string label, GameObject prefab, Vector3 pos, Vector3 euler)
    {
        s.placements.Add(new DressingPlacement
        {
            label = label, prefab = prefab,
            localPosition = pos, localEuler = euler, localScale = Vector3.one,
            spawnProbability = 1f
        });
    }

    static void Loot(RoomDressingSet s, string label, GameObject prefab, Vector3 pos, Vector3 euler, DressingSurface surface)
    {
        s.placements.Add(new DressingPlacement
        {
            label = label, prefab = prefab,
            localPosition = pos, localEuler = euler, localScale = Vector3.one,
            spawnProbability = 1f,
            lootAnchor = true, lootSurface = surface
        });
    }

    static void EnsureDir()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Maps/Shared"))
            AssetDatabase.CreateFolder("Assets/_Project/Art/Maps", "Shared");
        if (!AssetDatabase.IsValidFolder(DressDir))
            AssetDatabase.CreateFolder("Assets/_Project/Art/Maps/Shared", "Dressing");
    }
}
