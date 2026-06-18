# Game Concept — Black Commission

> **Index file.** Points to the canonical, current design docs so framework
> skills can find the concept. Do not duplicate content here — edit the source docs.

## One-liner

A 1–4 player co-op first-person "commission-running" game: a nearly bankrupt
office takes increasingly absurd outsourced civic jobs to stay afloat.
Identity: **Municipal Debt Noir**.

## Signature Loop

`HQ office → office computer (accept job / buy gear) → board dispatch van →
in-van transit → mission site → objective / partial-return choice → van return →
HQ settlement`

## Canonical Sources (authoritative — current)

- **Vision & rules**: `@AGENTS.md` — PM Yan Dai, design pillars, long-term constraints
- **Pillars & progression backbone**: `design/game-pillars.md` (locked 2026-06-09 —
  4 license stages, money-only economy, the moral slope)
- **System GDDs**: `design/gdd/`
  - `level-map-generation.md` — map / topology generation
  - `mission-state-machine.md` — mission flow: completion / partial-return / failure
  - `office-economy-progression.md` — money, settlement, license stages
- **World / lore (2098)**: `docs/world-background-2098.md`
- **Art direction (locked)**: `design/art/art-bible.md`
- **Spatial language**: `design/art/spatial-language-spec.md`
- **Long-term roadmap**: `docs/black-commission-long-term-roadmap.md`
- **Code ↔ design map**: `design/systems-index.md`

## Superseded (historical only — do NOT use for current rules)

These describe the older **"AccidentSquad" / school lost-item** build (reputation,
office levels 1–8, a 0–100 takeover bar, PS1 low-poly, the Homework Debt Collector):

- `docs/mvp-core-loop.md` — superseded
- `docs/design-decisions.md` — superseded (a few *principles* still hold; see its banner)

## Current Playable Focus

Abandoned pre-sale tower **"Earth Coast No. 1"** (`Assets/_Project/Scenes/Tower_EarthCoast_01.unity`):
retrieve the sales scale model via a heavy two-hand carry, restore power to reach
Floor 2, avoid the **Auditor (核查专员)**, return to the van to settle. The full
HQ → van → tower → settlement loop is playable end-to-end.
