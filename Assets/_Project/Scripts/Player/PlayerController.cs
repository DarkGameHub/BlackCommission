using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
    [SerializeField] float crouchCameraDrop = 0.55f;
    [SerializeField] float cameraCrouchLerpSpeed = 12f;

    CharacterController cc;
    PlayerHealth health;
    PlayerInputActions inputActions;
    Vector3 velocity;
    Vector3 standCameraLocalPosition;
    bool hasCameraStandPosition;
    bool isCrouching;
    bool isSprinting;

    public float Stamina { get; private set; }
    public bool IsExhausted { get; private set; }

    // Synced over network for other players to read (animations, carry state, etc.)
    public NetworkVariable<bool> IsCarrying = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> HiddenFromMonsters = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        TryGetComponent(out health);
        Stamina = maxStamina;
        CacheCameraStandPosition();
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
        {
            cameraRoot.GetComponentInChildren<Camera>()?.gameObject.SetActive(true);
            CacheCameraStandPosition();
            UpdateCrouchCamera(true);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputActions?.Disable();
        inputActions = null;
        Cursor.lockState = CursorLockMode.None;
    }

    new void OnDestroy()
    {
        inputActions?.Disable();
        inputActions = null;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (health != null && health.IsDowned.Value)
        {
            isSprinting = false;
            return;
        }
        if (HiddenFromMonsters.Value)
        {
            isSprinting = false;
            velocity = Vector3.zero;
            return;
        }
        if (MvpHud.IsComputerOpen)
        {
            isSprinting = false;
            velocity = Vector3.zero;
            return;
        }

        if (transform.position.y < -6f)
        {
            RecoverFromFall();
            return;
        }

        if (WaterLevelManager.Instance != null)
            SpeedMultiplier = WaterLevelManager.Instance.GetSpeedModifierForHeight(transform.position.y);

        HandleMovement();
        UpdateCrouchCamera(false);
        HandleStamina();
    }

    void HandleMovement()
    {
        if (inputActions == null) return;
        var moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        bool sprintPressed = inputActions.Player.Sprint.IsPressed();
        bool crouchHeld = inputActions.Player.Crouch.IsPressed();
        bool jumpPressed = inputActions.Player.Jump.WasPressedThisFrame();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            sprintPressed |= keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            crouchHeld |= keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            jumpPressed |= keyboard.spaceKey.wasPressedThisFrame;
        }

        bool hasMoveInput = moveInput.sqrMagnitude > 0.01f;
        bool emergencySprint = SchoolMonsterAI.IsEmergencySprintAllowed(transform.position);

        if (crouchHeld != isCrouching)
            SetCrouch(crouchHeld);

        isSprinting = sprintPressed && hasMoveInput && !isCrouching && (emergencySprint || (!IsExhausted && Stamina > 0));

        float currentSpeed = isCrouching ? crouchSpeed
                           : isSprinting ? sprintSpeed
                           : walkSpeed;

        // Water slowdown is applied from outside (WaterLevelManager)
        currentSpeed *= SpeedMultiplier;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        CollisionFlags horizontalFlags = cc.Move(move * currentSpeed * Time.deltaTime);

        bool grounded = cc.isGrounded || (horizontalFlags & CollisionFlags.Below) != 0;
        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        if (grounded && jumpPressed && !isCrouching)
            velocity.y = jumpForce;

        velocity.y += gravity * Time.deltaTime;
        CollisionFlags verticalFlags = cc.Move(velocity * Time.deltaTime);
        if ((verticalFlags & CollisionFlags.Below) != 0 && velocity.y < 0f)
            velocity.y = -2f;
    }

    void RecoverFromFall()
    {
        Vector3 safePosition = SceneManager.GetActiveScene().name == "HQ"
            ? new Vector3(0f, 1.15f, 0f)
            : new Vector3(transform.position.x, 1.15f, transform.position.z);

        cc.enabled = false;
        transform.position = safePosition;
        cc.enabled = true;
        velocity = Vector3.zero;
    }

    void HandleStamina()
    {
        bool emergencySprint = SchoolMonsterAI.IsEmergencySprintAllowed(transform.position);
        if (isSprinting && !emergencySprint)
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
        UpdateCrouchCamera(false);
    }

    void CacheCameraStandPosition()
    {
        if (cameraRoot == null || hasCameraStandPosition) return;
        standCameraLocalPosition = cameraRoot.localPosition;
        hasCameraStandPosition = true;
    }

    void UpdateCrouchCamera(bool instant)
    {
        if (cameraRoot == null || !hasCameraStandPosition) return;

        Vector3 target = standCameraLocalPosition;
        if (isCrouching)
            target.y -= crouchCameraDrop;

        if (instant)
        {
            cameraRoot.localPosition = target;
            return;
        }

        float t = 1f - Mathf.Exp(-cameraCrouchLerpSpeed * Time.deltaTime);
        cameraRoot.localPosition = Vector3.Lerp(cameraRoot.localPosition, target, t);
    }

    // External systems multiply this to slow the player (water, carry weight, etc.)
    public float SpeedMultiplier { get; set; } = 1f;

    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsHiddenFromMonsters => HiddenFromMonsters.Value;

    public void SetHiddenFromMonsters(bool hidden, Vector3 hidePosition, Quaternion hideRotation)
    {
        if (IsOwner)
        {
            transform.SetPositionAndRotation(hidePosition, hideRotation);
            velocity = Vector3.zero;
        }

        if (IsServer)
            HiddenFromMonsters.Value = hidden;
        else
            SetHiddenFromMonstersServerRpc(hidden);
    }

    [ServerRpc]
    void SetHiddenFromMonstersServerRpc(bool hidden)
    {
        HiddenFromMonsters.Value = hidden;
    }
}
