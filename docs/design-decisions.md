# AccidentSquad Design Decisions

Locked decisions from design review. This document is the single source of truth for high-level creative direction.

PM: Yan Dai

## Game Identity

AccidentSquad is a 1-4 player co-op horror game about a nearly bankrupt accident-handling office in a city where every uncomfortable problem has been outsourced. Players take increasingly strange civic commissions from parents, schools, companies, and landlords — jobs that always turn out worse than advertised.

One-line pitch:

> A broke office takes cheap outsourced jobs. Every job goes horribly wrong.

The game is NOT a Lethal Company clone. It shares the co-op extraction structure but diverges on setting, tone, progression, and mission design.

## Tone: Black Humor + Institutional Horror

The tone is built on contrast:

- **Commission descriptions are funny.** "Parent offers 100G to retrieve homework notebook from school. Child says he heard noises after class."
- **Missions are genuinely scary.** Inside the school, something patrols the halls in the dark.
- **Death is comedic.** Teammate screams cut through proximity voice chat, then silence.
- **Settlement is bittersweet.** Back at the office, still in debt, takeover pressure rising.

This contrast — mundane premise, horrifying execution — is the core emotional hook. Neither pure comedy nor pure horror; the game oscillates between both and lets proximity voice chat amplify whichever is happening.

## Art Style

### Target Aesthetic

PS1-era low-poly geometry rendered through URP Simple Lit. Flat colors and procedural noise textures. Lighting carries 90% of the atmosphere; geometry provides readable silhouettes and spatial structure.

The style name is **Municipal Debt Noir**: civic teal walls, dead rubber equipment, aged paper clutter, sodium amber work lights, stamp red warnings, dispatch green systems. Every surface should feel publicly funded, cheaply maintained, and slightly past its service life.

### Poly Budgets

| Asset Type | Triangle Range |
|---|---|
| Map modules (wall, floor, door) | 500–2000 |
| Small props (medkit, flashlight, paper stack) | 20–300 |
| Doors, desks, lockers | 100–800 |
| Player characters | 1000–3000 |
| Monsters | 800–2000 |
| Vehicles | 1000–5000 |

### Material Rules

- URP Simple Lit, rough, non-metallic for nearly everything.
- Flat base color + shape detail over high-resolution textures.
- Procedural 64×64 noise patterns (grime, tile, scanline, scratched, fabric) at 3× repeat.
- Point-filtered textures for crisp pixelated look.
- No PBR, no imported texture packs for MVP.
- Emissive materials only for screens, exit signs, status lights, and monster eyes.

### Visual References

| Reference | What to learn | What to avoid |
|---|---|---|
| **Paratopic** | Urban economic decay as visual language. Lo-fi 32-bit rendering of apartments, gas stations, strip malls. Poverty and debt embedded in environment. | Surrealist narrative, body horror, glitch effects. |
| **The Backrooms** | Empty institutional spaces are inherently unsettling. Flat fluorescent lighting as psychological weapon. Repetitive modular architecture. | Infinite featureless spaces with no navigation purpose. |
| **Night of the Consumers** | Mundane workplace as complete horror setting. PS1 rendering of contemporary commercial space. Labeled zones for player callouts. | Retail/customer-service premise. Simple chase-only mechanics. |
| **Control** | Bureaucratic indifference amplifies horror. Institutional lighting from sacred architecture principles. Repetition, process, and ritualism. | Brutalist mega-structures, levitating objects, AAA paranormal scope. |
| **Signalis** | Fine-art color discipline applied to lo-fi geometry. Every screenshot reads as deliberate composition. | Retrofuturist sci-fi register. Top-down perspective. |
| **Lethal Company** | Primitive-friendly environments, simple silhouettes, flashlight/darkness contrast, diegetic company tools, comedy from bad employment. | Scrap quota framing, company terminal wording, facility layouts, specific monsters, exact UI sounds. |

### Color Palette (Locked)

| Token | Hex | Semantic Meaning |
|---|---|---|
| Civic Teal | `#2F4F4B` | Walls, public spaces, company van, institutional panels |
| Deep Civic Teal | `#172422` | Fog, grime, shadow sections |
| Dead Rubber | `#111413` | Old equipment, shelves, computers, lockers, tires |
| Aged Paper | `#D6C89B` | Notices, paperwork, homework clutter, labels |
| Dispatch Green | `#7BCF8A` | Computer screen, approved tasks, route marks, exits |
| Stamp Red | `#C23A2B` | Overdue stamps, hostile acquisition, monster warning, danger |
| Monster Eye Red | `#F2140A` | Active threat focus, chase readability |
| Sodium Amber | `#D99A31` | Garage fixtures, old work lights, warning beacons |
| Dirty Bone | `#C9C2AA` | Worn plastic, light panels, fixture surfaces |
| Cheap Cardboard | `#73502A` | Office boxes, improvised props |
| Second-Hand Wood | `#4A3119` | Desks, counters, cheap school furniture |
| Tired Fabric | `#2C322B` | Sofa, chairs, cheap office textiles |

Scene balance: 55-65% teal/rubber, 15-20% paper/cardboard/wood, 8-12% sodium amber/dirty bone, 3-6% dispatch green, 3-5% stamp red.

## Differentiation from Lethal Company

| Lethal Company | AccidentSquad |
|---|---|
| Space/industrial setting | Modern city civic/institutional settings |
| Scrap collection (one verb) | Mission commissions with narrative context (multiple verbs) |
| Binary success/failure | Partial settlement system — leave early for reduced pay |
| No persistent progression | Office level, reputation, hostile takeover pressure |
| Procedural moon generation | Hand-crafted maps, each a distinct "urban legend" |
| Random scrap spawns | Objective items with narrative purpose |
| No company growth visual | Office upgrades visible as furniture/lighting improvements |
| Generic employee avatar | Low-poly civic work crew with visual variety |
| Uniform industrial palette | Municipal Debt Noir (teal, paper, amber, red, green) |

### Unique Pillars to Protect

1. **Partial settlement** — Players can return early for reduced pay. This creates a "stay or go" decision every run that Lethal Company does not have.
2. **Hostile takeover pressure** — Continuous business threat that builds across sessions, not a per-run quota reset.
3. **Office as home base** — Visual progression from broke to functional. The office changes as the company grows.
4. **Narrative commissions** — Each job has a client, a story, and a reason. Not "go collect generic scrap."
5. **Every map is a different urban legend** — School has a homework debt collector. Mall has rising water. Apartment has something behind the doors. Each is a distinct story.

## Commission Categories

Three core categories for initial development. Each introduces a different core gameplay verb:

### 1. Lost Item Recovery (Search)

**Core verb:** Find and extract.

Players search a dangerous location for a specific item and bring it back to the van. Risk concentrates in the exploration phase; finding the item triggers the retreat phase.

**Design pattern:** Scatter search zones across the map. Place the objective in a high-risk area. After pickup, the player carrying the item is slower and more vulnerable. Team must protect the carrier on the way out.

**Example missions:** School homework notebook, apartment keys, archive room document, clinic patient records.

### 2. Scene Cleanup (Clear)

**Core verb:** Stay and work.

Players must complete work at fixed locations within the map. They cannot simply grab something and leave — the job requires time spent in dangerous territory. Risk is continuous and escalates the longer players stay.

**Design pattern:** Multiple work stations across the map. Completing each station generates noise/light/disturbance that attracts threats. The last station is always in the deepest part of the map.

**Example missions:** Restaurant health inspection cleanup, accident scene evidence collection, flooded basement pump repair.

### 3. Personnel Rescue (Escort)

**Core verb:** Find and protect.

Players locate a survivor NPC and escort them to the exit. The NPC is uncontrollable — they walk at their own pace, may make noise, may freeze in fear, may take wrong turns. The team must adapt around the NPC's behavior.

**Design pattern:** NPC has simple AI (follow nearest player, but panic near threats). NPC cannot sprint. NPC produces sound that attracts monsters. Team must clear the path ahead while keeping the NPC moving.

**Example missions:** Trapped maintenance worker in parking garage, disoriented patient in closed clinic, lost child in abandoned mall.

### Three-Layer Reward Structure

Every mission regardless of category has three reward tiers:

| Tier | Condition | Reward | Risk |
|---|---|---|---|
| Partial | Return early, objective incomplete | 30-50% pay, slight pressure increase | Low |
| Complete | Primary objective done | Full pay, reputation gain | Medium |
| Bonus | Primary + optional secondary evidence | Extra pay, reputation boost | High |

This structure ensures the "stay or go" decision is present in every mission type.

## Monster Design Principles

### Behavioral Rules Over Stats

Each monster must have a **unique behavioral rule** that changes how players act. The rule should be learnable through death and experimentation — not explained in a tutorial.

**Bad design:** Monster B is faster than Monster A.
**Good design:** Monster B only chases players who are running. Walk slowly and it ignores you.

### Design Template

| Element | Requirement |
|---|---|
| Silhouette | Readable at distance. One dominant visual feature. |
| Behavior rule | One unique mechanic players must learn. |
| Visual state feedback | Players can read patrol/chase/stunned from the model without UI. |
| Thematic fit | The monster connects to the location and commission story. |
| Counter | At least one equipment item affects the monster in a non-obvious way. |

### Current and Planned Monsters

| Location | Monster | Behavior Rule | Signature Visual |
|---|---|---|---|
| School | Homework Debt Collector | 14s grace period, 5.5m detection, chases on sight | Red coat, ledger, glowing red eyes |
| Mall (planned) | — | Moves only in lit areas; turning off lights stops it but blinds you | — |
| Apartment (planned) | — | Remembers which doors you opened; appears behind opened doors | — |

## Blender Pipeline

### Export Standards

- 1 Unity unit = 1 meter
- Origin at bottom-center of each module
- Forward = +Z in Blender (maps to Unity's +Z after FBX import)
- Apply all transforms before export
- FBX export with "Apply Transform" checked

### Modular Kit Approach

Build maps from reusable modules:

| Module Type | Examples |
|---|---|
| Wall segments | Straight wall, corner, T-junction, wall with window, wall with door frame |
| Floor tiles | Clean tile, damaged tile, carpet patch, concrete |
| Ceiling | Drop ceiling panel, exposed pipe ceiling, fluorescent fixture |
| Doors | Single door, double door, sliding door, roll-up shutter |
| Furniture | Desk, chair, locker, shelf unit, bench |
| Props | Paper stack, box, fire extinguisher, notice board, trash can |

### Character Modeling

- 1000-3000 triangles per character
- Simple Rigify skeleton (spine, arms, legs, head)
- Priority animations: idle, walk, run, crouch, carry item, downed
- Visual differentiation: work uniform color variants, hat/helmet options, tool belt
- First-person: only hands and held items visible to self
- Third-person: full model visible to teammates

### Monster Modeling

- 800-2000 triangles per monster
- One signature visual feature per monster (red coat, paper head, glowing bar, etc.)
- Priority animations: patrol walk, chase run, attack, stunned, distracted
- Tall vertical silhouette for all monsters (readable against dark corridors)
- Red accent somewhere on every monster (brand consistency with Stamp Red)

## Priority Order

1. Audio system framework (ambient, SFX, monster sounds)
2. Second map + second monster (Blender)
3. Player character model (Blender)
4. Save/load system (JSON persistence for CompanyData)
5. Death spectator camera
6. Main menu and UI polish
7. Item position randomization
8. Additional monster behavior designs
