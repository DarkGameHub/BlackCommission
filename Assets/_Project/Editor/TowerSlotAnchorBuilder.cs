using System.Collections.Generic;
using BlackCommission.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bolts the slot system onto the existing AbandonedBuilding blockout. The blockout
/// builder already authors the room *shells* (walls/doors/corridors/stairs); this
/// drops a <see cref="RoomSlot"/> anchor at the centre of each room so the runtime
/// generator can fill content into them. Size is inferred from the room name token
/// (S/M/L) and role from keywords (PowerRoom, DeepTarget, StartVanArea, Stair).
/// </summary>
public static class TowerSlotAnchorBuilder
{
    const string ScenePath = "Assets/Scene/AbandonedBuilding_Blockout.unity";
    const string BlockoutRoot = "AB_FloorPlan_Blockout";
    const string SlotsRoot = "TOWER_SLOTS";

    [MenuItem("Tools/Black Commission/MVP/Tower/1. Add Slot Anchors To Blockout")]
    public static void AddSlotAnchors()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject blockout = GameObject.Find(BlockoutRoot);
        if (blockout == null)
        {
            Debug.LogError($"[TowerSlots] Could not find '{BlockoutRoot}'. Run " +
                           "'Rebuild Abandoned Building Floor Plan' first.");
            return;
        }

        GameObject old = GameObject.Find(SlotsRoot);
        if (old != null) Object.DestroyImmediate(old);
        var slotsRoot = new GameObject(SlotsRoot);

        int count = 0;
        foreach (Transform floor in blockout.transform)
        {
            int floorIndex = floor.name.Contains("02") ? 2 : 1;
            foreach (Transform room in floor)
            {
                if (!IsRoom(room)) continue;
                Vector3 centre = RoomCentre(room);

                var anchor = new GameObject($"SLOT_{room.name}");
                anchor.transform.SetParent(slotsRoot.transform);
                anchor.transform.position = centre;

                var slot = anchor.AddComponent<RoomSlot>();
                slot.slotId = room.name;
                slot.floor = floorIndex;
                slot.size = SizeFromName(room.name);
                slot.role = RoleFromName(room.name);
                count++;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[TowerSlots] Added {count} RoomSlot anchors under '{SlotsRoot}'. " +
                  "Review sizes/roles in the inspector, then assign a TowerRoomCatalog.");
    }

    [MenuItem("Tools/Black Commission/MVP/Tower/2. Preview Random Fill (Editor)")]
    public static void PreviewFill()
    {
        var catalog = FindCatalog();
        if (catalog == null)
        {
            Debug.LogError("[TowerSlots] No TowerRoomCatalog asset found. Create one via " +
                           "Assets > Create > Black Commission > Tower Room Catalog and add RoomDefs.");
            return;
        }

        ClearFill();
        var slots = new List<RoomSlot>(Object.FindObjectsByType<RoomSlot>(FindObjectsSortMode.None));
        if (slots.Count == 0)
        {
            Debug.LogWarning("[TowerSlots] No RoomSlots in scene — run step 1 first.");
            return;
        }

        int seed = new System.Random().Next(1, int.MaxValue);
        var placed = TowerLayout.Fill(slots, catalog, seed, isServer: true);
        Debug.Log($"[TowerSlots] Preview filled {placed.Count} slots from seed {seed}. " +
                  "(Editor preview only — runtime uses the seed-synced generator.)");
    }

    [MenuItem("Tools/Black Commission/MVP/Tower/3. Clear Preview Fill")]
    public static void ClearFill()
    {
        foreach (var slot in Object.FindObjectsByType<RoomSlot>(FindObjectsSortMode.None))
        {
            if (slot.placedContent != null) Object.DestroyImmediate(slot.placedContent);
            // Belt-and-suspenders: clear any decor parented under the slot.
            for (int i = slot.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(slot.transform.GetChild(i).gameObject);
        }
    }

    // --- helpers ---

    static bool IsRoom(Transform room)
    {
        // Room shells are parents that contain a child named "Floor".
        // Corridors are bare floor cubes (no "Floor" child); skip them.
        if (!room.name.StartsWith("F1_") && !room.name.StartsWith("F2_")) return false;
        return room.Find("Floor") != null;
    }

    static Vector3 RoomCentre(Transform room)
    {
        Transform floor = room.Find("Floor");
        return floor != null ? floor.position : room.position;
    }

    static RoomSizeClass SizeFromName(string name)
    {
        // name like "F1_S3_PowerRoom" / "F2_M4_SalesOfficeRichLoot" / "F1_L1_CentralConstructionHall"
        if (name.Contains("CentralConstructionHall")) return RoomSizeClass.Large;
        string[] parts = name.Split('_');
        if (parts.Length >= 2 && parts[1].Length >= 1)
        {
            switch (parts[1][0])
            {
                case 'S': return RoomSizeClass.Small;
                case 'M': return RoomSizeClass.Medium;
                case 'L': return RoomSizeClass.Large;
            }
        }
        return RoomSizeClass.Medium;
    }

    static RoomSlotRole RoleFromName(string name)
    {
        if (name.Contains("PowerRoom")) return RoomSlotRole.PowerGate;
        if (name.Contains("DeepTarget")) return RoomSlotRole.Objective;
        if (name.Contains("StartVanArea")) return RoomSlotRole.Van;
        if (name.Contains("Stair")) return RoomSlotRole.Stair;
        return RoomSlotRole.Random;
    }

    static TowerRoomCatalog FindCatalog()
    {
        string[] guids = AssetDatabase.FindAssets("t:TowerRoomCatalog");
        if (guids.Length == 0) return null;
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<TowerRoomCatalog>(path);
    }
}
