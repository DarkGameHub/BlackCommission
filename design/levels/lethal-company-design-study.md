# Design Study: Why Lethal Company Works — and How Black Commission Borrows the *Method*

> **Production-method reference only.** Per `@AGENTS.md`, Lethal Company is studied
> for *why* its design produces tension and disorientation. We do **not** copy its
> assets, UI, monsters, ship, quota fiction, item list, or map layouts. This doc
> extracts transferable *principles* and maps them onto Black Commission's own
> 2-floor abandoned-tower fiction.
>
> Authored 2026-06-07. Sourcing note: web search was blocked in the authoring
> environment (VPCSC). Mechanics below draw on the public **DunGen** documentation
> (the Unity procedural-dungeon asset Lethal Company uses) and established LC design
> knowledge, with the gameplay-loop summary confirmed against
> [Lethal Company — Wikipedia](https://en.wikipedia.org/wiki/Lethal_Company).
> Treat tile/dungeon-flow specifics as design intent to validate, not gospel.

---

## 1. The Tension Engine — why it succeeds despite "limited content"

LC's appeal is not content volume (IGN: it "transcends its limited content"). It is
several mechanics stacked into *continuous* risk-management tension:

1. **Time pressure** — leave before the ship departs or be abandoned. Exploration
   becomes a gamble; every extra room is a bet.
2. **Extraction stakes** — if no living player is aboard at departure, the whole haul
   is lost. Failure is expensive, so greed is punished.
3. **Two-handed loot + tiny inventory** — the *most valuable* loot makes you the
   *slowest and most vulnerable*, forcing multiple trips. This is the core friction
   of extraction horror.
4. **Threat scales with time/progress** — the longer you stay, the more and deadlier
   the spawns. The level "gets angrier." Greed has a rising cost curve.
5. **Proximity voice + information asymmetry** — see §2.4. Chaos becomes *social*,
   which is what made it spread.

**Takeaway for BC:** tension is a *system stack*, not a level-art problem. A prettier
whitebox alone won't fix "feels too easy" — the loop has to keep pressing.

---

## 2. The Disorientation Engine — getting lost is *designed*, not a bug

This is the property Yan Dai specifically wants. LC's "you get lost" feeling comes
from four things **stacked**:

### 2.1 Procedural *topology* (tile + doorway + main-path/branch)
Interiors are assembled from prefab **tiles** (room segments + corridor segments),
each with **doorway sockets**. DunGen wires them along a **main path** (a depth/length
of "lines") plus **branches** (dead-end spurs). **The connection graph itself is
different every run.** Factory / Manor / Mine are just different *tile sets* fed to the
same algorithm.

### 2.2 Tile repetition → weak landmarks
The same corridor and room prefabs recur, so **every junction looks alike**. You
genuinely can't tell where you are. Repetition is intentional — it manufactures
disorientation cheaply.

### 2.3 No away-team map
The away team has **no minimap**. Only the player who stays on the ship sees a crude
top-down radar via the terminal. Being lost becomes a **communication problem**, which
is what makes proximity voice essential and funny.

### 2.4 Darkness + limited flashlight
Your readable world shrinks to a light cone. You navigate **locally** and can never
assemble a **global** mental map. Darkness is a readability *throttle*, not decoration.

### 2.5 Fire exits
Besides the main entrance, several **secondary exits** sit at branch ends and connect
the interior to the surface. They are **escape valves** (you can flee) *and* a source
of disorientation (you pop out somewhere unexpected).

---

## ⚠️ 3. The key finding — why the current BC map feels like "child's play"

The existing slot system (`abandoned-tower-floorplan.md`, §2 and §6) explicitly
**"randomizes content, not topology"**: a *fixed* skeleton (envelope, corridors,
stairs, doors) with room *contents* shuffled into slots.

This is the **opposite** of LC. Consequence:

- **Fixed skeleton + reskinned rooms → players memorize the building in one run.**
  Run two onward, nobody is lost, nobody is tense → linear, predictable = feels trivial.
- LC's replayability and dread are rooted in **topology changing every run + tile
  repetition + no map**. Content-only shuffling can't produce that.

**The lever:** `TowerLayoutGenerator` already does **seed-synced determinism** (the
server picks one seed, replicates it as a `NetworkVariable<int>`, every peer runs the
same deterministic fill — only an `int` crosses the wire). That same machinery can be
upgraded from *random content* to *random topology* **without** breaking NavMesh bake
or netcode:

- Topology variation = toggling a **finite, known set** of connectors (doors/corridors)
  on/off per seed.
- Because the door graph is a known finite set and the seed is identical on all peers,
  **every variant is automatically navigable and net-syncable** — bake once, valid for
  all variants (or bake per-variant offline; the set is finite).

> This single change — **randomize topology, not just content** — is the biggest lever
> for making the tower feel like LC instead of a memorizable diorama.

---

## 4. The 8 transferable principles → Black Commission's 2-floor tower

| # | LC principle | Black Commission application (2-floor abandoned pre-sale tower) |
|---|---|---|
| 1 | **Procedural topology** (tiles, doorways, main path + branches) | Upgrade the slot system: connectors (corridors/doors) are **seed-toggled open/closed** so loops and dead-ends differ every run. Keep seed determinism. |
| 2 | **Tile repetition → weak landmarks** | The abandoned tower is naturally homogeneous (raw concrete, rebar, scaffold, tarps). Repetition *is* the disorientation and fits the noir-jobsite identity. |
| 3 | **1–2 strong landmarks for partial orientation** | Keep the **lit show-flat + scale model light-pedestal** as a beacon visible across the shaft; the **two stair towers** (one bright/exposed, one dim/enclosed) as the other anchor. |
| 4 | **Fire exits = escape valves** | Beyond the main return route, add **2–3 secondary descents** (side stair / maintenance ladder / scaffold fast-drop) **far from the van** → you *can* flee under pressure, but you surface somewhere unfamiliar. |
| 5 | **Time pressure + threat scales with time** | Reuse `MvpMissionClock`; make the **Infected Site Inspector** patrol wider / hunt harder the longer the team stays inside. |
| 6 | **Two-handed loot = extraction vulnerability** | The heavy two-hand scale model carry (slowed carrier, hotbar lock, droppable/relay) is already designed — keep it as the core friction. |
| 7 | **Darkness + navigable light** | Pre-power = flashlight-dependent dark (sodium-amber emergency strips); post-power = harsh worklights. Light is the only readability tool. |
| 8 | **Objective ↔ exit separation + risk gradient** | Scale model far NW on F2; van south-center on F1. Carrying it out forces the full descent — the pressure phase. Strengthen, don't change. |

---

## 5. What we deliberately do NOT take from LC

- No copying of LC's tile prefabs, factory/manor/mine art, monster designs, terminal,
  ship, or any specific room layout.
- BC keeps its **vertical-tower identity** (floor-by-floor risk + scaffold bridge over
  a shaft) — LC is mostly single-level sprawl; we borrow the *disorientation method*,
  not the footprint.
- BC's fiction, palette (Municipal Debt Noir), and satire (civic paperwork, partial
  settlement, hostile-takeover pressure) are entirely our own.

---

## 6. Decisions locked from this study (PM Yan Dai, 2026-06-07)

- Keep the **2-floor** structure; densify each floor.
- **Randomize topology, not just content** (the headline change).
- **3 room sizes: S / M / L** (drop XL).
- **~15 rooms per floor (~30 total)**, LC-density target, 15-minute 1–4p run.
- Players must feel **oppression but retain escape options** → multiple fire-exit
  descents + the dim safe stair as relief valves.

> Next artifact: the new annotated top-down map applying all of the above —
> `design/levels/abandoned-tower-redesign-v2.md` (or an update to the existing
> floorplan). 3D rebuild begins only after PM approves that map.
