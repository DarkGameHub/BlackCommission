# Architecture Review Report — 2026-06-09

**Engine:** Unity 6 (6000.4.7f1)  
**GDDs reviewed:** 0 (design/gdd/ is empty — requirements extracted from narrative design docs)  
**ADRs reviewed:** 2 (ADR-0001, ADR-0002)  
**Verdict:** CONCERNS — 19/23 requirements have no ADR coverage

---

## Traceability Summary

| Status | Count |
|---|---|
| ✅ Covered | 2 |
| ⚠️ Partial | 2 |
| ❌ Gap | 19 |
| **Total** | **23** |

---

## Full Traceability Matrix

| TR-ID | System | Requirement | ADR | Status |
|---|---|---|---|---|
| TR-net-001 | Networking | Host-authoritative state (ServerRpc/NetworkVariable) | ADR-0001 | ✅ |
| TR-net-002 | Networking | Unity Relay transport, 4-player cap, version gate | ADR-0002 | ✅ |
| TR-net-003 | Networking | Power gate building state syncs to all clients | — | ❌ |
| TR-net-004 | Networking | Heavy-carry carrier identity server-validated | — | ❌ |
| TR-net-005 | Networking | Seed value identical across all peers | — | ❌ |
| TR-mission-001 | Mission | State machine replication (complete/partial/fail) | ADR-0001 (implied) | ⚠️ |
| TR-mission-002 | Mission | Settlement math host-only | ADR-0001 (implied) | ⚠️ |
| TR-mission-003 | Mission | MvpMissionClock drives time-scaled monster speed | — | ❌ |
| TR-mission-004 | Mission | Monster AI server-driven; nest aggro on objective pickup | — | ❌ |
| TR-mission-005 | Mission | Evidence photo bonus payout | — | ❌ |
| TR-office-001 | Office | Office computer job queue (3 selectable jobs, server-auth) | — | ❌ |
| TR-office-002 | Office | CompanyData persistence between sessions | — | ❌ |
| TR-office-003 | Office | Settlement 3-outcome math (full/partial/fail) | — | ❌ |
| TR-office-004 | Office | Van dispatch scene flow | — | ❌ |
| TR-player-001 | Player | First-person controller (movement, interaction, flashlight) | — | ❌ |
| TR-player-002 | Player | Heavy two-hand carry (0.55× speed, hotbar lock, drop/relay) | — | ❌ |
| TR-player-003 | Player | Health/oxygen replication per-player | — | ❌ |
| TR-player-004 | Player | 5-slot hotbar (equip/drop/use) | — | ❌ |
| TR-tower-001 | Tower | Power gate server-auth, session-persistent | — | ❌ |
| TR-tower-002 | Tower | Heavy carry objective (tower tuning) | — | ❌ |
| TR-tower-003 | Tower | Seed topology with 5 solvability invariants, peer-synced | — | ❌ |
| TR-tower-004 | Tower | Multi-floor NavMesh across stair links, shaft void excluded | — | ❌ |
| TR-tower-005 | Tower | Time-scaled monster aggression curve (clock integration) | — | ❌ |

---

## Cross-ADR Conflicts

None. ADR-0001 → ADR-0002 ordering is correct and clean.

---

## Engine Compatibility Issues

| Domain | Risk | Issue |
|---|---|---|
| Input System | HIGH | No ADR confirms PlayerController uses new Input System (not deprecated `Input.*`) |
| NavMesh multi-floor | MEDIUM | No ADR for stair-link baking strategy or shaft void exclusion |
| URP lighting | MEDIUM | No ADR for dynamic light budget or show-flat lighting constraints |

---

## Required ADRs — Priority Order

**Before tower level implementation starts:**
1. `/architecture-decision "mission-state-machine"` → covers TR-net-003, TR-net-004, TR-mission-001, TR-tower-001, TR-tower-002
2. `/architecture-decision "player-input-and-controller"` → covers TR-player-001, TR-player-002, TR-player-004 + resolves Input System HIGH risk
3. `/architecture-decision "tower-navmesh-and-topology"` → covers TR-tower-003, TR-tower-004, TR-tower-005, TR-net-005

**Before Pre-Production gate:**
4. `/architecture-decision "office-economy-and-persistence"` → covers TR-office-001, TR-office-002
5. `/architecture-decision "settlement-and-reward-math"` → covers TR-mission-002, TR-office-003, TR-mission-005
6. `/architecture-decision "van-dispatch-scene-flow"` → covers TR-office-004

---

## Pre-Gate Checklist

- [ ] `tests/unit/` and `tests/integration/` — run `/test-setup`
- [ ] `.github/workflows/tests.yml` — run `/test-setup`
- [ ] `design/accessibility-requirements.md` — run `/ux-design`
- [ ] `design/ux/interaction-patterns.md` — run `/ux-design`
- [ ] Formal GDDs in `design/gdd/` — run `/design-system`

Gate-check not yet available until above items are complete.
