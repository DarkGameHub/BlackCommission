# Black Commission — Backlog

Deferred work, not in the active sprint. Pull into a sprint when prioritized.

## Audio

- **[B] Holistic audio quality overhaul (`/team-audio`)** — added 2026-06-13.
  - **Why**: every SFX is procedurally synthesized in `SynthAudio` (raw tones/noise) → reads as cheap beeps; PM: "太不好听". Audio is also a required feedback/accessibility channel (glass thud = completeness-loss, breaker buzz = progress, stamp = settlement) per `design/ux/hud.md`, so deleting it is not an option.
  - **Scope**: run the full `/team-audio` pipeline — `audio-director` (sonic identity + palette) → `sound-designer` (per-event SFX specs, mix groups, ducking) → `technical-artist` (bus structure, budgets) → `gameplay-programmer` (re-implement `AudioManager`/`SynthAudio` or move to real assets). Produces `design/audio/…` then implementation.
  - **PM intent**: "我肯定是做 B" — committed, just deferred until after the current UI pass.
  - **Note**: a quick stop-gap (synth warmth + mix pass, or master SFX mute) was offered and declined in favor of doing B properly later.
