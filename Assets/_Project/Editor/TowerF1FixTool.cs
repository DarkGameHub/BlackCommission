using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Floor-1-only fixups for the abandoned-tower whitebox, agreed in
/// design/levels/abandoned-tower-floor1-fix-plan.md. TWO menu commands, both idempotent
/// (safe to re-run) and Undo-able:
///
///   F1 - Cleanup overlaps & slots : delete stale TOWER_SLOTS root, trim VAN floor off the
///        warehouse, shrink the stretched STAIRA1 floor back to 4x8, delete the duplicate
///        stair prop. Logs the remaining "real conflicts" (SECUR-on-corridor, oversized T6).
///
///   F1 - Install doors : on every F1 room SIDE THAT HAS WALLS, normalize the opening(s) to a
///        standard width (>=2.3m -> 2.4 wide; 1.0-2.3 -> 1.2 narrow; <1.0 sliver -> closed) by
///        rebuilding that side's wall segments, then place the matching DoorIndustrial prefab,
///        centered, oriented along the wall. Sides with NO walls (intentional openings like the
///        LOBBY west) are LEFT OPEN — the tool never walls them up.
///
/// AUTHORED WITHOUT UNITY (no editor on this machine) — RUN IN UNITY AND VERIFY. Door
/// orientation/position may need a nudge; re-run after fixing geometry. Operates only on rooms
/// whose Floor sits at y &lt; 2 (Floor 2 is currently lifted to y~28 and is intentionally ignored).
/// </summary>
public static class TowerF1FixTool
{
    const string RootName = "Tower_v3_Whitebox";
    const string WidePath = "Assets/TirgamesAssets/Factory/Prefabs/DoorIndustrial01_1.prefab";
    const string NarrowPath = "Assets/TirgamesAssets/Factory/Prefabs/DoorIndustrial01_2.prefab";

    const float WideOpening = 2.4f;     // door-hole width for the double door
    const float NarrowOpening = 1.2f;   // door-hole width for the single door
    const float WideNative = 2.36f;     // measured clear width of DoorIndustrial01_1
    const float NarrowNative = 1.16f;   // measured clear width of DoorIndustrial01_2
    const float NarrowHinge = 0.568f;   // _2 leaf centre offset from prefab root (to re-centre it)
    const float WideToNarrowCut = 2.3f; // openings >= this become wide, else narrow
    const float MinOpening = 1.0f;      // smaller gaps are slivers -> closed up
    const float WallThickness = 0.28f, WallHeight = 3.2f;
    const float F1MaxY = 2f, SideTol = 0.6f;

    // ───────────────────────── object lookup ─────────────────────────

    static Transform Root()
    {
        var go = GameObject.Find(RootName);
        if (go == null) Debug.LogError($"[F1Fix] No '{RootName}' root in the open scene.");
        return go ? go.transform : null;
    }

    static Transform Child(Transform t, string name) => t == null ? null : t.Find(name);

    static IEnumerable<Transform> F1Rooms(Transform root)
    {
        Transform rooms = Child(root, "Rooms");
        if (rooms == null) yield break;
        foreach (Transform room in rooms)
        {
            Transform floor = FirstFloor(room);
            if (floor != null && floor.position.y < F1MaxY) yield return room;
        }
    }

    static Transform FirstFloor(Transform room)
    {
        foreach (Transform c in room)
            if (c.name == "Floor" || c.name.StartsWith("Floor ")) return c;
        return null;
    }

    // world-space rect of a slab transform: (x0,z0,x1,z1)
    static Vector4 Rect(Transform t)
    {
        Vector3 p = t.position, s = t.lossyScale;
        return new Vector4(p.x - s.x / 2, p.z - s.z / 2, p.x + s.x / 2, p.z + s.z / 2);
    }

    // ───────────────────────── menu: cleanup ─────────────────────────

    [MenuItem("Tools/Black Commission/MVP/Tower/F1 - Cleanup overlaps & slots")]
    public static void Cleanup()
    {
        Transform root = Root();
        if (root == null) return;
        int changed = 0;

        // 1) delete the stale old slot root.
        var oldSlots = GameObject.Find("TOWER_SLOTS");
        if (oldSlots != null) { Undo.DestroyObjectImmediate(oldSlots); changed++; Debug.Log("[F1Fix] deleted old TOWER_SLOTS root."); }

        // 2) VAN floor: trim west edge to x=12 so it stops overlapping the warehouse.
        Transform van = Child(Child(root, "Rooms"), "VAN");
        Transform vanFloor = van ? FirstFloor(van) : null;
        if (vanFloor != null)
        {
            Vector4 r = Rect(vanFloor);
            if (r.x < 11.9f)
            {
                float xMax = r.z; // x1
                float newW = xMax - 12f, newCx = (12f + xMax) / 2f;
                Undo.RecordObject(vanFloor, "trim VAN floor");
                vanFloor.position = new Vector3(newCx, vanFloor.position.y, vanFloor.position.z);
                vanFloor.localScale = new Vector3(newW / vanFloor.lossyScale.x * vanFloor.localScale.x, vanFloor.localScale.y, vanFloor.localScale.z);
                changed++; Debug.Log($"[F1Fix] trimmed VAN floor west edge to x=12 (was x0={r.x:F1}).");
            }
        }

        // 3) STAIRA1 floor: shrink the stretched slab back to 4x8 centred on its walls (z=32).
        Transform stairA = Child(Child(root, "Rooms"), "STAIRA1");
        Transform saFloor = stairA ? FirstFloor(stairA) : null;
        if (saFloor != null)
        {
            Vector4 r = Rect(saFloor);
            float depth = r.w - r.y;
            if (depth > 8.5f)
            {
                Undo.RecordObject(saFloor, "fix STAIRA1 floor");
                saFloor.position = new Vector3(saFloor.position.x, saFloor.position.y, 32f);
                saFloor.localScale = new Vector3(saFloor.localScale.x, saFloor.localScale.y, 8f / saFloor.lossyScale.z * saFloor.localScale.z);
                changed++; Debug.Log($"[F1Fix] shrank STAIRA1 floor to 4x8 (was depth {depth:F1}).");
            }
        }

        // 4) duplicate stair prop at (4.79, ~0, 18): keep one.
        var dupes = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .Where(t => t.name.Contains("Stairs03") &&
                        Mathf.Abs(t.position.x - 4.79f) < 0.4f &&
                        Mathf.Abs(t.position.z - 18f) < 0.6f &&
                        t.position.y < F1MaxY)
            .ToList();
        for (int i = 1; i < dupes.Count; i++) { Undo.DestroyObjectImmediate(dupes[i].gameObject); changed++; Debug.Log("[F1Fix] deleted duplicate stair prop at (4.79,18)."); }

        // 5) report the conflicts the tool deliberately does NOT auto-fix.
        Debug.LogWarning("[F1Fix] MANUAL DECISION NEEDED: (a) SECUR room sits on the LOBBY->POWER corridor " +
            "(E-LPWR) at [8,10]-[12,14] — move SECUR or reroute the corridor. (b) Connector_T6 is a 6.9x15.6 " +
            "slab (collapse<->foreman), not a corridor — delete & redraw at 4m.");

        MarkDirty();
        Debug.Log($"[F1Fix] Cleanup done. {changed} change(s). Re-bake NavMesh if you rely on it.");
    }

    // ───────────────────────── menu: install doors ─────────────────────────

    [MenuItem("Tools/Black Commission/MVP/Tower/F1 - Install doors")]
    public static void InstallDoors()
    {
        Transform root = Root();
        if (root == null) return;
        var wide = AssetDatabase.LoadAssetAtPath<GameObject>(WidePath);
        var narrow = AssetDatabase.LoadAssetAtPath<GameObject>(NarrowPath);
        if (wide == null || narrow == null) { Debug.LogError("[F1Fix] door prefab(s) not found at the expected paths."); return; }

        int doors = 0, sides = 0;
        foreach (Transform room in F1Rooms(root).ToList())
        {
            // remove previously-installed doors for idempotency.
            foreach (Transform c in room.Cast<Transform>().ToList())
                if (c.name.StartsWith("Door_")) Undo.DestroyObjectImmediate(c.gameObject);

            Transform floor = FirstFloor(room);
            Vector4 fr = Rect(floor); // x0,z0,x1,z1
            // gather builder wall segments per side.
            var segs = room.Cast<Transform>().Where(c => c.name.StartsWith("Wall_")).ToList();

            foreach (var side in new[] { Side.N, Side.S, Side.E, Side.W })
            {
                var (lo, hi, konst, horiz) = SideSpan(side, fr);
                var onSide = segs.Where(s => OnSide(s, side, fr)).ToList();
                if (onSide.Count == 0) continue; // intentionally-open side: leave it, no walls, no door.
                sides++;

                // covered intervals along the side axis -> gaps.
                var cover = onSide.Select(s => AxisSpan(s, horiz)).ToList();
                var openings = Gaps(cover, lo, hi)
                    .Where(g => g.Item2 - g.Item1 >= MinOpening)
                    .Select(g => (center: (g.Item1 + g.Item2) / 2f,
                                  wideDoor: (g.Item2 - g.Item1) >= WideToNarrowCut))
                    .ToList();

                Material mat = onSide[0].GetComponent<Renderer>() ? onSide[0].GetComponent<Renderer>().sharedMaterial : null;
                foreach (var s in onSide) Undo.DestroyObjectImmediate(s.gameObject);

                // rebuild the side as solid wall minus a std opening at each opening centre.
                var cuts = openings.Select(o => (a: o.center - (o.wideDoor ? WideOpening : NarrowOpening) / 2f,
                                                 b: o.center + (o.wideDoor ? WideOpening : NarrowOpening) / 2f))
                                   .OrderBy(c => c.a).ToList();
                float cursor = lo; int n = 0;
                foreach (var cut in cuts)
                {
                    if (cut.a > cursor + 0.05f) BuildWallSeg(room, mat, side, konst, cursor, cut.a, horiz, ++n);
                    cursor = Mathf.Max(cursor, cut.b);
                }
                if (cursor < hi - 0.05f) BuildWallSeg(room, mat, side, konst, cursor, hi, horiz, ++n);

                // place a door in each opening.
                foreach (var o in openings)
                {
                    PlaceDoor(room, o.wideDoor ? wide : narrow, o.wideDoor, side, konst, o.center, horiz);
                    doors++;
                }
            }
        }
        MarkDirty();
        Debug.Log($"[F1Fix] Installed {doors} door(s) across {sides} walled side(s). VERIFY orientation/position in the scene; re-run after any wall edits.");
    }

    // ───────────────────────── geometry helpers ─────────────────────────

    enum Side { N, S, E, W }

    // side axis range [lo,hi], the constant coord, and whether the wall runs horizontally (along x).
    static (float lo, float hi, float konst, bool horiz) SideSpan(Side side, Vector4 fr) => side switch
    {
        Side.N => (fr.x, fr.z, fr.w, true),   // z = z1
        Side.S => (fr.x, fr.z, fr.y, true),   // z = z0
        Side.E => (fr.y, fr.w, fr.z, false),  // x = x1
        _ => (fr.y, fr.w, fr.x, false),       // W: x = x0
    };

    static bool OnSide(Transform seg, Side side, Vector4 fr)
    {
        Vector3 p = seg.position, s = seg.lossyScale;
        bool horiz = s.x > s.z;
        return side switch
        {
            Side.N => horiz && Mathf.Abs(p.z - fr.w) < SideTol,
            Side.S => horiz && Mathf.Abs(p.z - fr.y) < SideTol,
            Side.E => !horiz && Mathf.Abs(p.x - fr.z) < SideTol,
            _ => !horiz && Mathf.Abs(p.x - fr.x) < SideTol,
        };
    }

    static (float, float) AxisSpan(Transform seg, bool horiz)
    {
        Vector3 p = seg.position, s = seg.lossyScale;
        return horiz ? (p.x - s.x / 2, p.x + s.x / 2) : (p.z - s.z / 2, p.z + s.z / 2);
    }

    static List<(float, float)> Gaps(List<(float, float)> cover, float lo, float hi)
    {
        var sorted = cover.Select(c => (Mathf.Max(c.Item1, lo), Mathf.Min(c.Item2, hi)))
                          .Where(c => c.Item2 > c.Item1).OrderBy(c => c.Item1).ToList();
        var gaps = new List<(float, float)>();
        float cursor = lo;
        foreach (var c in sorted)
        {
            if (c.Item1 > cursor + 0.05f) gaps.Add((cursor, c.Item1));
            cursor = Mathf.Max(cursor, c.Item2);
        }
        if (cursor < hi - 0.05f) gaps.Add((cursor, hi));
        return gaps;
    }

    static void BuildWallSeg(Transform room, Material mat, Side side, float konst, float a, float b, bool horiz, int idx)
    {
        if (b - a < 0.05f) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Wall_{side}_{idx:00}";
        Undo.RegisterCreatedObjectUndo(go, "build wall seg");
        go.transform.SetParent(room, true);
        float yC = WallHeight / 2f;
        if (horiz)
        {
            go.transform.position = new Vector3((a + b) / 2f, yC, konst);
            go.transform.localScale = new Vector3(b - a, WallHeight, WallThickness);
        }
        else
        {
            go.transform.position = new Vector3(konst, yC, (a + b) / 2f);
            go.transform.localScale = new Vector3(WallThickness, WallHeight, b - a);
        }
        if (mat) go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void PlaceDoor(Transform room, GameObject prefab, bool wideDoor, Side side, float konst, float center, bool horiz)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(inst, "place door");
        inst.name = $"Door_{side}_{center:F0}";
        inst.transform.SetParent(room, true);

        float rotY = horiz ? 0f : 90f;
        Quaternion rot = Quaternion.Euler(0f, rotY, 0f);
        float scaleX = (wideDoor ? WideOpening : NarrowOpening) / (wideDoor ? WideNative : NarrowNative);

        Vector3 pos = horiz ? new Vector3(center, 0f, konst) : new Vector3(konst, 0f, center);
        // single (narrow) door's leaf is offset from the prefab root — re-centre it in the opening.
        if (!wideDoor) pos += rot * new Vector3(-NarrowHinge * scaleX, 0f, 0f);

        inst.transform.SetPositionAndRotation(pos, rot);
        inst.transform.localScale = new Vector3(scaleX, 1f, 1f);
    }

    static void MarkDirty()
    {
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
