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

    // Real-scale high-roof cargo bay. The old 2.2x uniform scale made a 1.36 m whitebox
    // almost 3 m wide and pushed seated cameras into the ceiling.
    public const float Scale = 1f;

    // ── Imported real-scale interior model ──
    // The Blender cabin is authored to the same coordinates as the seats below and auto-fitted
    // from measured bounds. The procedural box cabin remains only as a missing-asset fallback.
    public static bool UseModeledInterior = true;
    public const string InteriorResourcePath = "GeneratedArt/BC_VanTransitInterior";
    public static readonly Vector3 ModelEuler = Vector3.zero;

    // Target the auto-fitter scales the model into (world units), matched to the procedural
    // cabin so a modeled interior occupies the same volume the seats are placed in.
    public static readonly Vector3 InteriorSize = new(3.42f, 1.93f, 1.78f);
    public static Vector3 InteriorCenter => Origin;
    public static float FloorWorldY => Origin.y;

    // Seat offsets in the cabin's UNSCALED local space (matches the procedural benches:
    // floor top ~y0.37, benches at z = ±0.52, passenger bay along +x of the cage).
    // Two benches facing each other across the aisle (z): 0 faces 1, 2 faces 3.
    static readonly Vector3[] LocalSeats =
    {
        new(-0.72f, 0.44f, -0.61f),
        new(-0.72f, 0.44f,  0.61f),
        new( 0.72f, 0.44f, -0.61f),
        new( 0.72f, 0.44f,  0.61f),
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
