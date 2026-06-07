---
status: reverse-documented
source: Assets/_Project/Scripts/Network/ConnectionManager.cs, MvpConnectionLimiter.cs, GameBuild.cs, QuickNetworkUI.cs, HQSpawnManager.cs
date: 2026-06-07
verified-by: Yan Dai
---

# ADR-0002: Relay Transport & Connection Approval

> **Note**: This ADR was reverse-engineered from the existing implementation plus
> design intent confirmed by the PM (Direct LAN is dev-only; 4-player is a permanent
> hard cap).

## Status

Accepted

## Date

2026-06-07

## Last Verified

2026-06-07

## Decision Makers

Yan Dai (PM, final decision maker); reverse-documented by Claude Code.

## Summary

Multiplayer transport uses Unity Relay (DTLS, anonymous auth) with join-code
matchmaking; Direct LAN hosting exists only as an editor/local test facility, not a
shipping feature. The host gates entry through a ConnectionApprovalCallback that
enforces a build-version match and a permanent 4-player cap.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6 (6000.4.7f1) |
| **Domain** | Networking |
| **Knowledge Risk** | MEDIUM — Unity Gaming Services (Relay, Authentication) and UTP Relay APIs sit near/after the LLM knowledge cutoff; verify against `docs/engine-reference/unity/`. |
| **References Consulted** | `Assets/_Project/Scripts/Network/ConnectionManager.cs`, `MvpConnectionLimiter.cs`, `GameBuild.cs`, `HQSpawnManager.cs` |
| **Post-Cutoff APIs Used** | `UnityServices.InitializeAsync`, `AuthenticationService` (anonymous sign-in), `RelayService` (CreateAllocation / GetJoinCode / JoinAllocation), `UnityTransport.SetRelayServerData(..., "dtls")`, `NetworkManager.ConnectionApprovalCallback` |
| **Verification Required** | Confirm RelayServerData/UTP signatures and ConnectionApprovalResponse fields against the NGO + UGS package versions in this project. |

> **Note**: Knowledge Risk is MEDIUM — re-validate if UGS / NGO / UTP packages are upgraded.

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (host-authoritative model must hold — host is the approving authority) |
| **Enables** | All session lifecycle (lobby, join, spawn) work |
| **Blocks** | Steam/Relay transport-services epic (future) |
| **Ordering Note** | Transport sits under the authority model; ADR-0001 first. |

## Context

### Problem Statement

Players need to host and join sessions over the internet without a custom backend,
with NAT traversal handled for them, while keeping out mismatched builds and capping
the room at the supported player count.

### Current State

- `ConnectionManager` initializes UGS, signs in anonymously, creates a Relay
  allocation (`MaxConnections = 3` → host + 3 = 4), exchanges a join code, and sets
  `UnityTransport` relay data with `"dtls"`. It exposes `HostGame` / `JoinGame` and
  `OnJoinCodeReady` / `OnConnected` / `OnError` events.
- `QuickNetworkUI` keeps a Direct Host path for editor/local testing when services
  are initializing or unavailable.
- `MvpConnectionLimiter` enables `ConnectionApproval`, sends `GameBuild.VersionPayload`
  as connection data, and in `ApproveConnection` rejects on version mismatch or when
  the room is full; `CreatePlayerObject` is set only when approved.
- `HQSpawnManager` teleports the local player to the spawn point (with retry, up to
  16 attempts) using `LocalClientId` for a lateral offset.

### Constraints

- No custom backend — Steam/Relay are transport/services only.
- 1–4 players, PC.
- Build-version consistency required across host and clients.

### Requirements

- Internet play with NAT traversal and encrypted transport.
- Reject clients whose build version differs from the host's.
- Enforce a hard 4-player room cap.
- Provide a local/editor host path that does not require live services.

## Decision

Use **Unity Relay** as the production transport (DTLS encryption, anonymous
authentication, join-code matchmaking). Retain **Direct LAN host as an
editor/local-test-only facility** (not a shipping feature). Gate all client entry on
the host via `ConnectionApprovalCallback` with two checks: **version match** and
**room-has-space (< 4)**.

### Player Cap — Permanent Hard Limit

The maximum is **4 players (host + 3)** and is a permanent design ceiling, not an MVP
placeholder. It MUST NOT be exceeded.

### Architecture

```
  HOST                                          CLIENT
  ConnectionManager.HostGame()                  ConnectionManager.JoinGame(code)
    UnityServices.Init + anon sign-in             UnityServices.Init + anon sign-in
    RelayService.CreateAllocation(3)              RelayService.JoinAllocation(code)
    GetJoinCode ──────────── join code ─────────▶ SetRelayServerData("dtls")
    SetRelayServerData("dtls")                    StartClient()
    StartHost()                                       │
        │                                             ▼
        ▼                          ConnectionApprovalCallback (on HOST)
  MvpConnectionLimiter.ApproveConnection:
    versionOk   = client GameBuild.Version == host GameBuild.Version
    roomHasSpace= ConnectedClients < 4
    Approved & CreatePlayerObject = versionOk && roomHasSpace
        │ approved
        ▼
  HQSpawnManager teleports local player to spawn (retry x16, offset by LocalClientId)
```

### Key Interfaces

```csharp
// ConnectionManager
public async void HostGame();              // Relay allocation + StartHost
public async void JoinGame(string code);   // Relay join + StartClient
public event Action<string> OnJoinCodeReady;
public event Action OnConnected;
public event Action<string> OnError;

// MvpConnectionLimiter (host-side gate)
void ApproveConnection(ConnectionApprovalRequest req, ConnectionApprovalResponse resp);
//   resp.Approved = versionOk && roomHasSpace;
//   resp.CreatePlayerObject = resp.Approved;
```

### Implementation Guidelines

- Production builds must use Relay; Direct LAN host stays behind editor/dev UI only.
- All approval logic stays host-side in `ApproveConnection`.
- Surface rejection reasons to the joining player (version mismatch / room full) —
  reasons are already bilingual (zh/en).

## Alternatives Considered

### Alternative 1: Direct IP / port-forwarding only

- **Description**: Players connect by IP; no relay.
- **Pros**: No UGS dependency; lowest latency on LAN.
- **Cons**: NAT/port-forwarding burden on players; poor for internet co-op.
- **Estimated Effort**: Lower.
- **Rejection Reason**: Bad UX for online co-op; kept only as a dev/LAN test path.

### Alternative 2: Steam P2P / Steam transport now

- **Description**: Ship on Steam networking from the start.
- **Pros**: Friends list, lobbies, NAT punch via Steam.
- **Cons**: Premature — Steam is planned as a later transport/service, not the MVP path.
- **Estimated Effort**: Higher (store integration).
- **Rejection Reason**: Out of MVP scope; revisit as a future transport ADR.

## Consequences

### Positive

- Internet co-op with NAT traversal and encryption, no custom backend.
- Version gate prevents protocol-mismatch desyncs.
- Hard cap enforced server-side at approval time.

### Negative

- Hard dependency on Unity Gaming Services availability for the production path.
- Anonymous auth only — no persistent player identity yet.

### Neutral

- Two host paths exist (Relay + Direct LAN); contributors must know Direct LAN is dev-only.

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| **4-player cap duplicated in two places** — `ConnectionManager.MaxConnections = 3` and `MvpConnectionLimiter.maxPlayers = 4` must be kept consistent by hand | MEDIUM | MEDIUM | Extract to a single shared constant / config (`const int MaxPlayers = 4;`) so both derive from one source. Tracked as tech debt. |
| UGS / Relay outage blocks hosting | LOW | HIGH | Direct LAN fallback for dev; surface clear error (already via `OnError`). |
| Version-string drift between builds blocks legitimate joins | LOW | MEDIUM | Single `GameBuild.Version` source; bump deliberately per release. |

## Performance Implications

| Metric | Before | Expected After | Budget |
|--------|--------|---------------|--------|
| Network | n/a | Relay-routed UDP/DTLS, ≤4 peers | TBD — measure at 4 players |
| Load Time | n/a | + UGS init + Relay allocation on host start | keep host-start < a few seconds |

## Migration Plan

Documents the existing shipped transport. No migration required. Recommended cleanup:

1. Introduce a single `MaxPlayers` constant and have both `ConnectionManager` and
   `MvpConnectionLimiter` derive from it (closes the duplication risk above).
2. Verify rejection-reason UX surfaces to the joining client.

**Rollback plan**: N/A (already in production).

## Validation Criteria

- [ ] A client on a mismatched `GameBuild.Version` is rejected with the version reason.
- [ ] A 5th joiner is rejected with the room-full reason; room never exceeds 4.
- [ ] Relay host/join round-trip works over the internet (two networks).
- [ ] Direct LAN host path remains editor/dev-only and is not exposed in shipping UI.
- [ ] Player cap value lives in a single source (post-cleanup).

## GDD Requirements Addressed

| GDD Document | System | Requirement | How This ADR Satisfies It |
|-------------|--------|-------------|--------------------------|
| `@AGENTS.md` / technical-preferences | Networking | "1–4 player online co-op"; "Steam/Relay considered later as transport/services, NOT a custom backend." | Unity Relay provides online transport with no custom backend; 4-player hard cap enforced at connection approval; Steam deferred to a future transport ADR. |

## Related

- Depends on **ADR-0001** (host-authoritative model; host is the approving authority).
- Code: `Network/ConnectionManager.cs`, `Network/MvpConnectionLimiter.cs`,
  `Network/GameBuild.cs`, `Network/QuickNetworkUI.cs`, `Network/HQSpawnManager.cs`.
- **Future ADR**: Steam transport/services integration (deferred).
