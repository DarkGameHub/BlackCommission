---
name: project-bc-settlement-satire
description: Black Commission — the per-item settlement reveal (price + client usage note) is Core, not Polish; it is Pillar 3's primary mechanical voice
metadata:
  type: project
---

On Black Commission, the per-item SETTLEMENT REVEAL (each delivered item's price +
the Mars client's authored "intended use" note, shown in emotional-weight order, with
a one-dispute step) is the game's satirical voice. It is CORE content, not a
Presentation-layer deferral.

**Why:** Pillar 3 "the contract speaks" — satire is delivered through settlement
screens and deduction clauses, never narration. The punchline lands at settlement,
not on the map (`docs/world-background-2098.md`). Without the reveal, the loop is just
hauling boxes for numbers.

**How to apply:** Treat the reveal as a blocking milestone for the scavenging loop, not
a Polish backlog item. The current build ships a total-only settlement card
(`SettlementCardOverlay`); the per-item reveal is the next required build, and the
settlement DATA already exists (`SettlementResult.Lines` carries per-item payout lines).
The moral-slope falsifiability test lives here: if you strip the mission-type label and
players can't tell Free Salvage from Black Commission by the commission text + settlement
notes alone, the satire failed. See [[project-bc-removed-systems]].
