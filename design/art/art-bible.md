# Black Commission — Art Bible

> **Status**: Complete — all 9 sections authored 2026-06-09.
> **Source of truth for style decisions**: `docs/art/black-commission-style-lock-v2.md` (低保真工业恐怖, 2026-06-10)
> — fidelity clauses in this bible are AMENDED by style-lock v2 §6 (lo-fi low-poly, ≤256px textures).
> **Art Director Sign-Off (AD-ART-BIBLE)**: Skipped — Lean mode (2026-06-09)
>
> **Note:** `docs/art/black-commission-style-lock-v1.md` partially superseded — warm tungsten amber is now the primary accent; CRT green is restricted to electronic screens only. Update style-lock-v1 to reflect this when convenient.

---

## 1. Visual Identity Statement

### The Litmus Test

> **If it looks like it belongs in a functioning, funded, or clean institution, it does not belong in Black Commission.**

Apply this to every asset decision. If a prop, material, lighting choice, or UI element reads as new, maintained, or purposefully designed, it fails. Every element of this game should read as something that was built cheap, used hard, and never properly replaced.

### Supporting Visual Principles

**Principle 1: Concrete and Green Before Anything Else**

Every environment is built from the same narrow, desaturated foundation — concrete gray (60%), military green (20%), old wood brown (15%), and rust (5%) — with CRT green (`#6CFF5F`) as the only allowed bright accent, reserved exclusively for electronics and UI.

Design test: When a prop's color is ambiguous, choose concrete gray or military green before any other option. If a color reads as clean, saturated, or decorative, remove it.

Pillar served: The broke office / Hostile takeover pressure.

---

**Principle 2: Wear Is Function, Not Decoration**

Every material must carry 20–40% visible weathering at roughness 0.6–0.85. Scratches, tape marks, dust, chipped paint, and stain residue are not polish-pass additions — they are the primary surface language that makes spaces feel like neglected public facilities rather than designed game environments.

Design test: When a material feels ambiguous, ask whether it looks like it has been used by underfunded workers for several years. If it could appear in a product render, it is too clean.

Pillar served: Suspicious civic paperwork / Weird local-client jobs.

---

**Principle 3: Every Prop Must Earn Its Place By Doing One of Three Jobs**

A prop is allowed in the scene only if it does one of the following: guides the player to an objective or exit, communicates the failing office and its debt pressure, or makes a recognizable public facility feel uncanny. Decorative density is not a goal. Fewer props with clear function always beats more props with ambiguous purpose.

Design test: When the purpose of a prop is ambiguous, ask which of the three jobs it performs. If the answer is none, cut it.

Pillar served: The dispatch van ritual / Partial settlement choices.

---

### Statement For Outsourcing Partners

Black Commission is a semi-realistic industrial horror co-op game. The visual world is built from worn concrete, military-green cabinets, old wood, rusted maintenance hardware, and CRT screen glow. The game takes place in neglected public facilities — schools, service corridors, warehouses, clinics — run by underfunded outsourced contractors.

The correct mood is: cheap, heavy, practical, and slightly wrong. The incorrect moods are: clean, heroic, polished, atmospheric-horror-generic, or stylized-cartoon.

The only allowed bright color is CRT green (`#6CFF5F`), used only on electronics and UI. All other color must be desaturated, worn, and readable under either cold 5000K industrial light or warm 3000K tungsten. No colored point lights. No neon. No chrome. No clean plastic.

When in doubt: **would this object survive five years in an underfunded government facility?** If yes, it belongs. If no, it does not.

---

## 2. Mood & Atmosphere

The palette never changes across game states. The feeling does — by shifting the ratio of the three allowed light families, the energy of movement, and the psychological weight of the space.

### State 1: HQ / Office

**Primary Emotion:** Anxious obligation — not fear. The threat here is financial, not physical.

**Lighting:** Warm tungsten 3000K dominant over desks and sofa corner. Cold industrial 5000K intrudes from the garage bay and supply corridor. CRT green at the office computer — the only source of clarity. High ambient, low contrast, shallow shadow depth.

**Atmospheric Descriptors:** Stale, indebted, procedural, dim-habitual, paper-heavy.

**Energy Level:** Contemplative with low-grade procedural anxiety. The pace is the player's to set.

**Mood Carrier:** The CRT monitor. Green bleeds softly onto the desk surface. Everything else recedes into tungsten amber. The screen is the brightest point in frame and the only thing in the room with an agenda.

---

### State 2: Dispatch Van Transit

**Primary Emotion:** Committed unease. Decision made. Players are locked in and moving.

**Lighting:** Cold industrial 5000K overhead strip only — the kind installed in cargo areas. CRT green from dashboard instruments and detector equipment. No tungsten. Tungsten lives in the office; its absence marks the van as not-home. Single hard overhead key, minimal fill, moderate shadow depth on faces.

**Atmospheric Descriptors:** Rattling, committed, tin-walled, instrument-lit, outbound.

**Energy Level:** Measured. The van has its own rhythm. Players are physically constrained and temporally suspended.

**Mood Carrier:** The view through the van windows — exterior darkness at 95%, just enough ambient to read passing streetlights or treeline silhouettes. Lit interior vs. opaque exterior.

---

### State 3: Mission Site Exploration

**Primary Emotion:** Investigative dread. The facility is wrong in ways that are hard to name.

**Lighting:** Cold industrial 5000K, but failing — tubes flickering, some dead, some at 60–70% output. CRT green from remaining powered equipment (exit signs, security panels, vending machines) on independent power — these do not flicker. No tungsten: tungsten belongs to human habitation, and these spaces were habited once; they are not now. Low ambient, high local contrast.

**Atmospheric Descriptors:** Institutional-hollow, fluorescent-faltering, still-used-feeling, wrongly-quiet, evidence-dense.

**Energy Level:** Measured to cautious. Movement is deliberate. Silence is the baseline.

**Mood Carrier:** The dead zone — every mission site has one section where all overhead lighting has failed and the only visible source is a distant CRT green glow. Players navigate toward the green.

---

### State 4: Chase / Threat Active

**Primary Emotion:** Visceral urgency — immediate, physical, present-tense danger with a specific direction.

**Lighting:** Same three families, ratios collapse under kinetic stress. Handheld lights swing and bounce, breaking State 3's flat distribution. Cold industrial becomes strobing or absent in areas the threat occupies (neglected zones fail naturally — no horror-movie reactive lighting). CRT green becomes the only reliable orientation signal. No new colors introduced.

**Atmospheric Descriptors:** Lurching, shadow-fractured, equipment-lit, sprint-compressed, tunnel-vision.

**Energy Level:** Frenetic. What was readable is now obstacle; what was ambient is now threat-sound.

**Mood Carrier:** The player's handheld light beam swinging across walls and floor during a run — the single most legible visual signal that the state has changed.

---

### State 5: Extraction

**Primary Emotion:** Calculated desperation. The objective is in hand. The question: how much is it worth to stay?

**Lighting:** Same as State 3, with one rule: the exit path is always lit. At least one functioning cold industrial source per corridor segment on the route out. CRT green from exit signs and detector panels forms a dotted visual path toward the extraction point. The game never hides the way out — the difficulty is choosing to take it.

**Atmospheric Descriptors:** Weight-carrying, exit-focused, countdown-pressured, deliberate-urgent, half-safe.

**Energy Level:** Urgent but not frenetic. Players move with purpose, slowed by the weight of the carried objective.

**Mood Carrier:** The exit door — framed by hard 5000K light visible at a distance. The brightest static point in the scene; reads as both destination and threshold.

---

### State 6: Settlement / Resolution

**Primary Emotion:** Exhausted reckoning. Back at HQ. The accounting begins.

**Lighting:** Exact return to State 1 HQ lighting — warm tungsten 3000K over desks, cold industrial from garage. CRT green on the settlement screen delivers the verdict. The sameness is deliberate: the office does not celebrate or mourn. It processes.

**Atmospheric Descriptors:** Carbon-copy-quiet, ledger-lit, transactional, post-adrenaline, numerically-honest.

**Energy Level:** The lowest in the game, by design. The loop must breathe before it restarts.

**Mood Carrier:** The settlement screen — the same CRT monitor from State 1, now showing the job outcome with the same green spill on the same desk surface. The visual rhyme between job-offer and settlement is the game's primary emotional callback.

---

### State Transition Summary

| From | To | Emotional Gear Shift |
|---|---|---|
| HQ | Van | Tungsten warmth drops out; cold strip takes over |
| Van | Mission Site | Controlled interior light gives way to institutional failure |
| Exploration | Chase | Static light becomes kinetic; ambient becomes obstacle |
| Chase | Extraction | Panic narrows to purpose; only the exit path matters |
| Extraction | Settlement | Urgency empties into accounting; same indifferent tungsten receives the players |

---

## 3. Shape Language

### The Foundational Rule

> **Every shape in Black Commission was made to be functional, affordable, and easy to repair. No shape exists because it was designed.**

Shape decisions follow government procurement logic: pick the cheapest flat-pack option from the nearest supplier catalog. No one hired a designer.

---

### Environment Geometry: The Dominant Shape Family

**The rectangle rules everything.** Five allowed geometry archetypes:

| Archetype | Examples | What It Communicates |
|---|---|---|
| **Slab** | Concrete walls, floor panels, tabletops, cabinet sides | Weight, permanence, cheapness |
| **Frame** | Door frames, shelf brackets, notice board borders | Structure that was assembled, not grown |
| **Tube** | Exposed pipes, conduit, railing, cable trunking | Utility on the surface because hiding it costs more |
| **Plate** | Cabinet doors, drawer fronts, locker faces, ceiling tile grids | Replaceable surfaces bolted onto frames |
| **Bolted assembly** | Tool walls, multi-drawer units, rack shelving, service panels | Put together with hardware, can be taken apart the same way |

Allowed curves: drain holes, pipe bends, rounded drawer pulls, worn corner edges where material has chipped to a radius. These are incidental, not designed. A deliberately sculpted organic curve on any prop is not allowed.

---

### Character Silhouette Philosophy

Black Commission treats characters as evidence, not as subjects.

**Player Characters:** Must be readable at 8–10m under a single overhead light at 30% ambient fill. Each player needs one distinct vertical silhouette break (helmet brim, hood, shoulder width). Equipment is worn, not sleek — rectangular backpacks, cylindrical flashlights, nothing with a sculpted ergonomic shell.

Silhouette check: *can you identify which player it is from a 2-color silhouette at thumbnail size?*

**Threats:** Break the rectangular rule selectively — wrong proportions in the right environment. Too tall for the door frame, too wide for the corridor, holding still where no worker would stand. Unsettling threat design comes from inappropriate scale and stillness in a recognizable institutional context, not from monster-fantasy shapes.

**Character-Light Implication:** The desk does more narrative work than the person sitting at it. Characters receive the lowest asset budget. The environment is the story.

---

### UI Shape Grammar: The Document Layer

The UI does not pretend to be a game HUD. It pretends to be paperwork.

All UI panels derive their shape from physical document formats: A4 ratios, stamp blocks, form fields, manila-folder tabs, receipt strips, ledger rows.

- **Panel borders:** single-line right-angle frames only — no rounded corners, no drop shadows, no glass effects
- **Buttons:** form-field rectangles. Active state = inverted (dark field, light text). Hover = thin underline or bracket mark, not a glow
- **Notifications:** stamp-mark blocks — heavy rectangular, uppercase, minimum decoration
- **Settlement screen:** a ledger — rows, columns, ruled lines, numeric entries
- **Hotbar:** locker-slot logic — equal-width rectangular slots in a horizontal row, items sit flat

Governing principle: *if a UI element's shape could not be reproduced on a photocopier using only black ink and ruled lines, it is too decorative.*

---

### Hero Shapes vs. Supporting Shapes

**Hero shapes** (draw the eye, hold it):
- The CRT monitor — brightest rectangle in HQ, communicates agency
- The notice board — densest vertical rectangle, layered paper, the environment's story surface
- The exit door — standard door frame + lit threshold, the extraction anchor
- Carried objectives — distinct value read from surrounding surfaces in first-person view

**Supporting shapes** (build density without competing):
- Stacked chairs, secondary shelving, filing cabinet banks, cable bundles, breaker boxes. Read as mass and texture from a distance, not as individual objects.

Rule: **one hero shape per quadrant of navigable space.** Never place two hero shapes adjacent — they cancel each other's visual pull.

---

### The Silhouette Test

> **If it does not read as a recognizable, purchasable object from a government supply catalog at 2-color silhouette, it does not belong.**

Convert prop to flat black fill on white, reduce to 64×64px. Ask:
1. Can it be named in two words or fewer? (filing cabinet, desk lamp, fire extinguisher)
2. Does its silhouette imply function without surface detail?
3. Could it appear across multiple institution types (office, school, clinic, depot)?

Yes to all three: the prop belongs.

---

### Shape Consistency Across Environments

The three environments share the same grammar with controlled variation:

| Environment | Character | Variation |
|---|---|---|
| **HQ Office** | Rectangular + paper-layer density | Higher prop count — actively inhabited |
| **Dispatch Van** | Strictest rectangular environment — panels, bench seats, locker unit, dashboard row | Lowest prop count — constraint, not accumulation |
| **Mission Sites** | Same vocabulary, visible failure — ceiling tile displacement, gap where a prop was removed | Higher darkness, lower density (neglect = absence, not ruin) |

The shape grammar does not change between environments. The **condition** of the grammar changes. A locker at HQ has its door closed and a padlock. A locker at a mission site has its door open and nothing inside.

---

## 4. Color System

### Primary Palette — Surface Colors

The surface palette is the world before any light touches it. These are material colors only — not chosen for aesthetic reasons, but by a purchasing officer from a catalog, in bulk, at the lowest acceptable price.

#### Concrete Gray Family

| Hex | Use |
|---|---|
| `#5E5E5E` | Main walls, floor slabs, exposed pillars, large poured-concrete forms |
| `#707070` | Worn panel faces, dusty painted concrete, secondary wall runs |
| `#4A4A4A` | Shadow-facing surfaces, grime accumulation, dark rubber seals |

**What it communicates:** Permanence bought cheap. The range from `#5E5E5E` to `#4A4A4A` reads depth and surface condition without additional color. This is the environment's silence — the color that recedes so light and accent can do their work.

**Dominant scene weight: 60%**

---

#### Military Green Family

| Hex | Use |
|---|---|
| `#55624A` | Metal cabinets, van body panels, metal furniture, lockers |
| `#68745C` | Faded painted metal, old interior doors, equipment cases |
| `#475040` | Dark green trim, heavily shadowed shelf undersides, heavy steel frames |

**What it communicates:** Institutional procurement — the standard finish for government-surplus metal furniture. Must read as faded, chalky, slightly yellow-brown at wear edges. Never decorative foliage green. Never military-precision green.

**Dominant scene weight: 20%**

---

#### Old Wood Brown Family

| Hex | Use |
|---|---|
| `#6B5440` | Desks, bookcases, school furniture, countertops |
| `#7C624A` | Worn chair components, floorboards, wooden crate slats |
| `#8A7158` | Edge wear, scuffed exposed grain, high-contact handles |

**What it communicates:** Human habitation at low cost. The surfaces are layered — original varnish, then scratches, then a cheaper second coat, then more scratches. This is where people sat, worked, and left behind the residue of daily labor.

**Dominant scene weight: 15%**

---

#### Rust Family

| Hex | Use |
|---|---|
| `#7B4B2A` | Small rust patches on lower wall sections, pipe joints, drain rims |
| `#8C5937` | Aged metal wear on high-contact edges, exposed pipes |
| `#A36842` | Tiny oxidation accents, bolt heads, screws, surface bleed |

**What it communicates:** Time and neglect. Rust is not a mood color — it is a material event. It appears only where water, friction, and absent maintenance meet physically plausible locations. Rust on a lower cabinet seam near the floor is authentic. Rust on the top face of a cabinet is not.

**Dominant scene weight: 5%**

---

#### Scene Balance Reference

| Environment | Concrete Gray | Military Green | Old Wood | Rust | Notes |
|---|---|---|---|---|---|
| HQ civic interior | 60% | 20% | 15% | 5% | Human-inhabited; wood presence higher |
| Dispatch van interior | 65% | 25% | 5% | 5% | Metal and rubber dominant |
| Underground warehouse / service corridor | 60% | 15% | 10% | 5% | + 10% faded industrial safety yellow (material only) |
| Mission site civic rooms | 60% | 20% | 15% | 5% | Same as HQ but worse condition |

---

### Accent System — Updated Direction

**This section supersedes all prior references to CRT green as the primary accent. Warm tungsten amber is now the primary accent. CRT green is secondary.**

---

#### Primary Accent: Warm Tungsten Amber

Warm yellow-orange tungsten light is the primary accent. It signals inhabitation — that someone was here, or still is. It is applied as actual light sources only, never as broad wall material color.

| Parameter | Value |
|---|---|
| Color temperature | 2700K–3000K |
| Hue range (light color) | `#FFAB40` to `#FF8C00` |
| Pendant / bare bulb | `#FF9820`, intensity 1.5–2.5 URP Lux |
| Desk lamp | `#FFAB40`, intensity 0.8–1.2 URP Lux |
| Work lamp | `#FFA030`, intensity 1.0–2.0 URP Lux |

**Where it appears:** pendant bulbs, desk lamps, aged fluorescent tubes rated warm, work lamps on poles or clamps.

**Where it must NOT appear:** van interior (van is cold light only — tungsten is home; the van is not home), any mission site that has lost electrical power, as a wall material tint, as a large area emissive.

**What it communicates:** This space is or was inhabited. During States 1 and 6 (HQ), tungsten amber dominates. During mission site exploration, a pocket of warm amber means someone was working here — and may not be gone.

---

#### Secondary Accent: CRT Green — Electronic Screens Only

CRT green has one role: the color of electronic display surfaces and their immediate spill.

| Parameter | Value |
|---|---|
| Hex | `#6CFF5F` |
| Allowed on | Computer monitors, detector panels, exit-sign LEDs, portable instrument readouts |
| Spill radius | Max 0.5m from screen surface |
| Emissive intensity | Low — readable in a dark room, does not light the room |
| Disallowed on | Walls, floors, ambient lighting, decorative props, large area lights |

**What it communicates:** This device is powered and processing. In a failing facility where overhead lights are dying, a powered CRT is a minor anomaly. It does not mean safe. It means something here still has electricity.

---

#### Tertiary: Faded Industrial Safety Yellow (Material Only)

Painted surface color. Appears on physical warning markings, floor stripes, stair edges, railings, bollards, forklift paint. Always desaturated, scratched, aged.

| Parameter | Value |
|---|---|
| Material hex range | `#C8A020` to `#A87E10` |
| Disallowed | Clean ANSI safety yellow `#FFD700` or brighter |
| Application | Floor stripes, step edges, barriers — always as material, never as light |

---

### Semantic Color Rules

This game does not use a standard danger/safe/interactive color-code system. Semantics are built from materials, lighting, shape, and sound.

| Signal | Cue | How It Works |
|---|---|---|
| Interactable / objective nearby | Warm amber spill `#FFAB40` + shape isolation | Light pools near or on the object |
| Powered / usable device | CRT green LED dot `#6CFF5F`, 2–4cm real-world on device face | Visible up close; also emits audio hum |
| Mission objective object | `#D4A020` warm golden-yellow material | Only object in the scene with this specific warm material |
| Threat presence | Deep amber-orange eye pinpoints `#FF6A00` in cold-light environment | Two isolated warm points in cold space + behavioral audio |
| Environmental hazard zone | Faded safety yellow stripes `#C8A020`–`#A87E10` | Physical marking at edges, stairs, barriers |
| Debt / failure / overdue | Stamp red `#C23A2B` on paper and signage only | Print / label material — never a light, never on creatures |
| Exit / extraction | Brightest cold-white zone in frame, or CRT green exit-sign spill if power out | Light-level differential, not unique hue |

**Danger is communicated by:** scale + stillness + behavioral wrongness + amber eye pinpoints — not by red color on the creature body.

---

### Per-Environment Color Temperature Rules

**HQ Office:** Warm primary (2700K–3000K pendant, desk lamps) + cold intrusion from garage threshold (5000K) + CRT green on computer only. Emotional read: lived-in refuge with an industrial threshold visible in the background.

**Dispatch Van:** Cold only (4500K–5000K overhead strip). No tungsten — hard rule. Tungsten is home; the van is not home. The cold strip communicates transit and commitment.

**Mission Sites:** Cold failing (4500K–5000K, flickering, some dead tubes) + pocket warm residual (a desk lamp still plugged in, signals *someone was here*) + CRT green exit signs if power is out. Exit corridor always has the brightest functioning cold source in the environment.

**HQ Settlement Return:** Identical to HQ Office. The office does not celebrate or mourn — it processes in the same indifferent warm light.

---

### Colorblind Safety

Every hue-dependent gameplay distinction has a shape + audio backup channel. No distinction is color-only.

| Distinction | Risk | Backup |
|---|---|---|
| CRT green device (powered) | Moderate | LED dot geometry on device face + audio hum |
| Threat amber-orange eyes vs. lamp amber | Moderate | Two isolated pinpoints vs. broad spill; creature has behavioral audio before visual contact |
| Safety yellow floor markings | Low | Stripe geometry at edges and stairs |
| Objective warm yellow vs. surrounding paper | Moderate | Shape isolation + interaction prompt at 2.5m approach |
| Exit sign green in dark | Moderate | Exit-sign silhouette geometry + directional audio at threshold |
| Stamp red on paper | Low (non-gameplay) | Text content of the stamp carries the message |

The surface palette (concrete gray, military green, old wood, rust) carries no gameplay-critical distinctions — only environmental storytelling.

---

## 5. Character Design Direction

### Governing Principle

> **Characters are evidence that people exist, not subjects that demand attention. The environment is the story. Characters are secondary detail within it.**

When a character decision conflicts with the character-light rule, the rule wins.

---

### Player Character Visual Archetype

**The "Cheap Commission Worker" silhouette** — someone hired on short notice from a temp agency, outfitted from a supply closet, and sent to a site they have not been to before. Nothing they wear was chosen. It was assigned.

| Zone | Required Element | Notes |
|---|---|---|
| Head | Practical cap, safety helmet, or hooded jacket | Must produce a distinct head-break. No bare heads, no themed helmets. |
| Torso upper | High-visibility vest over cheap jacket or coverall | Vest is the one deliberate silhouette widener. Always slightly oversized. |
| Torso middle | Company badge — small rectangle, clipped to left chest | Only identity element on the body. Tiny. Functional. |
| Back | Rectangular daypack or site bag | No sculpted ergonomic shells. A zipper and a strap. |
| Hands | Carried tool (torch, clipboard, equipment case) | The hand that holds something is the hand doing the job. |
| Legs | Cargo trousers, coverall lowers, or plain work trousers | No tactical, cargo-shorts, or athleisure. |
| Feet | Work boots — wide toe, visible sole ledge | The silhouette base must read heavy. No sneakers. |

**Underlying proportions:** Real-world scale. A 1.75m worker is 1.75m in the scene. No heroic scaling, no widened shoulders. Interest comes from what is worn and carried, not body proportion.

---

### Distinguishing Across 1–4 Players

Each player must be silhouette-readable in a 2-color thumbnail at 8–10m game-camera distance. Distinction is carried by head-zone variation and one high-contrast element — not by color.

| Player | Head Zone | High-Contrast Element | Default Carried Tool |
|---|---|---|---|
| P1 | Orange-band safety helmet — broadest head silhouette | Reflective safety tape band (physical material, not a color choice) | Handheld torch (cylindrical, short) |
| P2 | Knitted work beanie — narrow round silhouette | Lighter fleece over dark jacket creates tonal body split | Equipment case (rectangular, carried low) |
| P3 | Hard-shell bump cap with peak — flat top, forward brim | Peak casts a strong shadow line across the face | Clipboard and forms folder (thin, flat) |
| P4 | Hood up — widest soft silhouette | Hood hem and drawstring create broken outer edge | Coiled cable or shoulder-mounted unit (asymmetric mass) |

No player-color overlays, auras, or UI-style identity markers. If a teammate cannot be identified from their silhouette, the silhouette is the problem.

---

### Worker vs. Threat: Instant Read Rules

| Element | Player Characters | Threats |
|---|---|---|
| Overall height | 1.65–1.85m standard range | 1.95–2.4m minimum — noticeably wrong |
| Width | Normal corridor clearance | Wider than the corridor, or unnaturally narrow |
| Reflective element | Yes — safety tape | None |
| Head shape | Readable daily-wear headgear | Absent, malformed, or partially covered |
| Eye glow | None | Amber-orange pinpoints `#FF6A00` only |
| Carried object | Always present | Absent or held wrong |
| Torch beam | Always present when equipped | Never |

Threats are wrong by proportion, stillness, and location — not by monster-fantasy shape.

---

### Expression and Pose Style

**Target physical language:** tired competence. Workers who have done this kind of work before, not frightened by the job itself, but carrying the weight of debt pressure in how they hold themselves.

- Weight shifted to one hip, not standing at attention
- Slight forward lean when looking at anything — a habit of workers who bend toward what they are doing
- No crossed arms, no resting pose that reads as comfortable

**Under threat:** Posture drops to a crouch — threat-avoidance, not combat-stance. Workers run poorly and crouch well. They are not trained to fight.

**Threats:** If removing the threat from a scene would make that specific spot feel wrong, the pose is correct. Threats occupy space the way a stopped machine does — in functional locations for machines, not for people.

---

### LOD Philosophy

Characters receive the lowest asset budget in the game. The environment holds the detail.

| Distance | Preserved | Dropped |
|---|---|---|
| 0–3m | All silhouette elements; vest material; badge as a rectangle | Badge text, thread detail, zipper pulls |
| 3–8m (typical working range) | Head zone; high-contrast element; carried object shape | Face detail, material micro-detail |
| 8–15m | Overall silhouette; head zone distinguishable | Mid-resolution surface detail |
| 15m+ | Silhouette only — must still pass 2-color test | Everything except mass shape and head break |

**What is never needed:** Real-time facial animation (first-person game), subsurface scattering on skin (never seen), high-res hand models (gloves recommended).

---

### Monster Design Framework

#### The Formula

> **What ordinary workplace or public-service pressure became physical here?**

Every threat is a job-site condition that materialized. Not a demon, not a generic horror creature — a thing that belongs to this specific facility and this specific commission.

**Design sequence:**
1. **Name the pressure** — what stress did workers or visitors experience at this institution?
2. **Find the material form** — what physical materials are native to that pressure? (Paper, ledgers, ID lanyards, delinquency notices, maintenance tools, rubber stamps.)
3. **Make the proportions wrong** — wrong scale, wrong posture, wrong location, or wrong stillness.
4. **Apply the palette** — gray, military green, dirty fabric, old paper, rusted metal. No exotic colors.
5. **Set the signal** — amber-orange eye pinpoints `#FF6A00` only. No red body color. No fantasy luminescence.

**Example — School Commission Threat (Debt/Homework Collector):**
- Pressure: academic debt, parent financial obligation, inspection bureaucracy
- Material: layered examination papers and delinquency notices as body mass, ID lanyard too long (brushing floor), old uniform fabric
- Proportion wrong: head does not clear doorframes; moves at the pace of someone looking for something specific, not hunting
- Palette: gray paper mass, military-green uniform fragments, old-wood-brown ID card
- Signal: paper-rustle audio before visual; amber-orange pinpoints appearing inside a room the player was about to enter

#### Threat Design Rules

| Rule | Rationale |
|---|---|
| Each threat is site-specific | The formula is site-tied; recycling threats breaks the fiction |
| Threat bodies use only existing palette colors | No color budget for new hues; exotic colors signal fantasy horror |
| Amber-orange eyes are the only warm-color element on any threat | Warm light signals habitation; isolated pinpoints signal wrongness |
| No hero shapes | Wrong-proportioned recognizable forms, not fantasy-sculpted creatures |
| First signal is behavioral (stillness, wrong location), not visual | Eye glow is confirmation, not discovery |
| Threat names derive from the job, not taxonomy | "The inspector," "the collector," "the maintenance form" — not "shadow beast" |

---

### Character-Light Enforcement Summary

1. A character should never be the most visually complex object in the frame it occupies.
2. Indirect evidence of people outweighs direct depiction — an occupied chair, a still-steaming mug, a name badge on a desk.
3. Players are not the art. The commission is the art.
4. A scene that depends on its characters for atmosphere has failed before the characters were added.

---

## 6. Environment Design Language

### Architectural Style and Institutional History

Every space in Black Commission was built by the lowest bidder and maintained by whoever could be reached by phone. The architectural style is not a genre — it is a procurement outcome.

The world's institutional history is legible through the condition of its buildings. China's civic construction boom of the 1990s–2000s produced schools, clinics, apartment blocks, and warehouse complexes — functional, standardized, built to a budget, designed to be maintained on a recurring annual contract. Black Commission is set in the ruins of that maintenance contract. The buildings still stand. The contractor went bankrupt.

This is not gothic decay. Spaces have failed fluorescent tubes, walls that were never repainted after the 2014 water damage, and fire-exit signs running on independent battery because no one wired them into the main panel. The horror is administrative, not geological.

**Neglect gradient (player reads this as risk scale, no UI annotation needed):**

| Neglect Stage | Visible Evidence | Game State |
|---|---|---|
| Recent (0–2 years) | Furniture in place, paperwork on desks, lights mostly working | Arrival zones, HQ |
| Mid-term (2–8 years) | Some furniture removed, stain patterns formed, light at 60–70% | Mid-sections of mission sites |
| Long-term (8+ years) | Structural exposure, portable assets stripped, power unreliable | Deep interior, threat territory |

---

### Texture Philosophy

Texture does one job: communicates the history of a surface's contact with the world. Every mark was made by something specific.

**Why 2K coarse-grain for this game:** Producible by a solo developer, tileable, readable at first-person working distance (1–8m). Fine-grain photogrammetry would read correctly only at centimeter range and push the look toward documentary found-footage realism — wrong genre position.

**Four required layers on every surface material:**

1. **Base coat** — original painted, poured, or finished surface in intended color
2. **Use wear** — directional scratches and abrasion consistent with how the surface was actually used (horizontal scratch lines on a cabinet near handle height; vertical streaking from drips above a pipe joint)
3. **Environmental accumulation** — dust, stain gradients, biological marks that read as settled-in
4. **Institutional residue** — tape marks, sticker ghosts, painted stencil numbers, ink stamps never cleaned. **This layer is the most important identity carrier.**

Layer 4 is what separates a Black Commission surface from a generic industrial-horror surface. A concrete wall in a school has a registration number stenciled at waist height. A clinic corridor wall has an adhesive-sign ghost where a panel was peeled off. A warehouse column has a load-limit stencil with a handwritten chalk correction below it.

**Prohibited texture approaches:**
- Uniform wear across an entire object (aging must be directional)
- Procedural noise substituting for specific human-use wear patterns
- Scan-level detail on surfaces shared across institution types
- Clean flat color on any surface with more than six months of implied exposure

---

### Prop Density Rules

Density communicates: how recently was this space used, and by how many people doing how much work?

**The three density registers:**

**Sparse (0–3 props per 8m × 8m zone):** Spaces designed for movement. Corridors, stairwells, loading approaches. Empty space is the message.

**Functional (4–10 props per 8m × 8m zone):** Working rooms. Props cluster around task areas organized by workflow, not by a decorator. Piles form where work was set down. Gaps form where equipment was removed.

**Dense (11–20 props per 8m × 8m zone):** Storage, supply corridors, active workshop areas. Never reads as "interesting" — reads as institutional accumulation. Things are here because someone put them here when there was no room elsewhere and never moved them.

| Area Type | Register | Why |
|---|---|---|
| Arrival / extraction corridor | Sparse | Clear navigation; communicates others already cleared it |
| Mission objective room | Functional, sparse bias | Objective must be the visual anchor |
| Deep interior / threat territory | Sparse with isolated clusters | Neglect = absence, not ruin |
| Loot / side-room zones | Functional to Dense | Reward areas justify exploration attention |
| Threat territory | Dense, chaotic | Accumulation without human logic |

**Asymmetry rule:** If one wall is dense, the opposing wall is sparse. Equal coverage on every wall reads as designed — which fails the litmus test. Clusters follow human logic: desk near the power outlet, shelving near the door it serves.

**Anti-AI density rule:** One side of a corridor can be empty while the other is cluttered. Rails can be patched, bent, missing a section. Decals can be half-torn and misaligned. Lamps can have uneven pools. Exits and interactables must remain readable regardless of density.

---

### Environmental Storytelling Guidelines

No text boxes, no collectible lore entries. The story is in the physical evidence of a space that was once used by real people.

**Three storytelling layers:**

**Layer A — Institutional function:** What was this space built to do? A school corridor has numbered classrooms, a trophy case frame (contents removed), water fountain alcoves, painted floor tracks for student queuing. The player names the institution within 60 seconds of arrival.

**Layer B — Failure process:** The specific sequence that created the current state. The filing cabinet near the emergency exit is open because someone was pulling files in a hurry. The desk lamp is still on because the person who last used it left expecting to come back. The mug on the windowsill is still present because it wasn't worth taking.

**Layer C — Wrong detail:** One thing that does not fit. Wrong scale (a door opening onto a bricked wall), wrong content (a clipboard with today's date and names from the commission paperwork), wrong condition (one recently disturbed dust patch in a room otherwise in long-term neglect). **This is the horror — and it must be specific to this institution, not generic.**

**Environmental storytelling must never:**
- Explain itself with generic warning signs
- Use blood as a primary storytelling prop — overused, communicates nothing specific
- Cluster all evidence in one showcase room

**Concrete storytelling props by signal:**

| Signal | Physical Evidence |
|---|---|
| Abandoned suddenly | Personal items still present — coat on hook, lunch container on desk, unlocked locker with ID inside |
| Institution knew it was failing | Official notices stapled over earlier notices; budget stamps on requisition forms; bare patches where plaques were removed |
| Someone working here recently | Warm amber light still on at a desk; fresh dust disturbance on floor; chair angled away not pushed in |
| Threat has been in this space | Disturbed prop clusters at mid-room rather than against walls; things stored at unusual heights with wrong ergonomics |
| This commission filed before | BC paperwork with the same job number — route slips, labeled storage, route arrows from a prior team that didn't return |

---

### Per-Environment Design Notes

#### HQ Office

Three zones must be physically legible without signage:

- **Desk zone:** CRT monitor, document stacks, in-tray overflowing, rolling chair. Densest area. Warm amber dominant.
- **Gear zone:** Shelving units, locker section, equipment rack. Props carry commission-specific gear bought used. Military green dominant.
- **Departure threshold:** Garage bay door or corridor to van. Cold industrial intrudes here — visual temperature drops at the boundary between home and commission.

HQ props carry evidence of prior commissions. Notice boards accumulate between runs. Filing cabinets labeled by job type or year. An employee notice (hand-typed, yellowed paper) pre-dates the current team. **HQ must not become a lobby** — no clean reception desk, no decorative plants, no seating arrangement suggesting visitors are expected.

---

#### Dispatch Van Interior

Communicates three things simultaneously: constraint (you cannot leave), function (kitted for this kind of work), displacement from home (no warmth — this is not the office).

- **Dashboard wall:** Driver position, instrument cluster, CRT green readouts, route and job number. Only active screen in the van.
- **Bench seating:** Two sides, facing inward. Players face each other — co-op staging without stage-managing it.
- **Rear storage:** Locker unit bolted to rear wall. Props must be transit-safe — secured or in closed containers, not loose.
- **Floor:** Rubber grip mat or bare metal grating.

**Cold light rule (hard):** Van interior uses only 4500K–5000K cold overhead strip. No warm tungsten — ever. The temperature drop as players enter from HQ should be perceptible.

---

#### Mission Site: School

**Dominant props:** Student lockers (numbered, some open revealing nothing), painted floor tracks, wire-reinforced classroom door windows, layered notice boards, faded ceiling banners. Classrooms with desks in original rows (a suddenly-empty room). Administrator's office window with sliding pane.

**BC identity injection:** Notice board in the administrative corridor carries the district education bureau's termination of operation letter, dated. A stack of undelivered student report cards. A clipboard with the last day's attendance register — dated, all present. Something happened on a specific day; the records prove it.

**Texture specifics:** Linoleum floor tiles — original color at center, worn gray at high-traffic doorway lanes. Color-band stripe at child-waist height on painted concrete walls. Scuff marks from backpack contact at locker height.

---

#### Mission Site: Underground Warehouse / Service Corridor

The primary environment type for solo-development feasibility. Establishes the modular kit that other types reuse.

**Zone spatial logic:**
- **Loading approach:** Wide, sparse, vehicle-scale. Roll-up or numbered freight doors, yellow bollards, faded lane markers, dock leveler platform. Large-scale props.
- **Service corridor:** 3–4m wide, functional density. One side cluttered (pipes, conduit, vents); opposite side clear for clearance.
- **Storage zones:** Dense, organized by institutional logic — indexed by a system that makes sense for the facility but not for a stranger.
- **Utility core:** Mechanical or switchgear room. Dense prop count, one warm work lamp (still on — someone was here). Strong candidate for objective or threat territory.

**Modular kit components (reusable across all corridor maps):** Concrete wall panel / Pillar section / Freight door frame / Service door frame / Yellow railing segment (straight / corner / missing) / Grated floor panel / Stair module / Ramp / Loading platform edge / Overhead lamp (cold, working) / Overhead lamp (cold, dead) / Work lamp (warm, clamped) / Pipe run (horizontal / vertical) / Cable tray / Junction box / Vent panel / Wooden crate (large / medium / small) / Sandbag stack / Dull barrel / Pipe bundle / Bollard

---

#### Mission Site: Clinic

**Dominant props:** Curtain rails (curtains present or absent — both communicate), gurney, IV stand, empty hand-sanitizer dispenser, patient-call button panel (one section trailing a wire connecting to nothing). Overhead exam light (switched off or on with no bulb). Doctor's desk with prescription pad.

**Clinic-specific texture:** Floors are the most critical surface. Original medical-grade flooring has yellowed, seams lifted, high-traffic lanes worn through to the adhesive layer. Gurney wheel track impressions in the floor.

**BC identity injection:** A billing records room with insurance rejection notices. A staff bulletin note about "commission re-assignment" using BC-specific language. Equipment labeled with a Black Commission inventory tag — the firm contracted to assess or remove it.

---

#### Mission Site: Mall / Apartment

**Mall:** The public retail floor is stripped traversal space — bare concrete, empty shutter frames, dead signage. The commission lives in service corridors, loading docks, staff break rooms, and escalator machine rooms behind the retail skin. Every mall has a name whose logo, partially removed, appears on at least three surfaces (mounting-hole ghost, branded furniture, branded stationery in the supply room).

**Apartment:** Vertical structure creates tension — players search floor by floor. Ground-floor utility rooms use the warehouse kit. Residential floors run from inhabited to abandoned — shoes at a doorway, AC bracket with unit removed, bicycle locked to stairwell railing. Stairwell walls carry the most accumulated evidence: community notice boards layered with rent notices, lost pet signs, building management announcements, and BC commission routing paper.

---

### BC Identity Injection — Showing Which Institution Failed

Every mission site must pass the identity test: a player must be able to name the institution type within 60 seconds of arrival, and must be able to name which specific institution this is across multiple runs.

**BC physical language (never UI — always physical):**

- **Route arrows:** Adhesive or hand-painted arrows marked with the commission job number. From a prior survey or earlier team. They do not reliably mark the correct path — they mark the path the prior team took.
- **Inventory tags:** Stamped-metal or printed-label tags on equipment flagged for removal. Tag format: BC mark, job number, asset number, condition note (faded pen, barely legible).
- **Commission forms:** BC-7 (Job Acceptance), BC-12 (Site Entry Record), BC-19 (Partial Recovery Authorization). Filled with rubber-stamp dates, handwritten names, mostly empty checkboxes. Last checkbox on BC-19: "Confirmed return: ____"
- **Payment notices:** Stamp-red ink stamp "PAYMENT OVERDUE / 欠款未结" on overdue BC paperwork. Physical ink. Does not glow. Does not animate.
- **Debt access signage:** Stenciled or printed at zone transitions: "DEBT ACCESS ONLY / 欠款通道" — installed by the institution as a condition of the contract, humiliating the contractor.

**Per-institution failure signature:**

| Institution | Failure Document | Where It Lives |
|---|---|---|
| School | District bureau termination letter + final attendance register | Administrative office |
| Warehouse | Bankruptcy receiver's inventory manifest, partially completed | Warehouse office or receiving desk |
| Clinic | Insurance administrative closure notice + blank final intake form | Back-corridor admin area |
| Mall | Commercial lease termination filing on branded stationery | Mall management suite |
| Apartment | Building management committee dissolution record + final tenant notice | Ground-floor management office |

**Commission identity rhyme:** The paperwork the player accepts at the HQ computer shares language, formatting, or a specific reference number with at least one physical document in the mission site. The job order on the CRT screen and the Form BC-7 taped inside the mission site's utility closet are the same document — one digital copy, one physical copy left by a prior team. The paperwork is real. The commission is real. Someone else already tried to do this job.

---

## 7. UI/HUD Visual Direction

### Governing Principle: The Document Layer

> **The UI does not announce itself as a game interface. It pretends to be paperwork that happens to be interactive.**

Every screen element must pass the document-layer test: could this be reproduced on a government-issue photocopier using only black ink, ruled lines, and a rubber stamp? If it requires a gradient, drop shadow, rounded corner, icon glow, or motion blur to function, it is redesigned until it does not.

---

### Diegetic vs. Screen-Space Approach

**Fully diegetic (part of the game world):**

| Element | Diegetic Form | Location |
|---|---|---|
| HQ computer terminal | A real CRT monitor the player walks up to and presses interact | HQ desk zone |
| In-van transit overlay | Dashboard readouts + route sheet on bench | Van interior |
| Job briefing | Form BC-7 document displayed on the terminal screen | HQ computer |

The player never reads a floating objective in space. They read a printed card or check a form accepted at the computer.

**Screen-space (necessary HUD layer):** During mission navigation, hotbar, and threat-state status — elements occupy screen space. They must look like they were printed and taped onto the monitor glass from the inside. Form-field borders. Uppercase label text. No glows. No animations that could not be achieved with a stamp.

---

### Typography Direction

| Role | Direction |
|---|---|
| Primary / body | Monospaced, slab or typewriter class — matches the CRT terminal and printed-form aesthetic |
| Headers / labels | Same monospaced face, uppercase, tracked wide (+100–150 units) — reads as a stamp or stencil |
| Numbers / currency | Same monospaced face, tabular — ledger columns require alignment |
| Stamps / status | Heavy weight (bold), uppercase, maximum 2 lines — verified at a glance |
| Fine print | Same monospaced face, minimum size — generated, not designed |

**Forbidden:** Italic (underline only for emphasis), drop shadows, glows, gradients on text, mixed typefaces within a screen, any font with decorative swashes.

**Size hierarchy (1080p reference):**

| Level | Approx size | Usage |
|---|---|---|
| L1 | 28–32px | Screen identifier — one per screen maximum |
| L2 | 20–24px | Column headers, form section labels |
| L3 | 14–16px | All readable content — item names, values, status |
| L4 | 10–12px | Form field labels, keyboard shortcut hints |
| Stamp | 24–36px bold | Status overrides only |

---

### Iconography Style

**Type 1 — Stamp Mark:** Heavy-bordered rectangle with short uppercase text. Used for status overrides. Never a pictogram.

**Type 2 — Checkbox / Ledger Mark:** `[ ]` pending, `[X]` complete, `[-]` failed. The mark is typed or stamped, not drawn.

**Type 3 — Equipment Glyph (hotbar only):** Single-weight line drawing of the item, legible at 32×32px in 2-color. Functional catalog illustration style — no shading, no gradients, no color coding. Quantity appears as typed monospaced number bottom-right. No badge bubble.

**Prohibited:** Color-coded icons, pictograms requiring interpretation training, animated icons at rest state, pixel-art style.

---

### Animation Feel: The Office Machine

All UI animation derives from the physical behavior of old office equipment — a CRT warming up, a receipt printer feeding paper, a stamp landing. Animation is mechanical, functional, brief, and interruptible.

| Event | Animation | Duration |
|---|---|---|
| Screen open (terminal) | 3 scan-line flicker passes, then content at full opacity | 0.4–0.6s |
| Screen close (terminal) | Single horizontal scan-line collapse to center | 0.2s |
| Item acquired (hotbar) | Slot border draws in clockwise from top-left | 0.15s |
| Item consumed | Glyph fades to struck-through, held 0.5s, clears | 0.5s |
| Objective completed | `[ ]` → `[X]` — X draws in two strokes at typewriter speed | 0.2s |
| Stamp-mark appears | Drops to position with single-frame motion blur, ink-spread frame | 0.1s |
| Ledger row populates | Text types left-to-right at typewriter speed, numbers last | 0.05s/char |
| Hotbar selection | Previous slot loses bracket mark, selected slot gains it | Instant |

**Forbidden:** Default widget fade-in/slide, continuous idle animations, elastic/spring/bounce curves, screen shake applied to UI.

---

### Per-Screen Design Notes

#### HQ Computer Terminal

4:3 aspect ratio locked — the monitor is CRT. CRT green (`#6CFF5F`) text on black (`#0A0A0A`) only. Blinking block cursor (`█`) at command line — the only continuous animation. Unpurchased shop slots display as `[    ----    ]` — the form exists; the item was not ordered. Deficit funds use accounting parentheses: `(¥ 4,200)`.

```
┌────────────────────────────────────┐
│  BLACK COMMISSION                  │
│  COMMISSION TERMINAL v1.1          │
├──────────────────┬─────────────────┤
│  FUNDS:  (¥4200) │  STATUS: [    ] │
│  DEBT:   ¥8400   │  LEVEL:  [ 02 ] │
│  REPUTE: 12/100  │  TAKEOVER: 42%  │
├──────────────────┴─────────────────┤
│  CURRENT COMMISSION                │
│  [ ] Recover homework notebook     │
│  [ ] Photograph overdue ledger     │
│  [ ] Return to van                 │
├────────────────────────────────────┤
│  > NEXT ACTION: [E ACCEPT JOB]█    │
│  SHOP: F1 [MEDKIT¥200] F2 [DECOY]  │
│        F3 [SPRAY]      F4 [TORCH]  │
└────────────────────────────────────┘
```

---

#### Mission HUD

Minimum screen real estate. Readable in under one second without looking away from the scene.

```
TOP-LEFT                              TOP-RIGHT
[COMMISSION: BC-07]                   [TEAM: 2/4 ■ ■ □ □]

                 (clear center)

BOTTOM-LEFT                           BOTTOM-RIGHT
[ ] Recover notebook                  ┌──┐┌──┐┌──┐┌──┐┌──┐
[ ] Photograph ledger                 │  ││  ││  ││  ││  │
[ ] Return to van                     └──┘└──┘└──┘└──┘└──┘
                                        1   2   3   4   5
```

Alive = `■`, downed = `□`. No portrait thumbnails, no individual health bars, no map waypoints. Objectives in the same bureaucratic language as the BC-7 form.

When threat is actively pursuing: stamp block centered at top — `┌ HOSTILE ┐`. Snaps in, snaps out. No fade.

**Not in MVP HUD:** Minimap, countdown timer, kill counter, floating 3D objective markers.

---

#### Settlement / Ledger Screen

The CRT delivers the verdict in the same green text it used to offer the job. The office processes.

Rows populate with typewriter animation — income prints first, then deductions, then totals. Deficit values use accounting parentheses: `(¥ 4,575)`. If full team wipe: `┌ FAILED TO RETURN ┐` stamp block overlays top half before ledger populates. If hostile takeover hits 100%: `┌ NOTICE OF DEFAULT ┐` stamp overlaps CONFIRM button until acknowledged with `[ ] Acknowledge` checkbox. No celebration state on success — the font does not change. The office processes.

---

#### Hotbar

Five equal-width rectangular slots, positioned bottom-right, 16px from screen edge. Each slot 48×48px at 1080p.

| State | Visual |
|---|---|
| Empty | Bare slot border only |
| Populated | Line-art glyph + monospaced quantity number bottom-right |
| Selected | `┌` top-left, `┘` bottom-right corner brackets on border |
| Depleted (0) | Glyph with horizontal strike-through, quantity shows `0` |
| Unavailable | Slot border dims to 50% opacity — no lock icon |

Key label (1–5) shown as L4 subscript below each slot. Reference only — not interactive.

---

### Accessibility Baseline

| Condition | Requirement |
|---|---|
| Colorblind | No distinction is color-only — shape + text label always accompanies color |
| Low brightness | All UI legible at 50% calibrated brightness, min 4.5:1 contrast ratio |
| No audio | All state changes with a sound also have a visible UI shape/weight/stamp change |

CRT terminal (green `#6CFF5F` on black `#0A0A0A`) achieves ~14:1 contrast ratio — well above WCAG AA. Screen-space HUD white-on-dark must be verified in-engine against actual scene backgrounds.

---

## 8. Asset Standards

> This section is the binding technical specification for all art assets entering the project. Follow it before checking in any file. When a rule here conflicts with a contractor brief, this document wins.

---

### File Format Requirements

#### Textures

| Map type | Source format | Unity import | Color space |
|---|---|---|---|
| Albedo / Base Color | PNG (8-bit, no alpha unless masked) | Default → sRGB on | sRGB |
| Normal | PNG (8-bit, RGB, OpenGL Y-up convention) | Normal Map | Linear |
| Metallic/Smoothness (packed: R=metallic, A=smoothness) | PNG (8-bit, RGBA) | Default → sRGB off | Linear |
| Ambient Occlusion | PNG (8-bit, grayscale) | Default → sRGB off | Linear |
| Emission | PNG (8-bit, RGB) | Default → sRGB on | sRGB |
| Mask / Stencil | PNG (8-bit, grayscale) | Default → sRGB off | Linear |

Deliver loose PNGs. Do not embed textures inside FBX. Prefer packed metallic/smoothness (R=metallic, A=smoothness) — halves the texture sampler cost.

#### Meshes

| Requirement | Value |
|---|---|
| Export format | FBX (binary). glTF/GLB acceptable as secondary. |
| Coordinate space | Y-up, Z-forward. Blender Z-up: apply −90° X rotation before export. |
| Scale | Real-world metres in source app before export |
| Pivot | Base centre of bounding box. Wall-mounted props: wall-contact face centre. |
| Embedded cameras/lights | Excluded |
| Embedded materials | Excluded (`materialImportMode = None`) |
| Lightmap UVs | Generated for all static environment geometry |

#### Audio

| Type | Format | Sample rate | Channels |
|---|---|---|---|
| Ambient loop | WAV or OGG | 44.1 kHz | Stereo (ambient) / Mono (point-source) |
| SFX (one-shot) | WAV | 44.1 kHz | Mono |
| Music / stinger | OGG | 44.1 kHz | Stereo |

---

### Naming Convention

**Pattern:** `[Prefix]_[Category]_[Description]_[Variant].[ext]`

| Segment | Values |
|---|---|
| Prefix | `AS_` = Art asset. `M_` = Material. `T_` = Texture. `P_` = Prefab. `SM_` = Static Mesh. |
| Category | `Office`, `Van`, `Tower`, `School`, `Warehouse`, `Clinic`, `Mall`, `Apt`, `Kit`, `Char`, `UI` |
| Description | Two-to-three word description, PascalCase compound |
| Variant | Optional: `_A`, `_B`, `_LOD0`, `_LOD1`, version tag |

**Examples:**

| Asset | Correct name |
|---|---|
| FBX source | `AS_Office_FilingCabinet.fbx` |
| Generated prefab | `AS_Office_FilingCabinet.prefab` |
| Albedo texture | `T_Office_FilingCabinet_albedo.png` |
| Normal map | `T_Office_FilingCabinet_normal.png` |
| Metallic map | `T_Office_FilingCabinet_metallic.png` |
| URP material | `M_Tower_ConcreteSlab.mat` |
| LOD mesh | `SM_Kit_ConcreteWallPanel_LOD1.fbx` |

---

### Texture Resolution Tiers

All textures must have power-of-two dimensions.

| Category | Max resolution | Notes |
|---|---|---|
| Environment tile / architectural surface | 2048 × 2048 | Use 1024 if tile repeats at scale where 2K adds no visible detail at 1–8m |
| Hero prop (CRT monitor, filing cabinet, exit door, carried objective) | 2048 × 2048 | Full 2K always |
| Supporting prop (stacked chairs, barrels, bollards) | 1024 × 1024 | Shared trim sheet preferred over unique textures |
| Modular kit piece (wall segment, pillar, door frame, railing) | 1024 × 1024 or shared 2048 trim sheet | Kit pieces share a trim-sheet atlas |
| Character (player, threat) | 1024 × 1024 | 512 × 512 acceptable for LOD1+ |
| UI sprite | 512 × 512 per sprite; sprite atlases at 2048 × 2048 | No individual UI texture larger than 512 |
| Decal (leak, stamp ghost, warning stripe) | 512 × 512 | — |

---

### LOD Level Requirements

Use Unity LOD Group component. `Fade Mode = Cross Fade` on hero props; `Fade Mode = None` on background props and kit pieces.

#### Environment tiles / architectural surfaces (per 2m × 2m section)

| LOD | Distance | Max tris |
|---|---|---|
| LOD0 | 0–12m | 200 |
| LOD1 | 12–30m | 100 |
| Cull | 30m+ | — |

#### Hero props

| LOD | Distance | Max tris |
|---|---|---|
| LOD0 | 0–6m | 2,000 |
| LOD1 | 6–15m | 800 |
| LOD2 | 15–30m | 300 |
| Cull | 30m+ | — |

#### Supporting props

| LOD | Distance | Max tris |
|---|---|---|
| LOD0 | 0–8m | 600 |
| LOD1 | 8–20m | 200 |
| Cull | 20m+ | — |

#### Modular kit pieces

| LOD | Distance | Max tris per module |
|---|---|---|
| LOD0 | 0–15m | 400 |
| LOD1 | 15–30m | 120 |
| Cull | 30m+ | — |

#### Characters (player, threat)

| LOD | Distance | Max tris |
|---|---|---|
| LOD0 | 0–8m | 5,000 |
| LOD1 | 8–15m | 2,000 |
| LOD2 | 15–25m | 800 |
| Cull | 25m+ | — |

LOD0 at 5,000 is a ceiling, not a target. The silhouette read must pass at LOD1 resolution.

---

### Material Slot Limits

URP/Lit is the required shader for all opaque lit surfaces. Do not use the Standard shader.

| Category | Max slots | Required shader |
|---|---|---|
| Environment tile | 1 | `Universal Render Pipeline/Lit` |
| Hero prop | 2 | `Universal Render Pipeline/Lit` |
| Supporting prop | 1 | `Universal Render Pipeline/Lit` |
| Modular kit piece | 1 (trim sheet) | `Universal Render Pipeline/Lit` |
| Character | 1 | `Universal Render Pipeline/Lit` |
| Emissive prop (CRT screen, exit sign) | 2 max (body + emissive face) | Body: URP/Lit; Screen: URP/Lit with `_EMISSION` enabled |
| Transparent prop (dirty glass) | 1 | URP/Lit, Surface Type = Transparent |

Multi-material assets beyond 2 slots require explicit art director approval.

**Required material property ranges (all URP/Lit assets):**

| Property | Required range |
|---|---|
| Roughness (1 − smoothness) | 0.60 – 0.85 |
| Metallic (non-metal surfaces) | 0.0 |
| Metallic (bare aged metal) | 0.55 – 0.80 |
| Emission intensity | 0 on all surfaces except CRT screens and powered indicators |
| Base color HSV saturation | ≤ 0.30 on all non-accent surfaces |

---

### Unity Importer Settings

#### FBX / Model importer

| Setting | Value |
|---|---|
| Use File Scale | On |
| Mesh Compression | Off |
| Read/Write Enabled | On |
| Generate Colliders | Off (author as separate primitives on the prefab) |
| Weld Vertices | On |
| Import Blend Shapes | Off (unless explicitly animated) |
| Import Cameras | Off |
| Import Lights | Off |
| Generate Lightmap UVs | On for static environment geometry; Off for props and characters |
| Normal Import Mode | Import (from file) |
| Normal Smoothing Angle | 60° |
| Material Import Mode | None |
| Import Animation | Off for all static assets |

#### Texture importer

| Map type | Texture Type | sRGB | Compression |
|---|---|---|---|
| Albedo / Base Color | Default | On | BC7 (High Quality) |
| Normal | Normal Map | — | BC5 |
| Metallic (packed) | Default | Off | BC7 |
| AO | Default | Off | BC7 |
| Emission | Default | On | BC7 |
| Mask / Stencil | Default | Off | BC4 |
| UI Sprite | Sprite (2D and UI) | On | BC7 |

Mipmaps: On for all 3D-world textures. Off for UI sprites.
Filter Mode: Trilinear for environment/props. Bilinear for UI.
Aniso Level: 4 for floor tiles, 2 for walls, 1 for everything else.

Do not use DXT1/DXT5 (BC1/BC3) for new assets — block artifacts on coarse-grain albedos.

---

### Quality Acceptance Test

Run this checklist before committing any asset. **Hard** = blocks submission. **Advisory** = flagged for art director review.

#### A. File and naming

| # | Check | Gate |
|---|---|---|
| A1 | Filename matches naming convention | Hard |
| A2 | No spaces, no generic names | Hard |
| A3 | FBX has no embedded cameras, lights, or materials | Hard |
| A4 | Textures are PNG, power-of-two, at or below tier resolution | Hard |
| A5 | All required map suffixes present (`_albedo`, `_normal`, `_metallic`, `_emission`) | Hard |

#### B. Mesh

| # | Check | Gate |
|---|---|---|
| B1 | Vertex count at LOD0 ≤ category limit | Hard |
| B2 | No n-gons — all faces triangulated | Hard |
| B3 | No overlapping UVs in UV channel 0 | Hard |
| B4 | Lightmap UV (channel 1) non-overlapping for Static assets | Hard |
| B5 | Pivot at base centre (or wall-contact face centre) | Hard |
| B6 | Scale applied in source app before export | Hard |
| B7 | Silhouette test: flat black at 64×64px, named in two words, function readable | Advisory |

#### C. Material and texture

| # | Check | Gate |
|---|---|---|
| C1 | Shader is `Universal Render Pipeline/Lit` | Hard |
| C2 | Material slot count ≤ category limit | Hard |
| C3 | Smoothness 0.15–0.40 (roughness 0.60–0.85) | Hard |
| C4 | Albedo HSV saturation ≤ 0.30 on non-accent surfaces | Hard |
| C5 | Normal map: Texture Type = Normal Map, sRGB off | Hard |
| C6 | All linear data maps (metallic, AO, mask) have sRGB off | Hard |
| C7 | Emission off unless CRT screen or powered indicator | Hard |
| C8 | Weathering is directional, not uniform procedural noise | Advisory |
| C9 | Institutional residue layer present on any surface with 6+ months implied exposure | Advisory |
| C10 | Under three approved light families, asset reads correctly and introduces no new hues | Advisory |

#### D. Unity importer settings

| # | Check | Gate |
|---|---|---|
| D1 | FBX: Read/Write on, Generate Colliders off, Material Import Mode = None | Hard |
| D2 | FBX: Import Animation off, Import Cameras off, Import Lights off | Hard |
| D3 | FBX: Generate Lightmap UVs on for Static assets | Hard |
| D4 | Textures: BC7 (color) or BC5 (normals) — not DXT1/DXT5 | Hard |
| D5 | Textures: Mipmap Generation on | Hard |

#### E. In-engine functional check (requires Unity editor)

| # | Check | Gate |
|---|---|---|
| E1 | No pink / magenta materials in a URP scene | Hard |
| E2 | No console errors or warnings on import or scene placement | Hard |
| E3 | LOD Group transitions correct — no visible pop at expected distances | Advisory |
| E4 | Modular kit piece snaps cleanly to 0.5m or 1.0m grid with no visible seam gap | Hard |
| E5 | Collider does not protrude from visible mesh by more than 0.05m | Hard |
| E6 | GPU frame time delta ≤ 0.3ms when 20 instances placed in test scene vs. baseline | Advisory |

**Submission:** Deliver FBX, loose PNG maps, and completed checklist (A1–E6, name and date) to `Assets/_Project/Art/_Inbox/`. Assets without a completed checklist will not be moved to their production folder.

---

## 9. Reference Direction

> **How to read this section:** Each reference earns its place by contributing something the others do not. Read each entry as a lens, not a mood board. When an asset decision is unclear, identify which lens applies, extract only the specified technique, and stop before the reference bleeds into the adjacent domains it does not own.

---

### 9.1 Pacific Drive — Material Survival and Readable Functional Decay

**Primary domain:** Surface material language / prop legibility

**What to take:** Pacific Drive demonstrates that a rusted, failing vehicle interior reads as intimate and safe precisely because every material tells a specific repair story. Gaffer-tape patches, mismatched replacement knobs, hand-written labels on instrument panels — these are not decoration, they are evidence of someone problem-solving at low cost over a long time. The camera lives inside the vehicle, and the detail density per square metre is high but never chaotic because every element exists because a person needed it to.

Apply this to Black Commission's HQ and van: every prop modification — the padlock added after the original latch failed, the extension cord routed around the desk because the wall socket is dead, the hand-written label correcting a cabinet's printed index — should read as a specific low-budget solution to a specific problem. Do not scatter generic wear. Model a history of decisions.

**Texture technique specifically:** Pacific Drive uses a mid-range dirt layering strategy where the underlying material is still readable under the grime. Military-green cabinet at 70% under a 30% dust-and-smear overlay. The cabinet is dirty, not consumed. Black Commission uses the same ratio: base material always legible at reading distance (1–3m), wear layer legible at close distance (0–1m), never reversed.

**What to diverge from:** Pacific Drive's world is exterior — rain-slicked roads, pine forests, grey Pacific Northwest sky. Its decay is weather-driven. Black Commission's decay is administrative and interior. No moisture-streak surface treatments derived from exterior exposure. No muddy boot prints in the HQ. The wear is office wear: ink, tape residue, paper-stack impressions, and the yellowing of surfaces that were painted once and never repainted.

---

### 9.2 Control — Brutalist Institutional Geometry and Light-on-Concrete Drama

**Primary domain:** Architectural shape language / light-as-composition

**What to take:** Control's Federal Bureau of Control is a government institution whose scale and geometry create dread without any horror troping. The brutalist concrete, the exposed service infrastructure above drop ceilings, the identical repeating corridor bays — these produce unease because institutional sameness at wrong scale is inherently disorienting.

Two specific techniques apply directly to Black Commission:

1. **Light-on-concrete as depth signal.** Control places hard ceiling lights that produce strong vertical cast shadows on concrete columns. The column face closest to the player is lit; the flank face is in shadow. This single-source-per-bay approach creates depth and volumetric presence from flat geometry. Apply to BC's service corridors and stairwells: one overhead fixture per corridor bay, positioned to cast a shadow on the nearer wall surface, not to fill the space evenly.

2. **Bureaucratic object as cultural mass.** Control populates background shelving with dense stacks of identical administrative files, binders, and indexed boxes. The prop is not a single interesting item — it is institutional accumulation at scale. The visual message is: someone has been filing things here for decades. Use this for BC's HQ filing cabinet banks and mission-site administrative corridors — density communicates institutional depth, not collection.

**What to diverge from:** Control's brutalism is American modernist: poured concrete at heroic civic scale, architecture that announces its own importance, ceilings at 6–8m height. Black Commission's institutional spaces are Chinese civic budget construction: standard ceiling height (2.8–3.2m), pre-cast concrete panel walls, corridor widths sized for two people passing, not two vehicles. The institutions in BC were built small. Their claustrophobia is a function of undersized budget, not heroic brutalism.

Also: Control uses floating objects and reality-distortion as a narrative device. Black Commission's horror is administrative, not paranormal in presentation. No floating, no dissolution geometry, no impossible-space corridors.

---

### 9.3 Phasmophobia — Darkness as Default and Navigational Clarity Under Constraint

**Primary domain:** Lighting design / darkness management

**What to take:** Phasmophobia establishes that the player's own light source is the primary illumination in most of the game world. The environment is not lit for the player's comfort — it is lit for its own institutional logic (exit signs on battery, security lights on a separate circuit), and the player navigates the gap with a handheld torch.

Black Commission adopts this as a hard production principle: **mission site ambient is never sufficient to navigate without a handheld source.** Ambient provides enough light to see that there is a corridor ahead. It does not provide enough light to read a form, identify an item, or confirm that what is in the corner is furniture and not a threat.

The navigational clarity Phasmophobia maintains under extreme darkness is the other half of this lesson: exit signs, emergency lighting, and building geometry always provide enough structural read that the player knows they are in a building, not a void. A player in a Black Commission mission site should always be able to identify: am I in a corridor or a room? Does this space have another exit? The darkness creates threat; the geometry resolves navigation.

**Specific technique:** Phasmophobia's darkest areas are dark because the light source is absent, not because the renderer applies a heavy vignette or screen fog. Black Commission follows this: darkness is the absence of light sources, achieved through environment design. Vignette and post-process darkening are not allowed as stand-ins for actual lighting design.

**What to diverge from:** Phasmophobia uses darkness as a game mechanic (ghost power events cause blackouts). BC's threats operate under existing environmental light — they do not turn lights off, trigger blackout events, or use darkness as their weapon. The threat is a thing in a space that already has difficult light.

Also: Phasmophobia's environments are British domestic (suburban houses, schools, asylums). BC's institutions are Chinese civic — signage is Chinese, paperwork is Chinese government-issue, spatial logic is that of a mainland primary school or township service corridor.

---

### 9.4 The Stanley Parable — Environmental Narration Through Institutional Typography and Signage

**Primary domain:** Environmental storytelling / typographic identity

**What to take:** The Stanley Parable demonstrates that institutional signage, directory panels, and door labels are themselves a narrative layer. The Parable's office is legible as a specific kind of office before a single interactive event occurs, because the environmental text tells you who this building thinks you are and what it expects of you.

Black Commission draws one specific technique from this: **every readable surface in the game world should be written in the voice of the institution that produced it, not in the voice of the game designer.** A school corridor sign does not say "CAUTION: RESTRICTED AREA." It says "管理区域 — 非学生请勿入内" (Administrative Zone — Students Prohibited) in the typeface and stamp-red ink that a school administration would actually use. A warehouse bay marker is a stenciled number, not a floating waypoint.

This produces the BC identity injection mechanic described in Section 6: the commission's paperwork (BC-7, BC-12, BC-19) becomes legible as a distinct bureaucratic voice the moment it appears alongside the institution's own signage — a foreign administrative layer imposed on a pre-existing one.

**What to diverge from:** The Stanley Parable uses signage for comedy and meta-commentary; BC uses it for dread and bureaucratic authenticity. BC has no narrator, no meta-commentary, no dialogue addressing the player directly. The environmental text is sincere institutional communication that has become uncanny because the institution it served has failed.

---

### 9.5 INSIDE — Silhouette Hierarchy and Single-Axis Light Composition

**Primary domain:** Visual hierarchy / silhouette design

**What to take:** INSIDE is built entirely from the principle that a dark figure against a lit background communicates everything the player needs faster than any surface detail can. Three applications to Black Commission:

1. **Threat silhouette reads against a lit surface.** When a threat is visible, there is always a functioning light source behind or beside it. The threat is darker than the brightest thing in the frame. Its shape is the information, not its surface detail. This is enforced in level design: threat patrol zones must include at least one functioning overhead source that the threat will pass in front of.

2. **Player characters as silhouette at working range.** At 8–10m working distance, players must read as distinct silhouettes. If the player cannot be identified from their outline against the ambient background, the character design has failed, not the lighting.

3. **Exit framing as the INSIDE bright-background technique.** Exits are always visible as a bright zone through a doorframe before the player reaches them. The exit reads as light in dark — the same compositional signal INSIDE uses to orient its player at every transition.

**What to diverge from:** INSIDE's world is monochromatic with desaturated blue-grey tones and no warm color family. BC's world has warm tungsten amber as a primary accent. The technique is the principle of dark-subject-against-lit-background — not the specific palette or 2D side-scroller camera angle.

Also: INSIDE uses extreme minimalism on player character design. BC's player characters carry enough identifying equipment to pass a co-op silhouette read between four players. The application is compositional framing, not character minimalism.

---

### 9.6 Lethal Company — Production Method Reference

> **Project rule (binding):** Lethal Company is a production-method reference only. Copy nothing observable: not the ship, not the quota structure, not the monster designs, not the map layouts, not the store UI, not the loot items, not the terminal font treatment, not the moon/company naming. If it is visible in LC footage or screenable, it is off-limits as reference.

**Primary domain:** Co-op production method / repeatable ritual design / lo-fi readability

**What to take — four specific production principles:**

1. **Repeatable ritual over unique spectacle.** The LC loop works because every run starts with the same actions in the same order from the same space. Players develop muscle memory for the departure ritual. Black Commission's HQ-to-van-to-site loop operates on the same principle: the CRT terminal interaction, the locker check, the van boarding, the arrival. These actions must be fast, readable, and consistent across every single run. The ritual is not a tutorial; it is the rhythm that makes each deviation meaningful.

2. **Single-light-source lo-fi readability.** LC's interior levels read correctly under a single player-held light in a dark environment because the geometry is simple, the silhouettes are strong, and the level does not contain visual noise that competes with the torch beam. Apply as a production test: place the asset in a dark scene, add one handheld point light, move around it. Can it be identified and understood? If not, the asset has failed.

3. **Low-cost staging through prop placement, not asset count.** LC achieves institutional dread with a small set of reused modular props staged with deliberate placement. The staging decision — where a chair is pointed, whether a door is open or closed, whether a light is on — does more narrative work than the asset itself. BC should make the same bet: invest in the placement decision, not the prop count.

4. **Darkness with clear navigation.** LC's levels are dark, but exits, vents, and objective zones are always legible from their surrounding geometry. Players are never navigating a void — they are navigating a specific dark place with a shape. BC adopts this as a level design constraint: every navigable space must have a geometry-legible read even at minimum ambient.

**What to explicitly avoid:**

| Element | Why explicitly off-limits |
|---|---|
| Monster designs (Bracken, Coil-Head, Jester, etc.) | BC's threats derive from institution-specific civic pressure — see Section 5 Monster Framework |
| The quota mechanic and "PROFIT" framing | BC uses debt and partial settlement — economically adjacent but legally and fictionally distinct |
| The terminal font treatment and command interface | BC's terminal is a 4:3 CRT document form, not a command-line adventure-game interface |
| The ship and its interior layout | BC uses an HQ office and a dispatch van — different spatial grammar and different emotional register |
| The company store and its item list | BC's gear is sourced from commission supply requisitions, not a corporate store |
| Map and moon naming convention | BC's locations are named for real-world Chinese civic geography types |
| The audio-driven "found footage" mix | BC's audio design is institutional ambience — HVAC, fluorescent hum, paper rustle |

---

### 9.7 REPO — Lo-Fi Co-op Staging and Readable Item Value

**Primary domain:** Co-op item interaction / first-person object staging

**What to take:** REPO demonstrates that a lo-fi co-op horror game can achieve clear item value reads and co-op tension with minimal assets when the staging is deliberate. Two specific techniques:

1. **Item isolation as value signal.** In REPO, high-value objects are staged with enough surrounding clearance that they read as singular. The item earns its visual prominence from isolation, not from a highlight shader. BC's carried objectives should be staged in their discovery positions with at least 0.5m of lower-density surrounding space.

2. **Co-op failure legibility.** REPO's tension is readable across all players simultaneously because the critical states — item dropped, player downed, exit reached — produce unambiguous spatial changes visible from across the room. BC must follow this for extraction and partial-return moments: the carried objective is physically visible, its presence or absence is spatially clear, and the decision to leave or stay has a readable cost visible to all players without a HUD lookup.

**What to diverge from:** REPO's visual style is stylized low-poly with character-exaggerated proportions and bright contrast. BC is semi-realistic with desaturated surfaces and real-world proportions. The staging and readability principle is portable; the art style is not. No bright accent colors, no cartoon proportions, no high-saturation palette choices imported from REPO's visual language.

REPO's tone is also comic horror — the extraction chaos is partly comedic in execution. Black Commission's tone is debt noir — the extraction stakes are serious.

---

### Reference Domain Map

| Reference | Primary Domain | Key Technique | Off-Limits |
|---|---|---|---|
| Pacific Drive | Surface material language | Repair-history layering; base material legible under wear | Exterior/weather decay; moisture textures |
| Control | Architectural shape / light-on-concrete | Single-source per bay; bureaucratic object as cultural mass | Heroic civic scale; floating objects; paranormal geometry |
| Phasmophobia | Darkness management | Player-held light as primary; geometry resolves navigation | Blackout events; British domestic texture |
| The Stanley Parable | Environmental typography | Institutional voice in signage; bureaucratic text as narrative | Meta-narration; ironic commentary |
| INSIDE | Silhouette hierarchy | Dark subject against lit background; exit framing | Monochromatic palette; 2D compositional logic applied literally |
| Lethal Company | Production method | Repeatable ritual; lo-fi readability; low-cost staging; dark + legible | Everything observable — ship, monsters, terminal, quota, items, maps |
| REPO | Co-op item staging | Isolation as value signal; failure legibility | Stylized proportions; comic tone; high-saturation palette |
