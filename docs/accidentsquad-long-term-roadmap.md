# AccidentSquad Long-Term Roadmap

This roadmap keeps long-term direction separate from reusable agent roles.

## North Star

AccidentSquad should feel like a tiny, debt-buried civic contractor that survives by taking increasingly strange outsourced accident jobs. The game can learn from Lethal Company's clarity, ritual, and co-op pressure, but its memory hook must be the rundown office, dispatch van, municipal paperwork, partial settlements, and hostile business pressure.

## Pillars

1. **Office Ritual**
   The office computer, gear shop, debt pressure, and settlement screen are the team's home rhythm.

2. **Dispatch Van Ritual**
   The whole team boards, the driver leaves, players wait inside the van, and the mission begins outside the site. The van is also the return and partial-settlement anchor.

3. **Commission Jobs**
   Each mission is a strange contract from a client, not generic dungeon content. Objectives can have full, partial, and failed outcomes.

4. **Readable Horror-Comedy**
   Environments should be low-cost and readable, but every map needs one memorable wrong detail, one strong silhouette, and clean navigation.

5. **Company Survival**
   Money, reputation, debt, office level, hostile takeover pressure, and acquisition should create pressure without burying the core co-op loop.

## Phases

### Phase 0: Playable Core

- HQ office is navigable and scaled correctly.
- Dispatch van departure works for 1-4 players.
- School lost-item mission supports success, partial return, and failure.
- Rewards apply exactly once at the office computer.
- Hotbar gear has real effects.
- Smoke tests cover solo, two-client, and four-client basics.

### Phase 1: Signature Quality Pass

- Make the HQ, van, school, notebook, and HomeworkDebtCollector visually memorable within Municipal Debt Noir.
- Replace obvious blockout proportions with commercial-feeling primitive or Blender assets.
- Improve lighting so navigation is readable without losing pressure.
- Add clearer in-world signage for route, van boarding, return, and objective status.

### Phase 2: Second Job Category

- Add one new category that is not another lost item mission.
- Preserve the office -> van -> site -> van -> office loop.
- Introduce one new objective type and one new settlement variable.
- Avoid adding a full strategy layer before the second mission is fun.

### Phase 3: Multiplayer Reliability

- Harden host/join and reconnect expectations.
- Decide Steam + Relay transport/service path without adding a custom backend to the MVP.
- Add automated or editor-assisted smoke checks for scene flow and reward idempotency.
- Validate all shared resources and mission states under 1, 2, and 4 players.

### Phase 4: Office Growth

- Expand acquisition from tutorial hook into a simple business pressure layer.
- Add visual office upgrades that reflect debt, success, and acquisitions.
- Keep acquisition decisions tied to playable mission outcomes, not spreadsheet-only gameplay.

## Do Not Lose

- Do not make the game a direct Lethal Company clone.
- Do not let art polish outrun playable logic.
- Do not add eight job categories before two categories are fun.
- Do not add complex inventory before the van and hotbar are satisfying.
- Do not make failure binary when partial settlement is one of AccidentSquad's strongest differentiators.
