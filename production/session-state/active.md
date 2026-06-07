# Active Session State — Black Commission

**Last updated**: 2026-06-06
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

- Framework `.claude/` installed into the project; hooks intentionally skipped (Windows).
- Game code stays in `Assets/_Project/Scripts/` (framework `src/` convention maps here).
- `@AGENTS.md` remains the authoritative project rules.

## Open Questions / Next Steps

1. **Test gap (confirmed: basically untested)** — networked mission state machine,
   settlement math, and sync have no automated coverage. Highest technical risk.
2. Decide next feature push.

## Recovery

After compaction or a new session: read this file, then `design/systems-index.md`,
then the relevant docs in `docs/`. Files are the memory, not the conversation.
