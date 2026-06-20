using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using BlackCommission.Level.Generation;
// UnityEngine also defines a GridLayout (2D tilemap); alias to the generation type to avoid CS0104.
using GridLayout = BlackCommission.Level.Generation.GridLayout;

namespace BlackCommission.Level
{
    /// <summary>
    /// ADR-0003 Phase-4 runtime driver for a FLAT procedurally-generated site (direction B: a brand-new flat
    /// map; the authored vertical tower <c>Tower_EarthCoast_01</c> is left untouched). The server rolls one
    /// seed, replicates it via a <see cref="NetworkVariable{T}"/>, and EVERY peer (host + clients) rebuilds
    /// the IDENTICAL layout locally from that seed (<see cref="GridMapGenerator"/> →
    /// <see cref="GridLayoutInstantiator"/>). Only the int seed crosses the wire; the geometry is generated,
    /// not networked — which is sound because the generator is proven deterministic + reachable
    /// (GridMapReachabilityHarnessTests: 1000-seed determinism, 0 unreachable).
    ///
    /// Mirrors <see cref="TowerLayoutGenerator"/>'s seed-sync exactly. Place on a NetworkObject in the flat
    /// site scene, set the grid size + hero anchors, and assign the Corridor_*/Junction_* module prefabs
    /// (matched by name); unassigned names fall back to <c>Resources/Maps/Modules/&lt;name&gt;</c>.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class GridMapNetworkBuilder : NetworkBehaviour
    {
        /// <summary>Inspector-friendly hero anchor: an id (ENTRY/MID/DEEP) at a fixed grid cell.</summary>
        [System.Serializable]
        public struct AnchorDef
        {
            public string id;
            public int cellX;
            public int cellY;
        }

        [Header("Grid")]
        [SerializeField] int width = 14;
        [SerializeField] int height = 10;
        [SerializeField, Min(0)] int branches = 4;

        [Header("Hero anchors (fixed grid zones — Entry / Mid / Deep)")]
        [SerializeField] AnchorDef[] anchors =
        {
            new AnchorDef { id = "ENTRY", cellX = 1,  cellY = 1 },
            new AnchorDef { id = "MID",   cellX = 7,  cellY = 5 },
            new AnchorDef { id = "DEEP",  cellX = 12, cellY = 8 },
        };

        [Header("Modules")]
        [Tooltip("Corridor_*/Junction_* prefabs, matched by GameObject name. Unassigned names fall back to " +
                 "Resources/Maps/Modules/<name>.")]
        [SerializeField] GameObject[] modulePrefabs;

        [Header("Seed")]
        [Tooltip("Non-zero = the server forces this seed (repeatable tests). Zero = a random seed per session.")]
        [SerializeField] int fixedSeedForTesting = 0;

        // Replicated seed: server writes, everyone reads. -1 means "not generated yet".
        readonly NetworkVariable<int> netSeed =
            new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone,
                                         NetworkVariableWritePermission.Server);

        readonly Dictionary<string, GameObject> moduleByName = new Dictionary<string, GameObject>();
        Transform builtRoot;
        bool generated;

        public override void OnNetworkSpawn()
        {
            netSeed.OnValueChanged += OnSeedChanged;

            if (IsServer)
            {
                int seed = fixedSeedForTesting != 0
                    ? fixedSeedForTesting
                    : new System.Random().Next(1, int.MaxValue);
                netSeed.Value = seed; // triggers OnSeedChanged on the server too
                BuildIfReady();        // server can build immediately
            }
            else
            {
                // Late joiner: the value may already be set before our callback registered.
                BuildIfReady();
            }
        }

        public override void OnNetworkDespawn()
        {
            netSeed.OnValueChanged -= OnSeedChanged;
        }

        void OnSeedChanged(int previous, int current) => BuildIfReady();

        void BuildIfReady()
        {
            if (generated || netSeed.Value < 0) return;

            var specs = new List<GridMapGenerator.AnchorSpec>(anchors.Length);
            foreach (var a in anchors)
                specs.Add(new GridMapGenerator.AnchorSpec(a.id, new GridCoord(a.cellX, a.cellY)));

            GridLayout layout = GridMapGenerator.Generate(width, height, specs, netSeed.Value, branches);

            builtRoot = new GameObject($"GridMap_{netSeed.Value}").transform;
            builtRoot.SetParent(transform, false);

            int placed = GridLayoutInstantiator.Build(layout, builtRoot, ResolveModule);
            Debug.Log($"[GridMapNetworkBuilder] seed {netSeed.Value}: built {placed} module tiles " +
                      $"(server={IsServer}).");

            generated = true;
        }

        // Resolve a module name (e.g. "Corridor_NS") to a prefab: serialized array first (lazily indexed
        // by name), then a Resources fallback so a scene that didn't wire the array still builds.
        GameObject ResolveModule(string moduleName)
        {
            if (moduleByName.Count == 0 && modulePrefabs != null)
                foreach (var p in modulePrefabs)
                    if (p != null && !moduleByName.ContainsKey(p.name)) moduleByName.Add(p.name, p);

            if (moduleByName.TryGetValue(moduleName, out var prefab) && prefab != null) return prefab;
            return Resources.Load<GameObject>($"Maps/Modules/{moduleName}");
        }
    }
}
