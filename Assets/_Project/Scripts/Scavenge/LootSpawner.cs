using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using BlackCommission.Level;
using BlackCommission.Scavenge;

/// <summary>
/// Host-authoritative loot spawner. On mission start (server) it finds every <see cref="LootAnchor"/>
/// in the loaded scene, asks <see cref="LootSpawnPlanner"/> which anchors get which item this run,
/// then spawns the matching whitebox prefab (by weight class) as a networked <see cref="ScavengeItem"/>.
/// Clients receive the items via NGO replication and never run the planner. Server-instantiate →
/// <c>NetworkObject.Spawn</c> follows the existing TowerLayout pattern.
///
/// Place this on a scene NetworkObject in any map that uses the dressed room modules (e.g. the
/// Module_Showcase scene, whose rooms carry LootAnchors).
/// </summary>
public class LootSpawner : NetworkBehaviour
{
    [Tooltip("Run seed; -1 = random each run. Determinism only matters for replay/testing — " +
             "clients receive items via replication regardless of the seed.")]
    [SerializeField] int seed = -1;

    readonly List<NetworkObject> spawned = new();

    public override void OnNetworkSpawn()
    {
        if (IsServer) SpawnLoot();
    }

    void SpawnLoot()
    {
        var config = Resources.Load<ScavengingConfig>("Config/ScavengingConfig");
        int min = config != null ? config.itemsPerMapInstanceMin : 10;
        int max = config != null ? config.itemsPerMapInstanceMax : 14;

        // Item pool from the authored definitions (Resources/Scavenge/Items).
        var defs = Resources.LoadAll<ScavengeItemDefinition>("Scavenge/Items");
        if (defs == null || defs.Length == 0)
        {
            Debug.LogWarning("[LootSpawner] No ScavengeItemDefinitions in Resources/Scavenge/Items — nothing to spawn.");
            return;
        }
        var byId = new Dictionary<string, ScavengeItemDefinition>();
        var pool = new List<ScavengeItemSpec>();
        foreach (var d in defs)
        {
            if (d == null || string.IsNullOrEmpty(d.id) || byId.ContainsKey(d.id)) continue;
            byId[d.id] = d;
            pool.Add(d.ToSpec());
        }

        // Anchors placed by RoomDresser. Sort by world position for a stable, seed-reproducible id.
        var anchors = new List<LootAnchor>(Object.FindObjectsByType<LootAnchor>(FindObjectsSortMode.None));
        if (anchors.Count == 0)
        {
            Debug.LogWarning("[LootSpawner] No LootAnchors in the scene — nothing to spawn.");
            return;
        }
        anchors.Sort(CompareByPosition);

        var slots = new List<LootAnchorSlot>(anchors.Count);
        for (int i = 0; i < anchors.Count; i++)
            slots.Add(new LootAnchorSlot(i, (LootSurface)(int)anchors[i].surface)); // DressingSurface mirrors LootSurface

        int useSeed = seed >= 0 ? seed : System.Environment.TickCount;
        List<LootPlacement> plan = LootSpawnPlanner.Plan(useSeed, slots, pool, min, max);

        foreach (var placement in plan)
        {
            if (placement.AnchorId < 0 || placement.AnchorId >= anchors.Count) continue;
            if (!byId.TryGetValue(placement.ItemId, out var def)) continue;

            GameObject prefab = PrefabFor(def.weight);
            if (prefab == null) continue;

            Transform anchor = anchors[placement.AnchorId].transform;
            GameObject go = Instantiate(prefab, anchor.position, anchor.rotation);

            var netObj = go.GetComponent<NetworkObject>();
            if (netObj == null) { Destroy(go); continue; }
            netObj.Spawn(true);

            if (go.TryGetComponent<ScavengeItem>(out var item))
                item.ConfigureServer(def.id, def.weight, def.baseValue, def.category);

            spawned.Add(netObj);
        }

        Debug.Log($"[LootSpawner] seed {useSeed}: spawned {spawned.Count} item(s) across {anchors.Count} anchor(s).");
    }

    static int CompareByPosition(LootAnchor a, LootAnchor b)
    {
        Vector3 pa = a.transform.position, pb = b.transform.position;
        int c = pa.x.CompareTo(pb.x); if (c != 0) return c;
        c = pa.z.CompareTo(pb.z);     if (c != 0) return c;
        return pa.y.CompareTo(pb.y);
    }

    static GameObject PrefabFor(WeightClass weight)
    {
        switch (weight)
        {
            case WeightClass.Heavy:  return Resources.Load<GameObject>("Loot/Loot_Heavy");
            case WeightClass.Medium: return Resources.Load<GameObject>("Loot/Loot_Medium");
            default:                 return Resources.Load<GameObject>("Loot/Loot_Light");
        }
    }
}
