# Current Playable HQ Art Pipeline

## What Is Active In Play Mode

The playable HQ is currently a Unity runtime set, not the Blender HQ prefab.

1. `MvpProjectSetup` creates the base HQ scene objects, including the real
   `OfficeComputer` interaction component.
2. `MvpSceneStyleDirector.BuildOfficeStyle()` runs when the `HQ` scene loads.
   It hides the original blockout props and builds the visible office, garage,
   lighting, route paint, colliders, furniture, and set dressing from Unity
   primitives.
3. `GeneratedArtImporter` creates ASV4 prefabs from Blender FBX files.
4. The dispatch van can load `Resources/GeneratedArt/ASV4_PlayableDepartureVan`
   in Play Mode. If that prefab is missing, the runtime fallback van is used.

`CreateGeneratedOfficeVisualIfAvailable()` exists, but it is not called by the
current HQ flow. Regenerating the Blender HQ model will not change the playable
office unless the runtime scene code is also changed to use it.

## Current Direction

The playable HQ should use Lethal Company as a production method reference, not
as an asset reference:

- primitive-friendly shapes with strong silhouettes;
- a repeatable company ritual: computer, gear shelf, route line, van;
- darkness and practical lights that preserve navigation;
- one memorable company mark, not generic office decoration.

AccidentSquad's own identity anchors are:

- terminal-green company systems;
- red debt and hostile-takeover pressure;
- the green AS mark crossed by a red debt slash;
- a cheap dispatch van as the mission gateway;
- office equipment that looks bought second-hand under pressure.
