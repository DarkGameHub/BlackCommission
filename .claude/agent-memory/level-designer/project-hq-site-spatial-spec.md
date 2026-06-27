---
name: project-hq-site-spatial-spec
description: HQ home hub site spatial spec (2026-06-24) — wedge interior + dispatch yard + wild; builder-mapped constants
metadata:
  type: project
---

PM requested a full site spatial spec (interior + yard + wild) to fix the "box on a flat lawn" read. Deliverable is a zone table + dimension spec for diagram rendering and builder update.

**Why:** The runtime build diverged — wedge is correct but the 24x26 m yard has no designed enclosure, and the 220x220 m terrain with no visual boundary reads as a test lawn.

**How to apply:** All dimensions are pre-HqScale (builder applies 1.2x to Shell/Interior/Dressing/Yard but NOT Wilderness). Key fix: WallH should drop from 3.6 to 2.85 m so post-scale height = 3.42 m (within audit 2.6–3.6 m target). Yard keeps 24x26 but needs the enclosure (existing wall code) clearly designed + a second structure on the east flank. Wild visible extent should be limited by fog to ~90 m; treeline at 70-92 m ring closes the horizon.

Spec delivered: 2026-06-24.
Builder file: `Assets/_Project/Editor/HqOptionAProductionBuilder.cs`
Plan image: `design/hq/HQ_Option_A_LongAxis.png`

[[user-yan-dai]]
