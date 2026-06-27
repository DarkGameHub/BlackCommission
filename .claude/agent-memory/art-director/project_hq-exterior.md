---
name: project-hq-exterior
description: HQ exterior art direction doc authored 2026-06-22; PM complaint was no vegetation + warehouses feel like boxes; spec written to design/art/hq-exterior-art-direction.md
metadata:
  type: project
---

PM (David) raised two blockers for the HQ outdoor environment on 2026-06-22:
1. NO vegetation in the dispatch yard or derelict district.
2. The 20 warehouse backdrop blocks "feel like just blocks."

**Why:** The outdoor was built as a Phase 1 placeholder by `HqOptionAProductionBuilder.cs`. Phase 2 = full exterior pass.

**How to apply:** Full spec is at `design/art/hq-exterior-art-direction.md`. The implementation order is prioritized there (vegetation first, lighting second, then debris/dressing, then hero warehouse treatment). Do not touch the Phase 1 office interior — it is signed off.

Key decisions locked in the spec:
- Vegetation uses `Assets/Foliage Free/` (pine1a/b, pine2a/b, ferns.fbx) + FoliageSet.asset, same dead-olive palette as MapSiteBuilder.cs mission site (cTrunk `#332919`, cDead `#2C3124`). No TreePackVol.1 (not found).
- Yard lights replaced: symmetric 2-light → asymmetric 3-light sodium amber `#D9A850` (gate, van bay, west-flank dim).
- Hero warehouses: 3 blocks facing the yard gate get broken windows (WindowsBroken01.mat), lean-tos, rooftop equipment, DecalsDirt01 rust streaks, DoorIndustrial01 loading docks.
- No vegetation in van lane (x 2–7, z 18–44). All foliage collider-free.
