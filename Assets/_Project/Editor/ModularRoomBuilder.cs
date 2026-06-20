using UnityEngine;
using UnityEditor;
using System.IO;
using BlackCommission.Level;

/// <summary>
/// Generates shared room module prefabs for the modular map system (Route B).
/// All modules use a 4m grid and the tower's whitebox material palette.
/// Run AFTER 'Rebuild v8 Whitebox' so tower materials exist on disk.
///
/// Menu: Tools > Black Commission > MVP > Modules > Build All Shared Modules
/// Output: Assets/_Project/Art/Maps/Shared/Modules/
///
/// Module naming: Corridor_NS, Room_Office, etc.
/// Connection openings encoded as compass letters: N/S/E/W.
/// Each module has Con_* child transforms marking where adjacent rooms attach.
/// </summary>
public static class ModularRoomBuilder
{
    const string OutDir  = "Assets/_Project/Art/Maps/Shared/Modules";
    const string MatBase = "Assets/_Project/Art/Maps/Tower_EarthCoast_01/Materials/Whitebox/";

    // Dimensions — match TowerV8WhiteboxBuilder constants
    const float CeilH  = 3.2f;   // wall/room height
    const float WallT  = 0.3f;   // wall thickness
    const float FloorT = 0.2f;   // floor/ceiling slab thickness
    const float DoorW  = 2.0f;   // door opening width
    const float DoorH  = 2.25f;  // door clear height
    const float G      = 4.0f;   // 4m grid unit

    static Material matWall, matFloor, matCorr;

    // ── Entry point ───────────────────────────────────────────────────────────

    [MenuItem("Tools/Black Commission/MVP/Modules/Build All Shared Modules")]
    public static void BuildAll()
    {
        if (!LoadMaterials()) return;
        EnsureOutputDir();

        // ── Corridors (4×4 footprint, corridor material) ──────────────────
        Save(Module("Corridor_NS",    G, G, "NS"));
        Save(Module("Corridor_EW",    G, G, "EW"));
        Save(Module("Corridor_NE",    G, G, "NE"));
        Save(Module("Corridor_SE",    G, G, "SE"));
        Save(Module("Corridor_NW",    G, G, "NW"));
        Save(Module("Corridor_SW",    G, G, "SW"));
        Save(Module("Junction_T_NSE", G, G, "NSE"));
        Save(Module("Junction_T_NSW", G, G, "NSW"));
        Save(Module("Junction_T_SEW", G, G, "SEW"));
        Save(Module("Junction_T_NEW", G, G, "NEW"));
        Save(Module("Junction_4Way",  G, G, "NSEW"));

        // ── Dead-end caps: 1 opening + 3 solid walls. Fixes the hole the instantiator left when a
        //    1-link cell was mapped to a 2-opening corridor (open side faced empty space). ──
        Save(Module("Corridor_N", G, G, "N"));
        Save(Module("Corridor_S", G, G, "S"));
        Save(Module("Corridor_E", G, G, "E"));
        Save(Module("Corridor_W", G, G, "W"));

        // ── Rooms (wall material, functional spaces) ──────────────────────
        Save(Module("Room_Utility_4x4",  G,     G,     "S",  isRoom: true));
        Save(Module("Room_Office_4x8",   G,     G*2f,  "SE", isRoom: true));
        Save(Module("Room_Large_8x8",    G*2f,  G*2f,  "SW", isRoom: true));

        AssetDatabase.Refresh();
        Debug.Log($"[ModularRoomBuilder] 18 modules built → {OutDir}/");
    }

    // ── Module builder ────────────────────────────────────────────────────────

    static GameObject Module(string id, float w, float d, string open, bool isRoom = false)
    {
        var root = new GameObject(id);
        var m    = isRoom ? matWall : matCorr;

        // Floor slab
        Box(root, matFloor, w * 0.5f, -FloorT * 0.5f, d * 0.5f, w, FloorT, d, "Floor");

        // Ceiling slab
        Box(root, m, w * 0.5f, CeilH + FloorT * 0.5f, d * 0.5f, w, FloorT, d, "Ceiling");

        // Four walls — solid or with centered door opening
        BuildWall(root, m, 'z', d, 0, w, open.Contains('N'), "Wall_N");
        BuildWall(root, m, 'z', 0, 0, w, open.Contains('S'), "Wall_S");
        BuildWall(root, m, 'x', w, 0, d, open.Contains('E'), "Wall_E");
        BuildWall(root, m, 'x', 0, 0, d, open.Contains('W'), "Wall_W");

        // Connection markers — used by the assembly system to snap rooms
        if (open.Contains('N')) Marker(root, new Vector3(w * 0.5f, 0f, d),     "Con_N");
        if (open.Contains('S')) Marker(root, new Vector3(w * 0.5f, 0f, 0f),    "Con_S");
        if (open.Contains('E')) Marker(root, new Vector3(w,        0f, d*0.5f),"Con_E");
        if (open.Contains('W')) Marker(root, new Vector3(0f,       0f, d*0.5f),"Con_W");

        // RoomSlot for the topology/gameplay system
        var slot  = root.AddComponent<RoomSlot>();
        slot.slotId = id;
        slot.role   = isRoom ? RoomSlotRole.Random : RoomSlotRole.Fixed;

        return root;
    }

    // ── Geometry helpers ──────────────────────────────────────────────────────

    /// axis='z' → wall perpendicular to Z (north/south), spans X.
    /// axis='x' → wall perpendicular to X (east/west),  spans Z.
    static void BuildWall(GameObject parent, Material mat, char axis,
        float at, float lo, float span, bool hasOpening, string name)
    {
        if (!hasOpening)
        {
            WallSeg(parent, mat, axis, at, lo, lo + span, 0f, CeilH, name);
            return;
        }

        float doorLo = lo + (span - DoorW) * 0.5f;
        float doorHi = doorLo + DoorW;

        if (doorLo > lo + 0.05f)
            WallSeg(parent, mat, axis, at, lo,     doorLo,     0f,   CeilH, name + "_L");

        if (doorHi < lo + span - 0.05f)
            WallSeg(parent, mat, axis, at, doorHi, lo + span,  0f,   CeilH, name + "_R");

        WallSeg(parent, mat, axis, at, doorLo, doorHi, DoorH, CeilH, name + "_Hdr");
    }

    static void WallSeg(GameObject parent, Material mat, char axis,
        float at, float lo, float hi, float yLo, float yHi, string name)
    {
        float cx, cz, sw, sd;
        if (axis == 'z') { cx = (lo + hi) * 0.5f; cz = at;             sw = hi - lo; sd = WallT; }
        else             { cx = at;                cz = (lo + hi) * 0.5f; sw = WallT; sd = hi - lo; }

        Box(parent, mat, cx, (yLo + yHi) * 0.5f, cz, sw, yHi - yLo, sd, name);
    }

    static void Box(GameObject parent, Material mat,
        float cx, float cy, float cz, float w, float h, float d, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(cx, cy, cz);
        go.transform.localScale    = new Vector3(w, h, d);
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void Marker(GameObject parent, Vector3 localPos, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
    }

    // ── Prefab save ───────────────────────────────────────────────────────────

    static void Save(GameObject go)
    {
        string path = $"{OutDir}/{go.name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    static bool LoadMaterials()
    {
        matWall  = Load("V8_Concrete_WallRaw.mat");
        matFloor = Load("V8_Concrete_Slab.mat");
        matCorr  = Load("V8_Concrete_Poured.mat");

        if (matWall == null || matFloor == null || matCorr == null)
        {
            Debug.LogError("[ModularRoomBuilder] Materials missing. " +
                "Run 'Tools > BC > MVP > Tower > Rebuild v8 Whitebox' first.");
            return false;
        }
        return true;
    }

    static Material Load(string filename) =>
        AssetDatabase.LoadAssetAtPath<Material>(MatBase + filename);

    static void EnsureOutputDir()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Maps/Shared"))
            AssetDatabase.CreateFolder("Assets/_Project/Art/Maps", "Shared");
        if (!AssetDatabase.IsValidFolder(OutDir))
            AssetDatabase.CreateFolder("Assets/_Project/Art/Maps/Shared", "Modules");
    }
}
