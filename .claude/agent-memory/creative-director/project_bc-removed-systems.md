---
name: project-bc-removed-systems
description: Black Commission — reputation/XP/OfficeLevel/takeover-pressure are DESIGN-removed but still LIVE in code as of 2026-06-17; economy is money-only by intent
metadata:
  type: project
---

On Black Commission, PM Yan Dai removed (design decision, 2026-06-17): internal
Reputation, OfficeLevel (1–8), XP, and the HostileTakeoverPressure 0–100 counter +
pressure-ladder FSM. Settlement is intended to produce MONEY ONLY. Hostile takeover
survives ONLY as narrative flavor (letters/visitors) tied to being broke
(`CompanyState.Funds < 0`), never to a settlement-driven score or a UI pressure bar.

**Why:** these systems made the office a numbers-go-up management game, contradicting
the "broke office, survival is the only motive" pillar and the "not a numerical
pressure meter" anti-pillar.

**How to apply:** Reject any design that re-imports rep/XP/office-level/pressure-counter.
The 5 license stages (story-gated) are the ONLY progression. See [[project-bc-settlement-satire]].

**CRITICAL CODE REALITY (verified 2026-06-17):** the removed systems are STILL LIVE in
code. `CompanyState.ApplyMissionResult()` still runs `HostileTakeoverPressure` deltas
(−25/+12/+35), `TryApplyLevelUps()` (XP→OfficeLevel), `Reputation`, and
`TryApplyHostileTakeover()` (the FSM). The scavenge loop pays out through
`MvpPendingReward.Set(money,0,0,...)` → `MvpPendingReward.Claim()` →
`CompanyState.ApplyMissionResult(...)`, so a Failed scavenge run STILL increments the
pressure counter. `OfficeComputer` still gates tasks on `OfficeLevel`/`Reputation`.
A doc claiming "money-only, no pressure counter incremented" is FALSE against the build
until `CompanyState`/`MvpPendingReward`/`OfficeComputer` are gutted. Treat that gutting
as a blocking code change, not a downstream doc rewrite.
