# HQ Site Plan v2 — Office + Dispatch Yard + Designed Wild

**Date:** 2026-06-24
**Status:** DRAFT — awaiting PM approval (extends the interior-only "Option A" to the whole site)
**Diagram:** `design/hq/HQ_SitePlan_v3.png` (render with `python tools/hq_siteplan_v2.py`)
**Provenance:** 4-discipline huddle — ux-designer, game-designer, level-designer, art-director (2026-06-24), synthesized by lead. Compare against `design/hq/HQ_Option_A_LongAxis.png`.

---

## 1. Why this exists
The PM walked the built HQ and flagged three things: 太丑, the ground/grass Z-fight, and "doesn't match the floor plan." Root cause: the locked **Option A** plan is *interior-only* (9×19.5 m wedge). The 2026-06-23 "野外" directive bolted a 220×220 m wilderness + fenced yard onto it that was **never spatially designed**, so in-game it reads as a gray box marooned on an infinite bright-green lawn. This plan designs the **whole site** coherently so the build has a real target for the outdoor + grass — not a blank wilderness.

The interior is NOT being redesigned. The fixes are all **outdoor + proportion**.

---

## 2. Interior — LOCKED (Option A), with proportion corrections only
The Z-spine ritual is correct and unchanged: **spawn line (S) → CRT/job-select (SW) → gear wall (E) → muster pad (center-N) → roll-up door (N)**. The "target always visible from spawn" sightline is preserved.

Audit-compliance corrections (from `production/qa/hq-proportion-audit.md`):
- **Ceiling too tall:** `WallH` 3.6 → **2.85 m** (post-scale 4.32 → **3.42 m**). This is the main reason the building feels toy-scaled — a real single-storey civic depot, not a warehouse.
- **Roll-up opening:** `DoorH` 3.2 → **2.6 m** (post-scale 3.12 m — still clears the 2.83 m post-scale van).
- The floating computer sign-backplate the audit flagged at 2.56 m retires with the legacy objects (already handled by `RetireLegacyOffice`).

---

## 3. Dispatch Yard — the designed outdoor (24 × 26 m, enclosed)
A **stage, not a field**: board the van, watch it depart, watch it return, walk back in. Players step into the yard; everything past its walls is non-traversable backdrop.

- **Raise the perimeter walls 1.3 → 2.0 m** (above 1.7 m player eye height). This is the single biggest "box-on-a-lawn" fix — the current 1.3 m walls vanish below eye line so the yard reads as open ground.
- Single **6 m exit gate** in the north wall — the van's gate, not the player's (no `[E]` prompt for players; the closed gate + no affordances keep players from following the van out).
- Van parks in the marked **bay just outside the roll-up door**, nose in. The van is the office's one well-kept asset — brightest, warmest sodium pool over the bay.
- **NEW east-flank equipment shed** (≈4 × 6 m) breaks the flat east wall (west already has the powerbox) → the yard reads asymmetric and industrial, not like a fenced lawn patch.
- **3 sodium pools** (gate / van bay / west flank) with readable darkness *between* them.

---

## 4. The Wild — ground bands + a closed horizon
The lawn problem is literally the grass-density map: `HqGrassDensity()` returns a uniform 14–21 **everywhere**. Split it into distance bands from site center (4.5, 30):

| Band | Extent (radius) | Ground treatment | Grass density |
|---|---|---|---|
| Worn apron | 0–10 m around yard | near-black compacted/asphalt `(0.11,0.10,0.09)` | **0** (cleared) |
| Scrub mid-band | 10–45 m | dead-straw / olive bare earth | 5–10 (sparse) |
| Full wild | 45–65 m | wild grass + dry fern | 14–21 |
| Dead treeline | 70–90 m ring | dark soil, dead pines (`cTrunk`) | 0 (trees only) |
| Beyond | >90 m | fog-dissolved | — |

Close the horizon believably (read as "office alone off a back road," not an infinite plain): **dead treeline + fog end 90 m + raise terrain undulation (`HRANGE` 8 → 10) + bigger boulders**. The 220 m terrain **stays** (the van drives the north dirt track to Z≈150) — only the *visual* edge tightens.

**CUT:** the warehouse district (never reached — the treeline does its job) and the thin black gizmo-fence visible in the screenshot.

---

## 4.5 Playable boundary — a tight, bounded hub (spawn-design research, PM-requested 2026-06-24)
Comparable co-op hubs are **tight, enclosed, purpose-dense, with a hard believable edge** — never an open building on empty ground:
- **Lethal Company** — a small *enclosed* ship; terminal one end, exit door the other; "outside" is a railed platform / the mission, never empty lawn.
- **Deep Rock Galactic** — spawn in a cabin → a dense *enclosed* hangar of stations; "outside" is space, a hard backdrop you can't enter.
- **Level-design boundary theory** — a *believable hard edge* (walls / terrain / treeline) beats invisible walls or an open void.

**Decision:** the **playable area = office interior + the enclosed dispatch yard ONLY.** Dead woods + an earth berm press CLOSE on all four sides as **non-playable backdrop** (the believable hard edge) — the office is *nestled in the wild*, not marooned on a green disk. One controlled opening north (the van's gate + dirt road; gate closed + no `[E]`, so players can't follow). The diagram marks 可进入 vs 背景·不可进入 explicitly. This resolves the PM's "left/right/behind is empty" (the wild now closes in) and "can players enter?" (no — the boundary is hard and labelled).

## 5. The loop — including the return→settlement beat (the real gap)
**Depart:** spawn → CRT (pick job, debt board in view) → gear wall → muster → `[E]` roll-up door → board van → out the gate (commitment point).

**Return (currently undefined in the build):** van re-parks in the same bay → team walks **south** through the yard → back through the (open) roll-up door → **past the debt board** → settles at the CRT. The 3-beat decompression (gate → yard → walk in) mirrors the 3-beat prep (muster → door → board) — that symmetry is what makes it a ritual rather than an errand.

---

## 6. Economic fantasy, expressed in space (no UI)
- **Half-empty gear wall** at the start; it fills as you spend money the office can't spare.
- **Debt board unavoidable** on the spine, between spawn and the computer — "you owe this much" while you pick a job.
- **Deferred maintenance** in peripheral zones only (one dead ceiling tube, a cracked floor patch, a sheeted-over window) — never on the action spine.
- The **van is the one clean thing** on site.

---

## 7. OPEN — PM decision required
**Settlement location.** Recommended: **at the office CRT inside** — the homecoming walk past the debt board to the same desk where you took the job is the stronger broke-office beat. Alternative: at the van in the yard (faster, but loses the walk-in). This sets the return route's endpoint. *(Flagged by game-design; lead concurs with CRT-inside.)*

---

## 8. Implementation appendix — builder changes (`HqOptionAProductionBuilder.cs`)

**Already applied this session (art-bible compliance pass, uncommitted, awaiting rebuild):**
soft shadows on the sun + CRT light; grass colors → dead-straw/olive; interior warm-tungsten over the spine + brighter desk lamp; CRT light → phosphor green `#6CFF5F`; ambient 0.78 → 0.60; fog start/end 16/120 → 22/90 + darker color; bloom 1.05/0.40/0.6 → 1.2/0.28/0.4; `Flat()` smoothness clamp; yard ground → near-black asphalt; dirt-track recolor; **terrain pad recessed −0.12 m (kills the ground Z-fight)**.

**To do for this plan (structural + tuning):**
| Change | From → To | Source |
|---|---|---|
| `WallH` | 3.6 → 2.85 | level-design (proportion) |
| `DoorH` | 3.2 → 2.6 | level-design |
| Yard perimeter wall height | 1.3 → 2.0 | level-design / ux (enclosure) |
| `HqGrassDensity()` | binary flat-mask → distance bands (apron 0 / scrub 5–10 / wild 14–21 / treeline 0) | art / level-design |
| `HRANGE` (terrain undulation) | 8 → 10 | level-design / art (horizon) |
| `HqFlatMask` outer fade | 10 → 6 | level-design (sharper apron edge) |
| East-flank equipment shed | add Box in `BuildYard` | level-design |
| North dirt track south end | extend to Z≈50 (close gate→track gap) | level-design |
| Boulders | +30–40 % size | art (read through fog) |
| Warehouse district code | remove from rebuild | art (cut) |
| Tree material override + ring density | enforce `cTrunk`, vary fallback radius | art |
| Exterior mood tuning | key 0.4→0.25, ambient 0.60→0.55, fog→`#1E1E19`, moon fill→0.45, sodium gate→0.85, sat −15 / contrast +10, roof vent caps for silhouette | art |

Full art numbers: art-director output, 2026-06-24. See `[[art-bible-locations]]` for the governing specs.
