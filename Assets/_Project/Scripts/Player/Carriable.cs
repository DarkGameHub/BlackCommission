using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Attach to any object that players can pick up and carry.
/// Works with CarrySystem for single carry, and StretcherSystem for two-player carry.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class Carriable : NetworkBehaviour
{
    [SerializeField] bool isHeavy = false;      // heavy = two-hand carry, slows player
    [SerializeField] bool requiresTwoPlayers = false;  // true for stretcher
    [SerializeField] float dropDamageThreshold = 5f;   // fall velocity that causes damage

    Rigidbody rb;
    NetworkObjectReference carrierRef;
    Collider[] colliders;
    Transform holdAnchor;   // non-null while carried: the carrier's hold point we follow each frame

    public bool IsHeavy => isHeavy;
    public bool RequiresTwoPlayers => requiresTwoPlayers;
    public NetworkVariable<bool> IsBeingCarried = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool CanBeCarried => !IsBeingCarried.Value && !requiresTwoPlayers;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Cache physics colliders so carrying can switch them off. A held object keeps its
        // collider by default; for a heavy item (the 1.7 m eco-column capsule) that collider
        // floats at the carrier's chest and fights the player's CharacterController and every
        // wall, locking movement ("按 E 卡住"). Disabled while carried, restored on drop.
        colliders = GetComponentsInChildren<Collider>(true);
    }

    public void SetCarried(NetworkObject carrier, bool carried)
    {
        // Server only
        IsBeingCarried.Value = carried;
        carrierRef = carried ? new NetworkObjectReference(carrier) : default;
    }

    /// <summary>Server-side: the player NetworkObject currently carrying this, or null.</summary>
    public NetworkObject CurrentCarrier =>
        IsBeingCarried.Value && carrierRef.TryGet(out NetworkObject carrier) ? carrier : null;

    /// <summary>
    /// Server-side: find the carriable held by this player, if any. Used to force-drop
    /// when a carrier goes down mid-carry (GDD edge: 沙盘 drops in place, any teammate
    /// can pick it up).
    /// </summary>
    public static Carriable FindCarriedBy(NetworkObject carrier)
    {
        if (carrier == null) return null;
        foreach (Carriable candidate in FindObjectsByType<Carriable>(FindObjectsSortMode.None))
            if (candidate.CurrentCarrier == carrier)
                return candidate;
        return null;
    }

    /// <summary>
    /// Begin carrying: go kinematic, drop colliders, then snap to the carrier's hold point
    /// every frame. We deliberately do NOT reparent — this is a NetworkObject, and NGO forbids
    /// parenting a NetworkObject under a plain (non-NetworkObject) transform like the player's
    /// HoldPoint. That threw "Invalid parenting" and dumped the column at world origin. Each
    /// peer follows the anchor locally and stays in sync because the carrier's own transform
    /// is already replicated.
    /// </summary>
    public void AttachToHolder(Transform holdPoint)
    {
        rb.isKinematic = true;
        SetCollidersEnabled(false); // stop the carried body from blocking the carrier and walls
        holdAnchor = holdPoint;
        SnapToAnchor();
    }

    public void Detach()
    {
        holdAnchor = null;
        rb.isKinematic = false;
        // Restore world collision so it lands on the floor / cargo bay; the hard-landing
        // drop-damage check in OnCollisionEnter only counts once IsBeingCarried is false.
        SetCollidersEnabled(true);
    }

    // Follow the hold point after the carrier has moved this frame (kinematic, so a direct
    // transform write is fine — the disabled collider means no physics fighting).
    void LateUpdate()
    {
        if (holdAnchor != null) SnapToAnchor();
    }

    void SnapToAnchor()
    {
        if (holdAnchor == null) return;
        transform.position = holdAnchor.position;
        transform.rotation = holdAnchor.rotation;
    }

    void SetCollidersEnabled(bool value)
    {
        if (colliders == null) return;
        foreach (Collider c in colliders)
            if (c != null) c.enabled = value;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsBeingCarried.Value && collision.relativeVelocity.magnitude > dropDamageThreshold)
            OnDropDamage(collision.relativeVelocity.magnitude);
    }

    protected virtual void OnDropDamage(float impactForce)
    {
        // Override in subclasses (e.g. SurvivorCarriable reduces health on hard drop)
    }
}
