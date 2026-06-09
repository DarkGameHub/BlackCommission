# Active Session State — Black Commission

**Last updated**: 2026-06-09
**Stage**: Production (see `production/stage.txt`)

## Current Focus

Designing the first designated-commission map: **地球海岸壹号·烂尾预售楼**
(2-floor abandoned pre-sale tower, 15–20 min). Level GDD written to
`design/levels/abandoned-tower-earth-coast-01.md`, building on the existing
`Assets/Scene/AbandonedBuilding_Blockout.unity` blockout.

Locked decisions (PM Yan Dai, 2026-06-06):
- Objective = sales scale model (楼盘沙盘) — "the unbuilt Earth dream as a collectible"
- Floor-2 gate = restore power in `F1_S3_PowerRoom`
- Objective carry = heavy two-hand carry (new mechanic)

Two NEW server-authoritative mechanics needed (heavy carry, power gate) — both fall in
the untested-sync risk zone; need EditMode tests.

Floor plan done: `design/levels/abandoned-tower-floorplan.md` — modular kit (4 room
sizes S/M/L/XL on a 4m grid) + slot-based skeleton + random *content* assignment
(fixed corridors/stairs/shaft so NavMesh + netcode stay valid). Stairs A/B vertically
aligned; shaft void crossed by scaffold bridge; objective (TARGET, NW on F2) far from
van (south-center F1).

## Project Snapshot

- **Engine**: Unity 6 (6000.4.7f1), URP, C#, Netcode for GameObjects (host-authoritative)
- **Code**: 77 C# scripts across 9 systems — see `design/systems-index.md`
- **Playable**: HQ + Snow_Lotus_01 (白棘雪莲) mission loop
- **Design docs**: `docs/mvp-core-loop.md`, `docs/world-background-2098.md`,
  `docs/black-commission-long-term-roadmap.md`, `docs/design-decisions.md`,
  `docs/art/black-commission-style-lock-v1.md`

## Key Decisions

- Framework `.claude/` installed (v1.0.0). Runtime layer now installed on macOS:
  `hooks/` (12), `settings.json`, `statusline.sh` — all smoke-tested. Hooks are bash;
  Windows contributors need git-bash on PATH or they silently won't fire.
- Game code stays in `Assets/_Project/Scripts/` (framework `src/` convention maps here).
- `@AGENTS.md` remains the authoritative project rules.

## Session 2026-06-09 — Progression System + Pillar Unification

**Design locked (PM Yan Dai):**
- Progression backbone = 5 license stages (not office level 1-8, not reputation bar)
- Economy = money only (no reputation metric exposed to player)
- Player level is hidden (dev-visible only; drives job pool selection)
- Mission taxonomy = three tiers: 自由采集 / 指定委托 / 黑色委托 (the moral slope)
- Hostile takeover threat = narrative events only (letters, visitors), no 0-100 UI bar
- Endgame trigger = all story missions complete → 移民资格审查 (moral choice, not money threshold)
- Satire delivery mechanism = settlement screen client usage notes, NOT narrator commentary

**Files updated:**
- `design/game-pillars.md` — pillars 5+6 replaced, anti-pillars added, progression backbone table added
- `docs/world-background-2098.md` — Phase 4 officially named 黑色委托, design note added

**Conflict noted (not yet resolved):**
- `docs/mvp-core-loop.md` still describes the old system (reputation, office levels 1-8, acquisition after 2 jobs, 0-100 pressure). Leave untouched until `/design-system` writes a formal progression GDD to replace it.

## Open Questions / Next Steps

1. **Test gap (confirmed: basically untested)** — networked mission state machine,
   settlement math, and sync have no automated coverage. Highest technical risk.
2. Decide next feature push.

## Session 2026-06-09 — Art Bible + Consistency Pass

- **Art bible completed**: `design/art/art-bible.md` — all 9 sections authored and approved.
  Key decisions: warm tungsten amber = primary accent; CRT green restricted to screens;
  lo-fi silhouette-driven darkness; LC = production method only.
- **Canonical art pointer updated**: `design/game-concept.md` now points to art-bible.md.
  `docs/art/black-commission-style-lock-v1.md` is superseded (partially — add deprecation
  header when convenient).
- **7 visual consistency conflicts resolved**: level docs + pillars now use correct lighting
  vocabulary. Full report: `docs/consistency-report-2026-06-09.md`.

<!-- CONSISTENCY-CHECK: 2026-06-09 | Docs checked: 5 | Conflicts found: 7 | Resolved: 7 | Report: docs/consistency-report-2026-06-09.md -->
<!-- ARCHITECTURE-REVIEW: 2026-06-09 | Verdict: CONCERNS | Requirements: 23 total — 2 covered, 2 partial, 19 gaps | Top gaps: mission-state-machine, player-input-controller, tower-navmesh-topology | Report: docs/architecture/architecture-review-2026-06-09.md -->

## Recovery

After compaction or a new session: read this file, then `design/systems-index.md`,
then the relevant docs in `docs/`. Files are the memory, not the conversation.
