using UnityEngine;

public class ComputerCloseupCamera : MonoBehaviour
{
    static ComputerCloseupCamera instance;

    Camera closeupCamera;
    Camera disabledPlayerCamera;
    MonoBehaviour disabledCameraController;
    Vector3 baseRotation;

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
        DisablePlayerCamera();

        Vector3 screenCenter = new Vector3(computerTransform.position.x, 1.085f, 1.704f);
        Vector3 camPos = screenCenter + new Vector3(0f, 0.10f, -0.68f);

        var camGo = new GameObject("CloseupCam");
        camGo.transform.SetParent(transform);
        camGo.transform.position = camPos;
        camGo.transform.LookAt(screenCenter);
        baseRotation = camGo.transform.eulerAngles;

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
        float t = Time.unscaledTime;
        float swayX = Mathf.Sin(t * 0.4f) * 0.12f;
        float swayY = Mathf.Sin(t * 0.6f + 0.7f) * 0.06f;
        closeupCamera.transform.eulerAngles = baseRotation + new Vector3(swayX, swayY, 0f);
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
