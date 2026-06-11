# Abandoned Tower · Floor 1 As-Built Verification (Real Scene Geometry)

> Data source: `Assets/Scene/AbandonedBuilding_Blockout.unity` (version 2026-06-07 19:19).
> All coordinates restored from scene YAML by a read-only parse script (world coordinates,
> x→east, z→north, units in meters).
> Companion accurate diagram: `tower_floor1_accurate.svg`. **This is as-built, not design intent** —
> the design document (`abandoned-tower-redesign-v3.md`) and the old diagram `tower_floor1_zh.svg`
> are both outdated.

---

## 1. Floor 1 Real Floor Plan (ASCII, 4m/cell, north ↑)

```
  40   .    .    .    .    .  FOREMFOREM FIRE FIRE  .
  36 COLLACOLLACOLLACANTECANTEFOREMFOREM FIRE FIRE  .
  32 COLLACOLLACOLLACANTECANTE  .  STAIRASTAIRA .    .    .
  28 COLLACOLLACOLLA DORM DORM  .  STAIRASTAIRA .    .    .
  24 COLLACOLLACOLLA DORM DORM  .    .    .  SHANT  .
  20 STAIRB.    .   HALL HALL HALL  .    .   DOCK DOCK
  16 STAIRB TEMP  .   HALL HALL HALL  .    .   DOCK DOCK
  12 POWER  .  SECURSAMPL  .    .  WORKSWORKSREBARREBAR
   8   .    .    .    .    .    .  WORKSWORKSREBARREBAR
   4   .    .    .  LOBBYLOBBYLOBBY  .    .    .    .
   0   .    .    .  LOBBYLOBBYLOBBY PUMP  .    .    .
  -4 WAREHWAREHWAREH VAN  VAN  VAN   .    .    .    .
  -8 WAREHWAREHWAREH VAN  VAN  VAN   .    .    .    .
     x=0   4    8   12   16   20   24   28   32   36  40
```

## 2. Precise Room Rectangles `[x0,z0]-[x1,z1]` (meters)

| Room | Rectangle | Size | Role | Notes |
|---|---|---|---|---|
| WAREHOUSE West Warehouse | [0,-8]-[12,0] | L 12×8 | Random | Same row as the van (southernmost) |
| LOBBY | [12,0]-[24,8] | L 12×8 | Fixed · receives van | ★ Landmark ◆ Main entrance; floor = iron stairs material |
| PUMP Pump Room | [26,0]-[30,4] | S 4×4 | densify | x offset 2m from grid |
| POWER Electrical Room | [0,10]-[4,14] | S 4×4 | ⚡ Gate | z offset 2m from grid |
| SECUR Security Office | [8,10]-[12,14] | S 4×4 | Random · single 30 | z offset 2m |
| SAMPLE Sample Room | [12,10]-[16,14] | S 4×4 | Random · single 55 | z offset 2m |
| TEMP Temporary Office | [4,14]-[8,18] | S 4×4 | ! clue | z offset 2m |
| WORKSHOP | [24,8]-[32,16] | M 8×8 | Random · consumables | |
| REBAR Rebar Yard | [34,8]-[42,16] | M 8×8 | densify | x offset 2m |
| HALL Central Construction Hall | [12,16]-[24,24] | L 12×8 | Fixed · ▒ shaft | |
| DOCK Loading Dock | [34,16]-[42,24] | M 8×8 | Random · dual 110 | x offset 2m |
| STAIRB1 Stair B | [0,16]-[4,24] | 4×8 | Stairs · dim/safe | |
| DORM Worker Dorm | [12,24]-[20,32] | M 8×8 | ! evidence | |
| SHANTY Workers' Shed | [34,24]-[38,28] | S 4×4 | densify | x offset 2m |
| STAIRA1 Stair A | [26,**26.41**]-[30,**37.59**] | 4×**11.18** | Stairs · fast/exposed | ⚠ Distorted by scale, off-grid |
| CANTEEN | [12,32]-[20,40] | M 8×8 | Random · single 40 | |
| FOREMAN Foreman Office | [22,36]-[30,44] | M 8×8 | Random · single 90 · connects to fire exit | |
| VAN Dispatch Van | (14,-8)-(26,0) | — | Extraction (outdoor) | No floor (outdoor) |
| FIRE Fire Exit | (30,36)-(38,44) | — | ▲ Only exit | |
| COLLAPSE Collapsed Corner | (0,24)-(12,40) | — | ░ Open sky · fire exit side | No floor (void) |

Corridors/Doors (Connector Run, F1 has 29 edges total, all with geometry):
`E-VAN E-LH E-H-SA E-LPWR E-PWR-SB E-PWR-TEMP E-FIRE E-LSAMP E-SAMP-H E-HW E-WD
E-LW E-HN E-N-FORE E-FORE-SA E-SECUR-TEMP E-CANTEEN-FORE E-WS-REBAR E-LOBBY-PUMP
E-DOCK-SHANTY` + seed toggles `T1 T2 T3 T4 T5 T6 T10 T11 T12`.

---

## 3. Issue List (by severity)

### 🔴 Blocking
1. **Floor 2 has been lifted to y≈28.2**, but the stair prop (`Factory1Stairs04`) is at **y=4.2** (original designed floor height).
   This means Floor 2 was temporarily raised; the true floor height should be 4.2. Floor 2 must be moved back to 4.2
   before the game is playable, or the stair connection between floors will be broken.
   - (Note: PM has **intentionally deleted** the overlapping `Plate_F1_*` old floor panels — the floor was previously
     a pile of overlapping models. Now floored with "room floor tiles + corridor strips"; verification confirms the
     **walkable surface is fully connected**, see §3 ✅ — not an issue.)

### 🟠 High (Violates mandatory rules / affects generation)
3. **Stair floors misaligned** (mandatory rule: stairwells must share the same x,z).
   - Stair A: F1 center (28,32) vs F2 (26,28) → offset (2,4)
   - Stair B: F1 (2,20) vs F2 (2,16) → offset (0,4)
   The connecting ramp will become a floating diagonal stair and violates the "only stairs + shaft must align" design premise.
4. **STAIRA1 floor distorted by scale**: z 26.41→37.59 (11.18m, not 8m, off-grid). Accidentally dragged scale in editor; needs reset to 4×8.
5. **Two anchor roots coexist**: `TOWER_SLOTS` (19 old anchors, slotId still using old `F1_L1_CentralConstructionHall` naming, different coordinates)
   and `Tower_v3_Whitebox` (34 current anchors). The generator/RoomSlot scan will pick up both sets → duplicates/ambiguity. The old root should be deleted.

### 🟡 Medium (Grid / material / consistency)
6. **2m grid drift**: POWER/SECUR/SAMPLE/TEMP (z=10/14/18) and PUMP/DOCK/REBAR/SHANTY (x=26/30/34/38/42)
   sit on 2m offsets rather than the designed 4m grid. Result: walls/doors between these rooms and 4m-grid neighbors don't align.
7. **Materials half-finished and semantically misplaced**: iron stairs material (`Factory1Stairs01`) is applied to the **LOBBY floor** (not the stairs);
   only individual walls/floors are re-skinned (`Factory2Wall05`/`Concrete044C`/`Factory2Floor02`); **corridor runs are entirely un-textured**.
   Materials are hand-applied per object with no unified "room-type/material standard" convention.
8. **Shaft void not vertically transparent**: F2 shaft void (x12–28, z12–16) is not directly above F1 HALL (z16–24); the vertical sightline gimmick is broken.
9. **All documentation/diagrams differ from the scene**: `abandoned-tower-redesign-v3.md` ASCII and the old diagram `tower_floor1_zh.svg`
   both don't match the real scene (west warehouse position, S-room cluster layout, missing REBAR/PUMP/SHANTY rooms);
   the `tower_layout_v3.json` referenced in code comments as "source of truth" **does not exist in the repository**.

### ⚪ Pending PM Decision (possibly "the other problems" you mentioned)
10. **Silhouette not "abandoned" enough**: design calls for "pinwheel-offset wings + collapsed corner + scaffold"; current state is more of a compact rectangular block with weak wing separation.
11. **Densify rooms (REBAR/PUMP/SHANTY)** — keep or remove? They push each floor's room count to ~17 and all sit on the 2m drift grid.
12. **Building shell**: F1 only has room boxes + hoarding (Fence_N/S/W, missing E?); no real facade/outer walls; indoor/outdoor boundary is unclear.

### ✅ Verified OK (to avoid false positives)
- All 29 connectivity edges on Floor 1 **have geometry**; no island rooms.
- Room footprints on Floor 1 have **no overlaps**.
- **Walkable surface fully connected**: all 16 room floors are reachable from LOBBY (0.6m tolerance);
  every corridor strip connects to at least one room floor; no floating corridors. Floor surface remains walkable after PM deleted the overlapping floor panels.

### Stair Game Elements (PM-placed prefab instances, 8 total)
- `Factory1Stairs03` ×6 (y≈0, ground level): lobby (12,2.5), security/sample (9,14), next to TEMP (4.8,18),
  HALL/Stair-B (12,20.5), loading dock (34,24).
- `Factory1Stairs04` ×2 (**y=4.2**, floor-height clue): Stair-B top (1.2,24.2), Stair-A top (27.4,35.6).
- ⚠ **Duplicate**: two `Factory1Stairs03 (1)` sit at exactly the same coordinates (4.79, 0, 18.0); delete one.
- `Factory1Stairs01` is a **material**, applied to the LOBBY 12×8 floor; the actual stair elements are Stairs03/04 above.

---

## 4. Recommended Next Steps (pending approval)
- A. Fix plan implementation: start with **F1 continuous floor** (blocking issue 1).
- B. Structural cleanup: delete `TOWER_SLOTS` old root; reset Floor 2 to 4.2; align Stair A/B; reset STAIRA1 to 4×8.
- C. Grid correction: move 2m-drift rooms back to 4m grid (will affect adjacent walls/doors; connectivity must be re-verified).
- D. Material standards: define materials for "floor/wall/corridor/stairs" respectively, then apply in bulk (iron stairs material should go on the stairs).
- E. Single source of truth: use this as-built document as the F1 single source of truth; update the v3 design document accordingly; retire the `tower_layout_v3.json` reference.

> Items B/C above involve modifying scene YAML. Per `@AGENTS.md`, this requires explicit PM authorization;
> until authorized I will only produce an "editor manual edit checklist."
