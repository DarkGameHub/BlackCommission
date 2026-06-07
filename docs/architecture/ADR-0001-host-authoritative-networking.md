---
status: reverse-documented
source: Assets/_Project/Scripts/Network/, Assets/_Project/Scripts/Mission/, Assets/_Project/Scripts/Player/, Assets/_Project/Scripts/Office/
date: 2026-06-07
verified-by: Yan Dai
---

# ADR-0001: Host-Authoritative Networking Model

> **Note**: This ADR was reverse-engineered from the existing implementation.
> It captures the authority model already shipping in code plus the design intent
> confirmed by the PM. Sections marked INCOMPLETE reflect areas not yet built.

## Status

Accepted

## Date

2026-06-07

## Last Verified

2026-06-07

## Decision Makers

Yan Dai (PM, final decision maker); reverse-documented by Claude Code.

## Summary

1–4 player co-op runs entirely on a host-authoritative model: the host (server)
owns all shared game state, and clients request changes via RPC that the host
validates before writing back. This eliminates client-side authority over mission,
economy, and player state, satisfying the `@AGENTS.md` server-authoritative mandate.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6 (6000.4.7f1) |
| **Domain** | Networking |
| **Knowledge Risk** | MEDIUM — Netcode for GameObjects (NGO) sits near/after the LLM knowledge cutoff; verify NetworkVariable write-permission and RPC attribute signatures against `docs/engine-reference/unity/`. |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `Assets/_Project/Scripts/Network/`, `Assets/_Project/Scripts/Mission/LostItemMissionManager.cs` |
| **Post-Cutoff APIs Used** | NGO `NetworkVariable<T>`, `[ServerRpc]` / `[ClientRpc]`, `NetworkBehaviour`, `NetworkManager.Singleton` |
| **Verification Required** | Confirm NetworkVariable default write-permission is Server; confirm RPC ownership rules under NGO version bundled with 6000.4.7f1. |

> **Note**: Knowledge Risk is MEDIUM — re-validate this ADR if the project upgrades
> the NGO package or the Unity engine version.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None (foundational) |
| **Enables** | ADR-0002 (Relay transport & connection approval), and any future mission-generation / settlement ADR |
| **Blocks** | All multiplayer gameplay epics (mission state, settlement, van ritual) |
| **Ordering Note** | This is the root networking decision; all other networked-system ADRs depend on it. |

## Context

### Problem Statement

A 1–4 player co-op game with shared mission state, a shared office economy, and
partial-return choices needs a single source of truth. Without an authority model,
clients could desync mission progress or fabricate rewards, and there would be no
canonical state to resolve conflicts or recover from disconnects.

### Current State

The codebase already implements a consistent host-authoritative model:

- 16 `NetworkBehaviour` classes.
- Shared state expressed as `NetworkVariable` fields — heaviest in
  `Mission/LostItemMissionManager.cs` (18), `Player/PlayerController.cs` (15),
  with mission clock, exit point, office computer, oxygen, health, hotbar, and
  monster AI all carrying networked state.
- Client→host intent via `ServerRpc` (10 files).
- Host→client effects/broadcast via `ClientRpc` (7 files).
- State-mutating methods guarded by `IsServer` / `IsHost` (OfficeComputer 9 sites,
  PlayerController 8, SchoolMonsterAI 7, LostItemMissionManager 7, …).

### Constraints

- `@AGENTS.md`: multiplayer state MUST be server-authoritative; mission selection,
  notebook pickup, van lockers, completion, partial return, failure, and rewards
  MUST sync to clients.
- Stay in Unity + Netcode for GameObjects with host-authored state — no custom backend.
- Target: PC, 1–4 players, 60 FPS.

### Requirements

- Single authoritative owner for all shared gameplay state.
- Clients cannot directly write authoritative state — only request via RPC.
- Mission state transitions (complete / partial return / fail) resolve on host and
  replicate to all clients.
- Reward/settlement math computed on host only.

## Decision

Adopt **host-authoritative networking** as the project-wide model. The host process
acts as the server. All shared state lives in `NetworkVariable` fields whose write
permission is the server; clients mutate state only by invoking a `ServerRpc`, which
the host validates and applies. Host-driven effects and broadcasts use `ClientRpc`.

### Authority Boundary Rules

These decisions are made on the host and replicated; clients never decide them locally:

- Mission selection
- Notebook / evidence / homework item pickup
- Van locker contents
- Mission completion, partial return, failure transitions
- Reward / settlement computation

### Architecture

```
        CLIENT(S)                              HOST (server)
   ┌──────────────────┐                  ┌──────────────────────────┐
   │ Local input/UI   │ ── ServerRpc ──▶ │ Validate (IsServer guard)│
   │ Reads replicated │                  │ Mutate NetworkVariable   │
   │ NetworkVariables │ ◀── replicate ── │ (server write authority) │
   │ Plays ClientRpc  │ ◀── ClientRpc ── │ Broadcast effects        │
   └──────────────────┘                  └──────────────────────────┘
```

### Key Interfaces

```csharp
// Shared state — server writes, everyone reads.
NetworkVariable<T> someState = new(default,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server);

// Client requests a change; host validates and applies.
[ServerRpc(RequireOwnership = false)]
void RequestXServerRpc(/* args */) { if (!IsServer) return; /* validate + mutate */ }

// Host pushes an effect/broadcast to clients.
[ClientRpc]
void OnXClientRpc(/* args */) { /* presentation only */ }
```

### Implementation Guidelines

- Never write a `NetworkVariable` from a client path. Route through a `ServerRpc`.
- Begin every state-mutating server method with an `IsServer`/`IsHost` guard.
- Keep `ClientRpc` payloads presentation-only; the authoritative value is the
  replicated `NetworkVariable`.
- Settlement/reward math must run inside a server-guarded path, never client-side.

## Alternatives Considered

### Alternative 1: Client-authoritative / peer trust

- **Description**: Each client owns and broadcasts its own state.
- **Pros**: Lower host CPU; simpler ownership.
- **Cons**: Trivially cheatable economy/mission state; no canonical truth; desync hell.
- **Estimated Effort**: Lower.
- **Rejection Reason**: Violates `@AGENTS.md` server-authoritative mandate; unacceptable for shared economy.

### Alternative 2: Dedicated server backend

- **Description**: Authoritative state on a standalone server process / custom backend.
- **Pros**: No host-quit termination; stronger anti-cheat.
- **Cons**: Forbidden by project rules (no custom backend); ops cost; over-scoped for 1–4p co-op.
- **Estimated Effort**: Much higher.
- **Rejection Reason**: Explicitly out of scope — stay in Unity + NGO with host-authored state.

## Consequences

### Positive

- Single source of truth for all shared state.
- No client-side authority surface for cheating the economy or mission outcome.
- Disconnects/edge cases resolved by host as the canonical authority.

### Negative

- Host quit ends the session for everyone — no host migration (known limitation).
- Host CPU carries all authority/validation work.

### Neutral

- All networked systems must follow the NetworkVariable + ServerRpc/ClientRpc idiom;
  contributors must learn the pattern.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| Silent state desync (mission/settlement) ships untested | HIGH | HIGH | Add EditMode logic tests for mission state machine + settlement math (see Validation Criteria) — currently zero coverage. |
| Host quit ends session | HIGH | MEDIUM | Accepted limitation for MVP; document in UX. Host migration is out of scope. |
| Contributor writes NetworkVariable from client path | MEDIUM | HIGH | Code review checklist; `IsServer` guard convention; consider analyzer. |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| CPU (frame time) | n/a | host carries validation | 16.6 ms (60 FPS) |
| Network | n/a | NetworkVariable deltas + RPC | TBD (4-player ceiling, ADR-0002) |

## Migration Plan

Not a migration — this documents the existing shipped model. No code change required
by this ADR. Future work: enforce the pattern via review + tests.

**Rollback plan**: N/A (foundational, already in production).

## Validation Criteria

- [ ] EditMode logic tests cover mission state transitions (complete / partial
      return / fail) in `LostItemMissionManager` / `MvpMissionRuntime`.
- [ ] EditMode logic tests cover settlement/reward math (`MvpPendingReward`,
      `SettlementUIController`, `CompanyData`).
- [ ] No `NetworkVariable` is written from a non-server code path (grep/review audit).
- [ ] 4-player PlayMode smoke confirms mission outcome + rewards identical on all clients.

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `@AGENTS.md` (rules of record) | Networking | "Keep multiplayer state server-authoritative. Mission selection, notebook pickup, van lockers, completion, partial return, failure, and rewards must sync to clients." | Host owns all shared NetworkVariable state; clients mutate only via validated ServerRpc; all listed transitions resolve on host and replicate. |

> Foundational decision. Enables every networked gameplay system (mission, office
> economy, van ritual, settlement) and is depended on by ADR-0002.

## Related

- Enables / depended on by **ADR-0002** (Relay transport & connection approval).
- Code: `Assets/_Project/Scripts/Network/`, `Mission/LostItemMissionManager.cs`,
  `Player/PlayerController.cs`, `Office/OfficeComputer.cs`.
- **Follow-up ADR (future)**: mission generation / selection architecture
  (office computer offering 3 jobs, four room-size pools with randomized layout,
  story vs. casual "no-client" jobs, hidden player-level-driven assignment) —
  to be authored once that design settles; will depend on this ADR.
