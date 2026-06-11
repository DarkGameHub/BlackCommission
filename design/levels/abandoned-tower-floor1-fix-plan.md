# Abandoned Tower Floor 1 · Fix Plan (Doors + Overlaps + Slots)

> Covers **Floor 1 only**. Based on the real scene verification in `abandoned-tower-floor1-asbuilt.md`.
> Implementation method: Editor menu tool `Tools/Black Commission/MVP/Tower/F1 ...` (Unity not available
> on this machine; run the tool via menu on your machine, affects only this floor, preserves
> layout/materials/stairs, re-runnable, undoable).

## 1. Standard Doors (PM decision: use the two imported industrial doors; door openings left clear, no lintel)

| Use | Prefab | Clear Width | Opening Width | Height |
|---|---|---|---|---|
| Wide door (main routes / large rooms / corridor entries / stair entries) | `Assets/TirgamesAssets/Factory/Prefabs/DoorIndustrial01_1` double-leaf | ≈2.36m | **2.4m** | 2.17m |
| Narrow door (small rooms) | `Assets/TirgamesAssets/Factory/Prefabs/DoorIndustrial01_2` single-leaf | ≈1.16m | **1.2m** | 2.17m |

Rules:
- Existing opening **width ≥2.3m → wide door (2.4)**; **1.0–2.3m → narrow door (1.2)**; **<1.0m gaps → seal** (eliminate 0.1/0.2m stray gaps).
- Center the door opening at the original opening position; install door flush with wall, centered (single-leaf door centered on hinge offset); opening above the door is clear, **no lintel**.
- Door faces along the wall; height stays at 2.17m; only scale door width to match the opening width.

## 2. Overlapping Floor / Geometry Fixes

**Tool auto-fixes (safe, unambiguous):**
- **VAN floor** west edge x10.2 → 12 (no longer overlapping the west warehouse by 1.8m).
- **STAIRA1 floor** reset from distorted 11.2m back to wall dimensions **4×8** (center z=32).
- Delete the **duplicate stair prop** `Factory1Stairs03 (1)` at (4.79,0,18) — one of the two instances.
- Delete the entire old root **`TOWER_SLOTS`** (19 old invalidly-named slots).

**True conflicts requiring your call (tool flags but does not auto-fix):**
- **SECUR room and the LOBBY→POWER corridor (E-LPWR) occupy the same space** ([8,10]-[12,14]). This is a genuine room-over-corridor conflict;
  auto-cropping would break connectivity — either move SECUR or reroute the corridor.
- **T6 connector is a large 6.9×15.6 slab** (collapse corner ↔ foreman), not a corridor. Recommend deleting and redrawing as a standard 4m corridor.

## 3. Slots
- Delete the old `TOWER_SLOTS` root. Keep the v3 slots under `Tower_v3_Whitebox` (already centered in rooms).

## 4. Floor Plan
- Use **walls** as room boundaries throughout (confirmed walls ≈ floors; only STAIRA1 floor needs reset); redraw `tower_floor1_accurate.svg` based on walls after fix.

## 5. Implementation Steps
1. Run `F1 - Cleanup` in Unity (VAN/STAIRA1/duplicate stairs/delete old slots).
2. Run `F1 - Install Doors` (unify door openings + install wide/narrow doors).
3. Review results; resolve SECUR/T6 two true conflicts separately.
4. Redraw Floor 1 floor plan after fix.

> Tool written blind (Unity not available on this machine). **First run requires verification in the editor** (door orientation/position may need minor adjustment); re-runnable.
