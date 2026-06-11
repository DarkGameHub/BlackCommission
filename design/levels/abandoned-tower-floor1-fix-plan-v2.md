# Floor-1 Fix Plan v2 — connectivity & LC-alignment pass

> Status: **DRAFT for PM approval (Yan Dai).** No code / no scene edits until sign-off.
> Scope: **Floor 1 only, MVP single-building map.** Companion to
> `abandoned-tower-redesign-v3.md` (design intent) and
> `abandoned-tower-v3-connectivity.md` (the ring/invariant spec). Supersedes the open
> items in `abandoned-tower-floor1-fix-plan.md`.
>
> Final retained connectivity artifact: `Assets/_Project/Art/Maps/Tower_EarthCoast_01/References/Tower_EarthCoast_01_F1_Connected.png`
> Source of truth being changed: `Assets/_Project/Scripts/Level/Topology/TowerTopologyV3.cs`

## Why this pass exists

The v3 **design** is LC-correct (ring backbone, ≥2 routes to each descent, no
critical node on a dead-end, objective↔extraction separation). The **as-built** F1
drifted from it. This plan lists the drift and the minimal edits to recover it.

LC principles referenced (per `@AGENTS.md`, method-only): repeatable rituals,
readable staging, **fair co-op chase tension (loops, not single chokepoints)**,
darkness with clear navigation, risk-graded loot.

---

## Findings → fixes (prioritized)

### P0 — connectivity / fair-chase (the LC core)

**P0-1 / P0-2. The west side is a spine, not a ring; Stair-B dead-ends behind POWER.**
- As-built `STAIRB1` has exactly one fixed edge `E-PWR-SB` (POWER→Stair-B); its only
  alternative `T5` (COLLAPSE↔STAIRB1) is *toggleable*. The east side already rings
  Stair-A (`E-H-SA` + `E-FORE-SA` via DORM/FOREMAN), but the west has no loop.
- Violates connectivity-spec §5.1 (ring not spine), §5.3 (no critical descent on a
  dead-end), and weakens invariant **I7** (no single campable descent): camping the
  POWER corridor isolates a whole stair.
- **Fix:** promote **`T5` (COLLAPSE↔STAIRB1)** and **`T6` (COLLAPSE↔FOREMAN)** from
  `Toggle` → `Fixed`. This forms the west-north ring
  `LOBBY→POWER→STAIRB1→COLLAPSE→FOREMAN→DORM→HALL→LOBBY`, giving Stair-B a second
  interior route that does **not** use the POWER lane. Uses corridors that already
  exist — no new corridor authored.

**P0-3. COLLAPSE becomes an island the moment it is promoted to a room.**
- `COLLAPSE` currently has only `T5` + `T6` (both toggles). It is `Kind.Collapse`
  (no floor, excluded from invariant **I8** "every *room* reachable"). The moment we
  add a floor (your request) and it counts as a room, an all-toggles-closed seed
  orphans it → I8 fails. The topology code already added anchors for SECUR/CANTEEN
  for exactly this reason but missed COLLAPSE.
- **Fix:** the P0-1/2 promotion of `T6` to fixed **is** the anchor — COLLAPSE then has
  ≥1 fixedOpen edge. Promote COLLAPSE to `Kind.Room` (size L) only after that edge is
  fixed. (Editor tool already staged: `Tools ▸ Black Commission ▸ MVP ▸ Tower ▸ F1 -
  Add COLLAPSE Room`.)

### P1 — readability / geometry

**P1-4. The big black 过道 (the T6 run) is a 6.9 × 15.6 m blob, not a corridor.**
- Connectivity-spec §5.2 mandates 4 m corridors. As-built it reads as a room and
  bakes a messy NavMesh. (Already flagged in v1 fix-plan.)
- **Fix:** rebuild the COLLAPSE↔FOREMAN run as a clean 4 m corridor when it is
  promoted to fixed.

**P1-5. SECUR overlaps the LOBBY→POWER corridor.**
- `SECUR (8,10,4,4)` sits on the power-corridor `Run` lane (x4–12, z10–14).
- **Fix:** shift SECUR off the lane (proposal: keep its `E-SECUR-TEMP` / `T1`
  links, move it clear of z10–14). Exact target coord to confirm at scene-edit time.

### P2 — balance / variety

**P2-6. Risk gradient inverted: the heaviest carry is the safest grab.**
- `WAREHOUSE` holds **2H(140)** but sits at (0,−8), adjacent to van/lobby = safest
  tile. LC: richest/heaviest loot goes deepest.
- **Fix:** move the 140 two-hand haul to a deep room (DOCK is already deep-east at
  110; or north FOREMAN), leave WAREHOUSE as batteries/consumables.

---

## Proposed topology edits (concrete — `TowerTopologyV3.cs`, F1 only)

| Edit | Before | After |
|------|--------|-------|
| T5 | `Toggle("T5","COLLAPSE","STAIRB1",Corridor)` | `Fixed("E-COLL-SB","COLLAPSE","STAIRB1",Corridor)` |
| T6 | `Toggle("T6","COLLAPSE","FOREMAN",Corridor)` | `Fixed("E-COLL-FORE","COLLAPSE","FOREMAN",Corridor)` |
| COLLAPSE node | `Kind.Collapse` (no slot) | `Kind.Room`, size L (gets floor + RoomSlot) |

Net: 9 → 7 F1 toggles (still ample run-to-run variety), and the two promoted edges
were "load-bearing" toggles anyway — making them fixed is the correct call.

## Connectivity impact (invariants, all-toggles-closed case)

- **I7** (≥2 of 3 descents): unchanged-safe; Stair-B now has 2 fixed routes.
- **I8** (no island room): COLLAPSE now anchored → safe to be a room.
- Critical path I1–I6: unchanged (no critical edge altered).
- **EditMode tests to update:** add a case asserting COLLAPSE reachable with all
  toggles closed; re-run `topology_noIslandRooms` / `topology_atLeastTwoDescents`.

## Geometry edits (scene, after sign-off — needs explicit YAML go-ahead per @AGENTS.md)

1. Shrink COLLAPSE↔FOREMAN run to 4 m (P1-4).
2. Add COLLAPSE floor + RoomSlot (P0-3) — via the staged editor tool.
3. Move SECUR off the power lane (P1-5).
4. Re-bake NavMesh.

## Open decisions

1. P2-6 loot move — into DOCK, or a north room? (affects systems/loot table)
2. Scene edits: do them via editor tools/checklist, or grant YAML edit go-ahead?
