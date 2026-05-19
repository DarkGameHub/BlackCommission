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
    GUIStyle hintStyle;
    Texture2D hintBg;

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
            if (hit.transform.root == transform) continue;
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
