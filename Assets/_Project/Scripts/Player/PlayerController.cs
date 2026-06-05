using Unity.Collections;
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
    [SerializeField] float standClearancePadding = 0.04f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float groundProbeDistance = 0.14f;
    [SerializeField] float jumpGraceSeconds = 0.12f;

    [Header("Stamina")]
    [SerializeField] float maxStamina = 120f;
    [SerializeField] float staminaDrainRate = 15f;   // per second while sprinting
    [SerializeField] float staminaRegenRate = 10f;    // per second while not sprinting

    [Header("Camera")]
    [SerializeField] Transform cameraRoot;
    [SerializeField] float crouchCameraDrop = 0.55f;
    [SerializeField] float cameraCrouchLerpSpeed = 12f;

    [Header("Swimming")]
    [SerializeField] float swimSpeed = 3.2f;        // free-swim speed underwater (< walkSpeed)
    [SerializeField] float swimAcceleration = 7f;   // how fast velocity eases toward target (water drag)
    [SerializeField] float swimBuoyancy = 0.4f;     // gentle upward drift toward the surface when idle

    CharacterController cc;
    PlayerHealth health;
    PlayerInputActions inputActions;
    Vector3 velocity;
    Vector3 standCameraLocalPosition;
    float lastGroundedTime;
    float nextFootstepTime;
    bool hasCameraStandPosition;
    bool isCrouching;
    bool isSprinting;

    // Swim state — owner-local, fed by mission water triggers via SetWaterState().
    bool inWaterTrigger;
    float waterSurfaceY = float.NegativeInfinity;
    bool isSubmerged;

    public float Stamina { get; private set; }
    public float MaxStamina => maxStamina;
    public bool IsExhausted { get; private set; }

    // Synced over network for other players to read (animations, carry state, etc.)
    public NetworkVariable<bool> IsCarrying = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> HiddenFromMonsters = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> CharacterIndex = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // Player display name, set once by the owner at spawn and read by every peer
    // for nameplates and the lobby roster.
    public NetworkVariable<FixedString64Bytes> DisplayName = new(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> GestureId = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<float> NetworkMoveSpeed = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    // Van seat: -1 = not seated. Server assigns; the owner teleports itself into the
    // shared cabin (ClientNetworkTransform is owner-authoritative). While seated the
    // player can look / switch hotbar / gesture, but cannot move.
    public NetworkVariable<int> SeatIndex = new(-1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Synced so peers can react (future swim animation / nameplate state). Owner writes,
    // everyone reads. Movement itself is driven by the owner-local isSubmerged flag below.
    public NetworkVariable<bool> IsSubmerged = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public bool IsSeated => SeatIndex.Value >= 0;

    /// <summary>Owner-local: true while the camera/head is below the water surface.</summary>
    public bool IsSubmergedLocal => isSubmerged;
    public bool IsInWater => inWaterTrigger;

    float gestureEndTime;

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

        CharacterIndex.Value = PlayerCharacterPalette.SavedIndex;
        DisplayName.Value = PlayerProfile.Name;
        SeatIndex.OnValueChanged += OnSeatChanged;

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
        SeatIndex.OnValueChanged -= OnSeatChanged;
        CleanupInputActions();
        Cursor.lockState = CursorLockMode.None;
    }

    // ─── Van seating (owner-authoritative teleport) ───

    void OnSeatChanged(int previous, int current)
    {
        if (!IsOwner) return;

        if (current >= 0)
            EnterSeat(current);
        else
            ExitSeat();
    }

    void EnterSeat(int seatIndex)
    {
        VanTransitOverlay.EnsureCabin();

        Vector3 pos = VanCabin.SeatWorldPosition(seatIndex);
        Quaternion rot = Quaternion.Euler(0f, VanCabin.SeatYaw(seatIndex), 0f);

        if (cc != null) cc.enabled = false;          // CharacterController would fight the teleport
        transform.SetPositionAndRotation(pos, rot);
        velocity = Vector3.zero;
        isSprinting = false;
        isCrouching = false;
    }

    void ExitSeat()
    {
        // Drop back onto solid ground in the current scene. For scene transitions the
        // destination's spawn manager repositions again right after load; for a voluntary
        // "leave seat" at HQ this is what gets the player out of the floating cabin instead
        // of free-falling from the cabin origin (y≈80).
        Vector3 safe = GetSceneSafePosition();
        if (cc != null) cc.enabled = false;
        transform.position = safe;
        if (cc != null) cc.enabled = true;
        velocity = Vector3.zero;
        VanTransitOverlay.HideCabin();
    }

    /// <summary>Owner asks the server for a cabin seat (E at a van).</summary>
    public void RequestSeat()
    {
        if (IsServer) AssignSeatForClient(OwnerClientId);
        else RequestSeatServerRpc();
    }

    /// <summary>Owner asks to leave its cabin seat (X while seated, before departure).</summary>
    public void RequestLeaveSeat()
    {
        if (IsServer) ClearSeatForClient(OwnerClientId);
        else RequestLeaveSeatServerRpc();
    }

    [ServerRpc]
    void RequestSeatServerRpc(ServerRpcParams p = default)
    {
        AssignSeatForClient(p.Receive.SenderClientId);
    }

    [ServerRpc]
    void RequestLeaveSeatServerRpc(ServerRpcParams p = default)
    {
        ClearSeatForClient(p.Receive.SenderClientId);
    }

    static void ClearSeatForClient(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        var po = client.PlayerObject;
        if (po != null && po.TryGetComponent(out PlayerController target) && target.SeatIndex.Value != -1)
            target.SeatIndex.Value = -1;
    }

    // Server-side: find the lowest free cabin seat and assign it to this client's player.
    static void AssignSeatForClient(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;
        var po = client.PlayerObject;
        if (po == null || !po.TryGetComponent(out PlayerController target)) return;
        if (target.SeatIndex.Value >= 0) return; // already seated

        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int seat = 0; seat < VanCabin.Count; seat++)
        {
            bool taken = false;
            foreach (var pc in all)
                if (pc.SeatIndex.Value == seat) { taken = true; break; }
            if (!taken)
            {
                target.SeatIndex.Value = seat;
                return;
            }
        }
    }

    /// <summary>Server resets every player's seat (called around departure / scene load).</summary>
    public static void ClearAllSeatsServer()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in all)
            if (pc.SeatIndex.Value != -1) pc.SeatIndex.Value = -1;
    }

    // ─── Seating queries (shared by the HQ departure gate and the boarding HUD) ───

    /// <summary>
    /// True when every connected, living player is seated in the cabin. Downed players are
    /// excluded so a downed teammate can't block departure. Solo / no-network always true.
    /// </summary>
    public static bool AreAllLivingSeated()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening) return true;

        int living = 0;
        int seated = 0;
        foreach (var pair in network.ConnectedClients)
        {
            NetworkObject playerObject = pair.Value.PlayerObject;
            if (playerObject == null) continue;
            if (playerObject.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
                continue;

            living++;
            if (playerObject.TryGetComponent<PlayerController>(out var pc) && pc.IsSeated)
                seated++;
        }

        // Never require more seated than the cabin can physically hold, so an over-capacity
        // lobby (more living players than seats) can't permanently block departure.
        return living > 0 && seated >= Mathf.Min(living, VanCabin.Count);
    }

    /// <summary>Counts seated vs. living players for the boarding HUD (downed excluded).</summary>
    public static void GetSeatedCounts(out int seated, out int living)
    {
        seated = 0;
        living = 0;

        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening)
        {
            PlayerController solo = FindAnyObjectByType<PlayerController>();
            living = solo != null ? 1 : 0;
            seated = solo != null && solo.IsSeated ? 1 : 0;
            return;
        }

        foreach (var pair in network.ConnectedClients)
        {
            NetworkObject playerObject = pair.Value.PlayerObject;
            if (playerObject == null) continue;
            if (playerObject.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
                continue;

            living++;
            if (playerObject.TryGetComponent<PlayerController>(out var pc) && pc.IsSeated)
                seated++;
        }
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
        if (MvpHud.IsBlockingPanelOpen || MainMenuUI.IsGameplayInputBlockedByMenu)
        {
            isSprinting = false;
            velocity = Vector3.zero;
            return;
        }
        // Seated in the van: movement is locked. Hotbar switching + camera look stay
        // available (handled in their own controllers). Gestures are intentionally not
        // enabled here yet.
        if (IsSeated)
        {
            isSprinting = false;
            velocity = Vector3.zero;
            return;
        }

        UpdateGestureInput();

        if (transform.position.y < -100f)
        {
            RecoverFromFall();
            return;
        }

        HandleMovementOrSwim();
        UpdateCrouchCamera(false);
        HandleStamina();
    }

    void HandleMovementOrSwim()
    {
        UpdateSubmersionState();
        if (isSubmerged)
            HandleSwim();
        else
            HandleMovement();
    }

    // Decide whether the head is underwater. A small hysteresis margin around the surface
    // stops swim/walk from flickering while the player bobs at the waterline.
    void UpdateSubmersionState()
    {
        bool submergedNow;
        if (!inWaterTrigger)
        {
            submergedNow = false;
        }
        else
        {
            float headY = cameraRoot != null
                ? cameraRoot.position.y
                : transform.position.y + (cc != null ? cc.height : standHeight);
            const float margin = 0.12f;
            if (isSubmerged)
                submergedNow = headY < waterSurfaceY + margin;
            else
                submergedNow = headY < waterSurfaceY - margin;
        }

        if (submergedNow == isSubmerged) return;

        isSubmerged = submergedNow;
        if (isSubmerged)
        {
            if (isCrouching) SetCrouch(false);   // can't crouch while swimming
        }
        else
        {
            // Surfacing: drop leftover swim drift so walk mode (which only manages velocity.y)
            // doesn't keep sliding the player horizontally.
            velocity.x = 0f;
            velocity.z = 0f;
        }
        if (IsOwner && IsSubmerged.Value != isSubmerged)
            IsSubmerged.Value = isSubmerged;
    }

    // Free 3D swimming: WASD moves along the camera's look direction (pitch included),
    // Space ascends and Ctrl descends. No gravity; velocity eases toward target for water drag.
    void HandleSwim()
    {
        if (inputActions == null) return;

        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        bool ascend = inputActions.Player.Jump.IsPressed();
        bool descend = inputActions.Player.Crouch.IsPressed();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            ascend |= keyboard.spaceKey.isPressed;
            descend |= keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        }

        Transform aim = cameraRoot != null ? cameraRoot : transform;
        Vector3 dir = aim.forward * moveInput.y + aim.right * moveInput.x;
        dir += Vector3.up * ((ascend ? 1f : 0f) - (descend ? 1f : 0f));

        bool hasInput = dir.sqrMagnitude > 0.0001f;
        Vector3 targetVelocity = hasInput
            ? dir.normalized * swimSpeed * SpeedMultiplier
            : Vector3.up * swimBuoyancy;       // idle: drift gently toward the surface

        float t = 1f - Mathf.Exp(-swimAcceleration * Time.deltaTime);
        velocity = Vector3.Lerp(velocity, targetVelocity, t);
        cc.Move(velocity * Time.deltaTime);

        float targetNetSpeed = hasInput ? 0.5f : 0f;
        if (Mathf.Abs(NetworkMoveSpeed.Value - targetNetSpeed) > 0.05f)
            NetworkMoveSpeed.Value = targetNetSpeed;
    }

    /// <summary>Called by mission water triggers on the local owner as it enters/leaves water.</summary>
    public void SetWaterState(bool inWater, float surfaceY)
    {
        inWaterTrigger = inWater;
        if (inWater) waterSurfaceY = surfaceY;
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

        float targetNetSpeed = !hasMoveInput ? 0f : isSprinting ? 1f : isCrouching ? 0.25f : 0.5f;
        if (Mathf.Abs(NetworkMoveSpeed.Value - targetNetSpeed) > 0.05f)
            NetworkMoveSpeed.Value = targetNetSpeed;

        currentSpeed *= SpeedMultiplier;

        bool groundedBeforeMove = HasGroundContact();
        if (groundedBeforeMove)
            lastGroundedTime = Time.time;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        CollisionFlags horizontalFlags = cc.Move(move * currentSpeed * Time.deltaTime);

        bool grounded = groundedBeforeMove || cc.isGrounded || (horizontalFlags & CollisionFlags.Below) != 0;
        if (grounded)
            lastGroundedTime = Time.time;

        if (grounded && hasMoveInput && Time.time >= nextFootstepTime)
        {
            float interval = isSprinting ? 0.32f : isCrouching ? 0.6f : 0.45f;
            nextFootstepTime = Time.time + interval;
            AudioManager.Instance?.PlayFootstep(transform.position);
        }

        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        bool canJump = grounded || Time.time - lastGroundedTime <= jumpGraceSeconds;
        if (canJump && jumpPressed && !isCrouching)
        {
            velocity.y = jumpForce;
            lastGroundedTime = -999f;
        }

        velocity.y += gravity * Time.deltaTime;
        CollisionFlags verticalFlags = cc.Move(velocity * Time.deltaTime);
        if ((verticalFlags & CollisionFlags.Below) != 0 && velocity.y < 0f)
        {
            velocity.y = -2f;
            lastGroundedTime = Time.time;
        }
        if ((verticalFlags & CollisionFlags.Above) != 0 && velocity.y > 0f)
            velocity.y = 0f;
    }

    bool HasGroundContact()
    {
        if (cc == null) return true;

        Vector3 center = transform.TransformPoint(cc.center);
        float radius = Mathf.Max(0.05f, cc.radius * 0.86f);
        float halfHeight = Mathf.Max(cc.height * 0.5f, radius);
        Vector3 castOrigin = center + Vector3.down * (halfHeight - radius - 0.02f);

        bool wasEnabled = cc.enabled;
        if (wasEnabled) cc.enabled = false;
        bool grounded = Physics.SphereCast(
            castOrigin,
            radius,
            Vector3.down,
            out _,
            groundProbeDistance,
            ~0,
            QueryTriggerInteraction.Ignore);
        if (wasEnabled) cc.enabled = true;
        return grounded;
    }

    void UpdateGestureInput()
    {
        if (GestureId.Value != 0 && Time.time > gestureEndTime)
            GestureId.Value = 0;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        int pressed = 0;
        if (keyboard.f1Key.wasPressedThisFrame) pressed = 1;
        else if (keyboard.f2Key.wasPressedThisFrame) pressed = 2;
        else if (keyboard.f3Key.wasPressedThisFrame) pressed = 3;
        else if (keyboard.f4Key.wasPressedThisFrame) pressed = 4;
        else if (keyboard.f5Key.wasPressedThisFrame) pressed = 5;

        if (pressed > 0)
        {
            GestureId.Value = pressed;
            gestureEndTime = Time.time + PlayerGestures.Duration;
        }
    }

    void RecoverFromFall()
    {
        Vector3 safePosition = GetSceneSafePosition();

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
        if (!crouch && !HasStandClearance())
            return;

        isCrouching = crouch;
        cc.height = crouch ? crouchHeight : standHeight;
        cc.center = new Vector3(0, cc.height / 2f, 0);
        UpdateCrouchCamera(false);
    }

    bool HasStandClearance()
    {
        if (cc == null) return true;

        float radius = Mathf.Max(0.05f, cc.radius + standClearancePadding);
        Vector3 bottom = transform.position + Vector3.up * (crouchHeight + radius);
        Vector3 top = transform.position + Vector3.up * (standHeight - radius);

        bool wasEnabled = cc.enabled;
        if (wasEnabled) cc.enabled = false;
        bool blocked = Physics.CheckCapsule(bottom, top, radius, ~0, QueryTriggerInteraction.Ignore);
        if (wasEnabled) cc.enabled = true;
        return !blocked;
    }

    Vector3 GetSceneSafePosition()
    {
        GameObject spawn = GameObject.Find("PlayerSpawnPoint");
        if (spawn != null)
            return spawn.transform.position + Vector3.up * 0.05f;

        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName == "HQ")
            return new Vector3(-1.55f, 1.15f, 0.55f);
        if (sceneName.Contains("School"))
            return new Vector3(0f, 1.15f, -11.45f);

        return new Vector3(0f, 1.15f, 0f);
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

    public void RestoreControlAt(Vector3 position, Quaternion rotation)
    {
        ClearSeatForSceneSpawn();

        bool hadController = cc != null;
        if (hadController)
            cc.enabled = false;

        transform.SetPositionAndRotation(position, rotation);

        velocity = Vector3.zero;
        isSprinting = false;
        isCrouching = false;
        SpeedMultiplier = 1f;
        IsExhausted = false;
        Stamina = maxStamina;

        inWaterTrigger = false;
        waterSurfaceY = float.NegativeInfinity;
        if (isSubmerged)
        {
            isSubmerged = false;
            if (IsOwner) IsSubmerged.Value = false;
        }

        if (cc != null)
        {
            cc.height = standHeight;
            cc.center = new Vector3(0, cc.height / 2f, 0);
            cc.enabled = true;
        }

        UpdateCrouchCamera(true);

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (IsServer)
            HiddenFromMonsters.Value = false;
        else if (IsOwner)
            SetHiddenFromMonstersServerRpc(false);
    }

    void ClearSeatForSceneSpawn()
    {
        if (SeatIndex.Value == -1) return;

        if (IsServer)
            SeatIndex.Value = -1;
        else if (IsOwner)
            ClearSeatForSceneSpawnServerRpc();
    }

    [ServerRpc]
    void ClearSeatForSceneSpawnServerRpc(ServerRpcParams p = default)
    {
        if (p.Receive.SenderClientId != OwnerClientId) return;
        SeatIndex.Value = -1;
    }

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
