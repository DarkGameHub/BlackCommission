using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] float interactRange = 2.5f;
    [SerializeField] float aimAssistRadius = 0.12f;
    [SerializeField] float nearbyInteractRadius = 1.75f;

    PlayerController player;
    PlayerInputActions inputActions;
    IInteractable currentTarget;
    Camera playerCamera;
    bool isInteracting;

    public IInteractable CurrentTarget => currentTarget;
    GUIStyle hintStyle;
    Texture2D hintBg;

    void Awake() => player = GetComponent<PlayerController>();

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { enabled = false; return; }
        inputActions = new PlayerInputActions();
        inputActions.Enable();
        playerCamera = GetComponentInChildren<Camera>(true);
    }

    public override void OnNetworkDespawn()
    {
        CleanupInputActions();
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
        if (!IsOwner || inputActions == null) return;

        if (VanTransitOverlay.IsActive || MvpHud.IsBlockingPanelOpen || MainMenuUI.IsGameplayInputBlockedByMenu)
        {
            ClearCurrentInteraction();
            return;
        }

        if (IsDowned())
        {
            ClearCurrentInteraction();
            return;
        }

        FindTarget();
        HandleInput();
    }

    void FindTarget()
    {
        IInteractable aimedTarget = FindAimedTarget();

        if (aimedTarget != currentTarget)
        {
            if (isInteracting && currentTarget != null)
            {
                currentTarget.OnInteractEnd(player);
                isInteracting = false;
            }
            currentTarget = aimedTarget;
        }
    }

    IInteractable FindAimedTarget()
    {
        Transform aim = GetAimTransform();
        Vector3 origin = aim.position;
        Vector3 direction = aim.forward;

        RaycastHit[] hits = aimAssistRadius > 0f
            ? Physics.SphereCastAll(origin, aimAssistRadius, direction, interactRange, ~0, QueryTriggerInteraction.Collide)
            : Physics.RaycastAll(origin, direction, interactRange, ~0, QueryTriggerInteraction.Collide);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.transform.root == transform) continue;
            var interactable = hit.collider != null
                ? hit.collider.GetComponentInParent<IInteractable>()
                : hit.transform.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;
            if (string.IsNullOrEmpty(interactable.InteractHint)) continue;
            return interactable;
        }

        return FindNearbyTarget(origin);
    }

    IInteractable FindNearbyTarget(Vector3 origin)
    {
        Collider[] colliders = Physics.OverlapSphere(origin, nearbyInteractRadius, ~0, QueryTriggerInteraction.Collide);
        IInteractable best = null;
        float bestDistance = float.MaxValue;
        foreach (var collider in colliders)
        {
            if (collider == null || collider.transform.root == transform) continue;
            var interactable = collider.GetComponentInParent<IInteractable>();
            if (interactable == null || string.IsNullOrEmpty(interactable.InteractHint)) continue;

            float distance = Vector3.Distance(origin, collider.ClosestPoint(origin));
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            best = interactable;
        }

        return best;
    }

    Transform GetAimTransform()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);
        return playerCamera != null ? playerCamera.transform : transform;
    }

    bool IsDowned() => TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value;

    void ClearCurrentInteraction()
    {
        if (isInteracting && currentTarget != null)
            currentTarget.OnInteractEnd(player);

        isInteracting = false;
        currentTarget = null;
    }

    void HandleInput()
    {
        if (currentTarget == null) return;
        bool held = inputActions.Player.Interact.IsPressed();
        if (Keyboard.current != null)
            held |= Keyboard.current.eKey.isPressed;

        if (held && !isInteracting)
        {
            isInteracting = true;
            currentTarget.OnInteractStart(player);
        }
        else if (!held && isInteracting)
        {
            isInteracting = false;
            currentTarget.OnInteractEnd(player);
        }
    }

    void OnGUI()
    {
        if (MainMenuUI.IsGameplayInputBlockedByMenu || MainMenuUI.IsMenuVisible) return;
        if (currentTarget == null) return;
        string hint = currentTarget.InteractHint;
        if (string.IsNullOrEmpty(hint)) return;

        if (hintStyle == null)
        {
            hintBg = new Texture2D(1, 1);
            hintBg.SetPixel(0, 0, BlackCommissionUiTheme.ConcreteBlack);
            hintBg.Apply();

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                // Aged paper on rubber black: world-prompt grammar (green = screens only).
                normal = { textColor = BlackCommissionUiTheme.OldPaper, background = hintBg },
                padding = new RectOffset(16, 16, 8, 8)
            };
            MvpFontProvider.ApplyToStyle(hintStyle);
        }

        float w = 340, h = 38;
        GUI.Label(new Rect((Screen.width - w) / 2f, Screen.height * 0.7f, w, h),
            $"[E] {hint}", hintStyle);
    }
}
