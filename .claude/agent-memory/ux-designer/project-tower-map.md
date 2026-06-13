---
name: project-tower-map
description: Tower map (地球海岸壹号·烂尾预售楼) — V6 UX audit complete, V7 wayfinding spec and junction rules authored 2026-06-10
metadata:
  type: project
---

V6 floor plan proposal reviewed 2026-06-10 (F1 + F2 SVGs, generate_tower_floorplans_v6.js, level GDD, art bible).

PM-verified problems: 31 corridor bands cutting through room footprints, 10 corridors stamped onto walls of directly adjacent rooms (zero-width transitions). UX verdict: corridor/room spatial grammar is broken — threshold moments, doorway grammar, and mental-map building all fail in first-person.

Wayfinding system spec delivered (4 layers: thermal/light-temperature coding, construction-stencil breadcrumbs, utility infrastructure as directional system, power-state as global readable status).

V7 junction rules delivered (J1–J6): max 3 visible exits at junction, sightline anchor per junction (non-reused), breadcrumb asymmetric wall dressing, no T-junctions on return spine, stair exit facing requirement, 12m max corridor without intermediate anchor.

Heavy-carry return requirements delivered (H1–H5): min 2m clear approach at every spine doorway, no ambiguous T-junctions, 4-event countdown to van, escort sightline geometry at 4 hardest junctions, whitebox walkthrough test at 0.55x speed required before return spine is locked.

Stair UX notes delivered 2026-06-10 for A/B dog-leg stair redesign (`docs/design/tower-stair-ux-notes.md`). Key outputs: door-entry 1-sec visibility checklists per door geometry (end-wall door A vs long-side mid-wall door B); heavy-carry corner risk matrix with 4 mitigations (2.4m platform, nosing geometry, continuous L-bracket handrail, platform material contrast); B-stair sodium lamp coordinates B-L1(2.0,4.0,20.0) and B-L2(3.8,2.5,19.5) for pre-door light bleed. PM core complaint "看不到楼梯" resolved by P1 priority rule: first run tread silhouette or face must be visible when door is 30% open.

Key art-bible mappings used: warm tungsten amber = inhabited destination, cold industrial = transit, CRT green exit signs = extraction signal, BC routing stencils = breadcrumbs, debt-seizure notices = zone identity.

**Why:** V6 graph topology is sound; spatial encoding of that topology is broken. V7 needs these rules encoded at floor-plan level, not deferred to decoration pass.

**How to apply:** Before reviewing any future floor plan version, check whether corridor bands are in true interstitial space (not inside room footprints) and whether every junction node has a named sightline anchor.
