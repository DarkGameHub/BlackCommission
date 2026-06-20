using System.Collections.Generic;
using UnityEngine;
using BlackCommission.Level.Generation;
// UnityEngine also defines a GridLayout (2D tilemap); alias to the generation type to avoid CS0104.
using GridLayout = BlackCommission.Level.Generation.GridLayout;

namespace BlackCommission.Level
{
    /// <summary>
    /// Builds a complete procedural SITE for map 2 (ADR-0003, direction B), from one seed:
    ///   • an EDGE-BASED whitebox interior — floors/ceilings per cell + each wall built EXACTLY ONCE
    ///     (no holes at dead-ends, no double/z-fighting walls), with a deterministic FRONT DOOR carved from
    ///     the ENTRY anchor down to the south boundary;
    ///   • a seeded OUTDOOR natural approach OUTSIDE the building (whitebox ground + trees/rocks/bushes via
    ///     <see cref="OutdoorScatterGenerator"/>) with a marked van DROP-OFF and a clear path to the door.
    /// Same seed ⇒ identical site everywhere (host-deterministic — only the int seed need ever cross the wire).
    /// Engine geometry only (no networked objects). Called by both the editor menu (Build Full Map 2) and the
    /// runtime <see cref="MapSiteRuntime"/>. The generator core underneath is proven deterministic + solvable
    /// (GridMapReachabilityHarnessTests: 1000-seed byte-identical, 0 unreachable).
    /// </summary>
    public static class MapSiteBuilder
    {
        public const float Cell = 4f;     // grid unit (matches GridLayoutInstantiator.CellSize)
        public const float WallH = 3.2f;  // wall/ceiling height (matches ModularRoomBuilder.CeilH)
        public const float WallT = 0.3f;
        public const float FloorT = 0.2f;
        public const float DoorW = 2.0f;
        public const float DoorH = 2.25f;

        public struct Result
        {
            public Vector3 Dropoff;    // outdoor van drop-off (named DROPOFF_VanSpawn)
            public Vector3 FrontDoor;  // building entrance (outdoor meets indoor)
            public Vector3 Entry;      // ENTRY anchor (just inside)
            public Vector3 Deep;       // DEEP anchor (objective, deepest)
            public int Floors, Walls, Scatter, Width, Height;
        }

        // 8 hero waypoints winding across the big single floor: ENTRY (south, by the front door) → 6
        // intermediate rooms threaded around the grid → DEEP (far NE corner objective). The long, snaking
        // critical path + heavy branching is what stretches the level toward the 20-min target.
        static readonly GridMapGenerator.AnchorSpec[] Anchors =
        {
            new GridMapGenerator.AnchorSpec("ENTRY", new GridCoord(2, 2)),
            new GridMapGenerator.AnchorSpec("W1",    new GridCoord(9, 7)),
            new GridMapGenerator.AnchorSpec("W2",    new GridCoord(4, 15)),
            new GridMapGenerator.AnchorSpec("W3",    new GridCoord(15, 11)),
            new GridMapGenerator.AnchorSpec("W4",    new GridCoord(22, 5)),
            new GridMapGenerator.AnchorSpec("W5",    new GridCoord(12, 20)),
            new GridMapGenerator.AnchorSpec("W6",    new GridCoord(24, 16)),
            new GridMapGenerator.AnchorSpec("DEEP",  new GridCoord(25, 22)),
        };

        const int Branches = 40;    // seeded dead-ends → exploration spurs
        const int Loops = 24;       // cycles between corridors → multiple routes (LC-style interconnected maze)
        const int RoomRadius = 1;   // expand each waypoint into an open ~3×3–5×5 room (seeded size)

        static readonly Dictionary<Color, Material> MatCache = new Dictionary<Color, Material>();

        public static Result Build(Transform parent, int seed, int width = 28, int height = 24)
        {
            var res = new Result { Width = width, Height = height };
            GridLayout layout = GridMapGenerator.Generate(width, height, Anchors, seed, Branches, Loops, RoomRadius);

            // Carve a vertical entrance corridor ENTRY → south boundary so the building has a deterministic
            // FRONT DOOR (the south edge of the boundary cell) for the outdoor path to meet.
            GridCoord entry = Anchors[0].Cell;
            GridCoord frontCell = CarveEntrance(layout, entry);

            var indoor = Child(parent, "Indoor");
            BuildInterior(indoor, layout, frontCell, ref res);

            res.Entry = CellCenter(entry);
            res.Deep = CellCenter(Anchors[Anchors.Length - 1].Cell); // DEEP = last waypoint
            res.FrontDoor = new Vector3((frontCell.X + 0.5f) * Cell, 0f, frontCell.Y * Cell); // south edge centre

            var outdoor = Child(parent, "Outdoor");
            res.Dropoff = new Vector3(res.FrontDoor.x, 0f, res.FrontDoor.z - 80f); // van parks FAR (LC-style trek)
            BuildOutdoor(outdoor, seed, width, height, res.FrontDoor, res.Dropoff, ref res);

            return res;
        }

        // ── entrance ───────────────────────────────────────────────────────────────
        static GridCoord CarveEntrance(GridLayout layout, GridCoord entry)
        {
            for (int y = entry.Y; y > 0; y--)
            {
                var a = new GridCoord(entry.X, y);
                var b = new GridCoord(entry.X, y - 1);
                if (layout.Kind(a) == CellKind.Empty) layout.Set(a, CellKind.Corridor);
                if (layout.Kind(b) == CellKind.Empty) layout.Set(b, CellKind.Corridor);
                layout.Link(a, b);
            }
            return new GridCoord(entry.X, 0); // south-boundary cell; its south edge is the front door
        }

        // ── indoor (edge-based: each wall built once) ────────────────────────────────
        static void BuildInterior(Transform parent, GridLayout layout, GridCoord frontCell, ref Result res)
        {
            Material floor = Mat(new Color(0.30f, 0.30f, 0.33f));
            Material wall = Mat(new Color(0.58f, 0.58f, 0.62f));
            Material ceil = Mat(new Color(0.20f, 0.20f, 0.22f));

            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                {
                    var c = new GridCoord(x, y);
                    if (layout.Kind(c) == CellKind.Empty) continue;

                    Box(parent, floor, x * Cell + Cell * 0.5f, -FloorT * 0.5f, y * Cell + Cell * 0.5f,
                        Cell, FloorT, Cell, $"Floor_{x}_{y}");
                    Box(parent, ceil, x * Cell + Cell * 0.5f, WallH + FloorT * 0.5f, y * Cell + Cell * 0.5f,
                        Cell, FloorT, Cell, $"Ceil_{x}_{y}");
                    res.Floors++;

                    var nN = new GridCoord(x, y + 1);
                    var nS = new GridCoord(x, y - 1);
                    var nE = new GridCoord(x + 1, y);
                    var nW = new GridCoord(x - 1, y);

                    // Build N and E for every cell; build S and W only on the boundary (neighbour empty/out)
                    // so every interior edge is built EXACTLY ONCE. Door where the two cells are linked; the
                    // front-door cell's south boundary is forced open to the outdoors.
                    // Skip the wall entirely between two cells of the SAME room (open interior). The dedup
                    // rule already makes the S/W twin of an internal edge a no-op, so guarding N/E suffices.
                    if (!SameRoom(layout, c, nN)) EdgeWall(parent, wall, c, 'N', Linked(layout, c, nN), ref res);
                    if (!SameRoom(layout, c, nE)) EdgeWall(parent, wall, c, 'E', Linked(layout, c, nE), ref res);
                    if (!Solid(layout, nS))
                        EdgeWall(parent, wall, c, 'S', c.Equals(frontCell), ref res);
                    if (!Solid(layout, nW))
                        EdgeWall(parent, wall, c, 'W', false, ref res);
                }

            for (int i = 0; i < Anchors.Length; i++)
            {
                Color col = i == 0 ? new Color(0.20f, 0.70f, 0.90f)                  // ENTRY = teal
                          : i == Anchors.Length - 1 ? new Color(0.90f, 0.20f, 0.20f)  // DEEP  = red (objective)
                          : new Color(0.90f, 0.60f, 0.20f);                           // waypoint rooms = amber
                Marker(parent, CellCenter(Anchors[i].Cell), col, $"ANCHOR_{Anchors[i].Id}");
            }
        }

        static bool Solid(GridLayout layout, GridCoord c) =>
            layout.InBounds(c) && layout.Kind(c) != CellKind.Empty;

        static bool Linked(GridLayout layout, GridCoord a, GridCoord b) =>
            layout.InBounds(b) && layout.Linked(a, b);

        // Two cells of the same open room (non-null shared owner) → no wall between them. Corridors have
        // null owners, so corridor edges are unaffected.
        static bool SameRoom(GridLayout layout, GridCoord a, GridCoord b) =>
            layout.InBounds(b) && layout.Owner(a) != null && layout.Owner(a) == layout.Owner(b);

        // Build the wall on one edge of cell c. side: N=z+, S=z-, E=x+, W=x-. door → centred 2 m opening.
        static void EdgeWall(Transform parent, Material mat, GridCoord c, char side, bool door, ref Result res)
        {
            float x0 = c.X * Cell, x1 = (c.X + 1) * Cell, z0 = c.Y * Cell, z1 = (c.Y + 1) * Cell;
            char axis; float at, lo, hi;
            switch (side)
            {
                case 'N': axis = 'z'; at = z1; lo = x0; hi = x1; break;
                case 'S': axis = 'z'; at = z0; lo = x0; hi = x1; break;
                case 'E': axis = 'x'; at = x1; lo = z0; hi = z1; break;
                default:  axis = 'x'; at = x0; lo = z0; hi = z1; break; // 'W'
            }
            string id = $"{side}_{c.X}_{c.Y}";
            if (!door)
            {
                WallSeg(parent, mat, axis, at, lo, hi, 0f, WallH, "Wall_" + id);
                res.Walls++;
                return;
            }
            float dLo = lo + (hi - lo - DoorW) * 0.5f, dHi = dLo + DoorW;
            if (dLo > lo + 0.05f) { WallSeg(parent, mat, axis, at, lo, dLo, 0f, WallH, "WallL_" + id); res.Walls++; }
            if (dHi < hi - 0.05f) { WallSeg(parent, mat, axis, at, dHi, hi, 0f, WallH, "WallR_" + id); res.Walls++; }
            WallSeg(parent, mat, axis, at, dLo, dHi, DoorH, WallH, "WallHdr_" + id); res.Walls++; // header over door
        }

        static void WallSeg(Transform parent, Material mat, char axis, float at, float lo, float hi,
            float yLo, float yHi, string name)
        {
            float cx, cz, sw, sd;
            if (axis == 'z') { cx = (lo + hi) * 0.5f; cz = at; sw = hi - lo; sd = WallT; }
            else { cx = at; cz = (lo + hi) * 0.5f; sw = WallT; sd = hi - lo; }
            Box(parent, mat, cx, (yLo + yHi) * 0.5f, cz, sw, yHi - yLo, sd, name);
        }

        // ── outdoor (seeded scatter south of the building) ───────────────────────────
        static void BuildOutdoor(Transform parent, int seed, int w, int h, Vector3 frontDoor, Vector3 dropoff,
            ref Result res)
        {
            Material ground = Mat(new Color(0.10f, 0.16f, 0.10f)); // dark night grass
            Material padMat = Mat(new Color(0.80f, 0.66f, 0.14f)); // amber drop-off pad

            float minX = -20f, maxX = w * Cell + 20f;
            float minZ = dropoff.z - 20f, maxZ = 0f;
            // Ground top at y=0, coplanar with the indoor floor tops → seamless navmesh through the front door.
            Box(parent, ground, (minX + maxX) * 0.5f, -0.1f, (minZ + maxZ) * 0.5f,
                (maxX - minX), 0.2f, (maxZ - minZ), "Ground");

            // Drop-off pad + named van-spawn anchor + a tall marker. NO breadcrumb path: the trek to the door
            // is through dark dense forest, so the building must be FOUND (LC-style), not just followed to.
            Box(parent, padMat, dropoff.x, 0.03f, dropoff.z, 5f, 0.06f, 6f, "DROPOFF_Pad");
            var van = Child(parent, "DROPOFF_VanSpawn");
            van.position = dropoff;
            Marker(parent, dropoff, new Color(0.95f, 0.80f, 0.10f), "DROPOFF_Marker");

            // Dense seeded forest, clearing only the building footprint (+margin), a small porch at the door,
            // and the drop-off pad. Everything between is woods you weave through — connected but easy to lose
            // your bearings in. Canopies sit above head height, so only trunks are nav obstacles → the forest
            // stays navmesh-connected; finding the way is the player's problem, not the agent's.
            float bMinX = 0f, bMaxX = w * Cell, bMinZ = 0f, bMaxZ = h * Cell;
            System.Func<float, float, bool> blocked = (px, pz) =>
            {
                if (px >= bMinX - 2f && px <= bMaxX + 2f && pz >= bMinZ - 2f && pz <= bMaxZ + 2f) return true; // building
                if (Mathf.Abs(px - frontDoor.x) < DoorW + 1f && pz > -4f) return true;                          // porch at door
                if (Mathf.Abs(px - dropoff.x) < 5f && Mathf.Abs(pz - dropoff.z) < 6f) return true;               // drop-off clearing
                return false;
            };
            var items = OutdoorScatterGenerator.Generate(minX, minZ, maxX, 0f, seed, 3.4f, blocked, 0.62);
            Material trunk = Mat(new Color(0.22f, 0.15f, 0.09f));
            Material leaf = Mat(new Color(0.09f, 0.22f, 0.10f));
            Material rock = Mat(new Color(0.28f, 0.28f, 0.31f));
            foreach (var it in items)
            {
                if (it.Type == 0) Tree(parent, trunk, leaf, it);
                else if (it.Type == 1) Rock(parent, rock, it);
                else Bush(parent, leaf, it);
                res.Scatter++;
            }
        }

        static void Tree(Transform parent, Material trunk, Material leaf, ScatterItem it)
        {
            float hgt = 2.2f + 1.4f * it.Scale;
            var t = Prim(parent, PrimitiveType.Cylinder, trunk, $"Tree_{it.X:0}_{it.Z:0}");
            t.localPosition = new Vector3(it.X, hgt * 0.5f, it.Z);
            t.localScale = new Vector3(0.35f, hgt * 0.5f, 0.35f);
            var c = Prim(parent, PrimitiveType.Sphere, leaf, $"Canopy_{it.X:0}_{it.Z:0}");
            c.localPosition = new Vector3(it.X, hgt + 0.3f, it.Z);
            c.localScale = new Vector3(2.4f * it.Scale, 2.0f * it.Scale, 2.4f * it.Scale);
        }

        static void Rock(Transform parent, Material mat, ScatterItem it)
        {
            float s = 0.8f * it.Scale;
            var r = Prim(parent, PrimitiveType.Cube, mat, $"Rock_{it.X:0}_{it.Z:0}");
            r.localPosition = new Vector3(it.X, s * 0.35f, it.Z);
            r.localRotation = Quaternion.Euler(0f, it.Yaw, 0f);
            r.localScale = new Vector3(s * 1.4f, s * 0.7f, s * 1.1f);
        }

        static void Bush(Transform parent, Material mat, ScatterItem it)
        {
            float s = 0.9f * it.Scale;
            var b = Prim(parent, PrimitiveType.Sphere, mat, $"Bush_{it.X:0}_{it.Z:0}");
            b.localPosition = new Vector3(it.X, s * 0.3f, it.Z);
            b.localScale = new Vector3(s, s * 0.6f, s);
        }

        // ── helpers ─────────────────────────────────────────────────────────────────
        static Vector3 CellCenter(GridCoord c) => new Vector3((c.X + 0.5f) * Cell, 0f, (c.Y + 0.5f) * Cell);

        static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static void Box(Transform parent, Material mat, float cx, float cy, float cz,
            float sx, float sy, float sz, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(cx, cy, cz);
            go.transform.localScale = new Vector3(sx, sy, sz);
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static Transform Prim(Transform parent, PrimitiveType type, Material mat, string name)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go.transform;
        }

        static void Marker(Transform parent, Vector3 worldPos, Color color, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col); // markers must not block navigation
            go.transform.SetParent(parent, false);
            go.transform.position = worldPos + Vector3.up * 1.3f;
            go.transform.localScale = new Vector3(0.6f, 2.6f, 0.6f);
            go.GetComponent<Renderer>().sharedMaterial = Mat(color);
        }

        static Material Mat(Color c)
        {
            if (MatCache.TryGetValue(c, out var m) && m != null) return m;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            m = new Material(shader) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            MatCache[c] = m;
            return m;
        }
    }
}
