using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the four scavenge-mission network prefabs that <see cref="ScavengeMissionBootstrap"/>
/// spawns at runtime (Resources/Mission/*), and registers them in DefaultNetworkPrefabs.asset.
/// NGO forbids nested NetworkObjects and every piece [RequireComponent]s its own, so each piece
/// is its own prefab; sizes mirror TowerV8WhiteboxBuilder.BuildMissionManager. Idempotent —
/// re-running overwrites the prefabs in place and skips already-registered entries.
/// </summary>
public static class ScavengeMissionRigBuilder
{
    const string Folder = "Assets/Resources/Mission";
    const string NetworkPrefabsListPath = "Assets/DefaultNetworkPrefabs.asset";

    [MenuItem("Tools/Black Commission/Scavenge/Build Mission Rig Prefabs")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);

        GameObject manager = BuildPiece("ScavengeMissionManager", go =>
        {
            go.AddComponent<ScavengeMissionManager>();
        });

        GameObject cargo = BuildPiece("ScavengeCargoZone", go =>
        {
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(8f, 2.5f, 4f);
            go.AddComponent<ScavengeCargoZone>();
        });

        GameObject spawner = BuildPiece("ScavengeLootSpawner", go =>
        {
            go.AddComponent<LootSpawner>();
        });

        GameObject exit = BuildPiece("ScavengeVanExitPoint", go =>
        {
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2.5f, 2f, 2.5f);
            go.AddComponent<MissionVanExitPoint>();
        });

        int registered = RegisterNetworkPrefabs(manager, cargo, spawner, exit);
        AssetDatabase.SaveAssets();
        Debug.Log($"[ScavengeMissionRigBuilder] Built 4 prefabs under {Folder}; " +
                  $"{registered} newly registered in DefaultNetworkPrefabs.");
    }

    static GameObject BuildPiece(string name, System.Action<GameObject> configure)
    {
        var go = new GameObject(name);
        try
        {
            go.AddComponent<NetworkObject>();
            configure(go);
            string path = $"{Folder}/{name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Debug.Log($"[ScavengeMissionRigBuilder] Saved {path}");
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    /// <summary>Adds each prefab to the project's NetworkPrefabsList unless already present.</summary>
    static int RegisterNetworkPrefabs(params GameObject[] prefabs)
    {
        var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsListPath);
        if (list == null)
        {
            Debug.LogError($"[ScavengeMissionRigBuilder] {NetworkPrefabsListPath} not found — " +
                           "register the prefabs on the NetworkManager manually.");
            return 0;
        }

        int added = 0;
        foreach (GameObject prefab in prefabs)
        {
            if (prefab == null || list.Contains(prefab)) continue;
            list.Add(new NetworkPrefab { Prefab = prefab });
            added++;
        }
        if (added > 0) EditorUtility.SetDirty(list);
        return added;
    }
}
