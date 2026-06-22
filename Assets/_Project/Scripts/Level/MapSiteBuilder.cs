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

        // Map 2 is a BURIED club (PM 2026-06-21): the whole interior is sunk this many metres below the meadow
        // surface and reached by two enclosed ramps (front descent + back ascent / loot haul-up). 5 m clears the
        // 3.4 m interior height with a ~1.6 m earth cap; a 7 m ramp run keeps the slope ≈36° (navmesh-walkable).
        public const float Depth = 5f;
        const float RampRun = 7f;     // horizontal length (z) of each surface↔underground entrance ramp
        const float TubeW = 3.2f;     // entrance ramp corridor clear width

        public struct Result
        {
            public Vector3 Dropoff;    // outdoor van drop-off (named DROPOFF_VanSpawn)
            public Vector3 FrontDoor;  // building entrance (outdoor meets indoor)
            public Vector3 FireExit;   // back-ascent emergence MOUTH on the meadow (deep-side egress up to surface)
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
        const int RoomRadius = 1;   // >0 = expand waypoints into rooms (footprints LOCKED to S/M/L; see ExpandRooms)

        static readonly Dictionary<Color, Material> MatCache = new Dictionary<Color, Material>();

        public static Result Build(Transform parent, int seed, int width = 28, int height = 24)
        {
            var res = new Result { Width = width, Height = height };
            GridLayout layout = GridMapGenerator.Generate(width, height, Anchors, seed, Branches, Loops, RoomRadius);

            // Carve a vertical entrance corridor ENTRY → south boundary (FRONT DOOR) and a second corridor
            // DEEP → north boundary (FIRE EXIT) so a deep run can escape out the back instead of retracing the
            // whole maze (PM 2026-06-20). Both are deterministic boundary cells the shell + outdoor meet.
            GridCoord entry = Anchors[0].Cell;
            GridCoord deep = Anchors[Anchors.Length - 1].Cell;
            GridCoord frontCell = CarveEntrance(layout, entry);
            GridCoord backCell = CarveBackExit(layout, deep);

            var indoor = Child(parent, "Indoor");
            BuildInterior(indoor, layout, frontCell, backCell, ref res);
            indoor.localPosition = new Vector3(0f, -Depth, 0f); // SINK the whole club one storey underground

            // Depth-graded overgrowth + 3-4 lavish "hero" rooms + interior lighting (the surface sun never reaches
            // down here). Parented under the sunk Indoor so it all descends with the maze.
            DressInterior(indoor, layout, entry, deep);

            res.Entry = CellCenter(entry); res.Entry.y = -Depth; // anchors now live one storey down
            res.Deep = CellCenter(deep);   res.Deep.y = -Depth;  // DEEP = last waypoint (objective)
            res.FrontDoor = new Vector3((frontCell.X + 0.5f) * Cell, 0f, frontCell.Y * Cell);    // SURFACE point above the buried door
            res.FireExit = new Vector3((backCell.X + 0.5f) * Cell, 0f, (backCell.Y + 1) * Cell + RampRun); // back-ascent MOUTH on the meadow (deep-side egress UP; buried door = mouth − RampRun)

            var outdoor = Child(parent, "Outdoor");
            res.Dropoff = new Vector3(res.FrontDoor.x, 0f, res.FrontDoor.z - 80f); // van parks FAR (LC-style trek)
            BuildOutdoor(outdoor, seed, width, height, res.FrontDoor, res.FireExit, res.Dropoff, ref res);

            // Underground ⇒ no above-grade concrete box. Two enclosed ramps connect the meadow surface to the
            // buried front door (descent) and fire exit (ascent / loot haul-up); matching holes are punched in the
            // terrain so you can actually go down/up. Built AFTER BuildOutdoor so GroundY's params are initialised.
            var entrances = Child(parent, "Entrances");
            BuildEntrances(entrances, res.FrontDoor, res.FireExit);
            PunchEntranceHoles(outdoor, res.FrontDoor, res.FireExit);

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

        // Carve DEEP → north boundary so the building has a deterministic FIRE EXIT (the north edge of the
        // boundary cell), mirroring CarveEntrance — a deep run bails out the back instead of retracing the maze.
        static GridCoord CarveBackExit(GridLayout layout, GridCoord deep)
        {
            int top = layout.Height - 1;
            for (int y = deep.Y; y < top; y++)
            {
                var a = new GridCoord(deep.X, y);
                var b = new GridCoord(deep.X, y + 1);
                if (layout.Kind(a) == CellKind.Empty) layout.Set(a, CellKind.Corridor);
                if (layout.Kind(b) == CellKind.Empty) layout.Set(b, CellKind.Corridor);
                layout.Link(a, b);
            }
            return new GridCoord(deep.X, top); // north-boundary cell; its north edge is the fire exit
        }

        // ── indoor (edge-based: each wall built once) ────────────────────────────────
        static void BuildInterior(Transform parent, GridLayout layout, GridCoord frontCell, GridCoord backCell, ref Result res)
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
                    // front-door cell's south boundary AND the fire-exit cell's north boundary are forced open.
                    // Skip the wall entirely between two cells of the SAME room (open interior). The dedup
                    // rule already makes the S/W twin of an internal edge a no-op, so guarding N/E suffices.
                    if (!SameRoom(layout, c, nN)) EdgeWall(parent, wall, c, 'N', Linked(layout, c, nN) || c.Equals(backCell), ref res);
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
                Marker(parent, CellCenter(Anchors[i].Cell), col, $"ANCHOR_{Anchors[i].Id}", local: true);
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

        // ── outdoor terrain (gentle Perlin undulation, flattened under the building + drop-off) ──
        static int _gSeed;
        static float _gBW, _gBD, _gDropX, _gDropZ;
        const float GroundAmp = 2.9f; // undulation amplitude (m) — tunable (PM 2026-06-21: bumpier meadow)

        // Deterministic terrain height at (x,z): gentle Perlin undulation smoothly flattened to 0 near the
        // building footprint and the drop-off (so the flat building/floors/pad sit right and slopes stay walkable).
        static float GroundY(float x, float z)
        {
            float dxB = Mathf.Max(0f, Mathf.Max(-2f - x, x - (_gBW + 2f)));
            float dzB = Mathf.Max(0f, Mathf.Max(-2f - z, z - (_gBD + 2f)));
            float fB = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 18f, Mathf.Sqrt(dxB * dxB + dzB * dzB)));
            float dD = Mathf.Sqrt((x - _gDropX) * (x - _gDropX) + (z - _gDropZ) * (z - _gDropZ));
            float fD = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, 16f, dD));
            float f = Mathf.Min(fB, fD);
            float n = Mathf.PerlinNoise((x + _gSeed * 0.13f) * 0.045f, (z - _gSeed * 0.07f) * 0.045f)
                    + Mathf.PerlinNoise(x * 0.11f + 11.3f, z * 0.11f + 7.1f) * 0.50f
                    + Mathf.PerlinNoise(x * 0.20f + 4.7f, z * 0.20f + 19.2f) * 0.16f; // fine octave → "凹凸有致" texture
            return (n - 0.83f) * GroundAmp * f; // offset = mean(n) so the meadow averages level with the footprint
        }

        // Grid mesh over the outdoor rect with per-vertex GroundY → undulating ground (MeshCollider for navmesh +
        // physics; 0-1 UVs so the tiled grass material repeats as it did on the old flat box).
        static void BuildGroundMesh(Transform parent, float minX, float minZ, float maxX, float maxZ, Material mat)
        {
            int nx = Mathf.Max(2, Mathf.RoundToInt((maxX - minX) / 4f));
            int nz = Mathf.Max(2, Mathf.RoundToInt((maxZ - minZ) / 4f));
            var verts = new Vector3[(nx + 1) * (nz + 1)];
            var uvs = new Vector2[verts.Length];
            for (int iz = 0; iz <= nz; iz++)
                for (int ix = 0; ix <= nx; ix++)
                {
                    float x = Mathf.Lerp(minX, maxX, ix / (float)nx);
                    float z = Mathf.Lerp(minZ, maxZ, iz / (float)nz);
                    int vi = iz * (nx + 1) + ix;
                    verts[vi] = new Vector3(x, GroundY(x, z), z);
                    uvs[vi] = new Vector2(ix / (float)nx, iz / (float)nz);
                }
            var tris = new int[nx * nz * 6];
            int t = 0;
            for (int iz = 0; iz < nz; iz++)
                for (int ix = 0; ix < nx; ix++)
                {
                    int v0 = iz * (nx + 1) + ix, v1 = v0 + 1, v2 = v0 + (nx + 1), v3 = v2 + 1;
                    tris[t++] = v0; tris[t++] = v2; tris[t++] = v1;
                    tris[t++] = v1; tris[t++] = v2; tris[t++] = v3;
                }
            var mesh = new Mesh { name = "GroundMesh" };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject("Ground");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        // ── Unity Terrain ground (LC-style: heightmap undulation + GPU-instanced detail grass) ──
        // Built instead of the mesh when a FoliageSet supplies a grass TerrainLayer (PM 2026-06-21 pivot). The
        // terrain sits at world y = TBASE and spans HRANGE vertically, so a heightmap sample h maps to
        // worldY = TBASE + h·HRANGE; choosing h = (GroundY − TBASE)/HRANGE makes the terrain SURFACE equal GroundY
        // everywhere — the same height field the props use — so trees/rocks/dressing sit exactly on it and the
        // flattened building/drop-off footprints stay level. The TerrainCollider that CreateTerrainGameObject adds
        // is what the navmesh bakes over (NavMeshBuilder collects it as a Terrain source) → DROP-OFF→ENTRY walkable.
        const float TBASE = -4f;   // terrain base world Y — below the lowest GroundY (≈ −2.4 m)
        const float HRANGE = 8f;   // terrain vertical span (m); GroundY ∈ ≈[−2.4, 2.4] sits well inside

        static void BuildTerrain(Transform parent, float minX, float minZ, float maxX, float maxZ, FoliageSet fset)
        {
            float sizeX = maxX - minX, sizeZ = maxZ - minZ;
            const int Hres = 257;   // heightmap resolution (must be 2^n + 1)

            var td = new TerrainData();
            td.heightmapResolution = Hres;                 // set resolution BEFORE size (changing it resets size)
            td.size = new Vector3(sizeX, HRANGE, sizeZ);

            // Heights from the same GroundY the props sample (array indexed [z, x]).
            var heights = new float[Hres, Hres];
            for (int i = 0; i < Hres; i++)
            {
                float z = minZ + (i / (float)(Hres - 1)) * sizeZ;
                for (int j = 0; j < Hres; j++)
                {
                    float x = minX + (j / (float)(Hres - 1)) * sizeX;
                    heights[i, j] = Mathf.Clamp01((GroundY(x, z) - TBASE) / HRANGE);
                }
            }
            td.SetHeights(0, 0, heights);

            // One base grass layer painted fully (a single layer ⇒ normalised weight 1 everywhere).
            td.terrainLayers = new[] { fset.grassLayer };
            const int Ares = 64;
            td.alphamapResolution = Ares;
            var alpha = new float[Ares, Ares, 1];
            for (int i = 0; i < Ares; i++)
                for (int j = 0; j < Ares; j++)
                    alpha[i, j, 0] = 1f;
            td.SetAlphamaps(0, 0, alpha);

            // Detail grass — the bladed carpet, cleared (density 0) under the building + drop-off. URP reliably
            // draws INSTANCED Vertex-Lit detail MESHES (texture/billboard terrain grass it often fails to render),
            // so prefer the authored grass-card mesh prototype; only fall back to the flat billboard if no mesh
            // prototype exists. (PM 2026-06-21: the billboard read as "just a texture, not real grass".)
            if (fset.grassDetailPrefab != null || fset.grassDetail != null)
            {
                const int Dres = 512;
                td.SetDetailResolution(Dres, 16);
                DetailPrototype proto = fset.grassDetailPrefab != null
                    ? new DetailPrototype
                    {
                        usePrototypeMesh = true,
                        prototype = fset.grassDetailPrefab,
                        useInstancing = true,
                        renderMode = DetailRenderMode.VertexLit,        // instanced details must be Vertex-Lit
                        healthyColor = Color.white,                     // colour comes from the prefab material
                        dryColor = new Color(0.85f, 0.82f, 0.66f),
                        minWidth = 0.7f, maxWidth = 1.4f,
                        minHeight = 0.6f, maxHeight = 1.3f,
                        noiseSpread = 0.35f,
                    }
                    : new DetailPrototype                               // fallback (URP may not draw this)
                    {
                        prototypeTexture = fset.grassDetail,
                        usePrototypeMesh = false,
                        renderMode = DetailRenderMode.GrassBillboard,
                        healthyColor = new Color(0.40f, 0.48f, 0.28f),
                        dryColor = new Color(0.52f, 0.47f, 0.30f),
                        minWidth = 0.6f, maxWidth = 1.3f,
                        minHeight = 0.4f, maxHeight = 1.0f,
                        noiseSpread = 0.35f,
                    };
                td.detailPrototypes = new[] { proto };
                var details = new int[Dres, Dres];
                for (int i = 0; i < Dres; i++)
                {
                    float z = minZ + ((i + 0.5f) / Dres) * sizeZ;
                    for (int j = 0; j < Dres; j++)
                    {
                        float x = minX + ((j + 0.5f) / Dres) * sizeX;
                        details[i, j] = GrassDensity(x, z);
                    }
                }
                td.SetDetailLayer(0, 0, 0, details);
                td.wavingGrassStrength = 0.25f;   // affects Grass/Billboard modes; Vertex-Lit meshes don't wave
                td.wavingGrassAmount = 0.3f;
                td.wavingGrassSpeed = 0.4f;
                td.wavingGrassTint = new Color(0.5f, 0.55f, 0.4f);
            }

            var go = Terrain.CreateTerrainGameObject(td);   // adds the Terrain + TerrainCollider
            go.name = "Terrain";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(minX, TBASE, minZ);
            var terrain = go.GetComponent<Terrain>();
            if (fset.terrainMaterial != null) terrain.materialTemplate = fset.terrainMaterial;
            terrain.drawInstanced = true;            // instanced terrain (Unity 6 GPU-resident-drawer friendly)
            terrain.detailObjectDistance = 140f;     // grass stays visible across the long LC-style trek
            terrain.detailObjectDensity = 1f;
        }

        // Detail-grass density per world point (int per detail cell): grass grows EVERYWHERE including over the
        // buried club's surface footprint (it's underground now — the meadow crept over it, so no naked patch above
        // the rooms), 0 only on the drop-off pad. The two entrance mouths are terrain holes, and Unity renders no
        // detail over a hole, so the descent openings stay clear for free.
        static int GrassDensity(float x, float z)
        {
            if (Mathf.Abs(x - _gDropX) < 5f && Mathf.Abs(z - _gDropZ) < 6f) return 0;    // drop-off pad (kept bare)
            int h = Mathf.Abs(Mathf.RoundToInt(x * 5.3f) * 73856093 ^ Mathf.RoundToInt(z * 5.7f) * 19349663);
            return 18 + (h % 8); // 18..25 (≈21 avg) — PM 2026-06-21: lusher grass
        }

        // ── outdoor (seeded scatter south of the building) ───────────────────────────
        // ART PASS (2026-06-20, hybrid direction, zero-import): a dead/abandoned treeline approach restyled to
        // the locked Municipal Debt Noir palette (art-bible §4 — desaturated concrete/green/wood/rust, dark for
        // night) with a derelict INDUSTRIAL YARD dressing at the building (hazard posts, jersey barriers, a dead
        // floodlight, rusty barrels). All whitebox/procedural so MapSiteRuntime stays runtime-safe (no prefab or
        // AssetDatabase deps); the night skybox is set editor-side in Map2SceneBuilder.
        static void BuildOutdoor(Transform parent, int seed, int w, int h, Vector3 frontDoor, Vector3 fireExit, Vector3 dropoff,
            ref Result res)
        {
            var fset = Foliage();
            Material ground = (fset != null && fset.groundMaterial != null)
                ? fset.groundMaterial                          // tiled grass (from the foliage pack) when available
                : Mat(new Color(0.15f, 0.14f, 0.12f));         // else dark desaturated concrete-earth whitebox
            Material padMat = Mat(new Color(0.50f, 0.42f, 0.12f)); // faded safety-yellow (physical floor marking)

            // Wrap the ground + woods around the WHOLE building and reach far out on every side, so there is no
            // naked world edge near the building (PM 2026-06-20). The far perimeter thickens into a dense treeline
            // — a soft, diegetic boundary (no invisible wall); distance fog hides the actual edge.
            float bMinX = 0f, bMaxX = w * Cell, bMaxZ = h * Cell;
            const float Reach = 44f; // world extends this far beyond the building / drop-off on every side
            float minX = bMinX - Reach, maxX = bMaxX + Reach;
            float minZ = dropoff.z - Reach, maxZ = bMaxZ + Reach;

            // Terrain params (consumed by GroundY): flatten the undulation under the building footprint + drop-off.
            _gSeed = seed; _gBW = bMaxX; _gBD = bMaxZ; _gDropX = dropoff.x; _gDropZ = dropoff.z;

            // LC-style real ground: when a FoliageSet supplies a grass TerrainLayer, build a Unity Terrain
            // (heightmap undulation from GroundY + GPU-instanced detail grass). Otherwise fall back to the
            // undulating grass-tiled MESH (keeps the generator runtime-safe with no foliage assets). Either way
            // the surface follows GroundY, so the trees/rocks/bushes placed below (also via GroundY) sit on it.
            if (fset != null && fset.grassLayer != null)
                BuildTerrain(parent, minX, minZ, maxX, maxZ, fset);
            else
                BuildGroundMesh(parent, minX, minZ, maxX, maxZ, ground);

            // Drop-off pad (faded-yellow + hazard chevrons) + named van-spawn anchor. NO breadcrumb path:
            // the trek to the door is through dark woods, so the building must be FOUND. (Whitebox beacon
            // poles removed per PM 2026-06-21 — meaningless placeholders, to be replaced by real props: a van
            // here, an exit sign at the fire exit. The DROPOFF_VanSpawn anchor + res.FireExit still drive
            // the playtest spawn and the navmesh verify, so removing the visual poles changes nothing else.)
            float padY = GroundY(dropoff.x, dropoff.z);
            Box(parent, padMat, dropoff.x, padY + 0.03f, dropoff.z, 5f, 0.06f, 6f, "DROPOFF_Pad");
            HazardChevrons(parent, dropoff);
            var van = Child(parent, "DROPOFF_VanSpawn");
            van.position = new Vector3(dropoff.x, padY, dropoff.z);

            // The club is BURIED, so its surface footprint grows woods like the rest of the meadow (the meadow crept
            // over it — no naked patch above the rooms). Only the two ENTRANCE corridors stay clear (ramp + mouth +
            // apron) so no trunk spawns over a descent hole, plus the drop-off pad. Branches/canopies are collider-
            // free, so only TRUNKS are nav obstacles → the woods stay navmesh-connected and the agent weaves through.
            System.Func<float, float, bool> blocked = (px, pz) =>
            {
                if (Mathf.Abs(px - frontDoor.x) < 3.5f && pz > -RampRun - 5.5f && pz < 1f) return true;          // front descent corridor
                if (Mathf.Abs(px - fireExit.x) < 3.5f && pz > fireExit.z - RampRun - 2f && pz < fireExit.z + 5.5f) return true; // back ascent corridor
                if (Mathf.Abs(px - dropoff.x) < 5f && Mathf.Abs(pz - dropoff.z) < 6f) return true;               // drop-off
                return false;
            };

            // Locked palette bases; MatVar adds a tiny deterministic value-jitter per instance (from position,
            // NOT the scatter RNG) so large fields don't read as flat paint.
            Color cTrunk = new Color(0.20f, 0.16f, 0.12f); // old-wood brown, darkened
            Color cDead = new Color(0.17f, 0.19f, 0.14f);  // dead gray-green (military green, desaturated)
            Color cRock = new Color(0.20f, 0.20f, 0.21f);  // concrete gray

            // (1) base woods wrapping the whole site (building keeps breathing room) — PM 2026-06-21: denser fill
            ScatterWoods(parent, OutdoorScatterGenerator.Generate(minX, minZ, maxX, maxZ, seed, 4.2f, blocked, 0.58),
                cTrunk, cDead, cRock, ref res);

            // (2) dense perimeter treeline — independent seed/stream, ONLY the outer ~14 m ring → soft boundary
            const float Ring = 14f;
            System.Func<float, float, bool> ringOnly = (px, pz) =>
            {
                if (blocked(px, pz)) return true;
                bool inRing = px < minX + Ring || px > maxX - Ring || pz < minZ + Ring || pz > maxZ - Ring;
                return !inRing;
            };
            ScatterWoods(parent,
                OutdoorScatterGenerator.Generate(minX, minZ, maxX, maxZ, seed ^ 0x7EE, 3.3f, ringOnly, 0.85),
                cTrunk, cDead, cRock, ref res);

            // Hybrid's industrial half: a derelict yard at the building's front (flanks of the door approach,
            // never the central corridor) — ties the natural approach into the abandoned-facility fiction.
            BuildYardDressing(parent, seed, frontDoor);
        }

        static void ScatterWoods(Transform parent, List<ScatterItem> items,
            Color cTrunk, Color cDead, Color cRock, ref Result res)
        {
            foreach (var it in items)
            {
                if (it.Type == 0) Tree(parent, MatVar(cTrunk, it.X, it.Z), MatVar(cDead, it.X, it.Z), it);
                else if (it.Type == 1) Rock(parent, MatVar(cRock, it.X, it.Z), it);
                else Bush(parent, MatVar(cDead, it.X, it.Z), it);
                res.Scatter++;
            }
        }

        // Dead tree: a tapered trunk (the ONLY ground obstacle), 2 bare angular branches up high, and a sparse
        // flattened dead crown on ~60% of trees (the rest are bare snags). Branches/crown are collider-free so
        // the forest stays navmesh-passable — agents weave between trunks.
        static void Tree(Transform parent, Material trunk, Material dead, ScatterItem it)
        {
            var set = Foliage();
            if (set != null && set.HasTrees)
            {
                var prefab = set.trees[PickIndex(it.X, it.Z, set.trees.Length)];
                if (prefab != null)
                {
                    PlaceModel(parent, prefab, set.treeMaterial, it.X, it.Z, GroundY(it.X, it.Z),
                        set.treeHeight * (0.75f + 0.5f * it.Scale), it.Yaw, set.trunkRadius, $"Tree_{it.X:0}_{it.Z:0}");
                    return;
                }
            }

            float hgt = 2.4f + 1.5f * it.Scale;  // ── whitebox fallback (no FoliageSet) ──
            var t = Prim(parent, PrimitiveType.Cylinder, trunk, $"Tree_{it.X:0}_{it.Z:0}");
            t.localPosition = new Vector3(it.X, hgt * 0.5f, it.Z);
            t.localScale = new Vector3(0.34f, hgt * 0.5f, 0.34f);

            for (int i = 0; i < 2; i++)
            {
                float ang = it.Yaw + i * 165f;
                var b = Decor(parent, PrimitiveType.Cylinder, trunk, $"Branch_{it.X:0}_{it.Z:0}_{i}");
                b.localScale = new Vector3(0.11f, 0.5f + 0.25f * it.Scale, 0.11f);
                b.localPosition = new Vector3(it.X, hgt * (0.78f + 0.08f * i), it.Z);
                b.localRotation = Quaternion.Euler(50f, ang, 0f); // angled outward/up → dead-snag silhouette
            }

            if (((int)(it.X * 3f + it.Z * 5f) & 7) < 5)
            {
                var c = Decor(parent, PrimitiveType.Sphere, dead, $"Canopy_{it.X:0}_{it.Z:0}");
                float r = 1.6f * it.Scale;
                c.localPosition = new Vector3(it.X, hgt + 0.1f, it.Z);
                c.localScale = new Vector3(r, r * 0.5f, r * 0.85f); // flattened, asymmetric dead crown
            }
        }

        static void Rock(Transform parent, Material mat, ScatterItem it)
        {
            float s = 0.85f * it.Scale;
            var r = Prim(parent, PrimitiveType.Cube, mat, $"Rock_{it.X:0}_{it.Z:0}");
            r.localPosition = new Vector3(it.X, GroundY(it.X, it.Z) + s * 0.32f, it.Z);
            r.localRotation = Quaternion.Euler(0f, it.Yaw, Mathf.Repeat(it.Yaw, 12f) - 6f); // slight settle tilt
            r.localScale = new Vector3(s * 1.5f, s * 0.7f, s * 1.05f);
        }

        // Dead low scrub: 2–3 small flattened clumps, collider-free (decor only → never blocks the agent).
        static void Bush(Transform parent, Material dead, ScatterItem it)
        {
            var set = Foliage();
            if (set != null && set.HasBushes)
            {
                var prefab = set.bushes[PickIndex(it.X + 7.3f, it.Z + 1.1f, set.bushes.Length)];
                if (prefab != null)
                {
                    PlaceModel(parent, prefab, set.bushMaterial, it.X, it.Z, GroundY(it.X, it.Z),
                        set.bushHeight * (0.7f + 0.6f * it.Scale), it.Yaw, 0f, $"Bush_{it.X:0}_{it.Z:0}");
                    return;
                }
            }

            float s = 0.8f * it.Scale;  // ── whitebox fallback (no FoliageSet) ──
            int blobs = 2 + ((int)it.Yaw & 1);
            for (int i = 0; i < blobs; i++)
            {
                float a = (it.Yaw + i * 120f) * Mathf.Deg2Rad;
                float off = 0.35f * s;
                var b = Decor(parent, PrimitiveType.Sphere, dead, $"Bush_{it.X:0}_{it.Z:0}_{i}");
                b.localPosition = new Vector3(it.X + Mathf.Cos(a) * off, s * 0.26f, it.Z + Mathf.Sin(a) * off);
                b.localScale = new Vector3(s, s * 0.5f, s);
            }
        }

        // ── outdoor art helpers ──────────────────────────────────────────────────────
        // Diagonal hazard chevrons painted on the drop-off pad (collider-free; purely a read-cue for "park here").
        static void HazardChevrons(Transform parent, Vector3 pad)
        {
            Material dark = Mat(new Color(0.07f, 0.07f, 0.07f));
            for (int i = -2; i <= 2; i++)
            {
                var s = Decor(parent, PrimitiveType.Cube, dark, $"PadStripe_{i}");
                s.localPosition = new Vector3(pad.x + i * 0.95f, 0.05f, pad.z);
                s.localScale = new Vector3(0.32f, 0.04f, 5.6f);
                s.localRotation = Quaternion.Euler(0f, 28f, 0f);
            }
        }

        // Derelict industrial yard at the building's south face. Everything is placed on the FLANKS of the door
        // approach (|x − frontDoor.x| ≥ DoorW+2.6) so the central DROP-OFF→ENTRY corridor stays walkable; the
        // seed only jitters the barrel cluster (a separate RNG stream → never perturbs the scatter generator).
        static void BuildYardDressing(Transform parent, int seed, Vector3 frontDoor)
        {
            var rng = new System.Random(seed ^ 0x5C0FFEE);
            Color cConcrete = new Color(0.22f, 0.22f, 0.22f);
            Color cMetal = new Color(0.13f, 0.13f, 0.14f);
            Color cRust = new Color(0.30f, 0.20f, 0.12f);
            Color cYellow = new Color(0.50f, 0.42f, 0.12f);
            Color cDark = new Color(0.07f, 0.07f, 0.07f);

            float fx = frontDoor.x, lane = DoorW + 2.6f;

            // checkpoint gate: a striped hazard post each side of the approach
            HazardPost(parent, fx - lane, -6.5f, cYellow, cDark);
            HazardPost(parent, fx + lane, -6.5f, cYellow, cDark);

            // concrete jersey barriers staggered down the LEFT flank (asymmetry per art-bible §6)
            for (int i = 0; i < 3; i++)
            {
                float z = -2f - i * 2.3f;
                Barrier(parent, MatVar(cConcrete, fx - lane, z), fx - lane, z);
            }

            // dead floodlight pole on the RIGHT flank (unlit — amber/CRT light is reserved for live sources)
            Floodlight(parent, fx + lane + 1.2f, -8.5f, cMetal);

            // rusty barrel + debris cluster tucked into the left corner
            float bx = fx - lane - 1.6f, bz = -3f;
            for (int i = 0; i < 4; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float rr = (float)rng.NextDouble() * 1.4f;
                Barrel(parent, MatVar(cRust, bx, bz + i), bx + Mathf.Cos(a) * rr, bz + Mathf.Sin(a) * rr);
            }
        }

        // Yellow/black striped warning post (thin → keeps a collider; the bands are collider-free decor).
        static void HazardPost(Transform parent, float x, float z, Color yellow, Color dark)
        {
            var post = Prim(parent, PrimitiveType.Cylinder, Mat(dark), $"HazardPost_{x:0}_{z:0}");
            post.localScale = new Vector3(0.18f, 1.1f, 0.18f);
            post.localPosition = new Vector3(x, 1.1f, z);
            for (int i = 0; i < 3; i++)
            {
                var band = Decor(parent, PrimitiveType.Cylinder, Mat(i % 2 == 0 ? yellow : dark),
                    $"PostBand_{x:0}_{z:0}_{i}");
                band.localScale = new Vector3(0.2f, 0.18f, 0.2f);
                band.localPosition = new Vector3(x, 0.5f + i * 0.5f, z);
            }
        }

        static void Barrier(Transform parent, Material mat, float x, float z)
        {
            var b = Prim(parent, PrimitiveType.Cube, mat, $"Barrier_{x:0}_{z:0}");
            b.localScale = new Vector3(0.5f, 0.9f, 2.0f);
            b.localPosition = new Vector3(x, 0.45f, z);
        }

        // Dead floodlight: a thin pole (keeps a collider) + a tilted, unlit lamp head (collider-free decor).
        static void Floodlight(Transform parent, float x, float z, Color metal)
        {
            var m = Mat(metal);
            var pole = Prim(parent, PrimitiveType.Cylinder, m, $"Floodlight_{x:0}_{z:0}");
            pole.localScale = new Vector3(0.14f, 2.6f, 0.14f);
            pole.localPosition = new Vector3(x, 2.6f, z);
            var head = Decor(parent, PrimitiveType.Cube, m, $"FloodHead_{x:0}_{z:0}");
            head.localScale = new Vector3(0.7f, 0.4f, 0.5f);
            head.localPosition = new Vector3(x, 5.1f, z + 0.3f);
            head.localRotation = Quaternion.Euler(28f, 0f, 0f);
        }

        static void Barrel(Transform parent, Material mat, float x, float z)
        {
            var b = Prim(parent, PrimitiveType.Cylinder, mat, $"Barrel_{x:0}_{z:0}");
            b.localScale = new Vector3(0.55f, 0.6f, 0.55f);
            b.localPosition = new Vector3(x, 0.6f, z);
        }

        // ── underground entrances (front descent ramp + back ascent ramp + surface lids) ──────────────
        // Underground ⇒ no above-grade box. Each entrance is an ENCLOSED sloped corridor from a meadow "lid" down
        // to a buried door: front = descent to the front door, back = ascent from the fire exit (the loot haul-up).
        // The ramp floor is the navmesh slope that connects the surface to the sunk maze; the side walls hide the
        // raw earth (terrain-hole) sides. PunchEntranceHoles cuts the matching openings in the terrain.
        static void BuildEntrances(Transform parent, Vector3 frontDoor, Vector3 fireExit)
        {
            Material wall = Mat(new Color(0.24f, 0.24f, 0.26f)); // poured concrete entry
            Material lid = Mat(new Color(0.30f, 0.30f, 0.31f));

            // FRONT descent: a mouth on the meadow just south of the footprint → down to the buried front door.
            Vector3 fDoor = new Vector3(frontDoor.x, -Depth, 0f);
            Vector3 fMouth = new Vector3(frontDoor.x, 0f, -RampRun);
            fMouth.y = GroundY(fMouth.x, fMouth.z);
            SlopedTube(parent, wall, fMouth, fDoor, "Descent");
            MouthApron(parent, wall, fMouth, -1f); // bridge the mouth onto solid meadow (navmesh hardening)
            SurfaceLid(parent, lid, fMouth, -1f); // lid sits on the south (outer) side of the mouth

            // BACK ascent: the buried fire-exit door → up to a mouth on the meadow just north of the footprint.
            // fireExit is now the surface MOUTH; the buried door sits RampRun back toward the footprint edge.
            Vector3 bMouth = new Vector3(fireExit.x, 0f, fireExit.z);
            bMouth.y = GroundY(bMouth.x, bMouth.z);
            Vector3 bDoor = new Vector3(fireExit.x, -Depth, fireExit.z - RampRun);
            SlopedTube(parent, wall, bMouth, bDoor, "Ascent");
            MouthApron(parent, wall, bMouth, +1f); // bridge the mouth onto solid meadow (navmesh hardening)
            SurfaceLid(parent, lid, bMouth, +1f);
        }

        // Enclosed sloped corridor between a surface mouth (a) and a buried door (b) that share an x. Floor =
        // walkable ramp (the navmesh slope); two full-length side walls; a ceiling over the lower ~65% so the top
        // stays open as a daylight mouth. a/b are WORLD coords (the Entrances parent is at the origin).
        static void SlopedTube(Transform parent, Material mat, Vector3 a, Vector3 b, string name)
        {
            Vector3 d = b - a;
            float L = Mathf.Max(d.magnitude, 0.001f);
            Quaternion rot = Quaternion.LookRotation(d / L, Vector3.up); // local +Z = slope, +Y kept ≈ world up
            Vector3 up = rot * Vector3.up;     // tube "up" (perpendicular to the slope, always upward)
            Vector3 right = rot * Vector3.right; // tube width axis (≈ world X)
            Vector3 mid = (a + b) * 0.5f;
            float half = TubeW * 0.5f;

            RotBox(parent, mat, mid - up * (FloorT * 0.5f), rot, new Vector3(TubeW, FloorT, L), $"{name}_Floor");
            RotBox(parent, mat, mid + right * half + up * (WallH * 0.5f), rot, new Vector3(WallT, WallH, L), $"{name}_WallR");
            RotBox(parent, mat, mid - right * half + up * (WallH * 0.5f), rot, new Vector3(WallT, WallH, L), $"{name}_WallL");
            Vector3 ceilMid = Vector3.Lerp(a, b, 0.675f) + up * WallH; // lower 65% (door side) gets a roof
            RotBox(parent, mat, ceilMid, rot, new Vector3(TubeW, FloorT, 0.65f * L), $"{name}_Ceil");
        }

        // Lap a ramp mouth out onto SOLID meadow terrain (past the punched hole) so the navmesh bridges ramp ↔ meadow
        // regardless of the terrain-hole texel boundary (holes snap to a ~1 m grid, leaving the mouth floating 1–2 m
        // from solid ground — which connects for one ramp but not the mirror one). The apron's outer end sits at the
        // SAMPLED ground height so it meets the undulating meadow coplanar; the inner end overlaps the ramp top.
        // outwardZ = −1 (front, toward −z) or +1 (back, toward +z), i.e. away from the building.
        static void MouthApron(Transform parent, Material mat, Vector3 mouth, float outwardZ)
        {
            float outZ = mouth.z + outwardZ * 4.5f;                       // ≈2.5 m past the hole edge, onto solid ground
            Vector3 outer = new Vector3(mouth.x, GroundY(mouth.x, outZ), outZ);
            Vector3 d = outer - mouth;
            float L = Mathf.Max(d.magnitude, 0.001f);
            Quaternion rot = Quaternion.LookRotation(d / L, Vector3.up);
            Vector3 up = rot * Vector3.up;
            Vector3 mid = (mouth + outer) * 0.5f;
            RotBox(parent, mat, mid - up * (FloorT * 0.5f), rot, new Vector3(TubeW, FloorT, L + 0.8f), "MouthApron");
        }

        // Low surface frame around an entrance mouth so it reads as "go down here" (collider-free → never blocks the
        // approach onto the ramp). outwardZ points away from the building, toward the open meadow side.
        static void SurfaceLid(Transform parent, Material mat, Vector3 mouth, float outwardZ)
        {
            float half = TubeW * 0.5f + 0.35f;
            var curbR = Decor(parent, PrimitiveType.Cube, mat, "Lid_CurbR");
            curbR.localPosition = new Vector3(mouth.x + half, mouth.y + 0.25f, mouth.z);
            curbR.localScale = new Vector3(0.4f, 0.5f, 3.4f);
            var curbL = Decor(parent, PrimitiveType.Cube, mat, "Lid_CurbL");
            curbL.localPosition = new Vector3(mouth.x - half, mouth.y + 0.25f, mouth.z);
            curbL.localScale = new Vector3(0.4f, 0.5f, 3.4f);
            var hatch = Decor(parent, PrimitiveType.Cube, mat, "Lid_Hatch"); // thrown-open hatch lying on the meadow
            hatch.localPosition = new Vector3(mouth.x, mouth.y + 0.15f, mouth.z + outwardZ * 2.6f);
            hatch.localScale = new Vector3(TubeW, 0.12f, 2.2f);
            hatch.localRotation = Quaternion.Euler(outwardZ * 18f, 0f, 0f);
        }

        // Like Box but with an arbitrary local rotation (the entrance ramps are sloped).
        static void RotBox(Transform parent, Material mat, Vector3 pos, Quaternion rot, Vector3 scale, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // Cut the two ramp openings through the outdoor Terrain (so you can actually descend/ascend) and clear any
        // scatter that would float over / clip the now-open ramps. The Terrain's TerrainData is runtime-created in
        // BuildOutdoor, so editing its holes is safe + non-destructive (mesh-ground fallback can't be holed — but
        // this project ships a FoliageSet grass layer, so the Terrain path is the live one).
        static void PunchEntranceHoles(Transform outdoor, Vector3 frontDoor, Vector3 fireExit)
        {
            float half = TubeW * 0.5f + 0.6f;
            var rects = new (float xMin, float xMax, float zMin, float zMax)[]
            {
                (frontDoor.x - half, frontDoor.x + half, -RampRun - 1f, 0.5f),
                (fireExit.x  - half, fireExit.x  + half, fireExit.z - RampRun - 0.5f, fireExit.z + 1f),
            };

            var terrain = outdoor.GetComponentInChildren<Terrain>();
            if (terrain != null)
                foreach (var r in rects) PunchTerrainHole(terrain, r.xMin, r.xMax, r.zMin, r.zMax);

            var doomed = new List<Transform>();
            foreach (Transform c in outdoor)
            {
                Vector3 p = c.position;
                foreach (var r in rects)
                    if (p.x >= r.xMin && p.x <= r.xMax && p.z >= r.zMin && p.z <= r.zMax &&
                        (c.name.StartsWith("Tree") || c.name.StartsWith("Rock") || c.name.StartsWith("Bush") ||
                         c.name.StartsWith("Branch") || c.name.StartsWith("Canopy")))
                    { doomed.Add(c); break; }
            }
            foreach (var t in doomed) Object.DestroyImmediate(t.gameObject);
        }

        static void PunchTerrainHole(Terrain terrain, float xMin, float xMax, float zMin, float zMax)
        {
            var td = terrain.terrainData;
            Vector3 org = terrain.transform.position; // world origin (= minX, TBASE, minZ)
            Vector3 size = td.size;
            int hr = td.holesResolution;
            int x0 = Mathf.Clamp(Mathf.FloorToInt((xMin - org.x) / size.x * hr), 0, hr - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt((xMax - org.x) / size.x * hr), 0, hr);
            int z0 = Mathf.Clamp(Mathf.FloorToInt((zMin - org.z) / size.z * hr), 0, hr - 1);
            int z1 = Mathf.Clamp(Mathf.CeilToInt((zMax - org.z) / size.z * hr), 0, hr);
            int w = x1 - x0, h = z1 - z0;
            if (w <= 0 || h <= 0) return;
            var holes = td.GetHoles(x0, z0, w, h); // bool[h, w], z-major; false = hole
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    holes[j, i] = false;
            td.SetHoles(x0, z0, holes);
        }

        // ── underground dressing: depth-graded overgrowth, hero rooms, interior lights ────────────────
        // The buried club decays as you go deeper: shallow = green grass creeping in (safe-feeling), mid = moss +
        // roots, deep = pale cave fungus (the danger end). A handful of cells are lavish "hero" rooms (foyer, gaming
        // hall, conservatory, vault) — the contrast makes the luxury read while the rest stays cheap + derelict.
        // Everything is hash-placed from cell coords (NOT the test-locked scatter RNG) so the build stays
        // deterministic. Underground gets its own point lights — the surface sun never reaches down here.
        static void DressInterior(Transform parent, GridLayout layout, GridCoord entry, GridCoord deep)
        {
            float maxD = Mathf.Max(1f, new Vector2(deep.X - entry.X, deep.Y - entry.Y).magnitude);

            string foyer = layout.Owner(entry), vault = layout.Owner(deep);
            string gaming = layout.Owner(Anchors[3].Cell), garden = layout.Owner(Anchors[5].Cell);

            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                {
                    var c = new GridCoord(x, y);
                    if (layout.Kind(c) == CellKind.Empty) continue;
                    string owner = layout.Owner(c);
                    bool hero = owner != null && (owner == foyer || owner == vault || owner == gaming || owner == garden);
                    if (hero) continue; // hero rooms stay clean/lavish (conservatory adds its own lush green)
                    float ratio = Mathf.Clamp01(new Vector2(x - entry.X, y - entry.Y).magnitude / maxD);
                    Overgrowth(parent, x, y, ratio);
                }

            HeroFoyer(parent, layout, Anchors[0].Cell);
            HeroGaming(parent, layout, Anchors[3].Cell);
            HeroConservatory(parent, layout, Anchors[5].Cell);
            HeroVault(parent, layout, Anchors[Anchors.Length - 1].Cell);

            // sparse moody fill lighting: a dim point light at ~1 in 9 walkable cells, warm near the surface end,
            // cold + pale toward the deep fungus end.
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                {
                    var c = new GridCoord(x, y);
                    if (layout.Kind(c) == CellKind.Empty) continue;
                    if (Mathf.Abs((x * 73856093) ^ (y * 19349663)) % 9 != 0) continue;
                    float ratio = Mathf.Clamp01(new Vector2(x - entry.X, y - entry.Y).magnitude / maxD);
                    Color warm = new Color(1.0f, 0.78f, 0.45f);  // sodium amber (shallow)
                    Color cold = new Color(0.55f, 0.75f, 0.78f); // pale cold (deep)
                    PointLight(parent, CellCenter(c) + Vector3.up * (WallH - 0.4f),
                        Color.Lerp(warm, cold, ratio), 0.5f, Cell * 1.7f, $"Lamp_{x}_{y}");
                }
        }

        // One cell's depth-graded overgrowth (hash-gated so ~25% of cells stay bare → reads better than a uniform carpet).
        static void Overgrowth(Transform parent, int x, int y, float ratio)
        {
            int h = Mathf.Abs((x * 92821) ^ (y * 53987));
            if ((h & 3) == 0) return;
            float cx = (x + 0.5f) * Cell, cz = (y + 0.5f) * Cell;
            float jx = ((h >> 2) % 7 - 3) * 0.4f, jz = ((h >> 5) % 7 - 3) * 0.4f;

            if (ratio < 0.34f)
            {
                Material g = Mat(new Color(0.26f, 0.46f + 0.06f * (h % 3), 0.18f)); // green grass creeping in
                GrassTuft(parent, cx + jx, cz + jz, g, h);
            }
            else if (ratio < 0.67f)
            {
                Material moss = Mat(new Color(0.20f, 0.34f, 0.18f)); // dark moss patch on the floor
                var patch = Decor(parent, PrimitiveType.Cube, moss, $"Moss_{x}_{y}");
                patch.localPosition = new Vector3(cx + jx, 0.03f, cz + jz);
                patch.localScale = new Vector3(1.2f + (h % 3) * 0.4f, 0.06f, 1.2f + ((h >> 3) % 3) * 0.4f);
                if ((h & 4) == 0) // a thin root dropping from the ceiling
                {
                    var r = Decor(parent, PrimitiveType.Cylinder, Mat(new Color(0.28f, 0.22f, 0.15f)), $"Root_{x}_{y}");
                    r.localScale = new Vector3(0.10f, WallH * 0.5f, 0.10f);
                    r.localPosition = new Vector3(cx + jx, WallH * 0.5f, cz + jz);
                    r.localRotation = Quaternion.Euler(8f, h % 360, 4f);
                }
            }
            else
            {
                Material pale = Mat(new Color(0.74f, 0.78f, 0.68f)); // pale cave fungus — the danger end
                int caps = 2 + (h % 3);
                for (int i = 0; i < caps; i++)
                {
                    float a = (h + i * 97) % 360 * Mathf.Deg2Rad;
                    float off = 0.5f + i * 0.3f;
                    float s = 0.3f + ((h >> i) % 3) * 0.12f;
                    var cap = Decor(parent, PrimitiveType.Sphere, pale, $"Fungus_{x}_{y}_{i}");
                    cap.localPosition = new Vector3(cx + jx + Mathf.Cos(a) * off, s * 0.4f, cz + jz + Mathf.Sin(a) * off);
                    cap.localScale = new Vector3(s, s * 0.6f, s);
                }
            }
        }

        static void GrassTuft(Transform parent, float x, float z, Material mat, int h)
        {
            int blades = 3 + (h % 3);
            for (int i = 0; i < blades; i++)
            {
                float a = (h + i * 53) % 360 * Mathf.Deg2Rad;
                var b = Decor(parent, PrimitiveType.Cube, mat, "Grass");
                b.localScale = new Vector3(0.06f, 0.45f + (i % 2) * 0.2f, 0.06f);
                b.localPosition = new Vector3(x + Mathf.Cos(a) * 0.18f, b.localScale.y * 0.5f, z + Mathf.Sin(a) * 0.18f);
                b.localRotation = Quaternion.Euler((i % 3) * 7f, a * Mathf.Rad2Deg, (i % 2) * 6f);
            }
        }

        // Lavish overlay floor (every cell of the anchor's room) + an accent point light. Each hero method then
        // adds its own signature props on top.
        static void HeroRoom(Transform parent, GridLayout layout, GridCoord cell, Color floorCol, Color lightCol, float lightInt)
        {
            string id = layout.Owner(cell);
            for (int dx = -RoomRadius; dx <= RoomRadius; dx++)
                for (int dy = -RoomRadius; dy <= RoomRadius; dy++)
                {
                    var n = new GridCoord(cell.X + dx, cell.Y + dy);
                    if (!layout.InBounds(n) || layout.Kind(n) == CellKind.Empty) continue;
                    if (id != null && layout.Owner(n) != id) continue;
                    Box(parent, Mat(floorCol), (n.X + 0.5f) * Cell, 0.02f, (n.Y + 0.5f) * Cell,
                        Cell - 0.05f, 0.04f, Cell - 0.05f, $"HeroFloor_{cell.X}_{cell.Y}_{dx}_{dy}");
                }
            PointLight(parent, CellCenter(cell) + Vector3.up * (WallH - 0.5f), lightCol, lightInt, Cell * 3.2f,
                $"HeroLight_{cell.X}_{cell.Y}");
        }

        static void Column(Transform parent, float x, float z, Material m)
        {
            var c = Prim(parent, PrimitiveType.Cylinder, m, $"Column_{x:0}_{z:0}");
            c.localScale = new Vector3(0.5f, WallH * 0.5f, 0.5f);
            c.localPosition = new Vector3(x, WallH * 0.5f, z);
        }

        static void HeroFoyer(Transform parent, GridLayout layout, GridCoord cell)
        {
            HeroRoom(parent, layout, cell, new Color(0.62f, 0.66f, 0.68f), new Color(1.0f, 0.82f, 0.5f), 2.2f);
            Vector3 ctr = CellCenter(cell);
            Box(parent, Mat(new Color(0.28f, 0.20f, 0.14f)), ctr.x, 0.55f, ctr.z, 3.0f, 1.1f, 0.8f, "Foyer_Desk");
            Material marble = Mat(new Color(0.70f, 0.72f, 0.74f));
            Column(parent, ctr.x - 2.4f, ctr.z + 2.0f, marble);
            Column(parent, ctr.x + 2.4f, ctr.z + 2.0f, marble);
        }

        static void HeroGaming(Transform parent, GridLayout layout, GridCoord cell)
        {
            HeroRoom(parent, layout, cell, new Color(0.30f, 0.10f, 0.12f), new Color(1.0f, 0.55f, 0.45f), 2.0f); // deep-red carpet
            Vector3 ctr = CellCenter(cell);
            Material wood = Mat(new Color(0.20f, 0.13f, 0.10f));
            Material felt = Mat(new Color(0.10f, 0.40f, 0.20f));
            for (int i = -1; i <= 1; i += 2)
            {
                float gx = ctr.x + i * 2.2f;
                Box(parent, wood, gx, 0.45f, ctr.z, 2.2f, 0.9f, 1.3f, $"GameTable_{i}");
                Box(parent, felt, gx, 0.92f, ctr.z, 2.0f, 0.06f, 1.1f, $"GameFelt_{i}");
            }
        }

        static void HeroConservatory(Transform parent, GridLayout layout, GridCoord cell)
        {
            HeroRoom(parent, layout, cell, new Color(0.55f, 0.58f, 0.55f), new Color(0.85f, 1.0f, 0.8f), 3.2f); // bright greenish landmark
            Vector3 ctr = CellCenter(cell);
            var ring = Prim(parent, PrimitiveType.Cylinder, Mat(new Color(0.30f, 0.28f, 0.26f)), "Conservatory_Planter");
            ring.localScale = new Vector3(2.4f, 0.4f, 2.4f);
            ring.localPosition = new Vector3(ctr.x, 0.4f, ctr.z);
            Material leaf = Mat(new Color(0.24f, 0.55f, 0.22f)); // lush green "zero point" of the overgrowth gradient
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f, rr = 0.6f + (i % 3) * 0.3f;
                var blade = Decor(parent, PrimitiveType.Cube, leaf, $"Frond_{i}");
                blade.localScale = new Vector3(0.12f, 1.6f + (i % 3) * 0.4f, 0.12f);
                blade.localPosition = new Vector3(ctr.x + Mathf.Cos(a) * rr, 0.9f, ctr.z + Mathf.Sin(a) * rr);
                blade.localRotation = Quaternion.Euler((i % 4) * 10f, a * Mathf.Rad2Deg, (i % 3) * 8f);
            }
        }

        static void HeroVault(Transform parent, GridLayout layout, GridCoord cell)
        {
            HeroRoom(parent, layout, cell, new Color(0.45f, 0.38f, 0.16f), new Color(0.6f, 0.78f, 0.85f), 1.8f); // gold floor, cold light
            Vector3 ctr = CellCenter(cell);
            Box(parent, Mat(new Color(0.20f, 0.20f, 0.22f)), ctr.x, 0.5f, ctr.z, 1.4f, 1.0f, 1.4f, "Vault_Plinth");
            var prize = Prim(parent, PrimitiveType.Cube, Mat(new Color(0.85f, 0.70f, 0.25f)), "Vault_Prize");
            prize.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            prize.localPosition = new Vector3(ctr.x, 1.3f, ctr.z);
            Material glass = Mat(new Color(0.30f, 0.34f, 0.36f));
            for (int i = -1; i <= 1; i += 2)
            {
                var caseBox = Prim(parent, PrimitiveType.Cube, glass, $"Vault_Case_{i}");
                caseBox.localScale = new Vector3(0.9f, 1.6f, 0.9f);
                caseBox.localPosition = new Vector3(ctr.x + i * 2.6f, 0.8f, ctr.z);
            }
        }

        static void PointLight(Transform parent, Vector3 localPos, Color color, float intensity, float range, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None; // many cheap fills; no shadow cost
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

        // Decorative primitive: like Prim but with its collider stripped, so overhead/cosmetic geometry (tree
        // branches & crowns, scrub, hazard bands, lamp heads, pad stripes) never participates in the navmesh
        // bake — only structural obstacles (trunks, barriers, posts, barrels, rocks) block agents.
        static Transform Decor(Transform parent, PrimitiveType type, Material mat, string name)
        {
            var t = Prim(parent, type, mat, name);
            var col = t.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            return t;
        }

        // Material for a base colour with a small deterministic value-jitter from planar position (5 buckets →
        // MatCache stays tiny). Deliberately derived from (x,z), NOT the scatter RNG, so it can't shift the
        // test-locked OutdoorScatterGenerator stream.
        static Material MatVar(Color c, float x, float z)
        {
            int h = Mathf.Abs(Mathf.RoundToInt(x * 13.1f) * 92821 ^ Mathf.RoundToInt(z * 7.7f) * 53987);
            float f = 0.84f + (h % 5) * 0.07f; // {0.84, 0.91, 0.98, 1.05, 1.12}
            return Mat(new Color(Mathf.Clamp01(c.r * f), Mathf.Clamp01(c.g * f), Mathf.Clamp01(c.b * f)));
        }

        // ── real foliage (FoliageSet in Resources; absent ⇒ whitebox primitives) ─────
        static FoliageSet _foliageSet;
        static bool _foliageLoaded;
        static FoliageSet Foliage()
        {
            if (!_foliageLoaded) { _foliageSet = Resources.Load<FoliageSet>("FoliageSet"); _foliageLoaded = true; }
            return _foliageSet;
        }

        // Deterministic prefab pick from planar position (NOT the scatter RNG → can't shift the test-locked stream).
        static int PickIndex(float x, float z, int n)
        {
            if (n <= 1) return 0;
            int h = Mathf.Abs(Mathf.RoundToInt(x * 17.3f) * 73856093 ^ Mathf.RoundToInt(z * 19.7f) * 19349663);
            return h % n;
        }

        // Instantiate a model, auto-fit it to targetH (measure renderer bounds → scale → sit its base on y=0),
        // strip imported colliders, and (trunkR>0) add ONE unscaled trunk capsule as the sole nav obstacle — so
        // the real forest keeps the same navmesh behaviour the whitebox trunks had (agents weave between trunks).
        static void PlaceModel(Transform parent, GameObject prefab, Material mat, float x, float z, float groundY,
            float targetH, float yaw, float trunkR, string name)
        {
            var go = Object.Instantiate(prefab, parent);
            go.name = name;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;

            var rends = go.GetComponentsInChildren<Renderer>();
            // The imported FBX's material binding is unreliable (legacy name-search, no remap) → force the
            // converted URP material onto every renderer so the model isn't the default white.
            if (mat != null)
                foreach (var r in rends)
                {
                    var slots = new Material[Mathf.Max(1, r.sharedMaterials.Length)];
                    for (int i = 0; i < slots.Length; i++) slots[i] = mat;
                    r.sharedMaterials = slots;
                }
            if (rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float s = targetH / Mathf.Max(b.size.y, 0.001f);
                go.transform.localScale = Vector3.one * s;

                b = rends[0].bounds; // re-measure after scaling
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                go.transform.position = new Vector3(x, go.transform.position.y - b.min.y + groundY, z); // base → ground
            }
            else go.transform.localPosition = new Vector3(x, groundY, z);

            foreach (var col in go.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(col);

            if (trunkR > 0f)
            {
                var colGo = new GameObject(name + "_Col");
                colGo.transform.SetParent(parent, false);
                colGo.transform.localPosition = new Vector3(x, groundY, z);
                var cap = colGo.AddComponent<CapsuleCollider>();
                cap.radius = trunkR;
                cap.height = targetH;
                cap.center = new Vector3(0f, targetH * 0.5f, 0f);
            }
        }

        static void Marker(Transform parent, Vector3 pos, Color color, string name, bool local = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col); // markers must not block navigation
            go.transform.SetParent(parent, false);
            // local=true → place in the parent's space so the marker SINKS with the buried Indoor transform;
            // outdoor markers (dropoff / fire-exit) stay world-space at the surface.
            if (local) go.transform.localPosition = pos + Vector3.up * 1.3f;
            else go.transform.position = pos + Vector3.up * 1.3f;
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
