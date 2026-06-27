# HQ Exterior Art Direction — Dispatch Yard + Derelict Industrial District

**Status**: AUTHORED — Art Director (2026-06-22)
**Scope**: Outdoor environments built by `HqOptionAProductionBuilder.cs` (BuildYard + BuildEnvironment)
**Identity**: Municipal Debt Noir — civic teal, dead rubber black, aged paper, sodium amber, restrained dispatch green, stamp red
**Style lock**: `docs/art/black-commission-style-lock-v2.md` (lo-fi low-poly, ≤256px textures, no PBR micro-detail)
**Upstream**: `design/art/art-bible.md` §2, §4, §6; `HqOptionAProductionBuilder.cs` (coordinate system reference)
**Assets in scope**: `Assets/TirgamesAssets/Factory/` (all prefabs + materials), `Assets/Foliage Free/` (pine1a/b, pine2a/b, ferns.fbx), `Assets/_Project/Scripts/Level/MapSiteBuilder.cs` (ScatterWoods/Tree/Bush pattern reference), `Assets/Resources/FoliageSet.asset`

---

## Coordinate System Reference

The builder places world origin at the near-left (-X/-Z) corner of the wedge office.

| Anchor | World position |
|---|---|
| Office near wall (back) | z = 0 |
| Roll-up door (departure) | z ≈ 18 (centred on the canted far wall) |
| Yard starts | z = 18 |
| Yard centre | z ≈ 31 |
| Yard exit gate (north edge) | z = 44 |
| Cross-street E-W centreline | z ≈ 52 |
| Far perimeter wall N | z = 114 |
| Far perimeter wall S | z = -54 |
| Yard west wall | x ≈ -7.5 |
| Yard east wall | x ≈ 16.5 |
| Office centre x | x = 4.5 |

All placement coordinates below are in world space relative to this system.

---

## 1. Visual Identity Statement for the Exterior

The dispatch yard and derelict district are not atmospheric set dressing. They are the physical evidence of what the Black Commission office is: a nearly bankrupt firm in a declining industrial area, hemmed in by warehouses that have outlasted the companies that built them.

**The test**: if the exterior reads as a clean industrial park, a movie-backlot set, or a generic horror environment, it has failed. It must read as a real place that was last maintained six years ago by a contractor who also went bankrupt.

**What is NOT here**:
- Lush green vegetation of any kind
- Clean, uncracked asphalt
- Bright functional signage
- Well-maintained fencing
- Any light source that reads as new or maintained

**What IS here**:
- Dead, skeletal scrub bursting through cracked concrete
- Warehouses with failing surfaces, broken windows, and rusted roofs
- Cracked asphalt with oil-stain history and weed colonisation at every crack line
- Sodium light pools that illuminate specific zones but leave broad dark corridors
- Fog that begins to dissolve mid-range buildings and fully consumes the far district

---

## 2. Fog and Atmosphere

### Current builder state (reference baseline)
Linear fog: start 14m, end 60m, color `#2E302D` (concrete-gray).

### Required adjustments

**Fog color**: Shift from neutral concrete-gray to a very slight warm-tint to sell the sodium atmosphere. Target: `#2E2E29` (concrete gray with a trace of amber/brown). This is a 2-unit warm shift on B — barely perceptible but stops the fog reading as cold-white.

**Fog start/end**: The current 14–60 range is correct. Do not push start beyond 18 — the yard must be fully legible for co-op navigation. The 60m end means near-district warehouses (z 60–80) are at 50–100% fog, which is the target: readable silhouettes but no surface detail required.

**Night skybox**: Apply `Assets/TirgamesAssets/Factory/Models/Materials/SkyBoxIndustrial01Night.mat` (referenced in the Tirgames kit — verify the exact path in the Tirgames folder; the material name `SkyBoxIndustrial01Night` matches `LampCeiling01Night.mat` convention). If a skybox material is unavailable, the ambient trilight (`sky: #4B4C48`, `equator: #3D3F3A`, `ground: #262825`) already approximates a dark-sky industrial night — do not change it.

**Ambient intensity**: The current 0.92 is slightly high for a noir exterior. Reduce to 0.75–0.80 to deepen shadow contrast on the warehouse faces. The yard's sodium lights should feel like the primary readable light sources, not ambient fill.

**Post volume**: Current settings (grain Medium1 @ 0.20, bloom threshold 1.05 @ 0.40, saturation -10, contrast +7) are correct and should not be changed. The grain at 0.20 reads correctly as the lo-fi night-exterior grain target.

---

## 3. Vegetation — Dead Scrub and Skeletal Trees

### Design principle

This is not a natural setting. Vegetation here is the result of municipal abandonment — weeds and scrub that have taken over cracked concrete and dead soil in the years since maintenance ceased. The palette is dead-olive desaturated, matching the `cDead` color (`#2C3124`, ~Color(0.17, 0.19, 0.14)) and `cTrunk` (`#332919`, ~Color(0.20, 0.16, 0.12)) established in `MapSiteBuilder.cs`. This identical palette treatment ensures the HQ exterior and the mission site approach zones read as the same world.

### FoliageSet assets to use

| Asset | Path | Role |
|---|---|---|
| Tree (4 variants) | `pine1a.fbx`, `pine1b.fbx`, `pine2a.fbx`, `pine2b.fbx` (all in `Assets/Foliage Free/models/`) | Skeletal dead pines — use the FoliageSet treeMaterial, desaturated dead-olive override |
| Bush/scrub | `ferns.fbx` (`Assets/Foliage Free/models/ferns.fbx`) | Low scrub/fern clumps — use FoliageSet bushMaterial |
| FoliageSet | `Assets/Resources/FoliageSet.asset` | Config: treeHeight=6, bushHeight=0.9, trunkRadius=0.35 |

**Material override**: Both tree and bush materials should have their BaseColor desaturated to dead-olive. The FoliageSet's `treeMaterial` and `bushMaterial` are already configured for the mission-site dead palette. Reuse them directly — do not create new materials.

**Colliders**: All foliage is collider-free. Trunk collision is not needed in the non-playable environment district. In the dispatch yard (playable space), foliage must not block the van lane (x: 2–7, z: 18–44) or the player movement envelope (x: -7.5–16.5, y: 0–2.2m).

### Vegetation zones

#### Zone A — Yard Perimeter Scrub (dense low scrub, sparse trees)
Low weeds and scrub colonising the base of the concrete perimeter walls and the yard corners. This is the most visible vegetation zone — players will see it from the spawn point.

- **Coverage**: inner face of yard walls (Yard_WallW, Yard_WallE, Yard_WallN_L, Yard_WallN_R) and the two yard corners (SW and SE corners of the yard footprint)
- **Type**: Bush/scrub only (ferns.fbx), no trees here — too close to the playable movement path
- **Density**: ~24 scrub clumps total. 6 along the west wall (x: -7 to -6, z: 20–42), 6 along the east wall (x: 15.5–16, z: 20–42), 6 along the north wall gaps (z: 43–44.5), 6 in the two yard corners (SW: x -7 to -5, z 18–22; SE: x 14–16, z 18–22)
- **Placement**: Press to the wall base. Scale range: 0.5–1.1 (bushHeight 0.9 × scale = 0.45–0.99m real height). Random yaw.
- **Avoid**: Central van lane (x: 2–7), all collider-active areas within z 18–44

#### Zone B — Yard Corner Dead Trees (1–2 trees per corner)
One or two dead pine skeletons in the SW and SE yard corners, pressed against the yard wall. These read against the fog and give the yard corners silhouette interest without cluttering the movement space.

- **Placement**: SW corner: x -6.5, z 19 and x -5.8, z 21.5. SE corner: x 15.8, z 19 and x 15.2, z 22.
- **Type**: pine1a or pine2b (the narrower crown variants for tight placement)
- **Height**: treeHeight 6 × scale 0.65–0.75 = 3.9–4.5m (shorter, more compressed — urban scrub trees, not forest trees)
- **Count**: 3–4 total

#### Zone C — Cross-Street Weed Lines (cracked asphalt colonisation)
Along the E-W cross-street (Street_EW, centred z ≈ 52, width 170m × depth 11m) and the N-S cross-street (Street_NS), dead scrub clusters colonise the crack lines and the street edges.

- **Placement pattern**: Pairs or trios of scrub clumps at the street edges. Street_EW: scatter ~20 scrub clumps along the north and south edges of the E-W street, x range -40 to +50, z 48–50 (south edge) and z 54–56 (north edge). Avoid z 18–44 (yard), avoid x 0–9 (van exit lane).
- **Add 6–8 dead pine trees** along the north edge of Street_EW (z 54–58, x values -30, -15, -5, 12, 28, 42, 55) at heights 4–6m. These form the first "treeline" silhouette visible through the exit gate — the classic dead industrial perimeter. Spacing irregular (8–18m between trees).
- **Type**: pine2a, pine1b preferred (wider crown — reads as dead treeline silhouette at mid-range)

#### Zone D — Between-Warehouse Vacant Lots (mid-distance treeline)
The gap spaces between warehouse blocks in the north and east quadrants (z > 50, x outside the office footprint) should have thin dead treelines filling the negative space. These are purely silhouette elements lost at 50%+ fog density — they break up the box-wall visual rhythm and suggest the lots were once overgrown open land.

- **Target warehouses for gaps**: North cluster (blocks at (-14,62), (10,68), (34,60), (-34,70), (54,66)) — scatter 4–6 trees in the gaps between each pair, z 55–68, avoiding building footprints by 2m clearance.
- **East cluster**: gaps between (46,12), (52,34), (68,22) — scatter 3–5 trees each gap, x 30–44, z 15–30.
- **West cluster**: gaps between (-34,8), (-44,32), (-62,18) — 3–5 trees each, x -30 to -20, z 10–30.
- **Count**: ~35–45 trees total in Zone D
- **Height**: full treeHeight 6 × scale 0.85–1.2 = 5.1–7.2m. Taller than yard trees — these are unmanaged lot scrub that has had years to grow.
- **No scrub needed** in Zone D — at this distance the fog absorbs ground detail; only the tree silhouettes register.

#### Zone E — South Rear (behind the office)
The south blocks (behind the office at z < 0) are the least visible. Place 8–12 dead trees in the gaps between south blocks ((-8,-28), (16,-32), (-30,-38), (40,-28)), z -12 to -26, x 20–35 and x -20 to -5. These fill the southern void visible through the office windows (if any) and from the back of the scene.

### Summary count
| Zone | Trees | Scrub clumps |
|---|---|---|
| A — Yard perimeter scrub | 0 | 24 |
| B — Yard corner trees | 3–4 | 0 |
| C — Cross-street weed lines | 6–8 | 20 |
| D — Between-warehouse lots | 35–45 | 0 |
| E — South rear | 8–12 | 0 |
| **Total** | **52–69** | **44** |

---

## 4. Warehouse Believability — Box-to-Building Treatment

### Core problem

The current `Warehouse()` method produces two boxes: a body (concrete or brick material) + a roof cap (steel). This reads as programmer stand-in geometry. The fix is not a full rebuild — it is layered surface treatment and strategic facade elements that make each box read as a building that was once used and has since been failing.

### Distance tier system

**Tier 1 — Hero/Near (playable yard-facing facades, z 44–72, visible through the exit gate and over the yard wall)**
These are the warehouses the player sees at maximum detail when in the yard. Full treatment required.
Specific blocks: (-14,62), (10,68), (34,60) — the three directly north of the yard gate.

**Tier 2 — Mid-district (readable but fog-softened, z 72–100 / x ±30–60)**
Surface material variety is sufficient. No detailed facade elements needed — the fog handles fidelity.

**Tier 3 — Far/foggy silhouette (z > 100 or extreme x)**
Existing boxes are correct. Fog dissolves surface at this range. Only silhouette matters.

### Tier 1 — Hero Building Treatment

Each Tier 1 warehouse block requires the following additions to the builder (each is a child of the `Warehouse` game object parent, added programmatically alongside the existing Body + Roof boxes):

#### 4.1 Broken window patches (WindowsBroken01 material)
Apply a second small box on each hero building's long face(s), sized to suggest a window bank with some panels missing. Use `CommonGlass01.mat` for intact panels, `WindowsBroken01.mat` for broken panels. Placement: upper third of the building face (y = height × 0.62 to 0.78), centred on the face, width ~30–40% of facade width, height ~0.8–1.0m.

- Broken window band: `Box()` call, size (face_width × 0.35, 1.0, 0.12), material `Assets/TirgamesAssets/Factory/Models/Materials/WindowsBroken01.mat`
- Place on the south face of blocks (-14,62) and (10,68) (the faces visible through the exit gate)
- On the west face of block (34,60) (visible from the east side of the yard)

#### 4.2 Industrial roll-up doors / loading dock openings
Add a `DoorIndustrial01_1.prefab` or a flat box using `DoorIndustrial01.mat` on the ground-floor level of each Tier 1 building. These imply the building was a working loading facility. Placement: south or yard-facing face, ground level, width 3.5–4.5m, height 3.2m (matching the office roll-up door for consistency).

- Use `FitPrefab()` with `TIRP + "DoorIndustrial01_1.prefab"` at the base of the south face, scaled to (4.0, 3.2, 0.3)
- Fall back to `BoxRot()` with `DoorIndustrial01.mat` if prefab is missing
- Block (-14,62): door on east face (facing the street/yard approach). Block (10,68): door on south face. Block (34,60): door on west face (facing the yard flank).

#### 4.3 Lean-to structures / roof line variation
Add a secondary smaller box against the base of each Tier 1 block to break the pure rectangle silhouette. This simulates an added lean-to, loading shelter, or service annex.

- Lean-to box: ~(building_width × 0.4, building_height × 0.3, 3.0m), using `CommonSteelRoof01.mat`, placed against one flank of the building at ground level
- Adds a horizontal silhouette break at the roofline that destroys the perfect-rectangle profile
- Block (-14,62): lean-to on the east flank. Block (10,68): lean-to on the west flank. Block (34,60): lean-to on the north face.

#### 4.4 Rooftop equipment silhouettes
Add flat low-profile boxes on the roof surface to simulate dead ventilation units, water tanks, and rooftop plant. These are collider-free, purely silhouette.

Per hero building, add 2–3 of the following using `CommonMetalPainted01.mat` (military-green, toned down):
- Vent box: (1.2, 0.6, 1.2) on the roof surface — centred in quadrants, not symmetric
- Water tank cylinder (approximated as a Cylinder primitive): radius 0.6, height 1.4, placed near one corner
- Antenna stub (tall thin box): (0.1, 2.0, 0.1), near the ridge

#### 4.5 Rust streak decals (DecalsDirt01 material)
Add thin flat boxes using `DecalsDirt01.mat` on the lower walls of Tier 1 buildings, offset 0.02m from the wall surface (z-fighting prevention). These simulate rust streaks from roof drainage and window sills.

- 3–5 decal strips per building face
- Size: (0.15, building_height × 0.4, 0.02), placed at irregular x positions on the face
- Y position: from mid-height (building_height × 0.5) down to ground
- Material: `Assets/TirgamesAssets/Factory/Models/Materials/DecalsDirt01.mat`

#### 4.6 Material alternation between blocks
The current builder alternates between `CommonConcreteWall02.mat` and `CommonBricks01.mat` by the `m` parameter (0=concrete, 1=brick). This is good. Reinforce it for Tier 1:
- Block (-14,62): concrete primary, brick lean-to
- Block (10,68): brick primary, concrete lean-to
- Block (34,60): concrete primary, `CommonConcreteWall03.mat` on the lean-to (adds the third concrete variant)

### Tier 2 — Mid-district Treatment (no code changes needed for MVP)
The existing body + roof cap is sufficient at this range. The fog handles the fidelity gap. If a mid-district pass is done later, add `DecalsDirt01.mat` strips to the largest visible faces only.

### Tier 3 — Far/foggy silhouette
No changes. The far blocks (0,96), (-72,60), (82,52) are fully correct as fog silhouettes.

### Height variation (existing blocks)
The existing block heights range from 9 to 24m (h values in the blocks array). This is good variation. The critical read is that adjacent blocks should differ by at least 3m in height to produce a stepped skyline. Current layout achieves this. Do not flatten heights for consistency — the variation IS the believability.

---

## 5. Ground and Dressing — Cracked Asphalt and Debris

### 5.1 Asphalt crack decals
The yard ground (`YardGround`) and district streets (Street_EW, Street_NS) are currently flat single-material boxes. They need crack and oil-stain layer treatment.

**Method (builder-native, no new assets)**: Add flat decal boxes (y-scale 0.01, offset y +0.02 from ground) using existing materials:
- `CommonConcrete05.mat` for dark asphalt patches (cracked areas read slightly darker)
- `FactoryPropsGround.mat` for surface variation stripes

**Placement pattern in the dispatch yard**:
- Long crack line running diagonally: a series of thin decal strips (0.15 wide × 3–5m long), random yaw 5–25°, scattered across the yard surface (x 0–9, z 19–43). 6–8 crack strips total.
- Oil-dark patch near the van bay: 2 dark decal boxes (1.5 × 0.01 × 2.0) using a darker `CommonConcrete05.mat` tint near x 4.5, z 20–22 (where the van drips). These imply years of parked-vehicle oil drip.
- Safety-yellow bay marking degradation: the existing `VanBay` markMat (CommonMetalPainted01.mat) is acceptable but can be complemented with 2–3 smaller boxes at the van bay corners using faded safety-yellow tint (Color `#A87E10` fallback).

**Cross-street crack lines**:
- 3–4 crack decal strips across the E-W street at z 49–55, x -20 to +30 range. These are the first thing visible through the exit gate after the van departs.

### 5.2 Debris clusters (Debris01_* prefabs)
The Tirgames kit provides 13 debris variants (`Debris01_1` through `Debris01_13`). Use these to create 4–6 debris clusters in the yard periphery and along the near district edge.

**Cluster positions** (all collider-free, non-blocking):
| Cluster | Position | Prefabs | Notes |
|---|---|---|---|
| Yard_SW corner | x -6.5, z 22, y 0 | Debris01_2, Debris01_5, Debris01_8 | Rubble pile against west wall |
| Yard_NE corner | x 15.5, z 40, y 0 | Debris01_1, Debris01_3 | Small rubble pile |
| Yard_near gate | x 2.5, z 43.5, y 0 | Debris01_7, Debris01_11 | Debris pushed to gate-side |
| Street south edge | x -3, z 50.5, y 0 | Debris01_4, Debris01_6, Debris01_12 | First visible debris from yard |
| Street median | x 18, z 51.5, y 0 | Debris01_3, Debris01_13 | Suggests road damage |
| Block base (hero) | x -10, z 56, y 0 | Debris01_9, Debris01_10 | At base of (-14,62) block |

Each cluster: FitPrefab() calls with size target (0.6, 0.4, 0.6) for small pieces, (1.0, 0.6, 1.0) for larger. Material: `Assets/TirgamesAssets/Factory/Models/Materials/Debris01.mat`.

### 5.3 Barrel clusters
Extend the existing yard barrel placement (currently 2 barrels near xE at z0+3.5). Add:
- 3-barrel cluster at x -6.0, z 38 (yard west wall): `Barrel01b.prefab`, `Barrel01c.prefab`, `Barrel01d.prefab` (varying heights, 2 upright + 1 on its side)
- 2-barrel cluster at x 16, z 28: `Barrel01a.prefab`, `Barrel01c.prefab`
- Lone barrel at x -3, z 52 (street edge, near District_Ground)
Material: `Assets/TirgamesAssets/Factory/Models/Materials/Barrels01.mat` and `Barrels02.mat` alternated.

### 5.4 Dead floodlight poles
Add 2–3 dead floodlight pole structures in the yard using a simple Box composition (no specific Tirgames floodlight prefab identified — these are simple primitives):
- Pole: thin cylinder or box (0.1, 5.5, 0.1), `CommonMetal01.mat`
- Head: small box (0.6, 0.2, 0.4) at top, angled 20° downward, `CommonMetalPainted01.mat`
- No light component attached — these are dead/unlit, consistent with the art-bible rule (unlit amber is reserved for live sources)
- Positions: x -6, z 30, y 0 (yard west flank) and x 15, z 36, y 0 (yard east flank)

### 5.5 Gas/propane tank cluster
Add 1–2 `GasBallone01_2.prefab` units (propane-style cylinders, Tirgames kit) at the base of the office exterior wall, yard-side. These read as decommissioned utility equipment.
- Position: x 8.8, z 22, y 0 (against the +X office wall, south end of yard)
- Material: `GasBallone01.mat`

---

## 6. Lighting — Sodium Pools, Dead Lights, Atmosphere

### 6.1 Current state analysis
The builder places 2 yard lights (WarmTungsten, intensity 0.7, range 14) and 3 distant environment sodium glows. This is a functional baseline but produces uniform coverage. The noir target is defined pools separated by darkness.

### 6.2 Yard light repositioning
Replace the symmetric placement with asymmetric pools:

**Current** (delete these in the Phase 2 revision):
- `Yard_Light_W` at (xW+2.5, 5, zMid-5)
- `Yard_Light_E` at (xE-2.5, 5, zMid+5)

**Replacement** (3 lights, asymmetric):
| Name | Position | Color | Intensity | Range | Purpose |
|---|---|---|---|---|---|
| `Yard_Sodium_Gate` | (4.5, 7.0, 43.5) | `#D9A850` (sodium amber) | 1.0 | 16 | Illuminates the exit gate — the departure anchor |
| `Yard_Sodium_VanBay` | (4.5, 5.5, 22.5) | `#D9A850` | 0.85 | 12 | Illuminates the van — the ritual staging point |
| `Yard_Sodium_WallFlank` | (-5.5, 4.5, 30.0) | `#C89040` (dimmer, older lamp) | 0.5 | 9 | Dim light on the yard's west wall — shadows the east side |

Note: `D9A850` is a sodium street-lamp amber — slightly more orange-yellow than the `WarmTungsten` (#FFBB73) used inside. This is intentional: interior tungsten is habitation warmth; exterior sodium is municipal/industrial coldness-in-warmth. The color difference is subtle but sells the threshold between office-as-home and yard-as-departure.

### 6.3 Near-district sodium (environment backdrop)
Extend the existing 3 environment sodium glows with 2 more to create depth:

| Name | Position | Color | Intensity | Range |
|---|---|---|---|---|
| `Env_Sodium_4` | (-8.0, 9.0, 62.0) | `#D9A850` | 0.55 | 20 |
| `Env_Sodium_5` | (20.0, 10.0, 55.0) | `#C89040` | 0.45 | 18 |

These place sodium pools immediately north of the exit gate, visible through it during the departure ritual. They establish that the cross-street and near warehouses are lit (if dimly) — not a void.

### 6.4 Dead street light poles (no light component)
Add 3–4 dead pole structures along Street_EW (at x -20, -5, +15, +30, z 52) using the same pole-box composition as §5.4. These establish the infrastructure of a once-lit street. Their presence makes the live sodium glows read as the exception (1 in 4 still working), not the rule.

### 6.5 FanBig01 on a hero warehouse
Add a dead `FanBig01Motor01.prefab` on the face of hero block (10,68), mounted at approximately (x 9.5, y 10.5, z 51.5). This is a wall-mounted industrial extractor fan — static (no animation), dead condition. It breaks up the flat warehouse face and adds a recognizable industrial silhouette. Use `FanBig01.mat`.

---

## 7. Color Palette — Exterior Application

All exterior surfaces must sit within the Municipal Debt Noir palette. The key risk in the exterior is value creep — surfaces that are too light will destroy the night atmosphere.

### Surface value guide

| Surface | Material | Target albedo value | Notes |
|---|---|---|---|
| Yard asphalt | `FactoryPropsGround.mat` | 0.12–0.13 | Very dark — reads almost black in shadow |
| District ground | same | 0.10–0.11 | Slightly darker than yard |
| Street surface | `CommonConcrete05.mat` | 0.09–0.10 | Near-black |
| Perimeter wall | `CommonConcreteWall01.mat` | 0.26–0.28 (from builder) | Correct — mid concrete |
| Warehouse body (concrete) | `CommonConcreteWall02.mat` | 0.26–0.27 | Correct |
| Warehouse body (brick) | `CommonBricks01.mat` | 0.24–0.22/0.20 (builder RGB) | Correct warm-brick |
| Warehouse roof | `CommonSteelRoof01.mat` | 0.20–0.22 | Correct |
| Debris | `Debris01.mat` | existing | Do not tint |
| Lean-to steel | `CommonSteelRoof01.mat` | 0.18–0.20 | Slightly darker than main roof |
| Dead trees | FoliageSet treeMaterial | `(0.20, 0.16, 0.12)` | From MapSiteBuilder cTrunk |
| Scrub/ferns | FoliageSet bushMaterial | `(0.17, 0.19, 0.14)` | From MapSiteBuilder cDead |

### Saturation constraint
All exterior surfaces must have HSV saturation ≤ 0.20. The brick material is the highest-risk — `CommonBricks01.mat` at its native saturation may read as too warm/red at full resolution. If the material reads as brown-red rather than dark warm-gray, desaturate its BaseColor by -30% in URP/Lit or override the material color in the builder call to `(0.24f, 0.22f, 0.20f)` (existing builder value — correct, keep it).

---

## 8. Per-Zone Prop Density Register

Applying the art-bible §6 density grammar to the exterior zones:

| Zone | Register | Props per 8×8m | Governing rule |
|---|---|---|---|
| Dispatch yard — van lane | Sparse | 0–1 | Navigation clearance; van boarding ritual must be unambiguous |
| Dispatch yard — flanks | Functional | 3–6 | Dressing density communicates active-use yard without cluttering movement |
| Dispatch yard — perimeter | Functional | 4–8 (scrub + debris) | Wall base is the accumulation zone |
| Exit gate zone | Sparse | 0–2 | The gate is the departure threshold — keep it readable as a hero shape |
| Cross-street | Sparse | 1–3 | Navigation space; players pass through here during van departure/return |
| Near-district (z 50–75) | Functional-sparse | 2–5 visible elements per warehouse | Enough dressing to read as a building, not a box |
| Mid-district (z 75–100) | Sparse | Fog handles it | Material + silhouette only |
| Far district (z > 100) | None | — | Box + roof cap + fog = correct |

**Asymmetry rule enforcement**: The dispatch yard's west flank (x -7.5 to 0) is the dense/shadow side — more barrels, more scrub, more debris. The east flank (x 9 to 16.5) is the lighter/van-access side — fewer props, clearer of ground clutter. This asymmetry reads as: the east flank was kept clear for operational access; the west has accumulated the institutional detritus of years.

---

## 9. Implementation Priority Order

This order is based on visual impact vs. implementation cost for the builder:

1. **Vegetation scatter (Zones A/B/C/D/E)** — highest PM-requested impact. Uses the existing ScatterWoods/FoliageSet pattern from MapSiteBuilder; add a `BuildVegetation(Transform root)` method to the builder mirroring that pattern.
2. **Lighting restructure (§6.2)** — replace the symmetric 2-light yard setup with the asymmetric 3-light sodium arrangement. Single-method change in `BuildYard()`.
3. **Debris clusters (§5.2)** — 6 clusters, each 2–3 `FitPrefab()` calls. Add to `BuildYard()` and `BuildEnvironment()`.
4. **Barrel clusters extension (§5.3)** — 5 additional barrels. 5 `FitPrefab()` calls in `BuildYard()`.
5. **Hero warehouse Tier 1 treatment (§4, broken windows + lean-tos + rooftop)** — modify `Warehouse()` or add a `HeroWarehouse()` overload. Apply to 3 blocks.
6. **Asphalt crack decals (§5.1)** — flat box overlays in `BuildYard()` and `BuildEnvironment()`. ~10 Box() calls.
7. **Dead floodlight poles (§5.4)** — 4–5 composite primitives. Low cost.
8. **FanBig01 on warehouse (§6.5)** — 1 FitPrefab() call.
9. **Gas tank cluster (§5.5)** — 2 FitPrefab() calls.
10. **Fog color and ambient intensity tweak (§2)** — single-line changes in `ConfigureRenderSettings()`.
11. **Near-district sodium lights extension (§6.3)** — 2 additional PointLight() calls in `BuildEnvironment()`.

---

## 10. What NOT to Do (Constraints Checklist)

- No green vegetation. Ferns and pines are visually dead — override their material color if the FoliageSet material appears green/saturated.
- No new asset downloads. All vegetation uses `Assets/Foliage Free/` models + `FoliageSet.asset`. All props use `Assets/TirgamesAssets/Factory/Prefabs/`.
- No vegetation within x 2–7, z 18–44 (the van lane + roll-up door approach). Player must be able to walk from spawn to van without collision.
- No new light colors. Yard/district lights use sodium amber (`#D9A850`) only. Interior tungsten remains `#FFBB73`. CRT green is interior-only.
- No TreePackVol.1 assets (not found in project). Foliage Free only.
- Do not delete the existing `Yard_PowerBox` or `Yard_Barrels` (already placed in builder) — the new clusters supplement them.
- Do not touch the fog start distance below 14m or the post-volume settings — both are locked by the art-bible state.
- Debris prefabs are collider-free in the placement spec (the yard must remain navmeshable even if NavMesh is added later).
- No `LampCeiling01Night.prefab` in the exterior — this is an interior ceiling lamp. Use Box primitives for dead street/yard poles.
- Do not add vegetation inside the office perimeter (z 0–18, x 0–9). The interior is a signed-off environment.
