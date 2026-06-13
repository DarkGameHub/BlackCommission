# Player Character — Canonical Reference (PM 2026-06-12)

`85ee91cbb7ebed304e05616592bc2788.png` is the PM's locked impression of the player
character ("the cheap commission worker"). All character art — the in-game model,
the crew picker, nameplates — must read like this.

## What it locks

| Zone | Element |
|---|---|
| Head | Rust-orange **hard hat** + dark **respirator / gas mask** over the face |
| Torso | Oversized **hi-vis yellow vest** (the one widener) over a worn dark green/grey coverall |
| Chest | Small **ID / company badge** clipped to the left chest (the only identity element) |
| Back | Rectangular **daypack** (straps visible) |
| Hands | **Cylindrical flashlight** in hand |
| Legs | Dark green **work trousers / coverall lowers** |
| Feet | Heavy **work boots**, wide toe, scuffed |

## Style (important)

- **Semi-realistic + heavily weathered** — 20–40% wear, grime, scuffs, faded paint.
- **NOT low-poly, NOT anime, NOT a flat silhouette.** Real human proportions
  (~1.75 m). Tired-competence posture (weight on one hip, slight forward lean).
- Dark, moody, single soft key light — reads as evidence, not a hero.

This aligns with `design/art/art-bible.md §5 Character Design Direction`, adding the
respirator/gas-mask (the world already has a `GasMaskSentinel` prop) and confirming
the **semi-realistic, weathered** fidelity target for the character specifically.

## Implications for in-progress work

- The crew picker (`design/ux/lobby.md` amendment, mockup `ui-kit/14_crew_picker`)
  must show this model, not the crude SVG silhouettes drafted earlier.
- The runtime model in `PlayerFirstPersonRig.BuildThirdPersonVisual` (box torso +
  sphere head fallback / `ASV4_Worker_Cheap_Outsourced_Uniform` prefab) should move
  toward this look: add the respirator, the chest badge, the hi-vis vest weathering.
