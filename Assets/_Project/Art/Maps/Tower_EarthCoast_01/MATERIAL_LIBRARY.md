# Tower EarthCoast 01 Material Library

This folder is the first map's art material library.

## Texture Drop Paths

- `Textures/_Inbox/` - temporary drop folder for unsorted downloads.
- `Textures/Architecture/Concrete/` - raw concrete, poured slab, dusty wall, ceiling.
- `Textures/Architecture/Plaster/` - painted sales-office walls, cracked plaster.
- `Textures/Architecture/Tile/` - lobby/show-flat floor tile, bathroom/service tile.
- `Textures/Architecture/Metal/` - rebar, scaffold, rolling shutter, rusty beams.
- `Textures/Architecture/Glass/` - dirty glass, sales-office partitions, balcony glass.
- `Textures/Architecture/Wood/` - formwork, temporary doors, worker shanty boards.
- `Textures/Exterior/` - asphalt, mud, site gravel, curb, fence.
- `Textures/Props/` - furniture, boxes, tarps, cables, signs.
- `Textures/Decals/` - leaks, cracks, dust masks, warning stripes, dirt overlays.
- `Textures/Utility/TrimSheets/` - reusable trims for walls, beams, stairs.
- `Textures/Utility/Masks/` - ORM, grunge masks, alpha masks.

## Material Paths

- `Materials/Architecture/` - walls, floors, ceilings, stairs, exterior slab.
- `Materials/Props/` - props, scaffold, rebar, tarp, temporary construction pieces.
- `Materials/Decals/` - damage, stains, arrows, warning tape, room labels.

## Initial Material Set

Use `Tools/Black Commission/Art/Create Tower EarthCoast 01 Material Library` in Unity to create placeholder materials:

- `M_T01_Concrete_Slab`
- `M_T01_Concrete_WallRaw`
- `M_T01_Concrete_DarkVoid`
- `M_T01_Plaster_OffWhite`
- `M_T01_Tile_LobbyDusty`
- `M_T01_Metal_Rust`
- `M_T01_Rebar_Dark`
- `M_T01_Wood_Formwork`
- `M_T01_Tarp_Blue`
- `M_T01_Asphalt_Muddy`
- `M_T01_Glass_Dirty`
- `M_T01_Rubble`
- `M_T01_Decal_LeakDark`
- `M_T01_Decal_WarningYellow`

The same menu also creates `M_ACG_*` materials from the downloaded ambientCG texture sets if Unity has imported the textures.

## Downloaded CC0 Texture Sets

Downloaded from ambientCG as `1K-JPG` PBR map packs. License: Creative Commons CC0 1.0.

- `Concrete048` - https://ambientcg.com/a/Concrete048
- `Concrete034` - https://ambientcg.com/a/Concrete034
- `Plaster001` - https://ambientcg.com/a/Plaster001
- `PaintedPlaster017` - https://ambientcg.com/a/PaintedPlaster017
- `Tiles133D` - https://ambientcg.com/a/Tiles133D
- `Metal063` - https://ambientcg.com/a/Metal063
- `MetalWalkway014` - https://ambientcg.com/a/MetalWalkway014
- `CorrugatedSteel007A` - https://ambientcg.com/a/CorrugatedSteel007A
- `Planks037A` - https://ambientcg.com/a/Planks037A
- `Asphalt031` - https://ambientcg.com/a/Asphalt031
- `Gravel043` - https://ambientcg.com/a/Gravel043
- `Facade001` - https://ambientcg.com/a/Facade001

Source manifest: `ambientcg_download_manifest.json`.

## Naming

Recommended texture suffixes:

- `_BaseColor`
- `_Normal`
- `_ORM`
- `_Height`
- `_Emission`
- `_Mask`

Use 2K for most tiling surfaces. BaseColor and Emission should use sRGB. Normal, ORM, Height, and Mask should be non-color data.
