using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class PlayerInteraction : NetworkBehaviour
{
    [SerializeField] float interactRange = 2.5f;

    PlayerController player;
    PlayerInputActions inputActions;
    IInteractable currentTarget;
    bool isInteracting;

    void Awake() => player = GetComponent<PlayerController>();

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { enabled = false; return; }
        inputActions = new PlayerInputActions();
        inputActions.Enable();
    }

    public override void OnNetworkDespawn()
    {
        inputActions?.Disable();
    }

    void Update()
    {
        FindTarget();
        HandleInput();
    }

    void FindTarget()
    {
        var hits = Physics.OverlapSphere(transform.position, interactRange, ~0, QueryTriggerInteraction.Collide);
        IInteractable nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            var interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null) continue;
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < nearestDist) { nearest = interactable; nearestDist = dist; }
        }

        if (nearest != currentTarget)
        {
            if (isInteracting && currentTarget != null)
            {
                currentTarget.OnInteractEnd(player);
                isInteracting = false;
            }
            currentTarget = nearest;
        }
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

    public string DebugTargetName => currentTarget == null ? "" : currentTarget.GetType().Name;

    void OnGUI()
    {
        if (currentTarget == null) return;
        string hint = currentTarget.InteractHint;
        if (string.IsNullOrEmpty(hint)) return;

        var style = new GUIStyle(GUI.skin.box);
        style.fontSize = 18;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        float w = 320, h = 36;
        GUI.Box(new Rect((Screen.width - w) / 2f, Screen.height * 0.72f, w, h),
            $"[E] {hint}", style);
    }
}
