# Epics Index

> **Last Updated**: 2026-06-09
> **Engine**: Unity 6 (6000.4.7f1)
> **Note**: Epics define scope. Stories define implementation steps.
> Run `/create-stories [epic-slug]` per epic to generate implementable work.

| Epic | Layer | System | GDD | Stories | Status |
|---|---|---|---|---|---|
| [Mission State Machine](mission-state-machine/EPIC.md) | Core | Mission / Office / Progression | `design/gdd/mission-state-machine.md` (not yet written) | Not yet created | Ready (stories Blocked — needs mission ADR) |

## Prerequisite Chain

Before stories in any epic can reach `Ready` status:

```
/design-system [system]          → writes GDD
/architecture-decision [title]   → writes ADR, unblocks stories
/create-control-manifest         → writes manifest (story manifest-version field)
/create-stories [epic-slug]      → writes story files
```

## Epics Needed (not yet created)

Based on `design/systems-index.md` and architecture review gaps:

| Suggested Epic | Layer | Blocks |
|---|---|---|
| Office Economy | Feature | Settlement math, debt, takeover events |
| Van Dispatch Scene Flow | Core | Mission dispatch ritual |
| Player Controller | Core | Movement, interaction, heavy carry |
| HUD / UI | Presentation | In-mission HUD, settlement screen |
| Tower Level | Feature | Power gate, heavy carry objective, seed topology |
