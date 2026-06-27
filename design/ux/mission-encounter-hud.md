# UX Spec: Mission Monster-Encounter Feedback (Echo Mold HUD Layer)

> **Status**: In Design
> **Author**: David (PM) + ux-designer
> **Last Updated**: 2026-06-15
> **Journey Phase(s)**: Mission-site — encounter & extraction
> **Extends**: `design/ux/hud.md` (Approved 2026-06-11) — this is the encounter
> *layer* of the HUD, **not a new screen**.
> **Source GDD**: `design/gdd/monster-echo-mold.md` §UI Requirements (binding input);
> `design/gdd/monster-system.md` (danger-level scaling); `design/gdd/mission-state-machine.md` (Downed→Failure)
> **Template**: UX Spec (HUD-extension variant)

---

## 1. Purpose & Player Need

**Purpose (one line).** This layer makes the player *feel that their own voice is the
danger* — turning "open the mic" from a free cooperation tool into a deliberate,
learnable risk — while revealing **nothing** about the Echo Mold itself, so the
"is that really you?" paranoia stays intact.

**What the player is trying to do.** Coordinate a 1–4 person extraction (carry the
column, pull levers, recover a downed teammate) *without* feeding the Mold the voice
samples and position it needs to split the team. Moment to moment, the player needs
one honest answer: **"Am I transmitting right now?"** Everything else — where the Mold
is, whether a voice is fake, whether it's hunting — must be earned through *audio and
positioning*, never the HUD.

**What goes wrong without it.** If the player can't tell when their mic is live, voice
discipline becomes guesswork and "I died because I was talking" reads as unfair instead
of self-inflicted. The signature emotion — *self-blame with dark comedy* ("I followed my
own voice into a stairwell") — collapses into generic monster-chase frustration.

**The non-negotiable.** One legally-driven element rides alongside the fantasy: a
one-time notice that in-game voice is recorded and may be replayed (privacy/consent,
GDD Open Q#5). It is *not* part of the threat read — a setup-time courtesy, kept out of
the encounter.

**The line we don't cross.** No monster position, no hunt-state indicator, no "fake
voice" label, no 0–100 threat meter. The HUD mirrors *the player's own action*, not the
Mold's knowledge.

> The player arrives at this encounter wanting to **stay coordinated without being
> heard** — and the only thing the HUD owes them is the truth about whether their own
> mouth is open.

---

## 2. Relationship to the Base HUD

[To be designed]

---

## 3. Player Context on Arrival

[To be designed]

---

## 4. Design Constraints (binding)

[To be designed]

---

## 5. Information Architecture

### New Information Inventory

[To be designed]

### Categorization

[To be designed]

---

## 6. Layout & Elements

### Voice On-Air / "being sampled" indicator

[To be designed]

### Pursuit / Contact alert (A3 work-order grammar)

[To be designed]

### The 'tell' (real vs replayed voice)

[To be designed]

### Voice-monitoring consent notice (one-time)

[To be designed]

### Downed / All-Down

[To be designed]

### ASCII Wireframe

[To be designed]

---

## 7. States & Variants

[To be designed]

---

## 8. Interaction Map

[To be designed]

---

## 9. Events Fired

[To be designed]

---

## 10. Transitions & Animations

[To be designed]

---

## 11. Data Requirements

[To be designed]

---

## 12. Accessibility

[To be designed]

---

## 13. Localization Considerations

[To be designed]

---

## 14. Acceptance Criteria

[To be designed]

---

## 15. Open Questions

[To be designed]
