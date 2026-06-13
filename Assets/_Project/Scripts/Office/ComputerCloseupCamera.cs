using UnityEngine;

public class ComputerCloseupCamera : MonoBehaviour
{
    static ComputerCloseupCamera instance;

    Camera closeupCamera;
    Camera disabledPlayerCamera;
    MonoBehaviour disabledCameraController;

    // Entrance dolly: the camera starts at the player's head pose and leans in toward
    // the screen, so opening the terminal feels like physically getting close to it.
    const float LeanInDuration = 0.35f;
    Vector3 startPos;
    Quaternion startRot;
    Vector3 targetPos;
    Quaternion targetRot;
    float leanInStartTime;
    bool hasStartPose;

    public static bool IsActive => instance != null && instance.closeupCamera != null;

    public static void Enter(Transform computerTransform)
    {
        EnsureInstance();
        instance.SetupCloseup(computerTransform);
    }

    public static void Exit()
    {
        if (instance != null)
            instance.TeardownCloseup();
    }

    static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("MVP_ComputerCloseupCamera");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ComputerCloseupCamera>();
    }

    void SetupCloseup(Transform computerTransform)
    {
        TeardownCloseup();
        CaptureStartPose();   // grab the player's head pose before the camera is disabled
        DisablePlayerCamera();

        // The interactable's transform IS the screen centre (was hardcoded to the
        // pre-scale pose; the HQ has since been scaled ×1.25 — follow the object).
        Vector3 screenCenter = computerTransform.position;
        targetPos = screenCenter + new Vector3(0f, 0.125f, -0.85f);

        var camGo = new GameObject("CloseupCam");
        camGo.transform.SetParent(transform);
        camGo.transform.position = targetPos;
        camGo.transform.LookAt(screenCenter);
        targetRot = camGo.transform.rotation;

        // Start the camera back at the player's head (if we have it) and lean in toward
        // the screen; otherwise just sit at the target pose with no animation.
        if (hasStartPose)
        {
            camGo.transform.position = startPos;
            camGo.transform.rotation = startRot;
        }
        leanInStartTime = Time.unscaledTime;

        closeupCamera = camGo.AddComponent<Camera>();
        closeupCamera.fieldOfView = 44f;
        closeupCamera.nearClipPlane = 0.05f;
        closeupCamera.farClipPlane = 20f;
        closeupCamera.depth = 100f;
        closeupCamera.clearFlags = CameraClearFlags.SolidColor;
        closeupCamera.backgroundColor = new Color(0.012f, 0.016f, 0.014f);

        camGo.AddComponent<AudioListener>();
    }

    void Update()
    {
        if (closeupCamera == null) return;

        // Ease from the captured head pose to the screen, then settle into idle sway.
        float lean = hasStartPose
            ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.unscaledTime - leanInStartTime) / LeanInDuration))
            : 1f;
        Vector3 pos = Vector3.Lerp(startPos, targetPos, lean);
        Quaternion rot = Quaternion.Slerp(startRot, targetRot, lean);

        // Gentle breathing sway, faded in as the lean completes.
        float t = Time.unscaledTime;
        float swayX = Mathf.Sin(t * 0.4f) * 0.12f * lean;
        float swayY = Mathf.Sin(t * 0.6f + 0.7f) * 0.06f * lean;

        closeupCamera.transform.position = pos;
        closeupCamera.transform.rotation = rot * Quaternion.Euler(swayX, swayY, 0f);
    }

    void CaptureStartPose()
    {
        hasStartPose = false;
        PlayerCameraController[] controllers = FindObjectsByType<PlayerCameraController>(FindObjectsSortMode.None);
        foreach (var ctrl in controllers)
        {
            if (!ctrl.IsOwner) continue;
            Camera cam = ctrl.GetComponentInChildren<Camera>();
            if (cam == null) continue;
            startPos = cam.transform.position;
            startRot = cam.transform.rotation;
            hasStartPose = true;
            break;
        }
    }

    void TeardownCloseup()
    {
        RestorePlayerCamera();
        if (closeupCamera != null)
        {
            Destroy(closeupCamera.gameObject);
            closeupCamera = null;
        }
    }

    void DisablePlayerCamera()
    {
        PlayerCameraController[] controllers = FindObjectsByType<PlayerCameraController>(FindObjectsSortMode.None);
        foreach (var ctrl in controllers)
        {
            if (!ctrl.IsOwner) continue;
            Camera cam = ctrl.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.enabled = false;
                disabledPlayerCamera = cam;
            }
            AudioListener listener = ctrl.GetComponentInChildren<AudioListener>();
            if (listener != null)
                listener.enabled = false;
            ctrl.enabled = false;
            disabledCameraController = ctrl;
            break;
        }
    }

    void RestorePlayerCamera()
    {
        if (disabledPlayerCamera != null)
        {
            disabledPlayerCamera.enabled = true;
            disabledPlayerCamera = null;
        }
        if (disabledCameraController != null)
        {
            disabledCameraController.enabled = true;
            AudioListener listener = disabledCameraController.GetComponentInChildren<AudioListener>();
            if (listener != null)
                listener.enabled = true;
            disabledCameraController = null;
        }
    }

    void OnDestroy()
    {
        RestorePlayerCamera();
        if (instance == this) instance = null;
    }
}
