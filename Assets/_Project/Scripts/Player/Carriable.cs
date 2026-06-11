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
    Transform originalParent;
    NetworkObjectReference carrierRef;

    public bool IsHeavy => isHeavy;
    public bool RequiresTwoPlayers => requiresTwoPlayers;
    public NetworkVariable<bool> IsBeingCarried = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool CanBeCarried => !IsBeingCarried.Value && !requiresTwoPlayers;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        originalParent = transform.parent;
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

    public void AttachToHolder(Transform holdPoint)
    {
        rb.isKinematic = true;
        transform.SetParent(holdPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    public void Detach()
    {
        rb.isKinematic = false;
        transform.SetParent(originalParent);
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
