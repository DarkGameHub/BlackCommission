using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] float interactRange = 2.5f;
    [SerializeField] float aimAssistRadius = 0.12f;

    PlayerController player;
    PlayerInputActions inputActions;
    IInteractable currentTarget;
    Camera playerCamera;
    bool isInteracting;
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
        inputActions?.Disable();
    }

    void Update()
    {
        if (!IsOwner || inputActions == null) return;

        if (VanTransitOverlay.IsActive || MvpHud.IsBlockingPanelOpen)
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

        return null;
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
        if (currentTarget == null) return;
        string hint = currentTarget.InteractHint;
        if (string.IsNullOrEmpty(hint)) return;

        if (hintStyle == null)
        {
            hintBg = new Texture2D(1, 1);
            hintBg.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
            hintBg.Apply();

            hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white, background = hintBg },
                padding = new RectOffset(16, 16, 8, 8)
            };
        }

        float w = 340, h = 38;
        GUI.Label(new Rect((Screen.width - w) / 2f, Screen.height * 0.7f, w, h),
            $"[E] {hint}", hintStyle);
    }
}
