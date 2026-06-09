# Consistency Report — 2026-06-09

**Skill**: /consistency-check (manual art-bible review mode — registry empty)
**Scope**: Existing design docs vs. completed art bible (`design/art/art-bible.md`)
**Verdict**: RESOLVED — all 7 conflicts fixed

---

## Documents Reviewed

| Document | Status | Conflicts | Gaps | Resolution |
|---|---|---|---|---|
| design/game-concept.md | RESOLVED | 1 | 0 | Art direction pointer updated |
| design/game-pillars.md | RESOLVED | 1 | 1 | Identity palette corrected; monster design rule added |
| design/levels/abandoned-tower-earth-coast-01.md | RESOLVED | 3 | 1 | Lighting language corrected; BC identity injection added |
| design/levels/abandoned-tower-redesign-v3.md | RESOLVED | 2 | 1 | Van + show-flat lighting corrected |
| design/levels/lethal-company-design-study.md | CLEAR | 0 | 0 | No action needed |

---

## Root Cause

All 7 conflicts were the same underlying problem: lighting terminology ("sodium amber,"
"warm gold," "warm safe pool") coined in level design docs before the art bible was
locked. These terms conflicted with three hard art-bible rules:

1. **Van interior must be cold (5000K)** — not a warm safe pool
2. **Mission site ambient is cold-dominant** — show-flat warm ambient violated this
3. **Warm amber = inhabitation signal only** — not a general exterior color or area fill

Additionally, `game-concept.md` pointed to the superseded `style-lock-v1.md` as the
art authority, causing any framework skill reading that index to load the wrong accent
definition.

---

## Fixes Applied

### game-concept.md
- Updated canonical art direction pointer from `docs/art/black-commission-style-lock-v1.md`
  to `design/art/art-bible.md` with supersession note inline.

### game-pillars.md
- Replaced Identity palette: removed "civic teal," "sodium amber," "dispatch green" —
  replaced with "dead-rubber black, concrete gray, aged paper, warm tungsten amber
  (inhabitation light), military green, stamp red."
- Added Threat Design section with the monster design formula:
  "What ordinary workplace or public-service pressure became physical here?"

### abandoned-tower-earth-coast-01.md
- Fixed lighting section: "sodium amber emergency strips" → warm tungsten residual
  desk lamps (inhabitation signal); post-power show-flat = cold overheads, one warm
  desk lamp on sales desk.
- Fixed color palette section: removed "fake-luxury warm gold" — wrongness now described
  through pristine material condition, not a distinct hue.
- Updated show-flat landmark description to match cold-beacon rule.
- Updated Points of Interest table description for show-flat.
- Added BC Identity Injection section (art-bible Section 6 gap): 5-location table
  specifying commission forms BC-12, "欠款通道 / DEBT ACCESS ONLY" signage, route
  arrows, tagged equipment, and overdue payment notices.

### abandoned-tower-redesign-v3.md
- Fixed exterior lighting: "failing sodium-amber site floods + van warm safe pool" →
  dead/near-dead cold-adjacent site floods + cold van interior (5000K strip) + cold
  headlights. Van beacon = contrast, not warm color.
- Fixed show-flat room table: "warm 'wrong' light" → cold overheads + single warm
  desk lamp + pristine material condition as the wrong detail.
- Fixed acceptance criteria item 5: added lighting spec for show-flat beacon.

---

## Remaining Advisory Items

- `docs/art/black-commission-style-lock-v1.md` should have a deprecation header added
  at the top to prevent future confusion. The art bible header already notes supersession;
  the style-lock file itself does not. Low priority — update when convenient.
