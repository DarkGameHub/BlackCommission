# Floor Plan & Modular Room Kit — Earth Coast No.1 Abandoned Tower

> Companion to `abandoned-tower-earth-coast-01.md`. Defines a **modular room-size kit**
> (4 sizes) and a **slot-based floor plan** for both floors so rooms can be **randomly
> assigned** into fixed slots without breaking navigation or netcode sync.
> Grid unit **G = 4 m** (matches the existing `_4m` corridors in the blockout).
> All dimensions are W (x, east) × D (z, north); floor-to-floor height = 4 m, clear ≈ 3.2 m.

---

## 1. Room Size Kit (4 standard sizes)

| Class | Footprint | Grid (G=4m) | Area | Typical use |
|-------|-----------|-------------|------|-------------|
| **S** Small | 4 × 4 m | 1 × 1 | 16 m² | utility/side: power, maintenance, temp office, closet |
| **M** Medium | 8 × 8 m | 2 × 2 | 64 m² | standard rooms: workshop, dorm, sales office, show-flat |
| **L** Large | 12 × 8 m | 3 × 2 | 96 m² | feature rooms: warehouse, lobby, deep target |
| **XL** Hall | 16 × 12 m | 4 × 3 | 192 m² | hero space: central construction hall / shaft void |

**Shared modules (not "rooms"):**
- **Corridor**: 4 m wide (1 G). Main spine + cross corridor.
- **Door opening**: 2 m wide, centered on a wall, on a 4 m grid line.
- **Stair core**: 4 × 8 m (1 × 2 G), switchback, occupies the SAME x,z on both floors (vertical alignment is mandatory).
- **Shaft void**: an open double-height hole (8 × 4 m) in the F1 ceiling / F2 floor — the vertical sightline, crossed on F2 by the scaffold bridge.

Rule of thumb for the artist/builder: every room footprint, corridor, door, and stair
**snaps to the 4 m grid**. Nothing is off-grid. This is what makes slots interchangeable.

---

## 2. The Slot System (how random assignment works)

Each floor is a **fixed skeleton** (envelope walls + corridors + stairs + doors +
shaft) with a set of **room slots**. A slot is just an anchor + a size tag:

```csharp
// programmer's mental model
struct RoomSlot {
    Vector3   anchor;     // world origin (snapped to 4m grid)
    SizeClass size;       // S / M / L / XL  -> only same-size rooms fit
    Floor     floor;      // F1 / F2
    SlotRole  role;       // Fixed (objective/power/...) or Random (pool)
    Door[]    doors;      // pre-authored openings to the corridor/neighbors
}
```

Room prefabs are authored to **exactly fill one size class** and tagged:

```csharp
struct RoomDef {
    SizeClass size;       // must match the slot
    RoomType  type;       // Warehouse / Dorm / Workshop / ...
    bool      floor1Only; // e.g. PowerRoom is ground-floor only
    int       weight;     // spawn weight in the random pool
}
```

**Generation = fill slots:**
1. Place **Fixed-role** slots first (objective, power gate, monster nest, van, stairs).
2. For each **Random** slot, pick a `RoomDef` where `def.size == slot.size` and
   constraints hold (floor, "at most one of type", etc.), then instantiate at `slot.anchor`.
3. Corridors/doors/NavMesh/stairs are authored ONCE on the skeleton → **always valid**,
   no matter how rooms shuffle. (This is why we randomize *content*, not *topology*.)

> Upgrade path to fuller randomness later: keep the skeleton, but allow a slot to host
> a same-size room OR a "blocked rubble" filler, and add 1–2 optional cross-corridor
> toggles. Still bakeable/syncable because the door graph is a known finite set.

---

## 3. Floor 1 — Ground / Arrival (envelope 36 × 20 m)

Top-down (north ↑). Each `#`/box ≈ a slot; `===` corridor; `▓` wall; `▒` shaft void.
Van/forecourt is OUTSIDE the south wall.

```
z=20 ┌────────────┬───────┬───────┬─────┐
     │  N1  (L)   │ N2(M) │N3(S) │     │   NORTH BAYS  (z 12–20, depth 8)
     │ Warehouse  │ Dorm  │ util │STAIR│
z=12 ├───┬────────┴───┬───┴──────┤  A  │
     │   │            │          │(4×8)│
 spine===│====▒shaft▒=│==========│=====│   MAIN SPINE corridor (z 8–12, 4m)
z=8  │STR│            │          ├─────┤
     │ B ├───┬────────┴───┬──────┤ S4  │
     │4×8│S1 │  S3   (L)  │ S2(S)│ (M) │   SOUTH BAYS (z 0–8, depth 8)
     │   │(S)│  Lobby     │      │Work │
z=0  └───┴───┴──┬──────┬──┴──────┴─────┘
                │ VAN  │  forecourt (z<0): F1_S1_StartVanArea
                └──────┘
     x=0   4   8      20      28     32  36
```

| Slot | Default room (blockout name) | Size | Origin (x,z) | Role | Notes |
|------|------------------------------|------|--------------|------|-------|
| VAN | `F1_S1_StartVanArea` | — | x14–22, z<0 | **Fixed** | Spawn / return / partial-settlement |
| S3 | `F1_M1_LobbySecurityPassage` | L (12×8) | 8,0 | **Fixed** | Entry hall; "Power Off" readout; opens to van |
| S1 | `F1_S3_PowerRoom` | S (4×4) | 4,0 | **Fixed (gate)** | Restore power → unlock F2 |
| S2 | `F1_S2_TemporaryOffice` | S (4×4) | 24,0 | Random (S, F1) | Holds power-room clue |
| S4 | `F1_M2_EastAssistantWorkshop` | M (8×8) | 28,0 | Random (M, F1) | Consumables |
| N1 | `F1_L2_WestMaterialWarehouse` | L (12×8) | 4,12 | Random (L, F1) | Consumables loot |
| N2 | `F1_M3_MainWorkerDorm` | M (8×8) | 16,12 | Random (M, F1) | **Evidence** (Isolation Notice) |
| N3 | util/storage | S (4×4) | 24,12 | Random (S) | Filler/loot |
| STAIR-A | `F1_A_MainStair` | core (4×8) | 32,8 | **Fixed** | Fast/exposed; aligns with F2 |
| STAIR-B | `F1_B_SideStair` | core (4×8) | 0,4 | **Fixed** | Slow/safe; aligns with F2 |
| HUB | `F1_L1_CentralConstructionHall` | (spine+shaft) | center | **Fixed** | Double-height; see up to F2 |

---

## 4. Floor 2 — Show-flat / Sales floor (same envelope 36 × 20 m)

Stairs A/B sit at the **same x,z** as F1. The **shaft void** is the same hole, now
crossed by the **scaffold bridge** — the ONLY direct east→west route, which forces the
objective run across the gap (or a long safe loop via Stair-B in the west).

```
z=20 ┌────────────┬──────────┬─────┐
     │  DEEP TARGET (L)      │N-util│     NORTH BAYS (z 12–20)
     │  Scale model + monster nest  │ (S) │STAIR│  <- objective FAR from van/StairA
z=12 ├──────┬──────┴────┬────┴─────┤  A  │
     │ L4(M)│           │          │(4×8)│
 spine======│===[SCAFFOLD▒BRIDGE]==│=====│  cross the open shaft here
z=8  │ Sample           │          ├─────┤
     │ STR ├──────┬─────┴────┬─────┤ M4  │
     │ B   │ S4(S)│  L3 (L)  │S5(S)│ (M) │  SOUTH BAYS (z 0–8)
     │4×8  │Maint │UnfinShaft│Dang │Sales│
z=0  └─────┴──────┴──────────┴─────┴─────┘
     x=0   4      8         20    28  32 36
```

| Slot | Default room (blockout name) | Size | Origin (x,z) | Role | Notes |
|------|------------------------------|------|--------------|------|-------|
| TARGET | `F2_L5_DeepTargetArea` | L (12×8) | 4,12 | **Fixed (objective+nest)** | Scale model on lit pedestal; far NW |
| L4 | `F2_L4_SampleOffice_HalfFinished` | M (8×8) | 4,8 | Random (M, F2) | Show-flat; "wrong" warm light |
| M4 | `F2_M4_SalesOffice_RichLoot` | M (8×8) | 28,0 | Random (M, F2) | Rich loot near Stair-A |
| L3 | `F2_L3_FlowPlatform_UnfinishedShaft` | L (12×8) | 8,0 | **Fixed (shaft edge)** | Wraps the void; fall risk |
| S4 | `F2_S4_MaintenanceRoom` | S (4×4) | 4,0 | Random (S, F2) | Patrol-gap shortcut |
| S5 | `F2_S5_DangerousShaftRoom` | S (4×4) | 24,0 | Random (S, F2) | High-risk extra loot |
| N-util | util/storage | S (4×4) | 24,12 | Random (S) | Filler |
| BRIDGE | `F2_M5_ScaffoldBridge` | (corridor over void) | center | **Fixed** | Only direct E→W route |
| STAIR-A | `F2_A_MainStair` | core (4×8) | 32,8 | **Fixed** | Aligns with F1 |
| STAIR-B | `F2_B_SideStair` | core (4×8) | 0,4 | **Fixed** | Aligns with F1; safe west loop |

---

## 5. Why this layout works (design checks)

- **Vertical gimmick**: stairs A/B + shaft + scaffold bridge = floor-by-floor risk
  with a chase choice (fast exposed Stair-A vs slow safe Stair-B-west loop).
- **Objective ↔ exit separation**: TARGET is NW on F2; VAN is south-center on F1.
  Carrying the heavy scale model out forces the full descent — the pressure phase.
- **Risk gradient**: F1 lobby/bays (low→med) → ascend → F2 sales/sample (med) →
  scaffold + deep target (high).
- **One memorable wrong detail / strong silhouette**: the pristine warm-lit show-flat
  (L4) + lit scale model pedestal glowing inside the dark concrete shell, visible across the
  shaft from F1 — doubles as a navigation beacon.
- **Readability for co-op**: 4 m corridors and a central double-height shaft keep
  navigation legible for 1–4 players + a chasing monster.

---

## 6. Random-assignment rules (constraints the generator must honor)

**Fixed (never shuffled):** VAN, both STAIRs (must vertically align), F1 PowerRoom,
F1 Lobby (must touch van), F2 TARGET (objective+nest), F2 ScaffoldBridge, F2 ShaftEdge.

**Random pools (fill remaining same-size slots):**
- Each `RoomDef` is `floor1Only` / `floor2Only` / any.
- `PowerRoom` clue must spawn in a Random **S** slot on **F1** (so the gate is always solvable).
- `Evidence (Isolation Notice)` spawns in exactly **one** Random slot on F1 (M preferred: dorm/office).
- "At most one of each rich-loot room per floor" to avoid loot flooding.
- Guarantee ≥1 consumable room reachable **before** any stair (teach gear use early).
- Monster nest is tied to the Fixed TARGET slot — never randomized onto a stair/van.

**Validity invariant:** because corridors, doors, stairs, and the shaft are authored on
the skeleton, **every shuffle is automatically navigable and net-syncable**. The
generator only swaps room *contents*; it never moves a door. Test once, valid forever.

---

## 7. Build order (maps to your existing blockout)

1. Promote `Assets/Scene/AbandonedBuilding_Blockout.unity` to `Tower_EarthCoast_01.unity`.
2. Lay the **skeleton** to this grid: envelope, 4 m spine + cross corridor, shaft void,
   Stair-A/B (vertically aligned), scaffold bridge, all 2 m doors.
3. Drop empty **slot anchors** (S/M/L/XL) at the origins in the tables above.
4. Author room prefabs by size class (start by reusing the named blockout rooms).
5. Bake NavMesh on the skeleton once; verify stairs A/B traverse and agents do NOT
   cross the scaffold gap unless intended.
6. (Later) write the slot-filler generator; until then, hand-place defaults from the tables.

> **Test note (high-risk per `design/systems-index.md`):** the slot generator and the
> power-gate/heavy-carry state are server-authoritative. Add EditMode tests that the
> generated layout is identical on host and clients (seed-synced) and always solvable.
