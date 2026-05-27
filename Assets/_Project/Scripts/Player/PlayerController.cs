using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person player movement. Only runs logic on the owning client.
/// Stamina affects sprint, carry, and breathing audio.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 7f;
    [SerializeField] float crouchSpeed = 2f;
    [SerializeField] float crouchHeight = 1f;
    [SerializeField] float standHeight = 2f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpForce = 5f;

    [Header("Stamina")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float staminaDrainRate = 20f;   // per second while sprinting
    [SerializeField] float staminaRegenRate = 10f;    // per second while not sprinting

    [Header("Camera")]
    [SerializeField] Transform cameraRoot;

    CharacterController cc;
    PlayerHealth health;
    PlayerInputActions inputActions;
    Vector3 velocity;
    bool isCrouching;
    bool isSprinting;

    public float Stamina { get; private set; }
    public bool IsExhausted { get; private set; }

    // Synced over network for other players to read (animations, carry state, etc.)
    public NetworkVariable<bool> IsCarrying = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        TryGetComponent(out health);
        Stamina = maxStamina;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        // Lock cursor only for the local player
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        inputActions = new PlayerInputActions();
        inputActions.Enable();

        // Activate local camera
        if (cameraRoot != null)
            cameraRoot.GetComponentInChildren<Camera>()?.gameObject.SetActive(true);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputActions?.Disable();
        inputActions = null;
        Cursor.lockState = CursorLockMode.None;
    }

    void OnDestroy()
    {
        inputActions?.Disable();
        inputActions = null;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (health != null && health.IsDowned.Value) return;

        if (WaterLevelManager.Instance != null)
            SpeedMultiplier = WaterLevelManager.Instance.GetSpeedModifierForHeight(transform.position.y);

        HandleMovement();
        HandleStamina();
    }

    void HandleMovement()
    {
        if (inputActions == null) return;
        var moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        bool sprintPressed = inputActions.Player.Sprint.IsPressed();
        bool crouchPressed = inputActions.Player.Crouch.WasPressedThisFrame();
        bool jumpPressed = inputActions.Player.Jump.WasPressedThisFrame();

        // Crouch toggle
        if (crouchPressed)
            SetCrouch(!isCrouching);

        // Sprint only if not exhausted and not crouching
        isSprinting = sprintPressed && !IsExhausted && !isCrouching && Stamina > 0;

        float currentSpeed = isCrouching ? crouchSpeed
                           : isSprinting ? sprintSpeed
                           : walkSpeed;

        // Water slowdown is applied from outside (WaterLevelManager)
        currentSpeed *= SpeedMultiplier;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        cc.Move(move * currentSpeed * Time.deltaTime);

        // Jump
        if (cc.isGrounded)
        {
            velocity.y = -2f;
            if (jumpPressed && !isCrouching)
                velocity.y = jumpForce;
        }

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    void HandleStamina()
    {
        if (isSprinting)
        {
            Stamina = Mathf.Max(0, Stamina - staminaDrainRate * Time.deltaTime);
            if (Stamina <= 0) IsExhausted = true;
        }
        else
        {
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegenRate * Time.deltaTime);
            if (Stamina > 20f) IsExhausted = false;
        }
    }

    void SetCrouch(bool crouch)
    {
        isCrouching = crouch;
        cc.height = crouch ? crouchHeight : standHeight;
        cc.center = new Vector3(0, cc.height / 2f, 0);
    }

    // External systems multiply this to slow the player (water, carry weight, etc.)
    public float SpeedMultiplier { get; set; } = 1f;

    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
}
