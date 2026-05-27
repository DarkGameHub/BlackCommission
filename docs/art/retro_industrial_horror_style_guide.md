# Retro Industrial Horror Art Direction

This is an original style target inspired by low-poly co-op horror games, not a copy of any specific game's assets.

## Core Look

- Low-poly geometry with simple, readable silhouettes.
- Coarse industrial spaces: concrete, metal doors, warning paint, office clutter, pipes, vents, lockers.
- Dark but readable lighting: strong pools of light, dim corners, dirty green/yellow fluorescents.
- Limited color palette: charcoal, cold gray, hazard yellow, faded orange/red, sickly green screens.
- Low-resolution textures and flat materials are acceptable; prioritize shape language and mood.
- Objects should feel functional and slightly cheap, like a worn public-service workplace.

## Asset Rules

- Use modular kits for buildings and maps.
- Keep most props blocky and reusable.
- Give important gameplay objects a strong silhouette and one accent color.
- Avoid realistic micro-detail until gameplay scale and composition feel good.
- Do not build an entire map as one giant mesh. Build wall, floor, door, corner, stair, office, corridor, and street modules.

## Suggested Poly Targets

- Small props: 20 to 300 triangles.
- Doors, lockers, desks: 100 to 800 triangles.
- Humanoid characters: 700 to 2500 triangles for MVP.
- Monsters: 1000 to 4000 triangles, with silhouette doing most of the work.
- Vehicles: 1000 to 5000 triangles for MVP.
- Map modules: simple geometry; spend detail on doors, trims, signage, and interactables.

## Production Split

- Blender: characters, monsters, vehicles, props, modular wall/floor/door pieces.
- Unity: level assembly, lighting, collisions, prefab variants, post-processing, gameplay setup.
- Textures: start with flat colors and procedural noise, then replace important surfaces later.

## First Vertical Slice

Create one strong test area before remaking the whole game:

- Agency entrance and reception room.
- One playable worker character.
- One monster with a distinctive silhouette.
- One emergency vehicle.
- One corridor/street map module set.

When this slice feels coherent in Unity, expand the same rules to the rest of the game.
