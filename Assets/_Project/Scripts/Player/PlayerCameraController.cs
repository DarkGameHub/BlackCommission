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
    Camera localCamera;
    float verticalAngle;

    public static float HorizontalSensitivity
    {
        get => PlayerPrefs.GetFloat("AS.Camera.HorizontalSensitivity", 2f);
        set => PlayerPrefs.SetFloat("AS.Camera.HorizontalSensitivity", Mathf.Clamp(value, 0.25f, 8f));
    }

    public static float VerticalSensitivity
    {
        get => PlayerPrefs.GetFloat("AS.Camera.VerticalSensitivity", 2f);
        set => PlayerPrefs.SetFloat("AS.Camera.VerticalSensitivity", Mathf.Clamp(value, 0.25f, 8f));
    }

    public static bool InvertY
    {
        get => PlayerPrefs.GetInt("AS.Camera.InvertY", 0) != 0;
        set => PlayerPrefs.SetInt("AS.Camera.InvertY", value ? 1 : 0);
    }

    public static float FieldOfView
    {
        get => PlayerPrefs.GetFloat("AS.Camera.FieldOfView", 68f);
        set => PlayerPrefs.SetFloat("AS.Camera.FieldOfView", Mathf.Clamp(value, 55f, 95f));
    }

    public override void OnNetworkSpawn()
    {
        if (playerBody == null)
            playerBody = transform.root;

        var cam = GetComponentInChildren<Camera>(true);
        var listener = GetComponentInChildren<AudioListener>(true);

        if (!IsOwner)
        {
            if (cam != null) cam.enabled = false;
            if (listener != null) listener.enabled = false;
            enabled = false;
            return;
        }

        // Local owner: make sure camera is active and enabled
        if (cam != null)
        {
            cam.gameObject.SetActive(true);
            cam.enabled = true;
            localCamera = cam;
            ApplyFieldOfView();
        }
        if (listener != null)
        {
            listener.gameObject.SetActive(true);
            listener.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        inputActions = new PlayerInputActions();
        inputActions.Enable();
    }

    public override void OnNetworkDespawn()
    {
        inputActions?.Disable();
        inputActions = null;
    }

    void OnDestroy()
    {
        inputActions?.Disable();
        inputActions = null;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (inputActions == null) return;
        ApplyFieldOfView();
        if (MvpHud.IsBlockingPanelOpen || VanTransitOverlay.IsActive) return;
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        var look = inputActions.Player.Look.ReadValue<Vector2>();
        float fallbackSensitivity = Mathf.Max(0.01f, mouseSensitivity);
        float mouseX = look.x * (HorizontalSensitivity / 2f) * fallbackSensitivity;
        float mouseY = look.y * (VerticalSensitivity / 2f) * fallbackSensitivity;
        if (InvertY)
            mouseY = -mouseY;

        verticalAngle = Mathf.Clamp(verticalAngle - mouseY, -verticalClamp, verticalClamp);
        transform.localRotation = Quaternion.Euler(verticalAngle, 0f, 0f);
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

    public void AddSway(float amount)
    {
        // Called by stability system to add screen sway when stressed
        verticalAngle += Random.Range(-amount, amount) * 0.1f;
    }

    void ApplyFieldOfView()
    {
        if (localCamera == null)
            localCamera = GetComponentInChildren<Camera>(true);
        if (localCamera != null)
            localCamera.fieldOfView = FieldOfView;
    }
}
