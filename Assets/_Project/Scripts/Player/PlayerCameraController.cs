using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mouse-look for first-person camera. Only active for the local owner.
/// Attach to the camera root (child of the player capsule).
/// </summary>
public class PlayerCameraController : NetworkBehaviour
{
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float verticalClamp = 85f;
    [SerializeField] Transform playerBody;  // rotate body for left/right; rotate this for up/down

    PlayerInputActions inputActions;
    float verticalAngle;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Disable camera for remote players — they have their own
            GetComponentInChildren<Camera>().enabled = false;
            enabled = false;
            return;
        }

        inputActions = new PlayerInputActions();
        inputActions.Enable();
    }

    public override void OnNetworkDespawn()
    {
        inputActions?.Disable();
    }

    void Update()
    {
        if (!IsOwner) return;

        var look = inputActions.Player.Look.ReadValue<Vector2>();
        float mouseX = look.x * mouseSensitivity;
        float mouseY = look.y * mouseSensitivity;

        verticalAngle = Mathf.Clamp(verticalAngle - mouseY, -verticalClamp, verticalClamp);
        transform.localRotation = Quaternion.Euler(verticalAngle, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public void AddSway(float amount)
    {
        // Called by stability system to add screen sway when stressed
        verticalAngle += Random.Range(-amount, amount) * 0.1f;
    }
}
