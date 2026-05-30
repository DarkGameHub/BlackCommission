# Black Commission Art Bible: Low-Cost Outsourced Horror Style

Owner: Banach, Art Direction Agent  
PM: Yan Dai  
Document scope: MVP visual direction for the rundown office, the school lost-item map, and future maps.  
Current target: 1-4 player MVP loop, `HQ -> School_LostItem_01 -> HQ`, primitive geometry, URP Lit/Simple Lit materials, OnGUI HUD.

Style lock: [Black Commission Style Lock v1](art/black-commission-style-lock-v1.md) is the current source of truth for production art decisions. This file remains the broader art bible, but the lock document decides naming, motifs, acceptance tests, and copy-risk boundaries.

## North Star

Black Commission should look like a broke commission office taking tiny outsourced jobs in spaces that society has stopped caring for. The first mission is not "epic horror"; it is a parent's missing homework request that becomes dangerous because bureaucracy, debt, and neglected public spaces have turned hostile.

The visual promise:

> Cheap office comedy outside the mission, cold civic horror inside the mission, and enough absurdity that players laugh while running.

This style is intentionally low-cost. The goal is not realism through asset density. The goal is readable spaces, strong silhouettes, dirty institutional color, oppressive light, and a few memorable props that sell the joke.

## Reference Principles

Use references as methods, not as asset sources.

| Reference | What Black Commission can learn | What to avoid copying |
|---|---|---|
| Lethal Company | Primitive-friendly environments, simple silhouettes, comedy from low-budget labor, strong flashlight/darkness contrast, diegetic company tools. | Do not copy scrap quota framing, company terminal wording, employee suits, facility layouts, monsters, UI sounds, or exact scan/sell loop. |
| Buckshot Roulette | One intense room can carry a whole mood; objects on a table can feel dangerous when the framing is tight. | Do not copy gambling ritual, shotgun/table composition, dealer imagery, or high-contrast black room identity. |
| The Stanley Parable | Banal office spaces become funny and uncanny through signage, repetition, corporate blandness, and voice-of-system attitude. | Do not copy narrator structure, office maze identity, or clean corporate satire tone. Black Commission is poorer and dirtier. |
| Control | Institutional architecture, red warning language, paranormal bureaucracy, strong signage hierarchy. | Do not copy Federal Bureau brutalism, levitating objects, Hiss/red-room treatment, or polished paranormal government identity. |
| Phasmophobia | Familiar locations, co-op readability, handheld equipment, tension from audio/light clues. | Do not copy ghost-hunting premise, EMF/spirit-box identity, house map style, evidence board loop, or ghost taxonomy. |
| The Exit 8 / Observation Duty | Repetition makes anomalies noticeable; small environment changes can become gameplay. | Do not copy endless passage format, exact anomaly-spotting rules, or sterile transit aesthetic. Use the principle for future tasks. |
| GTFO | Team hazard language, industrial readability, light discipline, silhouettes in darkness. | Do not copy prisoner expedition framing, bio-lab monsters, tactical UI, or hardcore military tone. |

The shared takeaway is: fewer assets, stronger rules. Every prop should either guide play, sell the joke, or warn the player.

## Style Pillars

1. **Outsourced Civic Horror**  
   Spaces feel public, cheap, neglected, and over-paperworked: schools, offices, apartments, clinics, warehouses, subway service areas, local government basements.

2. **Municipal Debt Noir**
   The base palette is civic teal, dead rubber, aged paper, dirty bone, and sodium amber. Dispatch green and stamp red are sparse semantic accents, not the whole art style.

3. **Primitive But Designed**  
   Cubes, capsules, cylinders, planes, and TextMesh are allowed to be obvious, but composition must be intentional. Low cost is the style, not an excuse for random grayboxes.

4. **Readable Before Pretty**  
   Players must instantly read: where to go, what to grab, what can kill them, what room they are in, who has the objective, and where the exit is.

5. **Comedy Through Specificity**  
   A generic spooky school is weak. A school with "HOMEWORK DEBT OFFICE", overdue stickers on lockers, and a monster carrying a ledger is Black Commission.

## Global Visual Language

### Shape Language

- Company/HQ objects: squat, second-hand, uneven, patched, stacked, improvised.
- Public institutions: rectangular, modular, repetitive, fluorescent, low ceiling, cheap partitions.
- Threats: tall, thin, vertical, slightly too long, with one strong red readable feature.
- Objective items: compact, bright, flat, and label-like; they should stand out without looking magical.
- Exit/safety objects: muted dispatch-green planes, clear arrows, stable light.

### Color Tokens

Use these as a consistent palette. Exact RGB can drift slightly per scene, but semantic meaning should not drift.

| Token | Suggested color | Meaning |
|---|---|---|
| Civic Teal | `#2F4F4B` | Walls, old civic paint, company van, institutional panels |
| Deep Civic Teal | `#172422` | Fog, grime, shadowed wall sections |
| Dead Rubber | `#111413` | Old computer, shelves, lockers, equipment cases, tires |
| Aged Paper | `#D6C89B` | Notices, worksheets, homework clutter, labels |
| Dispatch Green | `#7BCF8A` | Office computer, approved tasks, route marks, company systems |
| Stamp Red | `#C23A2B` | Overdue stamps, hostile acquisition, monster warning, critical danger |
| Monster Eye Red | `#F2140A` | Active threat focus, chase readability |
| Sodium Amber | `#D99A31` | Garage fixtures, old work lights, warning beacon |
| Dirty Bone | `#C9C2AA` | Old plastic, light panels, worn helmet, fabric wear |
| Cheap Cardboard | `#73502A` | Office boxes, improvised props |
| Second-Hand Wood | `#4A3119` | Desks, counters, cheap school furniture |
| Tired Fabric | `#2C322B` | Sofa, chairs, cheap office textile |

### Material Rules

Use URP Lit or Simple Lit by default. Avoid imported texture dependency for MVP unless the texture clearly multiplies value.

- Prefer flat base color plus shape detail over high-resolution textures.
- Use rough, non-metallic materials for almost everything.
- Add material variety through color blocks, decals made of thin cubes/planes, and TextMesh labels.
- Use emissive-looking colors through unlit materials or small point lights when actual emission is not set up.
- Keep materials reusable: `MVP_school_wall`, `MVP_debt_red`, `MVP_terminal_green`, `MVP_dirty_paper`, `MVP_dead_metal`.
- Avoid shiny sci-fi panels, clean marble, fantasy loot glow, or polished horror-house materials.

### Lighting Rules

Lighting carries the horror load more than geometry.

- HQ: warm, weak overhead light plus small dispatch-green computer spill. It should feel poor but safe.
- School: cold civic fluorescent light, low ambient, fog allowed, with stamp-red threat accents.
- Monster areas: red point lights or red eye light should reveal threat direction without filling the whole room.
- Exits: stable green light. Safety must be visually calmer than the monster.
- Darkness should create uncertainty, not hide navigation. Keep doorways, exits, and primary corridors legible.

Suggested scene lighting:

| Scene type | Ambient | Key light | Accent |
|---|---|---|---|
| HQ | Warm low teal/rubber | Soft sodium overhead | Dispatch green near computer, stamp-red debt board |
| School | Low cold teal | Dirty-bone fluorescent strips | Stamp-red monster warning, muted green exit |
| Future civic maps | Low neutral/cold | Practical fixtures | One map-specific color plus shared semantic accents |

## Low-Cost Primitive Implementation Strategy

The current codebase already supports this direction through `MvpProjectSetup`, `MvpSceneStyleDirector`, and primitive runtime visuals. Continue that path for MVP.

### Primitive Kit

Use this kit before asking for custom models:

| Primitive | Use |
|---|---|
| Cube | Walls, tables, signs, lockers, doors, shelves, desks, paper stacks, badges |
| Thin cube | Posters, warning notices, labels, screen strips, arrows, grime bands |
| Cylinder | Flashlight, spray can, pipes, lamps, chair legs |
| Capsule | Temporary characters, monster body, mannequins, wrapped items |
| Sphere | Hands, heads, knobs, bulbs, bells, anomaly eyes |
| TextMesh | World labels, warning signs, room numbers, fake forms |
| Point light | Terminal glow, exit glow, eye glow, fluorescent pools |

### Detail By Layering

Every low-cost asset should have 3 layers:

1. **Mass**: the big primitive shape that reads from far away.
2. **Identifier**: color block, sign, label, silhouette detail, or icon.
3. **Context clutter**: nearby papers, boxes, stickers, cables, stains, or small repeated props.

Example: the notebook is not just a yellow box. It is a yellow flat box, with a white name label, sitting among dull paper clutter, with a small warm light only visible nearby.

### Performance And Production Rules

- Keep generated decorative props collider-free unless needed for gameplay.
- Never let decorative primitives block NavMesh or interaction rays.
- Use shared materials when assets are persistent. Runtime-only materials are acceptable for temporary visual dressing, but avoid unbounded creation.
- Avoid high density clutter in chase corridors. Players need movement readability.
- Prefer 1-3 strong signs per room over 20 small unreadable signs.
- When a prop needs gameplay meaning, reserve a unique color/silhouette for it.

## HQ: Rundown Commission Office

The HQ is the player's poor home base. It should be slightly funny, slightly sad, and always functional.

### Mood

- Warm but underfunded.
- Too much paperwork, not enough furniture.
- The computer is the only object that feels organized.
- Debt is visible as a permanent environmental pressure.

### Required Visual Zones

| Zone | Visual goal | Required assets |
|---|---|---|
| Spawn / entry | Players understand they are in a tiny office, not a menu void. | Door mat, coat rack or boxes, cheap sign, floor grime. |
| Computer desk | Primary interaction focus. | Old monitor, green screen glow, keyboard slab, desk, chair, cable strip. |
| Debt wall | Explains pressure and hostile takeover visually. | Red warning board, overdue notices, competitor flyers, final-warning slot. |
| Equipment shelf | Shows gear economy. | Medkit block, spray can, decoy bell, flashlight, empty shelf spaces. |
| Decoration/upgrades | Makes office progression visible later. | Broken sofa, wall cracks, upgrade markers, taped floor outline for future furniture. |
| Acquisition hook | Shows future company growth. | City map, pinned competitor office cards, level markers. |

### HQ Composition Rules

- Put the office computer where it is visible within 2 seconds of spawn.
- Terminal green should be the brightest non-danger color in HQ.
- Red debt signage should be visible but not dominate the interaction target.
- Keep one "hero wall" behind or near the computer for Steam screenshots: computer glow, warning board, boxes, equipment shelf.
- Avoid cozy startup-office styling. No clean plants, glass walls, premium desks, or polished logo wall.

### HQ Dispatch Garage And Transit Layer

The office must not feel like a menu room disconnected from missions. HQ should have a visible dispatch layer: computer selects the job, equipment is bought in the office, then players physically walk to the garage vehicle to leave.

MVP implementation:

- Computer: claim rewards, buy equipment, lock the current commission.
- Garage vehicle: start the selected commission and load the mission scene.
- Mission return vehicle: every mission should place the same company vehicle at or near the entry/exit point, so extraction means physically returning to the van.
- Garage bay: concrete floor, dented roll-up door, yellow-black floor warnings, oil stains, weak amber light, and company green beacon.
- Company vehicle: ugly second-hand civic fleet van, dispatch-green AS mark, stamp-red debt slash, roof rack, dirty glass, small interaction zone.

This creates a ritual:

```text
Computer accepts work -> players buy gear -> cross the office -> board the van -> mission loads -> complete job -> return to the parked van -> office payout.
```

Do not build full driving first. Start with a parked dispatch vehicle and short loading ritual. Later upgrades can add selectable transport, route hazards, vehicle upgrades, or a short playable approach, but the first goal is continuity, not a vehicle simulator.

### HQ Prop Language

Good:

- Second-hand CRT/cheap monitor.
- Dented filing cabinet.
- Cardboard box labeled "DONATED EQUIPMENT".
- Sofa with one cushion missing.
- Printer jam paper trail.
- Debt notices taped over motivational poster.
- Flickering sign with one dead letter.

Bad:

- Sleek sci-fi terminal.
- Clean corporate lounge.
- Fantasy shop stall.
- Random trash with no job/debt/company joke.

## School Map: Lost Homework Mission

The school is the first mission identity. It should feel familiar, cold, and wrong, with the homework/debt joke embedded in the environment.

### Mood

- After-hours public school.
- Fluorescent cold light.
- Repetition: doors, lockers, desks, paper.
- One absurd bureaucratic horror layer: homework debt, overdue stamps, parent notices, red ledger marks.

### Minimum Map Language

Use a simple map structure:

```text
Green Exit / Entrance
  -> Main hallway spine
    -> Classroom A search space
    -> Classroom B search space
    -> Locker / clutter alcove
    -> Teacher office or "Homework Debt Office" risk room
  -> Monster patrol crossing route
```

The player should learn the map fast enough to panic-run back to the exit.

### Required School Assets

| Asset | Primitive implementation |
|---|---|
| Main corridor | Long floor cube, side wall cubes, ceiling strips, repeated door frames. |
| Classrooms | Rectangular rooms, desk grids, teacher desk, blackboard, notice board. |
| Lockers | Repeated tall cubes with small handle strips and red overdue stickers. |
| Homework clutter | Thin paper cubes, stacks, forms, books, bags. |
| Room identity | TextMesh room numbers, colored door plates, wall arrows. |
| Exit point | Broad muted dispatch-green floor/door marker plus stable light and readable text. |
| Fluorescent lamps | Thin cyan cubes with point lights under ceiling. |
| Risk room | Red banner/sign, denser paper clutter, monster crossing path. |

### School Color And Light

- Walls: gray-green, not pure gray.
- Floor: dark desaturated green/blue with subtle striping from geometry.
- Lockers: slightly blue, darker than walls.
- Papers: dirty cream.
- Notebook: yellow or yellow-orange, reserved for mission objective.
- Monster: deep red coat/body with red eyes.
- Exit: muted dispatch green, separate from the computer screen but still semantic safety.

### Search Readability

The mission item should be findable without random pixel hunting.

- Put fake clutter in groups; put the real notebook in a visually framed spot.
- Use a small nearby warm point light for the notebook, not a giant fantasy glow.
- The notebook should disappear/sync when collected, so its world presentation must be unmistakable before pickup.
- Avoid making every book yellow. Reserve yellow for the target and maybe one or two warning labels only.

## Monster: HomeworkDebtCollector

The monster is a bureaucratic threat made physical. It is not a generic demon.

### Silhouette

- Tall vertical body.
- Red coat or red torso mass.
- Long arms or ledger-like rectangle carried near chest.
- Small head with red warning eyes.
- Optional paper/ledger slab that reads as "collector" from mid-distance.

### States

| State | Visual read |
|---|---|
| Patrol | Dim red eyes, slow upright shape, mostly silhouette. |
| Chase | Brighter eye light, stronger red spill, faster movement. |
| Stunned | Eye light drops or shifts dull orange; pose/height should visually pause. |
| Distracted | Head/torso turned toward decoy, red focus away from players. |

### Boundaries

- Do not make it a ghost. The game is not ghost-hunting.
- Do not make it a combat boss. It is an avoidance pressure for the MVP.
- Do not make it too detailed; too much face detail fights the primitive style.
- Do not make it goofy enough to remove threat. The joke is the concept, not slapstick animation.

## Notebook Objective

The homework notebook is the first mission trophy. It must be tiny but iconic.

### Visual Spec

- Flat rectangular cuboid.
- Yellow cover with a white name label.
- Red overdue stamp or parent note mark.
- Slight thickness so it reads in first person.
- Optional carried version attached to hand/backpack in a visible color.

### Placement Rules

- Always framed by dull clutter.
- Never placed inside a fully dark corner without a nearby cue.
- Use small warm glow only within the immediate search area.
- Do not occupy hotbar; it is a mission carrier state.

## Office Computer And UI Visual Direction

The computer is diegetic: an old work machine running a cheap commission platform.

### Screen Identity

- Green terminal glow.
- Simple rectangular panels.
- Text-heavy commission cards.
- Red warnings for debt, takeover pressure, failed jobs.
- Locked future categories shown as dull gray/red stamped entries.

### UI Tone

The interface can be ugly in-world, but it must be clean for players.

- Use plain labels and consistent column alignment.
- Avoid modern SaaS polish.
- Use old terminal/cheap admin software flavor.
- Keep the 5-slot hotbar clear and symbol-first.
- Reward claim should feel like the office computer grudgingly approves payment.

## Future Map Language

Future maps should not each invent a new art style. They should feel like different outsourced civic spaces in the same broke city.

### Shared Rules

Every map needs:

1. A recognizable public/service location.
2. A clear entry/exit safety language.
3. One map-specific job object.
4. One map-specific threat or anomaly rule.
5. Stamp-red debt/danger language and dispatch-green company/safety language.
6. Repeated low-cost props with one unique hero prop.

### Category Visual Seeds

| Category | Location language | Hero object | Threat color/accent |
|---|---|---|---|
| Lost Item Recovery | Schools, apartments, offices, lockers, archives | Notebook, phone, keys, document folder | Red overdue stamps |
| Scene Cleanup | Warehouses, rental units, event halls | Bio bin, mop cart, sealed box | Yellow hazard tape plus red warnings |
| Personnel Rescue | Clinics, dorms, elevators, basements | Stretcher, ID card, locator tag | Red alarm lights |
| Anomaly Handling | Municipal offices, storage rooms, transit corridors | Containment form, strange device | Unstable cyan/red contrast |
| Debt and Dispute Mediation | Pawn shops, landlord offices, back rooms | Contract folder, ledger, receipt printer | Debt red, dirty gold |
| Corporate Crisis Management | Boardrooms, server closets, hotel conference rooms | Evidence drive, report binder | Cold white, red legal notices |
| Security and Escort | Parking structures, service corridors, loading docks | Badge, package, client marker | Orange route markers plus red threat |
| Black Commissions | Closed clinics, underground offices, sealed archives | Black folder, encrypted case | Near-black with red minimal accents |

### Reusable Map Kit

Maintain a shared modular kit:

- Wall block, floor block, ceiling strip.
- Door frame, locked door, exit door.
- Sign plate, arrow plate, room number plate.
- Paper notice, red warning sticker, green approved sticker.
- Shelf, desk, chair, locker, box stack.
- Fluorescent lamp, red warning light, green exit light.
- Search clutter group.
- Safe spawn group.

## Steam Screenshot Direction

First Steam image should communicate the entire MVP:

- Foreground: player holding or reaching for the yellow homework notebook.
- Midground: teammate near lockers/desks with flashlight or spray.
- Background: HomeworkDebtCollector at the end of a cold school hallway, red eyes visible.
- Right/left frame edge: green exit sign or school room signage.
- UI: minimal HUD/hotbar only if it improves comprehension.

Avoid screenshots of empty hallways, pure UI, or the HQ alone for the first image. HQ is image 2 or 3: it explains progression after the hook image sells the mission.

## Two-Week Art Target

The two-week target is a style-complete MVP slice, not final asset production.

### Week 1: Language Lock

- Finalize shared palette materials.
- Dress HQ with computer, debt wall, equipment shelf, sofa/boxes, acquisition map.
- Dress school corridor and 2-3 search rooms with lockers, desk grids, paper clutter, room labels.
- Upgrade HomeworkDebtCollector primitive silhouette: red coat, long arms, ledger, eye light.
- Upgrade notebook: label, stamp, carried readability.

### Week 2: Steam-Readable Pass

- Add lighting pass: HQ warm/green, school cold/cyan, monster red, exit green.
- Add signage pass: room numbers, debt notices, exit arrows, homework debt banner.
- Add clutter pass that does not block NavMesh.
- Capture 3 screenshot compositions and adjust scene dressing for readability.
- Make a 30-second trailer shot list with current MVP interactions.

### Two-Week Done Criteria

- A new player can identify the office computer within 2 seconds in HQ.
- A new player can identify the school exit within 3 seconds after spawning.
- The notebook is visually distinct from other papers/books.
- The monster is readable as a red threat from the far end of the hallway.
- Green and red meanings are consistent across HQ, school, HUD, and props.
- No scene looks like default Unity graybox from the main camera path.

## Do Not Do

- Do not chase realistic asset packs before the MVP loop is validated.
- Do not add decorative clutter that breaks NavMesh, interaction, or player movement.
- Do not create a one-note dark blue horror palette or a neon red/green game. Black Commission needs civic teal, dead rubber, aged paper, sodium light, dispatch-green systems, and stamp-red debt.
- Do not copy any reference game's exact monster, terminal, room layout, logo, or gameplay-facing iconography.
- Do not make the office aspirational too early. Its brokenness is the progression baseline.
- Do not make the school pure haunted-house horror. It is a civic/institutional job site that has become absurdly dangerous.

## Acceptance Checklist For Art Reviews

Use this checklist before asking QA to test a dressed scene:

- The core objective, exit, threat, and computer are visually obvious.
- Every strong color has semantic purpose.
- Decorative objects are collider-free unless gameplay needs collision.
- Room signage supports multiplayer callouts.
- Monster state changes are visible without reading HUD text.
- Lighting does not hide required interaction objects.
- The scene still fits the "broke office taking outsourced civic jobs" premise.
- The result feels inspired by low-cost co-op horror methods, not copied from any reference title.
