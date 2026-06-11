# Black Commission Style Lock v1

> **DEPRECATED (2026-06-10)**: superseded by `black-commission-style-lock-v2.md`
> (PM 回锁类 LC 低保真方向). Kept for history only. 另：v1 期间 art-bible 已将主点缀色
> 修正为暖钨丝橙黄、CRT 绿限电子屏——该修正在 v2 中继续有效。

Status: SUPERSEDED by v2
Locked style name: **Semi-Realistic Industrial Horror**
Working description: a semi-realistic industrial ghost-story look, between
photorealistic and stylized.

This file replaces the earlier "Municipal Debt Noir" direction. Future assets,
scene passes, materials, lighting, and prompts should follow this lock unless a
newer style lock explicitly supersedes it.

## One-Sentence Style

Black Commission is a grounded semi-realistic industrial horror game about a
failing commission office sending workers into worn public facilities, where
ordinary maintenance spaces, debt pressure, and malfunctioning equipment become
uncanny.

Use this phrase for future asset generation:

```text
Semi-realistic industrial horror environment, desaturated colors, military green and concrete gray palette, worn wooden furniture, aged metal surfaces, 20-40% weathering, high roughness materials, CRT green accent lighting, grounded realistic proportions, Pacific Drive and Control-inspired maintenance facility aesthetic.
```

## Style Position

The visual target sits between:

- photorealistic
- stylized

It is not:

- cartoon
- low poly
- raw photogrammetry or scan-realism
- cyberpunk
- glossy sci-fi

The look should feel practical, heavy, used, and slightly uncanny. Every prop
should look like it belongs in a neglected office, workshop, school maintenance
room, clinic back corridor, mall service route, apartment utility space, or
warehouse.

## Reference Use

References are for methods, not assets. Do not copy logos, creatures, UI,
vehicles, maps, sound identities, or exact loop wording from reference games.

Useful reference qualities:

- Pacific Drive: grounded garage/workshop mood, improvised equipment, worn
  utilitarian surfaces.
- Control: institutional spaces, brutal concrete, unsettling offices and
  maintenance zones.
- Phasmophobia: readable co-op horror tools in ordinary spaces.
- The Stanley Parable: mundane office language turning strange.
- INSIDE: restrained silhouettes, discipline, and negative space.

Black Commission must keep its own identity: a broke outsourcing office,
absurd public-service jobs, practical gear, and civic/workplace horror.

## Color Lock

Color discipline is the most important rule. The project should use a narrow,
desaturated palette.

Approximate scene balance for HQ and civic interiors:

- 60% concrete gray
- 20% military green
- 15% old wood brown
- 5% rust

Approximate scene balance for underground warehouse and service corridors:

- 60% concrete gray and dark steel
- 15% military green or dirty blue-gray wall trim
- 10% old wood, crates, sacks, and paper clutter
- 10% faded industrial safety yellow
- 5% rust, grime, and localized warning marks

### Main Palette

| Role | Hex | Use |
|---|---:|---|
| Concrete Gray 1 | `#5E5E5E` | walls, floors, pillars, large concrete forms |
| Concrete Gray 2 | `#707070` | worn panels, dusty paint, secondary concrete |
| Concrete Gray 3 | `#4A4A4A` | grime, shadowed concrete, dark rubber |
| Military Green 1 | `#55624A` | cabinets, van/body panels, metal furniture |
| Military Green 2 | `#68745C` | faded painted metal, old doors, equipment cases |
| Military Green 3 | `#475040` | dark green trim, dirty shelves, heavy frames |
| Old Wood Brown 1 | `#6B5440` | desks, shelves, school furniture |
| Old Wood Brown 2 | `#7C624A` | worn chair parts, boards, counters |
| Old Wood Brown 3 | `#8A7158` | exposed edges, scuffed wood highlights |
| Rust 1 | `#7B4B2A` | small rust patches and stains |
| Rust 2 | `#8C5937` | aged metal wear, old pipes |
| Rust 3 | `#A36842` | tiny exposed oxidation accents only |

### Gameplay Accent Color

Use exactly one bright gameplay/electronic accent color:

| Role | Hex | Use |
|---|---:|---|
| CRT Green | `#6CFF5F` | CRT screens, UI, radar, detectors, signal lights |

Faded industrial safety yellow is allowed as a material color, not as a
gameplay/electronic accent. Use it for worn railings, hazard stripes, cargo
lift edges, forklift paint, route arrows, and bollards. Keep it dirty,
desaturated, scratched, and physically painted onto objects.

Do not scatter blue, purple, red, or clean saturated yellow through the scene.
Do not use red as the default danger color in this style lock. If warning
information is needed, express it through CRT green UI state, movement, sound,
layout, shape, safety-yellow physical markings, or material damage before
adding another hue.

Warm tungsten light is allowed as light color only, not as a broad yellow
material accent.

## Lighting Lock

Only three light families are allowed:

- cold white industrial light, 5000K
- warm tungsten light, 3000K
- CRT green emission, `#6CFF5F`

### Use

- 5000K cold white for maintenance areas, corridors, garage/workshop zones,
  utility rooms, and service routes.
- 3000K warm tungsten for sofa areas, desks, tired office corners, and small
  human spaces.
- CRT green only for computer screens, UI panels, radar, monster detectors,
  interaction feedback, and low-intensity electronic spill.

### Avoid

- pure white light
- random colored point lights
- blue, purple, red, or yellow decorative lighting
- neon strips
- cyberpunk glow
- big pools of green light that turn the scene arcade-like
- darkness so heavy that routes, exits, objectives, and interactables disappear

Lighting rule: readable industrial darkness, not blind darkness.

## Material Lock

Materials must feel used, dusty, and high roughness.

Required roughness range:

```text
0.6 to 0.85
```

Required weathering:

```text
20% to 40%
```

Use:

- chipped paint
- scratches
- dust
- tape marks
- sticker residue
- edge wear
- rubbed handles
- dull metal
- stained concrete
- faded military green paint
- old varnished or exposed wood

Avoid:

- clean showroom assets
- shiny metal
- mirror reflections
- chrome
- glossy sci-fi plastic
- cyberpunk emissive materials
- wet-looking surfaces unless the map explicitly needs water or leaks

## Shape Language

The world should stay rectangular, practical, and repairable.

Keep using:

- cabinets
- desks
- notice boards
- CRT computers
- printers
- tool walls
- drawers
- shelves
- service doors
- lockers
- square ceiling lights
- exposed pipes and brackets

Most objects should read as boxes, plates, frames, slabs, and bolted-together
parts. This is a strength of the current art direction.

Do not introduce:

- futuristic spacecraft shapes
- streamlined furniture
- rounded consumer-tech products
- elegant sci-fi curves
- toy-like silhouettes
- soft cartoon proportions

## Texture Lock

Use one consistent texture target:

```text
2048 x 2048
```

Texture style:

- coarse grain
- visible wear
- light stains
- chipped or faded paint
- subtle dirt variation
- practical, readable detail

Avoid:

- 8K scan-level detail
- ultra-sharp photogrammetry
- procedural noise so strong that object function becomes unclear
- clean flat colors with no age

## Required Motifs

These motifs replace the previous teal/red dispatch system:

1. **The failing office**
   A cheap, worn office is the ritual start point: computer, desk, documents,
   gear storage, sofa corner, and exit route.

2. **Improvised industrial gear**
   Equipment should look bought used, repaired, borrowed from maintenance
   closets, or assembled from workshop parts.

3. **CRT green systems**
   The computer, UI, radar, and detectors are the only bright accent family.
   Players should remember the green glow from old electronics, not neon.

4. **Public facilities gone wrong**
   Schools, malls, apartments, clinics, and warehouses must feel like real
   public or service spaces that became neglected, strange, and dangerous.

## Latest Environment Concept: Underground Service Warehouse

This is the current practical mission-environment direction for solo
development. It should reduce dependence on character-heavy art and replace
AI-looking hero compositions with reusable, grounded environment kits.

Working concept name:

```text
Underground debt logistics / cheap industrial commission site
```

The concept is strongest when it reads as a low-cost underground warehouse,
service corridor, loading bay, or maintenance tunnel owned by an underfunded
outsourced contractor. It should not feel like premium sci-fi, military
special forces, or a generic asset-store warehouse.

### Production Intent

- Build environments first; use workers sparingly.
- Favor modular kits over one-off cinematic set pieces.
- Make reusable wall panels, rails, stairs, pipe runs, grates, doors, crates,
  lamps, warning barriers, and route markers.
- Let decals and labels carry identity instead of relying on detailed
  characters.
- Keep shapes simple enough for one developer to model, texture, light, and
  iterate.

### Visual Anchors

Use these as the main image language for underground warehouse/corridor maps:

- concrete walls, pillars, slabs, and retaining panels
- rock-to-concrete tunnel transitions
- numbered freight doors, roll-up doors, and service doors
- yellow railings, bollards, stair modules, and hazard trim
- black/yellow worn hazard stripes on edges and barriers
- grated metal floors, short stairs, ramps, and loading platforms
- exposed pipe runs, brackets, cable trays, vents, and junction boxes
- wooden cargo crates, sandbags, dull barrels, pipe bundles, and forklifts
- cold overhead lamps plus small warm work lamps
- green exit/service lights only when needed for readable navigation

### Black Commission Identity Injection

Every underground warehouse map needs at least three Black Commission-specific
marks so it does not become a generic industrial corridor:

- `COMMISSION LIFT 03`
- `DEBT ACCESS ONLY`
- `ACCIDENT SQUAD STORAGE`
- `PAYMENT OVERDUE ZONE`
- `CONTRACTOR ROUTE`
- `PARTIAL SETTLEMENT EXIT`
- taped commission forms, debt notices, old work orders, route arrows, and
  stamped paper signs

These signs should look physically printed, painted, stamped, taped, or bolted
onto the environment. Avoid clean UI panels unless the interaction is actually
electronic.

### Anti-AI Rules

Avoid the common AI-concept look:

- perfectly balanced composition in every room
- every corner filled with a different prop
- over-detailed surfaces that cannot be produced consistently
- dramatic character silhouettes as the main style carrier
- beautiful cinematic haze hiding unclear layout
- glossy wet floors or premium reflections
- clean, readable labels that look composited after the fact

Prefer practical irregularity:

- one side of a corridor can be empty while the other side is cluttered
- a rail can be patched, bent, repainted, or missing one warning stripe
- decals can be half torn, dirty, or misaligned
- lamps can have uneven pools of light as long as exits and interactables stay
  readable

### Character-Light Rule

Characters should support the world, not carry it. For this direction, the
best evidence of people is often indirect:

- hanging ID cards
- empty chairs
- lockers, gloves, helmets, and cheap masks
- half-finished work orders
- printed commission receipts
- abandoned tools
- used mugs, taped notes, and dirty keyboards

If worker art appears, keep it cheap, grounded, anonymous, and secondary to the
environment.

## Asset Family Rules

### HQ

Required readable zones:

- CRT computer desk
- worn document/status wall
- gear shelf or tool rack
- sofa or tired office corner
- garage or workshop threshold
- mission departure route

Forbidden:

- clean startup office
- premium lounge furniture
- fantasy shop stall
- unexplained decorative clutter
- floating furniture or props
- saturated colored accent objects

### Office And Workshop Props

Props should be second-hand, rectangular, and worn.

Good examples:

- military-green filing cabinet
- old wooden desk
- dusty CRT computer
- beige or gray printer
- metal tool wall
- old safety board
- taped extension cord
- dirty fluorescent fixture
- worn sofa in muted fabric

### Vehicle

If the mission vehicle is visible, it should read as a dirty, practical service
vehicle rather than a heroic sci-fi transport.

Required:

- military green or concrete-gray body
- dull metal trim
- dirty glass
- roof rack or practical storage
- 20-40% weathering
- only small CRT green system lights if needed

Forbidden:

- futuristic troop carrier
- clean ambulance/police read
- glossy black silhouette
- saturated warning colors as primary identity

### Worker

Worker silhouette:

- cheap uniform
- practical helmet or cap
- tired vest or jacket
- small company badge
- backpack or carried tool
- grounded, underfunded, not heroic

### Monsters

Monster design still asks:

```text
What ordinary workplace or public-service pressure became physical here?
```

The first school monster can still be a homework/debt collector, but it should
now fit the industrial horror palette: gray, military green, dirty fabric,
old paper, rusted metal details, and CRT green detection/eye/UI accents if
needed.

Do not use a saturated red body as the default monster solution.

## Map Rules

Each map needs one public-facility identity:

- School: corridors, classrooms, lockers, homework storage, maintenance rooms.
- Mall: shutters, service corridors, delivery areas, flooded back routes.
- Apartment: stairs, elevator, utility rooms, floor-by-floor searching.
- Clinic: curtains, gurneys, billing windows, staff-only back rooms.
- Warehouse: underground service corridors, loading platforms, freight doors,
  roll-up doors, inventory cages, railings, grates, pipe runs, and carried
  objectives.

Do not build generic horror spaces. Every map must show which institution
failed.

## Modeling And Grounding Rules

These are bug-prevention rules, not just art taste.

### Unity Runtime Primitives

Unity primitive cube scale is the full rendered size. To place an object on a
floor at `floorY`, set:

```text
centerY = floorY + height * 0.5
```

Checklist:

- sofa base bottom touches floor
- chair feet touch floor
- filing cabinets touch floor
- desk legs touch floor and reach desk underside
- monitor base touches desk
- shelves have visible uprights/planks, not one solid floating block
- vehicle body has complete side panels unless intentionally open

### Blender Script

The Blender helper applies cube scale directly before export, so scale values
are full dimensions in object space. Use the same grounding logic:

```text
centerZ = floorZ + height * 0.5
```

Every asset needs a 1.8m human-readability pass:

- desk top around 0.58-0.70m for the current stylized worker/player relation
- chair/sofa seat around 0.32-0.45m
- shelves grounded, not levitating
- vehicle body closed from all visible sides
- monster height clearly taller than worker

## Production Lock

Current Play Mode source of truth:

- playable HQ visuals: `MvpSceneStyleDirector`
- generated Blender assets: `docs/art/blender_outsourced_civic_commercial_v4.py`
- generated prefabs/resources: `GeneratedArtImporter`

Until the HQ pipeline changes, fixing the playable office means editing
`MvpSceneStyleDirector`. Fixing Blender alone will not change the current HQ
scene.

If Blender HQ becomes authoritative later:

1. call the generated HQ prefab from the HQ flow
2. keep the real `OfficeComputer` interaction component
3. disable overlapping runtime decorative office props
4. keep runtime colliders/boundaries explicit
5. keep the computer, gear shelf, office route, and mission departure ritual

## Acceptance Tests

Run these before calling an art pass complete:

1. **Style position test**: the asset reads as semi-realistic industrial horror,
   not cartoon, low-poly, or scan-real.
2. **Palette test**: the scene is mostly concrete gray, military green, old
   wood, and rust, with CRT green as the only bright accent.
3. **Lighting test**: only 5000K cold white, 3000K warm tungsten, and CRT green
   are used.
4. **Material test**: roughness feels in the 0.6-0.85 range, with 20-40%
   weathering.
5. **Shape test**: props stay rectangular, practical, grounded, and repairable.
6. **Texture test**: 2K-style coarse wear is consistent; no 8K scan mismatch.
7. **Grounding test**: no sofa, shelf, cabinet, chair, monitor, or vehicle panel
   floats.
8. **Specificity test**: every prop either guides play, sells the failing office,
   supports co-op gear, or makes a public facility feel uncanny.
9. **Copy-risk test**: remove any asset that reads as a copied reference-game
   suit, monster, vehicle, terminal, map, or logo.
10. **Modularity test**: underground warehouse content can be rebuilt from
    repeatable wall panels, railings, stairs, pipes, doors, crates, decals, and
    light fixtures instead of one-off concept-art shapes.
11. **Character-light test**: the scene remains readable and memorable without
    a prominent human character in frame.
12. **Identity test**: a warehouse/corridor scene includes at least three
    physical Black Commission-specific labels, forms, route marks, or debt
    traces.

If a feature fails two or more tests, it is not style-locked.

## Final Direction Statement

Black Commission should feel like a four-person team built a grounded co-op
horror game out of concrete corridors, military-green cabinets, worn wood,
rusted maintenance hardware, and old CRT systems.

The game is memorable when players can describe it as:

```text
That co-op horror game where your broke commission office sends you into failed
public facilities with used industrial gear, and the only clean glow is the CRT
green screen telling you something is wrong.
```
