# HUD Design — Black Commission

> **Status**: In Design
> **Author**: Yan Dai + ux-designer
> **Last Updated**: 2026-06-09
> **Template**: HUD Design

---

## HUD Philosophy

The HUD does not pretend to be a game overlay. It pretends to be paperwork.

During missions, all survival-critical information is always visible — but organized as a field brief clipped to a mental clipboard, not as a game UI. The information exists; its presentation reads as bureaucratic documentation. A line of text, a ruled column, a stamp block.

**The field-form test**: every HUD element must be reproducible in black ink on a photocopied form from a third-rate civic office. If it looks like game chrome rather than administrative paperwork, it fails.

**Density rule — "present but subdued."** All decision-relevant data surfaces during missions: objective, clock, monster status, HP, hotbar. None of it competes for attention. Elements exist until they need to be urgent — at which point they shift register to stamp-red to demand a glance, then fall back.

**HQ context**: the office computer terminal is the game's loud information surface. The in-mission HUD is its inverse — quiet, ledger-formatted, background. They share the same visual grammar but operate at opposite volume levels.

**The wristwatch rule**: the time mechanic doubles as a design test for the whole HUD. Without a wristwatch, the player sees daylight-judgment language ("ambient daylight read") — approximate, ambient, human. With a wristwatch, they see exact clock values. The HUD earns precision only when the player invests in it. Apply this thinking to other elements: start subdued, escalate only when earned or urgent.

---

## Information Architecture

### Full Information Inventory

All information the game generates during a mission, mapped to its source system:

| # | Information | Source System | Notes |
|---|---|---|---|
| 1 | HP (current / max) | PlayerHealth.cs | NetworkVariable, per-player |
| 2 | Stamina (current / max) | PlayerController | Depletes on sprint; recovers on idle |
| 3 | 5-slot hotbar (item icon, quantity, slot number) | MvpHotbar.cs | Slot 1–5, bottom-center |
| 4 | Flashlight battery | FlashlightController | Displayed inside slot, not as a separate element |
| 5 | Mission objective (brief text) | MvpMissionManager | Changes on phase transition |
| 6 | Carrier identity (who holds the target) | MvpMissionManager | Activates on target pickup |
| 7 | Field clock / time remaining | MvpMissionClock | Wristwatch unlocks exact value |
| 8 | Overtime warning | MvpMissionClock | Fires when clock expires |
| 9 | Bonus evidence collected | MvpMissionManager | Optional secondary objective |
| 10 | Monster state (quiet / alert / chasing) | MonsterAI | Audio-primary; chasing fires HUD alert |
| 11 | Power gate restored | PowerGateController | Server-auth; fires once when F1 power restored |
| 12 | Heavy carry state | PlayerController | Active while holding the sales model |
| 13 | Downed / being revived state | PlayerHealth.cs | Per-player; fires on HP = 0 |
| 14 | Floor indicator | Environment | Implicit — stair geometry and signage |
| 15 | Crosshair / interaction prompt | MvpHud.cs | Center dot; turns CRT green on interactable |
| 16 | Damage flash | PlayerHealth.cs | Full-screen red tint on hit |

### Categorization

**Design direction**: Minimal persistent HUD — only survival-critical inputs the player cannot perceive without UI. Everything else is contextual (appears on trigger, fades) or diegetic.

| Category | Element | Rationale |
|---|---|---|
| **Must Show** (always visible) | HP bar | Cannot judge remaining survivability without it |
| **Must Show** | Stamina bar | Cannot judge sprint availability without it |
| **Must Show** | 5-slot hotbar | Inventory state is a continuous tactical input |
| **Must Show** | Flashlight battery (in slot) | Darkness makes battery state safety-critical |
| **Must Show** | Crosshair / interaction prompt | Spatial anchor; interaction requires feedback |
| **Must Show** | Damage flash | Immediate hit confirmation — no other channel |
| **Contextual** (triggers, then fades) | Mission objective text | Appears on phase change (≈4 s), then fades |
| **Contextual** | Carrier identity | Appears on target pickup (≈4 s), then fades |
| **Contextual** | Monster chase alert | Appears when chase begins/ends; absent when quiet |
| **Contextual** | Overtime warning | Appears when clock expires; stays until departure |
| **Contextual** | Power gate restored | One-time notification when F1 power is restored |
| **Contextual** | Downed / revive prompt | Active while downed; clears on revive |
| **Contextual** | Heavy carry badge | Appears on pickup; fades; returns on toggle if needed |
| **Diegetic / On Demand** | Clock / time | Without wristwatch: no HUD. With wristwatch: exact time shown. |
| **Hidden / Audio-primary** | Monster state (quiet / alert) | Sound design carries quiet and alert states; HUD is silent |
| **Hidden / Environmental** | Floor indicator | Architecture, signs, and stair geometry do this job |
| **Hidden** | Bonus evidence count | Removed from always-on; may surface as a one-time contextual |

---

## Layout Zones

### Zone Map

```
┌─────────────────────────────────────────────┐
│ ZONE A — Contextual Alerts (top-left)        │
│ Mission objective / Chase alert / Notif.     │
│ Shown on trigger only; fades in/out; no      │
│ persistent screen space                      │
│                                              │
│                    +                         │  ← ZONE B: Crosshair (center, fixed)
│                                              │
│                                              │
│                                              │
│          ┌──┬──┬──┬──┬──┐                  │
│          │  │  │  │  │  │                   │  ← ZONE C: Hotbar (bottom-center)
│          └──┴──┴──┴──┴──┘                  │
│          [HP  ▓▓▓▓▓▓░░░░]                  │
│          [STA ▓▓▓░░░░░░░]                  │  ← ZONE D: Status bars (above hotbar)
└─────────────────────────────────────────────┘
```

### Zone Definitions

| Zone | Position | Elements | Display Rule |
|---|---|---|---|
| **A — Contextual** | Top-left, 24px screen margin | Mission objective text, carrier identity, chase alert, overtime warning, power gate restored, heavy carry badge | Fade in on trigger (0.2s), hold 4s (notifications) or persist until state ends (warnings), fade out (0.5s) |
| **B — Crosshair** | Screen center | Crosshair (default neutral color; turns CRT green on interactable) | Always visible |
| **C — Hotbar** | Bottom-center, 24px from bottom edge | 5-slot hotbar (icon, quantity, slot number); flashlight slot contains battery strip | Always visible |
| **D — Status bars** | Directly above hotbar, 6px gap | HP bar (top), Stamina bar (bottom) | Always visible; both bars equal width, aligned to hotbar total width |

### Bar Alignment Rules

- HP bar and Stamina bar width = hotbar total width (sum of 5 slots including gaps)
- HP bar on top, Stamina bar below, 6px row gap
- Neither bar shows numeric labels; information is conveyed through fill length alone
- When HP drops to ≤25%, color switches to stamp-red (#C23B2B) to trigger urgency
- Stamina bar fades to 60% opacity at full value ("present but quiet"), returns to 100% during sprint

### Damage Flash

Full-screen overlay, not attached to any fixed zone. Red (#C23B2B, alpha 0.35), full opacity on hit frame, fades out over 0.3s.

---

## HUD Elements

[To be designed]

---

## Dynamic Behaviors

[To be designed]

---

## Platform & Input Variants

[To be designed]

---

## Accessibility

[To be designed]

---

## Open Questions

[To be designed]
