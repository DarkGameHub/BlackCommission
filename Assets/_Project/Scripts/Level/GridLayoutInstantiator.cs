using UnityEngine;
using BlackCommission.Level.Generation;
// UnityEngine also defines a GridLayout (2D tilemap); alias to the generation type so the unqualified
// GridLayout below isn't CS0104-ambiguous.
using GridLayout = BlackCommission.Level.Generation.GridLayout;

namespace BlackCommission.Level
{
    /// <summary>
    /// Turns a pure <see cref="GridLayout"/> (ADR-0003) into Unity geometry. For each non-empty cell it
    /// picks the directional corridor/junction module whose open sides match the cell's links — the modules
    /// are pre-oriented by compass-letter name (Corridor_NS / Corridor_NE / Junction_T_NSE / Junction_4Way),
    /// authored on a 4 m grid with their pivot at the min corner (ModularRoomBuilder G=4), so placing a tile
    /// at (cx*4, 0, cy*4) with no rotation tiles seamlessly. Anchor cells also get a marker so Entry/Mid/Deep
    /// are visible. Engine-coupled (instantiates prefabs), so it lives in Assembly-CSharp — not the pure
    /// Generation asmdef. The prefab resolver is injected so this works from the editor (AssetDatabase) or
    /// at runtime (Resources).
    /// </summary>
    public static class GridLayoutInstantiator
    {
        public const float CellSize = 4f; // ModularRoomBuilder G

        static readonly GridCoord N = new GridCoord(0, 1), S = new GridCoord(0, -1),
                                  E = new GridCoord(1, 0), W = new GridCoord(-1, 0);

        /// <summary>
        /// Instantiate the layout under <paramref name="parent"/>. <paramref name="loadPrefab"/> resolves a
        /// module name (e.g. "Corridor_NS") to a prefab. Returns the number of module tiles placed.
        /// </summary>
        public static int Build(GridLayout layout, Transform parent, System.Func<string, GameObject> loadPrefab)
        {
            if (layout == null || parent == null || loadPrefab == null) return 0;

            int placed = 0;
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                {
                    var c = new GridCoord(x, y);
                    if (layout.Kind(c) == CellKind.Empty) continue;

                    string module = ModuleFor(layout, c);
                    if (module != null)
                    {
                        GameObject prefab = loadPrefab(module);
                        if (prefab != null)
                        {
                            var go = (GameObject)Object.Instantiate(prefab,
                                new Vector3(x * CellSize, 0f, y * CellSize), Quaternion.identity, parent);
                            go.name = $"{module}_{x}_{y}";
                            placed++;
                        }
                    }

                    if (layout.Kind(c) == CellKind.Anchor)
                        AddAnchorMarker(parent, x, y, layout.Owner(c));
                }
            return placed;
        }

        // Compass-letter module name from the cell's open links, in N,S,E,W order (the authored naming).
        static string ModuleFor(GridLayout layout, GridCoord c)
        {
            bool n = layout.Linked(c, c + N), s = layout.Linked(c, c + S),
                 e = layout.Linked(c, c + E), w = layout.Linked(c, c + W);
            int count = (n ? 1 : 0) + (s ? 1 : 0) + (e ? 1 : 0) + (w ? 1 : 0);
            string letters = (n ? "N" : "") + (s ? "S" : "") + (e ? "E" : "") + (w ? "W" : "");
            switch (count)
            {
                case 4:  return "Junction_4Way";
                case 3:  return "Junction_T_" + letters;
                case 2:  return "Corridor_" + letters;
                case 1:  return n ? "Corridor_N" : s ? "Corridor_S" : e ? "Corridor_E" : "Corridor_W"; // dead-end cap (1 opening, 3 solid walls)
                default: return null;                                      // isolated cell — nothing to place
            }
        }

        static void AddAnchorMarker(Transform parent, int x, int y, string id)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = $"ANCHOR_{id}";
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition =
                new Vector3(x * CellSize + CellSize * 0.5f, 2.6f, y * CellSize + CellSize * 0.5f);
        }
    }
}
