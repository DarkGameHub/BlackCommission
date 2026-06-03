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
