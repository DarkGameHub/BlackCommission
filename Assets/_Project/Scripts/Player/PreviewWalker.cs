using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Lightweight, NON-networked first-person walker for blockout walkthroughs — drop it in a scene,
/// press Play, and walk around to feel the space. This is deliberately NOT the real
/// <see cref="PlayerController"/> (which is a NetworkBehaviour that only moves for its owner and so
/// needs a host). Uses the Input System directly (Keyboard/Mouse.current) like the rest of the
/// project. WASD move, mouse look, Shift sprint, Space jump, Esc frees/locks the cursor.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PreviewWalker : MonoBehaviour
{
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float sprintSpeed = 7f;
    [SerializeField] float jumpHeight = 1.1f;
    [SerializeField] float gravity = -18f;
    [SerializeField] float mouseSensitivity = 0.12f;
    [SerializeField] Transform cam;

    CharacterController cc;
    float pitch;
    float vy;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (cam == null)
        {
            var c = GetComponentInChildren<Camera>();
            cam = c != null ? c.transform : (Camera.main != null ? Camera.main.transform : null);
        }
        // Walkthroughs should preview the real look: URP post is off by default.
        var camComp = cam != null ? cam.GetComponent<Camera>() : null;
        if (camComp != null)
            camComp.GetUniversalAdditionalCameraData().renderPostProcessing = true;
    }

    void OnEnable()
    {
        // Walkthrough rig only: in a hosted session the real networked players arrive
        // via the HQ flow, so the preview rig removes itself.
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
        {
            gameObject.SetActive(false);
            return;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        var mouse = Mouse.current;

        if (kb.escapeKey.wasPressedThisFrame)
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        // Mouse look (yaw on the body, pitch on the camera).
        if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 d = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, d.x, 0f);
            pitch = Mathf.Clamp(pitch - d.y, -89f, 89f);
            if (cam != null) cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        // Planar movement.
        float ix = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
        float iz = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
        Vector3 dir = Vector3.ClampMagnitude(transform.right * ix + transform.forward * iz, 1f);
        float speed = kb.leftShiftKey.isPressed ? sprintSpeed : walkSpeed;

        // Gravity + jump.
        if (cc.isGrounded)
        {
            vy = -2f;
            if (kb.spaceKey.wasPressedThisFrame) vy = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        vy += gravity * Time.deltaTime;

        cc.Move((dir * speed + Vector3.up * vy) * Time.deltaTime);

        UpdateInteract(kb);
    }

    // ── preview-only interaction (E) ──
    // Lets walkthroughs exercise interactables that have an offline fallback (e.g. the
    // P-01 breaker's local-solo hold). Interactables that genuinely need a network
    // session may reject the null player — that's expected; full tests run hosted.
    IInteractable currentTarget;
    bool interacting;

    void UpdateInteract(Keyboard kb)
    {
        IInteractable target = null;
        if (cam != null && Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, 2.5f,
                ~0, QueryTriggerInteraction.Collide))
            target = hit.collider.GetComponentInParent<IInteractable>();

        if (!ReferenceEquals(target, currentTarget))
        {
            EndInteract();
            currentTarget = target;
        }
        if (currentTarget == null) return;

        if (kb.eKey.wasPressedThisFrame && !interacting)
        {
            interacting = true;
            try { currentTarget.OnInteractStart(null); }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PreviewWalker] interactable needs a real player session: {e.Message}");
                interacting = false;
            }
        }
        if (kb.eKey.wasReleasedThisFrame) EndInteract();
    }

    void EndInteract()
    {
        if (interacting && currentTarget != null)
        {
            try { currentTarget.OnInteractEnd(null); }
            catch { /* preview-only: a networked interactable may throw on null player */ }
        }
        interacting = false;
    }

    void OnGUI()
    {
        if (currentTarget == null) return;
        string hint = currentTarget.InteractHint;
        if (string.IsNullOrEmpty(hint)) return;
        var style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 };
        GUI.Label(new Rect(Screen.width * 0.5f - 200f, Screen.height * 0.62f, 400f, 28f), $"[E] {hint}", style);
    }
}
