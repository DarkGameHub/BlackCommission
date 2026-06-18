# Game Pillars — Black Commission

> Source of truth: `@AGENTS.md`. This file restates the pillars for framework
> skills (consistency-check, design-review, scope-check).

## Identity

**Municipal Debt Noir** — dead-rubber black, concrete gray, aged paper, warm tungsten amber
(inhabitation light — pendant lamps, desk lamps), military green, stamp red.

## Memorable Pillars

1. **The broke office** — debt-ridden from day one; no jobs = no rent = takeover. Survival is the only motivation.
2. **The dispatch van ritual** — the team boarding and riding out together; the loop is the ritual.
3. **The contract speaks** — satire is delivered through settlement screens, client usage notes, and deduction clauses — never through narration.
4. **Partial settlement choices** — extract now for less, or push for more.
5. **The moral slope** — three mission tiers: Free Collection (safe, low pay) → Designated Commission (commissioned, satirical) → Black Commission (high pay, dark cost). Higher reward always comes with a darker price.
6. **License as leash** — progress is gated by Mars-capital-issued permits; "advancement" is itself the satire.

## Production-Method Reference (NOT to copy)

Lethal Company is a *method* reference only: strong repeatable rituals, low-cost
readable staging, co-op extraction tension, darkness with clear navigation.
**Do NOT copy** its assets, UI, monsters, ship, quota fiction, item list, or maps.

## Threat Design

**"What ordinary workplace or public-service pressure became physical here?"**
Every threat is an institutional role under extreme duress — not a creature-feature monster.
Design test: name the job title first, then what broke. See art-bible.md Section 5 (Monster Framework).

## Anti-Pillars (what this game is NOT)

- **Not a hero narrative** — players are bottom-rung contractors; the invoice is the motivation, not a cause.
- **Not a numerical pressure meter** — license revocation threat is expressed as narrative events (letters, office condition, clients going cold), never as a 0–100 UI bar.
- **Not a moral judge** — the game presents facts; the settlement screen does the talking.
- **Not a Lethal Company clone** — LC is a production-method reference only (ritual, co-op tension, readable staging). No copied assets, UI, quota fiction, or map layouts.
- **Not a clear right-vs-wrong ending** — both endings (Go to Mars / Stay on Earth) must be genuinely tempting. "Stay" is the harder choice; "Go" is the easier one. Neither is a verdict. See `docs/world-background-2098.md` Ending Design Philosophy.

## Design Priorities

- Playability and logical physical scale **before** visual decoration.
- Server-authoritative multiplayer state (host-authored) for all mission-critical state.
- Unity + Netcode for GameObjects; no custom backend (Steam/Relay later as transport).

## Progression Backbone (updated 2026-06-18)

Four license stages drive all progress. Two independent tracks run simultaneously:
- **License Stage** (1–4): gates job access, advances through story mission completions
- **Earth Deterioration Level** (1–5): drives safety infrastructure cost, advances with total missions elapsed regardless of license stage

| Stage | License | Job access | Emotional register |
|---|---|---|---|
| 1 | 临时采回许可 | Basic commissioned jobs | Just trying to pay the debt |
| 2 | 正式采回许可 | Weirder clients; free salvage unlocks | These people are consuming Earth |
| 3 | 特殊样本转运许可 | **Black Commission** available — high pay, dark cost | These jobs are getting darker |
| 4 | 移民资格审查 | Ending-choice gate: Go to Mars or Stay on Earth | Do you still want to go? |

**License revocation = game over.** Mars-capital revokes the license when the office cannot sustain safety standards (mid-game) or when a Stay-on-Earth player can no longer operate (ending). No competitor takeover — the license was always theirs to withdraw.

**Stay-on-Earth path:** Mars clients stop commissioning work after Stage 4. Free salvage remains. Earth Deterioration continues. Safety costs outpace free salvage income — the office fades out rather than ending suddenly.
