using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles picking up, carrying, and dropping interactable objects.
/// Single-player carry: F to pick up, G to drop.
/// Two-player carry (stretcher/heavy objects) is handled by StretcherSystem.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class CarrySystem : NetworkBehaviour
{
    [SerializeField] Transform holdPoint;       // where carried objects sit (in front of camera)
    [SerializeField] float pickupRange = 2.5f;
    [SerializeField] float heavySpeedPenalty = 0.55f;   // multiplied into PlayerController.SpeedMultiplier

    PlayerController playerController;
    PlayerInputActions inputActions;
    Carriable carriedObject;
    Camera playerCam;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
    }

    public override void OnNetworkSpawn()
    {
        // Server watches for the carrier going down: a downed carrier drops the object in
        // place so any teammate can pick it up (heavy two-hand carry rule). The owner
        // can't do this client-side — IsGameplayBlocked() rightly blocks downed input.
        if (IsServer && TryGetComponent<PlayerHealth>(out var health))
            health.IsDowned.OnValueChanged += HandleDownedChangedServer;

        if (!IsOwner) return;
        inputActions = new PlayerInputActions();
        inputActions.Enable();
        inputActions.Player.Carry.performed += _ => TryPickUp();
        inputActions.Player.Drop.performed += _ => Drop();

        playerCam = GetComponentInChildren<Camera>();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && TryGetComponent<PlayerHealth>(out var health))
            health.IsDowned.OnValueChanged -= HandleDownedChangedServer;
        CleanupInputActions();
    }

    void HandleDownedChangedServer(bool wasDowned, bool isDowned)
    {
        if (!isDowned || !IsServer) return;

        Carriable carried = Carriable.FindCarriedBy(NetworkObject);
        if (carried == null) return;

        carried.SetCarried(NetworkObject, false);
        DropClientRpc(carried.NetworkObject);
    }

    public override void OnDestroy()
    {
        CleanupInputActions();
        base.OnDestroy();
    }

    void CleanupInputActions()
    {
        if (inputActions == null) return;
        inputActions.Player.Disable();
        inputActions.Disable();
        inputActions.Dispose();
        inputActions = null;
    }

    /// <summary>
    /// Interact-key (E) path: an IInteractable carriable (e.g. the eco column) forwards
    /// itself here so pickup speaks the same language as every other interaction.
    /// Owner-only, same gating as the F-key raycast path.
    /// </summary>
    public void TryPickUp(Carriable carriable)
    {
        if (IsGameplayBlocked()) return;
        if (carriedObject != null) return;
        if (carriable == null || !carriable.CanBeCarried) return;
        PickUpServerRpc(carriable.NetworkObject);
    }

    void TryPickUp()
    {
        if (IsGameplayBlocked()) return;
        if (carriedObject != null) return;
        if (playerCam == null) return;

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, pickupRange)) return;

        var carriable = hit.collider.GetComponentInParent<Carriable>();
        if (carriable == null || !carriable.CanBeCarried) return;

        PickUpServerRpc(carriable.NetworkObject);
    }

    [ServerRpc]
    void PickUpServerRpc(NetworkObjectReference objRef)
    {
        if (!objRef.TryGet(out NetworkObject netObj)) return;
        var carriable = netObj.GetComponent<Carriable>();
        if (carriable == null || !carriable.CanBeCarried) return;

        carriable.SetCarried(NetworkObject, true);
        PickUpClientRpc(objRef);
    }

    [ClientRpc]
    void PickUpClientRpc(NetworkObjectReference objRef)
    {
        if (!objRef.TryGet(out NetworkObject netObj)) return;
        carriedObject = netObj.GetComponent<Carriable>();
        carriedObject.AttachToHolder(holdPoint);

        if (carriedObject.IsHeavy)
        {
            playerController.SpeedMultiplier *= heavySpeedPenalty;
            AudioManager.Instance?.PlayHeavyPickup(netObj.transform.position);
        }
        else
        {
            AudioManager.Instance?.PlayPickup(netObj.transform.position);
        }

        if (playerController != null && playerController.IsOwner)
            playerController.IsCarrying.Value = true;
    }

    public void Drop()
    {
        if (IsGameplayBlocked()) return;
        if (carriedObject == null) return;
        DropServerRpc(carriedObject.NetworkObject);
    }

    [ServerRpc]
    void DropServerRpc(NetworkObjectReference objRef)
    {
        if (!objRef.TryGet(out NetworkObject netObj)) return;
        var carriable = netObj.GetComponent<Carriable>();
        carriable?.SetCarried(NetworkObject, false);
        DropClientRpc(objRef);
    }

    [ClientRpc]
    void DropClientRpc(NetworkObjectReference objRef)
    {
        if (!objRef.TryGet(out NetworkObject netObj)) return;
        var c = netObj.GetComponent<Carriable>();
        c?.Detach();

        if (carriedObject != null && carriedObject.IsHeavy)
            playerController.SpeedMultiplier = Mathf.Min(1f, playerController.SpeedMultiplier / heavySpeedPenalty);

        carriedObject = null;
        if (playerController != null && playerController.IsOwner)
            playerController.IsCarrying.Value = false;
    }

    bool IsGameplayBlocked()
    {
        if (!IsOwner) return true;
        if (MvpHud.IsBlockingPanelOpen || VanTransitOverlay.IsActive) return true;
        if (playerController != null && playerController.IsHiddenFromMonsters) return true;
        return TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value;
    }

    public bool IsCarrying => carriedObject != null;

    /// <summary>Two-hand carry: while true the carrier's hotbar is locked (GDD tuning knob).</summary>
    public bool IsCarryingHeavy => carriedObject != null && carriedObject.IsHeavy;

    public Carriable CarriedItem => carriedObject;
}
