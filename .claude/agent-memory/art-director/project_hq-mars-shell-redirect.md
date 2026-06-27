---
name: project-hq-mars-shell-redirect
description: PM rejected the Mars-shell whitebox as "too ugly" (2026-06-24); art-direction redirect delivered + FIRST PASS IMPLEMENTED in the builder (form/material-color/north-decay/grazing-light, compiled+built clean). PM verdict: form PASSES, but material "still lacks something" + lighting/atmosphere need another round. Doc + bible §3 still NOT written (gated on look-approval).
metadata:
  type: project
---

**IMPLEMENTATION STATUS (2026-06-24):** The redirect's FORM/MATERIAL-COLOR/DECAY/LIGHT changes are implemented in `Assets/_Project/Editor/HqMarsFreightWhitebox.cs` (north-crest sweep, Section P2 lerp 0.30→0.52 = extrude→sweep, arch12/zSteps72, cold blue-grey `mShell` 0.60/0.69/0.86 + `mShellDark`, north-clustered torn prow/rebar/missing-panels/ribs/conduit/vertical-stains, grazing key 0.62 + `Prow_Rim` shaft). Compiled 0-err, built 0-warn, verified in-scene. **PM accepted the FORM ("还行吧") but flagged: ① material still "差点意思" ② lighting/atmosphere need another pass.** Root cause of ①: I did COLOR-ONLY and SKIPPED the panel-seam texture (the §below "Panels" bullet) — the shell still reads as one poured color, not assembled alien plates. **Next = Stage A (procedural cold panel-seam + value-variation + normal/roughness texture) then Stage B (volumetric god-ray shafts, dust motes, fog/pool-separation, post split-tone, emissive life).** Full staged plan in `production/session-state/active.md` (Session cont.6 → Stage A/B/C). Doc + bible §3 exception (below, BLOCKING) still pending look-approval.


PM (David) built the Mars-freight HQ shell via `Assets/_Project/Editor/HqMarsFreightWhitebox.cs` and rejected it: 「这个建筑整体长得有点太丑了」and「我们是不是要突出这是曾经的火星建筑被遗弃的?」(emphasize it's FORMER Mars architecture, now abandoned). On 2026-06-24 I delivered a buildable art-direction redirect (Part A) + a map/HQ coherence review (Part B), returned in conversation — NOT written to any file yet (PM reviews first).

**The four diagnosed failure modes (root cause in the code):**
1. Symmetric blob — `Ridge[]` (L56-60) holds apex 13.6 mid-span then decays equally both z-ends → centered lump, not a directional gesture. `Section` P2 (L244) fixes the apex at 0.42·span every slice → the arch is *extruded* straight down Z, not *swept*.
2. Faceting — `arch=6`, `zSteps=45` (L183) too low over a ~14m span.
3. Muddy material — `mShell` (L441) is a faint cold tint over warm-grey `CommonConcreteWall02`; warmth bleeds through, reads "gray-brown blob"; UVs (L208) tile concrete = a *poured* read, not assembled panels.
4. Flat murk — single cold key at intensity 0.35 (L372), no grazing/rim light to sculpt the curve.

**The redirect (all map to existing builder params — no new machinery except ONE ≤256px panel-seam texture):**
- Form: north-bias the Ridge apex to a single crest at z≈19-22 (over the door) with a SHORT sharp prow fall-off to ~7.5 at z27; lerp `Section` P2 lateral 0.30→0.52 by z to convert extrude→sweep; bump `arch=12`, `zSteps=72`. Hero silhouette = low west haunch rising to a cantilevered, torn prow cresting over the lit freight door.
- Panels: ≤256px seam-panel texture, ~2.5×2.0m plates following the loft UVs; 3-4 geometry ring-ribs; bake per-panel value variation into the texture. Makes residual faceting read as intentional plating.
- Decay: cluster ALL damage NORTH (torn cantilever + missing panels + exposed inner ribs via skipped skin-quads + crack origin at the prow tear + hanging conduit + Mars regolith dust drift); keep SOUTH/nest intact (squatters chose the soundest corner). Fix `Stain_Run` rotations to vertical gravity-streaks.
- Material: push `mShell` COLD PALE BLUE-GREY, deliberately OUTSIDE the Earth concrete/green/wood/rust palette (Mars=alien). 3 tiers: T1 intact (val 0.62-0.68), T2 weathered (0.42-0.50), T3 exposed structure/ribs (0.28-0.34, dark blue-steel). All S≤0.10.
- Light: raise + re-aim the cold key to ~0.55-0.7 GRAZING NW→SE along the sweep (raking light reveals the parametric flow); add a 2nd cold shaft rimming the broken prow; keep warm tungsten WALLED into the nest pocket; let the apex fade into dark/fog to sell the 1:4.86 dwarf ratio.

**THE LOAD-BEARING RULE (Part B [BLOCKING]):** the art bible §3 says "the rectangle rules everything; no sculpted organic curve allowed" — that governs EARTH. The Mars shell is the ONE sanctioned exception: the dead-alien parametric curve is the antithesis of the human rectangle, on purpose (the §2「扎哈=火星」thesis). It must be written into [[project-hq-exterior]]'s sibling doc / the art bible so no future "consistency sweep" orthogonalizes the curve or warms the shell back into Earth concrete. The nest + props stay 100% bible-orthogonal; shell stays 100% swept; the two vocabularies must never touch.

**Why:** the contrast (cold vast dead Mars shell over a tiny warm cheap Earth nest) IS the game's argument, and a narrative seed for the secret Mars ending (per `design/gdd/map-sequence-and-modular-system.md`).

**How to apply:** if PM approves, the redirect should be written to a new section of `design/hq/hq-architecture-pass.md` (or a companion under design/art/), THEN implemented as a focused builder task in `HqMarsFreightWhitebox.cs`. The exposed-substructure skin-hole is the only fiddly procedural bit (gate specific (r,c) quads out of the BuildShellMesh tris loop + add inner rib-tubes); cheapest fallback = dark recessed "missing-panel" quads with rib-tubes laid proud, avoiding breaking the watertight loft. See also [[art-bible-locations]] for where the governing docs live.
