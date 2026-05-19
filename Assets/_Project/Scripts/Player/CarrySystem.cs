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
        if (!IsOwner) return;
        inputActions = new PlayerInputActions();
        inputActions.Enable();
        inputActions.Player.Carry.performed += _ => TryPickUp();
        inputActions.Player.Drop.performed += _ => Drop();

        playerCam = GetComponentInChildren<Camera>();
    }

    public override void OnNetworkDespawn()
    {
        inputActions?.Disable();
    }

    void TryPickUp()
    {
        if (carriedObject != null) return;

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
            playerController.SpeedMultiplier *= heavySpeedPenalty;

        playerController.IsCarrying.Value = true;
    }

    public void Drop()
    {
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
        playerController.IsCarrying.Value = false;
    }

    public bool IsCarrying => carriedObject != null;
}
