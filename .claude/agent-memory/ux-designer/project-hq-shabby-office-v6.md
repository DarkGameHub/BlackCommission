---
name: project-hq-shabby-office-v6
description: HQ floor plan v6 "破旧事务所+车库湾" — Mars direction retired 2026-07-01, UX audit delivered NEEDS REVISION with 2 concrete fixes
metadata:
  type: project
---

PM retired the Mars-freight-hall HQ direction on 2026-07-01 ("不能用火星白模,他应该就是一个破旧事务所"). New HQ =
small single-storey commission office (8x7m, 2.9m ceiling) + attached garage bay (7.2x8.2m, 4.3m ceiling), pure
interior, front door court-sealed (crew enters/exits only through the garage). Plan source: `tools/hq_shabby_office_plan_v1.py`
(renders to `design/hq/HQ_ShabbyOffice_Plan_v1.png`). Old HQ builders (Mars whitebox, Option A) are parked, not deleted.

UX audit delivered 2026-07-01, verdict **NEEDS REVISION** (structure sound, two concrete fixes needed before build):

1. **Blocking**: 折叠桌 (folding table, cx=1.6 cz=2.5 w=1.2 d=0.8) sits directly in the straight-line path from all
   4 spawn points to the CRT desk — coordinate math confirms the path's x=1.9 line crosses the table's z=2.1-2.9
   band, and the only clear bypass is a ~1.0m lane hugging the west wall (tight for 0.8m-diameter player capsules
   passing each other). Fix: shift table cx 1.6→2.0 (east 0.4m), widening the west lane to ~1.4m clear.
2. **High**: `OfficeMonsterBestiary` (bestiary notebook interactable, confirmed live in `HqOfficePropRestorer.cs`
   under the old HQ as a standalone desk prop) has NO anchor anywhere in the v6 plan — only `OfficeCabinetStorage`
   (mapped to 补给柜) is represented. Needs an explicit placement (e.g. notebook prop on 折叠桌 or near the debt board).

Also flagged (medium/low, non-blocking): the plan's own palette declares 3 of 4 "light-language" anchors (LW tungsten
rest-corner, LG CRT-phosphor-green, LS sodium-garage) but only LC (cold window) is actually pinned to coordinates —
these need to be placed explicitly in the next builder pass since "read the room via light, not UI" is a stated
pillar. Boarding zone (登车区, 2.6x0.9m) is snug depth-wise for a 4-person gather beat, though boarding is confirmed
sequential-per-player via `OfficeDepartureVan.cs` (E to seat, host presses SPACE once all seated) so it is not a
hard blocker. Sealed-door/padlock cues are currently color-only (red) — accessibility checklist requires a
non-color cue (physical tape/padlock geometry) added at the 3D build pass.

Verified as sound (do not undo when iterating): sealed front door forcing garage-only entry (storytelling baked
into circulation), 2.4m curtain width (no bottleneck), muster pad giving arrival buffer right past the curtain,
2.4m open lane between curtain and the van's west face (van does NOT actually pinch the muster→boarding walk,
despite first appearance), and the return "settle beat" sightline (curtain → CRT desk + debt board) — geometrically
the two anchors are ~9deg apart in bearing from the curtain threshold, both comfortably within one glance.

**Why:** PM explicitly wants floor plan approved before building (`production/session-state/active.md` session
2026-07-01 cont.4) — this is the gate before `HqShabbyOfficeBuilder.cs` gets written.

**How to apply:** Before reviewing the next revision of this plan, check whether the table has moved and whether
a bestiary anchor now exists; re-verify coordinate math rather than trusting this summary once the .py source changes.
See [[project-tower-map]] for the unrelated mission-map wayfinding work, and [[user-yan-dai]] for PM's review-style preferences.
