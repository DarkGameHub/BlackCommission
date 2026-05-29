# Current Playable HQ Art Pipeline

## What Is Active In Play Mode

The playable HQ is currently a Unity runtime set, not the Blender HQ prefab.

Style source of truth: [AccidentSquad Style Lock v1](accidentsquad-style-lock-v1.md).

1. `MvpProjectSetup` creates the base HQ scene objects, including the real
   `OfficeComputer` interaction component.
2. `MvpSceneStyleDirector.BuildOfficeStyle()` runs when the `HQ` scene loads.
   It first creates the playable colliders, camera, and atmosphere, then uses
   the ASV4 Blender HQ prefab when available. If that prefab is missing, it
   falls back to the Unity-primitive office, garage, route paint, furniture,
   floor storage mat, and set dressing. The computer desk is a standing
   interaction point; there is no chair in front of the terminal.
3. `GeneratedArtImporter` creates ASV4 prefabs from Blender FBX files.
4. The dispatch van can load `Resources/GeneratedArt/ASV4_PlayableDepartureVan`
   in Play Mode. If that prefab is missing, the runtime fallback van is used.
5. `OfficeDepartureVan` now treats the van trigger as the boarding area: the
   host can drive only after all connected players are inside the van bounds.
   The selected job is synced to clients, and `VanTransitOverlay` covers the
   scene load with a compact facing-seat van cabin: two seats left, two seats
   right, driver silhouette, and moving window scenery.
6. `MvpSceneStyleDirector.BuildSchoolStyle()` also strengthens the playable
   school at runtime: it adds an exterior forecourt, a push-open entrance door,
   record-room obstacles, the optional overdue ledger evidence pickup, readable
   return-van dressing, and extra debt-office set dressing even if the saved
   school scene is still a simple MVP layout.

`CreateGeneratedOfficeVisualIfAvailable()` is active in the current HQ flow.
Regenerating the Blender HQ model now changes the playable HQ visual, while
gameplay colliders and interaction components remain owned by Unity runtime
setup.

## Current Direction

The playable HQ should use Lethal Company as a production method reference, not
as an asset reference:

- primitive-friendly shapes with strong silhouettes;
- a repeatable company ritual: computer, gear shelf, route line, van;
- darkness and practical lights that preserve navigation;
- one memorable company mark, not generic office decoration.

AccidentSquad's own identity anchors are:

- civic-teal municipal surfaces and a dirty paper/dead-rubber base palette;
- small dispatch-green system accents for computer, route, and extraction;
- stamp-red debt and hostile-takeover pressure;
- the AS dispatch seal crossed by a stamp-red debt slash;
- a cheap civic fleet dispatch van as the mission gateway;
- a school-gate threshold: accident van outside, door into danger, door back to
  the return van;
- a four-slot mission van locker for shared medkit/decoy/spray/flashlight
  supplies;
- an office floor storage mat for spare hotbar gear dropped with `G`;
- grounded office equipment that looks bought second-hand under pressure.
