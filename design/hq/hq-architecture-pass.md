# HQ Architecture Pass —「废弃火星轨道货运堂」纯室内 (Option B)

**Date:** 2026-06-24
**Status:** LOCKED (concept + dimensions + interior-only) — awaiting only the builder delta. Supersedes the earlier「弃用市政验车站」municipal framing of this same file; that concept is RETIRED. The section-led method and the interior Z-spine ritual are KEPT.
**Drawings:**
- Section (hero): `design/hq/HQ_Section_AA_v3.png` — render `python tools/hq_section_v3_interior.py`
- Plan (companion): `design/hq/HQ_SitePlan_v5.png` — render `python tools/hq_siteplan_v5.py`
- The PM-approved freight section `design/hq/HQ_Section_AA_v2.png` is kept unchanged as the size reference v3 reverted to.
**Provenance:** concept + Option-B + size lock came from a 4-lead review (art-director, level-designer, game-designer, technical-director) plus a 2-lead size-revert pass (art-director + level-designer). PM (David) locked the concept, then locked the dimensions ("行，继续做吧").
**Governing specs:** `design/art/spatial-language-spec.md` (§1 — players read the 截面), `design/art/hq-exterior-art-direction.md` (sodium anchors, asymmetry, dead palette), `design/art/art-bible.md`, `AGENTS.md` (Municipal Debt Noir; the dispatch ritual).

---

## 1. The unlock — section-led, not a zoning plan
Three earlier top-down plans were rejected as「没有设计感」. Our own PM-locked `spatial-language-spec.md` §1 says:

> 玩家看不到平面图——他们读的是**截面**：宽度、天花高度、顶部轮廓、门洞比例、光的种类。**几何语法先于道具语言。**

A floor plan *structurally cannot* show ceiling compression (压顶), volume rhythm, threshold proportion, or the light-and-dark pattern that ARE the spatial identity in a first-person flashlight game. So this pass leads with a **section** and treats the plan as its companion — the HQ is now designed the way the project already designs the Tower.

## 2. Sense of place —「扎哈 = 火星」(the characterful idea)
The broke surface-retrieval agency **squats in a derelict, contaminated abandoned MARS ORBITAL-FREIGHT DEPOT (「货运堂」)** — a dead piece of off-world municipal infrastructure that Mars walked away from. The agency cannot afford it; it just occupies the bottom of the dead hall.

**The thesis is the contrast, and the contrast is the architecture:**

| | The DEAD SHELL (火星弃物) | The NEST (人类对位) |
|---|---|---|
| Authorship | late-Zaha fluid **parametric** envelope — swept, curved, cold, expensive, alien | cheap **orthogonal** box, hand-bolted into the corner |
| State | contaminated, dead, vast, unmaintainable | small, warm, lived-in, salvaged |
| Reads as | what Mars discarded | what Earth's indebted poor can still afford to occupy |

The dead Mars cathedral above + the tiny warm human nest below is BOTH the section's design drama AND the game's fiction (Earth's broke squatting in Mars's ruins). This is the "real place last maintained years ago" test from the exterior-art doc, passed at the concept level.

## 3. Parti & focal hierarchy
**Parti = a tiny orthogonal nest occupying the floor of one vast curved dead shell** — figure-ground IS the thesis. From spawn the eye runs the deep interior enfilade: warm CRT corner → down the cold hall under the dead conveyor → muster under the shell-crack daylight → the van in its sodium pool → and through the rolled-up freight door, a **framed picture** of the dead Mars yard beyond (never entered).

Focal hierarchy (brightest / most contrasted → recedes):
1. **The van in interior sodium** — the ritual object, staged in the loading bay.
2. **The CRT job terminal** — the decision point (phosphor green = the only "live signal" colour on site; also where you settle).
3. **The debt / takeover board** — the pressure, deliberately on the path between spawn and the computer.
4. **The framed door-view** to the dead Mars yard — the thesis made visible; the horizon you can see but not reach.
5. Everything else falls into noir shadow.

## 4. The promenade — the dispatch ritual, ALL under the shell (Option B)
Compression/release, light/dark — the beats the section draws. **Everything happens inside**; the door is a threshold you look through, not walk out of.

**Depart:** ① spawn in the low, warm, orthogonal nest (desk-lamp corner) → ② commit a job at the CRT, the debt board unavoidable on the way → ③ past the half-empty gear wall → ④ muster on the pad under the *contaminated daylight* of the shell crack (the gather beat, the one cold-bright pool) → ⑤ board the van in the interior sodium bay → ⑥ the freight roll-up rises (revealing the whole van head + a sliver of dead sky) and the van pulls out through the threshold — **departure is a screen overlay**, the player never drives the yard.

**Return** reverses and flips the valence: the van rolls back **in** through the door → parks the bay → you step down and walk back down the cold hall → **past the debt board** (the reckoning) → settle at the CRT, the same desk where you took the job. The homecoming walk-in to the settlement desk is the strongest broke-office beat, and it is the reason settlement is at the CRT (§10).

## 5. Section logic — the locked dimensions (体积韵律 + 压顶 + the dwarf ratio)
All values are FINAL metres (post the builder's `HqScale ×1.2`). These are LOCKED.

| Element | Final metre | Why |
|---|---|---|
| **Shell apex** (火星货堂顶) | **13.6 m** | the vast cold dead volume; the top of the dwarf ratio |
| **Hall net width** | **11 m** | reads wide-but-not-warehouse; companion plan shows this |
| **Total depth** | **~27 m** | nest ~6 + hall ~21; the deep enfilade |
| **Freight roll-up door** | **4.8 m** tall | the ONLY size change from v2 (4.6→4.8): the rising door reveals the whole van head + a sliver of sky — zero build cost |
| **Nest footprint** | **6.6 × 7.0 m** | tiny; never enlarged |
| **Nest roof** | **2.8 m** | the warm human ceiling; the bottom of the dwarf ratio |
| **Dwarf ratio** | **13.6 : 2.8 ≈ 1 : 4.86** | ≈ Zaha DDP; already oppressive — going higher only makes the number bigger and the silhouette squatter |

- **An enlargement trial (15.5 / 13) was built and REJECTED by the PM as「不需要这么大」.** Two leads independently converged on reverting to the v2-grade size. Height AND width revert as a group (a tall shell on a too-wide footprint reads squat) — both reverted.
- **压顶**: the tenant's self-installed dead-conveyor / cable bundle runs low over the central walkway, giving the cold hall a corridor identity (spec §2) and telling the broke-retrofit story at once. Pools of light are *separated by required dark* (spec §2「必须留暗区」).
- **The nest is never enlarged.** A vast shell over a tiny nest IS the thesis; enlarging the nest weakens the contrast. This is a hard rule.

## 6. Option B — interior-only (LOCKED, unanimous 4-lead)
**The whole ritual — muster, board the van, settle — happens UNDER the shell.** There is no playable outdoor yard.

- **The freight roll-up door is a THRESHOLD / framed picture, not a path.** Beyond it is a **NON-WALKABLE Mars-scrapyard BACKDROP** (dead sodium pole silhouettes, an abandoned cargo container, a ruin landmark, dead-ground haze) seen *through* the open door, never entered.
- The van moves from just outside the door (z≈22 in build space) to the interior bay by transform only; departure is already a screen overlay, so this costs **near-zero new code**.
- **Why B won:** cutting the outdoor removes ≈ −150 builder lines, −1 NavMesh surface, −1 Terrain object, and an entire class of "院子怪怪的 / 左右后面空" boundary problems — while *strengthening* the thesis (the dead yard you can SEE but never own is more pointed than a yard you walk). The section already frames it; the plan companion confirms it (`HQ_SitePlan_v5.png`).

### Two tradeoffs accepted (logged at PM request)
1. **Hostile-takeover / repo-agent staging** moves to the door / threshold (a notice taped to the roll-up, a repo silhouette in the framed yard-view) instead of an outdoor confrontation — estimated ~80% of the dramatic effect at zero outdoor cost.
2. **The outdoor return-decompression beat** (the single strongest argument for an exterior — the relief of re-entering the lit hall from the dark) is either *accepted as a loss* or substituted **inside** by an interior threshold + the walk-back-down-the-cold-hall + the door-rolling-shut overlay. The return promenade in §4 is the chosen substitute.

## 7. Materiality & wear-storytelling
- **The dead shell (火星弃物):** clean, cold, late-Zaha **parametric** concrete/composite — fluid swept surfaces, now contaminated: bio-stain runs, a structural **crack** leaking grey daylight, a dead overhead conveyor, a **broken cantilever** torn out over the door. Expensive geometry, zero maintenance. The shell never reads warm.
- **The nest (人类对位):** cheap orthogonal salvage — mismatched desks, CRT + taped cable nest, debt board = a repurposed notice board, a space heater, a half-empty gear rack. Warm tungsten only here.
- **Freight-depot detritus:** the abandoned container, dead loading-bay markings, a leaning Y tree-column (volunteer growth through the dead floor), the foreclosure story (ghost signage, a taped eviction/takeover notice on the door).
- **The one cared-for thing:** the van — clean, warm headlights, the office's logo hand-painted over a freight stencil.
- Deferred-maintenance decay lives in the **periphery and the shell**, never on the action spine.

## 8. Light-anchor contracts (spatial-language-spec §4 format) — INTERIOR ONLY
| Anchor | Position (build-space, pre-scale) | Colour | Contract |
|---|---|---|---|
| Desk lamp | nest CRT desk (≈1.4, 1.4) | warm tungsten `#EC C4 78` | habitation warmth; the spawn-corner pool — the only warm light on site |
| CRT phosphor | screen (≈1.0, 0.6) | `#96 E2 8C` | the ONLY live-signal colour; job select + settlement |
| Crack daylight | muster (≈4.5, 15.6) | cold contaminated grey `#C6 D2 C8` | the one cold-bright pool — the "gather" beat lands here |
| Van-bay sodium | interior bay (≈7.2, 22.0) | `#E0 B0 5C` | the brightest staged pool — the ritual object (the van) |
| (backdrop) yard sodium | beyond the door (silhouette only) | dim sodium | NOT a playable anchor — a silhouette in the framed view |
**Rule:** pools separated by required dark (spec §2). Interior = tungsten (the nest, home) + one cold daylight crack + one sodium bay. Asymmetric: warm SW nest corner, cold hall, sodium bay. No outdoor light rig.

## 9. Playable boundary
Playable = **the office nest + the freight hall interior ONLY.** The one opening is the freight roll-up door (a threshold; the van leaves via overlay, the player cannot follow). Everything beyond the door is `背景·不可进入`, drawn as such in the plan. The hard edge is *believable* (a dead off-world yard you can see through the door) and needs no fenced-field justification.

## 10. Settlement location — at the CRT inside (LOCKED)
Settlement happens **at the office CRT**, the same desk where the job was taken. The homecoming walk past the debt board to that desk is the strongest broke-office beat and keeps the whole loop interior-consistent. This sets the return route's endpoint (§4) and the builder's settlement-trigger collider (§11). (Alt considered: settle at the van — faster, loses the walk-in. Rejected.)

## 11. Builder implications (`Assets/_Project/Editor/HqOptionAProductionBuilder.cs`)
> **VERIFIED 2026-06-24 (read of the 937-line builder):** the current builder produces the **OLD「Option A」municipal concrete-wedge** in full, with **ZERO** Mars-freight geometry. Constants: `UnitW = 9` (not 11), `DepthShort/Long = 17 / 19.5` (not ~27), `WallH = 3.6` (not 13.6), `DoorW/H = 4 / 3.2` (not 4.8). `BuildShell` lays simple Floor/Roof/Wall **boxes** + a canted far wall with a 4 m roll-up; `BuildInterior` drops `AS_Office*` props **directly on the wedge floor — there is no enclosing NEST box**; then `BuildYard` + `BuildWilderness` + `BuildVegetation` + `BuildHqTerrain` build the outdoor Phase-2 stuff. There is no swept/faceted shell, no 13.6 m apex, no nest enclosure, no dead conveyor / broken cantilever / shell crack / leaning column.

**Therefore the Mars-freight + Option-B build is NOT a tweak — it is a from-scratch rebuild of `BuildShell` + `BuildInterior`.** Honest scope:

| Work | Detail | Size |
|---|---|---|
| **Rebuild `BuildShell`** | replace the 9×19.5×3.6 wedge with the faceted swept Mars envelope: curved EAST wall + slanted NORTH loading wall, apex **13.6 m**, width **11 m**, depth **~27 m**, door **4.8 m**; add the dead conveyor, broken cantilever, shell crack/skylight, leaning Y-column (all faceted boxes/prisms — no NURBS) | LARGE (~the whole method) |
| **Build the NEST** in `BuildInterior` | a small **6.6 × 7.0 m, 2.8 m-roof orthogonal box** bolted into the SW corner, with its front-wall door gap; re-home the `AS_Office*` props + CRT + debt board + folding table inside it; gear wall + muster on the hall floor as today | MEDIUM |
| **Move the van + `VanBoardZone` + `ScavengeCargoZone` INSIDE** | from z≈22 outside → the interior loading bay; settlement-trigger collider at the CRT (§10) | SMALL |
| **Delete / gate `BuildYard` + `BuildWilderness` + `BuildVegetation` + `BuildHqTerrain`** | Option B cuts the exterior; the only thing past the door is a **non-walkable backdrop** (container, dead pole silhouettes, ruin landmark, haze) — no NavMesh, no colliders past the threshold | MEDIUM (mostly deletion) |
| **Re-point lights to the interior bay** | interior rig only (§8): nest tungsten, CRT phosphor, one cold crack-daylight on the muster, one sodium bay; drop the outdoor lamps | SMALL |
| `HqScale ×1.2` parents, retire-legacy, anchors, post/fog | mostly carry over | carries over |

This is a multi-hundred-line structural rewrite of a generated **editor** builder; it needs Unity open to validate (Build menu → walk it → smoke test). **Recommend gating it behind PM sign-off on the now-final SECTION (`HQ_Section_AA_v3.png`) + PLAN (`HQ_SitePlan_v5.png`) + this doc**, then implementing it as its own focused task — the builder should be built *to* the approved design, not ahead of it.

Tuning numbers (key/ambient/fog/sodium intensities, sat/contrast, soft shadows, dead-straw palette, `Flat()` smoothness, terrain pad) carry over from the art-bible-compliant pass already applied this session and from `design/art/hq-exterior-art-direction.md` §2/§6.
