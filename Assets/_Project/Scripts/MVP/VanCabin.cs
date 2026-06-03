using UnityEngine;

/// <summary>
/// Shared, deterministic geometry for the seated van cabin. The cabin is an enclosed
/// (windowless) space parked far above the world so the transit reads as its own room.
/// Seat world positions are computed identically on every peer, so the server only needs
/// to assign a seat index — each owner teleports itself there (ClientNetworkTransform is
/// owner-authoritative). VanTransitOverlay builds the matching visual cabin at Origin with
/// localScale = Scale.
/// </summary>
public static class VanCabin
{
    // Parked 80m up, away from scene geometry/lights — same offset the overlay always used.
    public static readonly Vector3 Origin = new(0f, 80f, 0f);

    // The procedural cabin mesh is authored small; scale it up so full-size players fit.
    public const float Scale = 2.2f;

    // ── Optional imported interior model ──
    // The interior is PROCEDURAL by default (benches authored in the same space as the seat
    // offsets below, so seating is exact by construction — nothing to hand-tune). Only flip
    // UseModeledInterior to true once a real interior mesh exists at InteriorResourcePath;
    // VanTransitOverlay then AUTO-FITS it from its measured bounds (no guessed transform) so
    // it lands centred on the bay with its floor at the seat height. ModelEuler is the only
    // manual hint — set it if the mesh imports facing the wrong way (bounds can't infer facing).
    public static bool UseModeledInterior = false;
    public const string InteriorResourcePath = "GeneratedArt/ASV4_VanTransitInterior";
    public static readonly Vector3 ModelEuler = Vector3.zero;

    // Target the auto-fitter scales the model into (world units), matched to the procedural
    // cabin so a modeled interior occupies the same volume the seats are placed in.
    public static readonly Vector3 InteriorSize = new(2f * Scale, 1.08f * Scale, 1.36f * Scale);
    public static Vector3 InteriorCenter => Origin + new Vector3(0.45f * Scale, 0f, 0f);
    public static float FloorWorldY => Origin.y + 0.36f * Scale;

    // Seat offsets in the cabin's UNSCALED local space (matches the procedural benches:
    // floor top ~y0.37, benches at z = ±0.52, passenger bay along +x of the cage).
    // Two benches facing each other across the aisle (z): 0 faces 1, 2 faces 3.
    static readonly Vector3[] LocalSeats =
    {
        new(0.15f, 0.37f, -0.52f),
        new(0.15f, 0.37f,  0.52f),
        new(0.80f, 0.37f, -0.52f),
        new(0.80f, 0.37f,  0.52f),
    };

    // Yaw so each player faces across the aisle toward the opposite bench.
    static readonly float[] Yaws = { 0f, 180f, 0f, 180f };

    public static int Count => LocalSeats.Length;

    public static Vector3 SeatWorldPosition(int index)
    {
        index = Mathf.Clamp(index, 0, LocalSeats.Length - 1);
        return Origin + LocalSeats[index] * Scale;
    }

    public static float SeatYaw(int index)
    {
        index = Mathf.Clamp(index, 0, Yaws.Length - 1);
        return Yaws[index];
    }
}
