using UnityEngine;

/// <summary>
/// Optional per-map override for the scavenge loot budget. Drop one in a mission scene
/// (map builders create it) and <see cref="LootSpawner"/> uses these counts instead of the
/// global <c>ScavengingConfig</c> values — big maps (Mars v2, 15-minute target) want far
/// more anchors filled than the tower's 10–14 without touching the other maps' pacing.
/// Pure data marker: no networking (the host's LootSpawner is the only reader).
/// </summary>
public class ScavengeMapProfile : MonoBehaviour
{
    [Tooltip("Minimum items spawned per run on this map.")]
    public int itemsMin = 10;
    [Tooltip("Maximum items spawned per run on this map.")]
    public int itemsMax = 14;
}
