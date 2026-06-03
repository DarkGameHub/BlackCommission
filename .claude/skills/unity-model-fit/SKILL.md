---
name: unity-model-fit
description: >
  Methodology for placing/aligning an imported 3D model (FBX/prefab) to code-defined anchors
  or a target volume in a Unity project WITHOUT hand-tuning transform numbers or asking the
  user to eyeball the result. Use whenever you need a model to line up with gameplay anchors
  (seats, sockets, mount points), fit inside a known volume, or sit on a known floor — e.g.
  the van cabin interior, a weapon in a hand socket, props snapped to a grid. Trigger when a
  task involves "align/fit/scale/position this model", a model looks mis-scaled or offset, or
  someone proposes guessing transform values to nudge in the editor.
---

# Aligning imported models in Unity: measure, don't guess

Hand-tuning `position`/`scale`/`rotation` by trial-and-error — and asking the user to verify
each guess — is the wrong approach. A model's real size and pivot only exist at runtime/edit
time as **bounds**. The correct method is to *measure the bounds and compute the transform*,
so placement is deterministic and needs no human verification.

## Core principle

> You cannot see the mesh, but the engine can. Make the engine measure it and compute the fit.

Never write `localScale = 2.2f // looks about right`. Instead derive every number from
`Renderer.bounds` / `Mesh.bounds` against an explicit target (a size, a center, a floor height,
or a socket transform).

## The recipe (fit a model into a target volume, floor-aligned)

1. Instantiate at identity scale; apply only a known orientation hint (`ModelEuler`) if the
   mesh imports facing the wrong way — facing cannot be inferred from an axis-aligned box.
2. Measure combined world bounds across all child `Renderer`s (encapsulate them).
3. Uniform scale = `min(target.x/size.x, target.y/size.y, target.z/size.z)` (fit *inside*; use
   `max(size, epsilon)` to avoid divide-by-zero on flat meshes).
4. Apply scale, then **re-measure** (bounds change with scale).
5. Translate so `bounds.center` maps to the target center on the horizontal axes and
   `bounds.min.y` maps to the target floor height (objects rest on the floor, not centered in it).
6. `Debug.Log` the computed scale + fitted size so the result is self-evident in Console — this
   replaces "please check if it looks right."

```csharp
static bool TryGetWorldBounds(GameObject root, out Bounds b) {
    b = default;
    var rs = root.GetComponentsInChildren<Renderer>();
    if (rs.Length == 0) return false;
    b = rs[0].bounds;
    for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
    return true;
}

static void FitInto(GameObject root, Vector3 targetSize, Vector3 targetCenter, float floorY, Vector3 euler) {
    root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(euler));
    root.transform.localScale = Vector3.one;
    if (!TryGetWorldBounds(root, out var b)) { root.transform.position = targetCenter; return; }
    float s = Mathf.Min(targetSize.x/Mathf.Max(b.size.x,1e-4f),
                        targetSize.y/Mathf.Max(b.size.y,1e-4f),
                        targetSize.z/Mathf.Max(b.size.z,1e-4f));
    root.transform.localScale = Vector3.one * s;
    TryGetWorldBounds(root, out b); // re-measure
    root.transform.position += new Vector3(targetCenter.x-b.center.x, floorY-b.min.y, targetCenter.z-b.center.z);
    Debug.Log($"[fit] scale={s:F3} fittedSize={b.size}");
}
```

## Snapping to a socket instead of a volume

If aligning to an anchor transform (hand socket, seat, mount): parent the model to the socket,
zero local position/rotation, then offset by the *measured* pivot-to-feature delta
(`socket.position - bounds.center` for center-align, or use a named child transform on the model
as the true anchor and align that child to the socket).

## Deriving anchors FROM a model

When the model defines where gameplay points go (e.g. real bench transforms), don't hardcode
seat coords — read named child transforms (`Find("Seat_01")`) or compute from bounds (e.g. seats
inset from `bounds.min/max` along the long axis at `bounds.min.y`). Keep one source of truth.

## When to prefer procedural geometry instead

If no model exists, or the model is an auto-generated blob that won't read as the intended space,
build the geometry procedurally from primitives and author the gameplay anchors in the **same
coordinate space** as the geometry. Then alignment is exact *by construction* — there is nothing
to fit. (This is what `VanCabin` + `VanTransitOverlay.CreateProceduralInterior` do: the benches
and the seat offsets are defined together, so seating never drifts.) The project's locked art
direction is PS1-era low-poly primitives, so procedural primitive geometry is on-style, not a hack.

## Editor-time variant (bake the numbers once)

For a one-off, do the same measurement in an `[MenuItem]` editor command that loads the prefab,
computes the transform, and either writes it onto a scene instance or logs the constants to paste
in. Same math; runtime auto-fit is usually simpler because it self-heals when the asset changes.

## Rules

- Never commit a guessed transform constant with a comment like "looks right / tune later".
- Every placement number must trace to a measurement or an explicit target.
- Surface the computed result via `Debug.Log`; don't ask the user to verify alignment by eye.
- Orientation (facing) is the only thing bounds can't give you — expose a single Euler hint.
