# Monster — Civic Idol（市政圣像）

Status: v1 implemented (2026-07-03) · Owner: David (PM) · Brain: `CivicIdol.cs` · Visual: CC0 re-skin (Quaternius "Demon" → verdigris bronze statue)

## 1. Overview

A beatified municipal statue — verdigris bronze, horns, wings, and a civic halo — that
stands inert while any player can see it and sprints at the nearest crew member the
moment nobody is looking. The freeze-when-watched rule is the public-domain "living
statue" genre archetype (gargoyle folklore lineage); the rules, numbers, code, and
re-skinned visual are Black Commission's own. It punishes solo tunnel-vision and
rewards a crew that assigns a spotter.

## 2. Player Fantasy

"That ugly monument in the warehouse hall… wasn't it facing the other way a second
ago?" First contact reads as set dressing. The dread arrives in glances: every time
the crew looks away and back, it is closer, frozen mid-stride. The counter-play
feeling is *holding a threat at bay with nothing but your eyes* — one teammate
walking backwards, staring, while the others carry loot past it.

## 3. Detailed Rules

- **Watched**: at least one living player's horizontal view cone (half-angle
  `viewHalfAngleDeg`, max `watchMaxRange`) contains the idol AND an unobstructed
  physics line of sight exists (level geometry blocks; player bodies and other
  monsters do not). Camera pitch is owner-local and never synced, so the server
  judges from body yaw only — generous to players by design.
- **Frozen** (watched, plus `unfreezeGrace` after the last watcher looks away):
  NavMeshAgent stopped, Animator paused on every peer (it halts mid-stride), deals
  **no damage** even at melee range. Downed players cannot watch. Hidden players
  (locker/van) still count as watchers.
- **Dormant**: no valid target within `senseRadius` — a statue. Eye light off.
- **Stalk**: valid target in range → moves at `stalkSpeed` toward the nearest valid
  player whenever unfrozen. Stamp-red eye light on (threat telegraph). Target beyond
  `loseRadius` → Dormant.
- **Attack**: within `attackRange`, unfrozen → heavy hit every `hitInterval`.
  Downing the target returns it to Dormant. It never chases hidden players.
- **Dead**: `Kill()` (future weapons) plays the death clip and disables it.
- All state is host-authoritative (`PoseState`, `Frozen` NetworkVariables); clients
  only mirror pose and pause.

## 4. Formulas

- `watched(p) = IsWithinViewCone(eye_xz(p), yaw_forward_xz(p), head_xz(idol), watchMaxRange, viewHalfAngleDeg) ∧ LoS(eye(p), head(idol))`
  — cone math in `IdolGazeLogic.IsWithinViewCone` (pure, EditMode-tested).
- `frozen(t) = watchedNow ∨ (t − lastWatchedTime < unfreezeGrace)`
  — `IdolGazeLogic.ShouldFreeze`.
- Gaze sampled at `gazeScanInterval` (10 Hz); freeze is applied instantly on watch,
  release waits out the grace.
- DPS in reach, unwatched: `dmgPerHit / hitInterval` = 40 / 0.8 = **50/s** (a fast
  down, but any glance stops it — lethality lives in attention economy, not stats).

## 5. Edge Cases

- **Raycast flicker** (shelf edges, door frames): `unfreezeGrace` (0.35 s) absorbs it.
- **Looking at the floor while facing it**: still "watched" (yaw-only cone) — errs
  safe for players; revisit only if a synced camera pitch ever exists.
- **All players downed/hidden**: no valid target → Dormant; downed players don't
  freeze it, so a lone survivor must actually look at it while reviving.
- **Spawn**: `Frozen` defaults true — it can never move on frame 1.
- **Offline preview** (no NetworkManager listening): brain runs locally, same rules.
- **Multiple idols**: each judges independently; they don't block each other's LoS.

## 6. Dependencies

- `PlayerController` (position, yaw, `IsHiddenFromMonsters`), `PlayerHealth`
  (`TakeDamage`, `IsDowned`) — the existing monster contact-damage seam.
- `MonsterSpawnBootstrap` seed routing: keywords `IDOL` / `STATUE`;
  seeded in Mars Logistics hall (`MonsterSeed_ML_HALL_IDOL`).
- NavMesh on the host map; Netcode for GameObjects (registered network prefab).
- Art pipeline: `tools/rigging/recolor_atlas.py` (statue mode) +
  `CivicIdolSetup.cs` (import/controller/prefab build).

## 7. Tuning Knobs

All serialized on `CivicIdol` (data-driven, per-prefab):

| Knob | v1 | Intent |
|---|---|---|
| `watchMaxRange` | 45 m | Can be pinned from across the hall |
| `viewHalfAngleDeg` | 50° | ≈ on-screen for a 16:9 first-person camera |
| `unfreezeGrace` | 0.35 s | Flicker guard; raise if it "twitches" under stare |
| `senseRadius` / `loseRadius` | 25 / 32 m | How far it wakes / gives up |
| `stalkSpeed` | 4.6 m/s | Above player walk (4) — closes in glances |
| `attackRange` / `dmgPerHit` / `hitInterval` | 1.7 m / 40 / 0.8 s | Heavy but glance-stoppable |

## 8. Acceptance Criteria

- [ ] EditMode: `IdolGazeLogicTests` pass (cone membership, edge angles, degenerate
      forward, freeze hysteresis).
- [ ] Play (host): idol in Mars hall is inert while on screen; moves only when no
      one renders it; every peer sees identical frozen poses.
- [ ] Staring at it from melee range: zero damage taken while watched.
- [ ] Breaking line of sight behind a shelf lets it advance; re-acquiring sight
      freezes it within one glance.
- [ ] Eye light: off while dormant, stamp-red once it has prey, off again on give-up.
- [ ] Client-side: joins mid-mission and sees the correct pose + freeze state.
