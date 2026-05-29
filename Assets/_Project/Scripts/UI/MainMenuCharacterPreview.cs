using UnityEngine;

/// <summary>
/// 3D worker preview shown in the main menu UI. Spawns the Blender-generated worker
/// FBX in a hidden render stage, rotates it slowly, and applies palette colors so the
/// menu reflects the player's chosen variant.
/// </summary>
public class MainMenuCharacterPreview : MonoBehaviour
{
    const int PreviewLayer = 31; // top user layer — kept off main camera view
    const int RenderWidth = 320;
    const int RenderHeight = 280;

    Transform stageRoot;
    Transform modelMount;
    GameObject modelInstance;
    Camera previewCamera;
    RenderTexture renderTexture;
    Light keyLight;
    Light backLight;
    int currentIndex = -1;
    float yaw;

    public RenderTexture Texture => renderTexture;

    void Awake()
    {
        BuildStage();
    }

    void OnDestroy()
    {
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
            renderTexture = null;
        }
        if (stageRoot != null) Destroy(stageRoot.gameObject);
    }

    void Update()
    {
        if (modelMount != null)
        {
            yaw += 18f * Time.unscaledDeltaTime;
            modelMount.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }

    void BuildStage()
    {
        renderTexture = new RenderTexture(RenderWidth, RenderHeight, 16, RenderTextureFormat.ARGB32);
        renderTexture.name = "MainMenuCharacterPreviewTexture";
        renderTexture.antiAliasing = 4;
        renderTexture.Create();

        stageRoot = new GameObject("MainMenuPreviewStage").transform;
        stageRoot.position = new Vector3(0f, -500f, 0f); // hide far below the level

        // Backdrop card so the preview reads on the menu (slight dispatch-green wash).
        var backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(backdrop.GetComponent<Collider>());
        backdrop.name = "Backdrop";
        backdrop.transform.SetParent(stageRoot, false);
        backdrop.transform.localPosition = new Vector3(0f, 1.0f, 2.6f);
        backdrop.transform.localScale = new Vector3(4f, 3.5f, 1f);
        var backdropRenderer = backdrop.GetComponent<Renderer>();
        backdropRenderer.sharedMaterial = MakeMaterial(new Color(0.040f, 0.063f, 0.055f), 0f);
        SetLayer(backdrop, PreviewLayer);

        modelMount = new GameObject("ModelMount").transform;
        modelMount.SetParent(stageRoot, false);
        modelMount.localPosition = new Vector3(0f, 0f, 0f);

        InstantiateWorker();

        var keyGo = new GameObject("KeyLight");
        keyGo.transform.SetParent(stageRoot, false);
        keyGo.transform.localPosition = new Vector3(1.6f, 2.4f, -1.4f);
        keyGo.transform.localRotation = Quaternion.LookRotation(new Vector3(-0.6f, -0.8f, 1f).normalized);
        keyLight = keyGo.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(1f, 0.93f, 0.82f);
        keyLight.intensity = 1.05f;
        keyLight.shadows = LightShadows.Soft;
        keyLight.cullingMask = 1 << PreviewLayer;

        var backGo = new GameObject("BackLight");
        backGo.transform.SetParent(stageRoot, false);
        backGo.transform.localPosition = new Vector3(-1.3f, 1.6f, 1.4f);
        backGo.transform.localRotation = Quaternion.LookRotation(new Vector3(0.7f, -0.3f, -1f).normalized);
        backLight = backGo.AddComponent<Light>();
        backLight.type = LightType.Directional;
        backLight.color = new Color(0.45f, 0.85f, 0.55f);
        backLight.intensity = 0.55f;
        backLight.cullingMask = 1 << PreviewLayer;

        // Worker is ~2.05m tall, feet at local Y=0, helmet top at Y~2.05.
        // Frame the full body: camera at mid-body height, distance set so a 32° FOV
        // sees ~2.6m of vertical extent (leaves headroom + foot margin).
        var camGo = new GameObject("PreviewCamera");
        camGo.transform.SetParent(stageRoot, false);
        camGo.transform.localPosition = new Vector3(0f, 1.0f, -4.6f);
        camGo.transform.localRotation = Quaternion.Euler(2f, 0f, 0f);
        previewCamera = camGo.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.020f, 0.028f, 0.024f, 1f);
        previewCamera.orthographic = false;
        previewCamera.fieldOfView = 32f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 14f;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.targetTexture = renderTexture;
        previewCamera.allowHDR = false;
        previewCamera.depth = -50;
    }

    void InstantiateWorker()
    {
        var prefab = Resources.Load<GameObject>("GeneratedArt/ASV4_WorkerCheapOutsourcedUniform");
        if (prefab == null)
        {
            CreateFallbackCapsule();
            return;
        }

        modelInstance = Instantiate(prefab, modelMount);
        modelInstance.name = "PreviewWorker";
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        foreach (var collider in modelInstance.GetComponentsInChildren<Collider>())
            collider.enabled = false;

        SetLayer(modelInstance, PreviewLayer);

        // Ensure renderers cast/receive shadows for a readable silhouette.
        foreach (var renderer in modelInstance.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    void CreateFallbackCapsule()
    {
        modelInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(modelInstance.GetComponent<Collider>());
        modelInstance.name = "PreviewFallbackBody";
        modelInstance.transform.SetParent(modelMount, false);
        modelInstance.transform.localPosition = new Vector3(0f, 1f, 0f);
        SetLayer(modelInstance, PreviewLayer);
    }

    public void ApplyCharacterIndex(int index)
    {
        if (modelInstance == null) return;
        if (index == currentIndex) return;
        currentIndex = index;

        var colors = PlayerCharacterPalette.Get(index);
        foreach (var renderer in modelInstance.GetComponentsInChildren<Renderer>())
        {
            string n = renderer.gameObject.name.ToLowerInvariant();
            if (n.Contains("uniform") || n.Contains("torso") || n.Contains("arm") || n.Contains("leg") || n.Contains("fabric") || n.Contains("sleeve") || n.Contains("cuff"))
                renderer.material.color = colors.uniform;
            else if (n.Contains("vest") || n.Contains("safety") || n.Contains("reflective"))
                renderer.material.color = colors.vest;
            else if (n.Contains("helmet") || n.Contains("hardhat") || n.Contains("brim"))
                renderer.material.color = colors.helmet;
            else if (n.Contains("glove"))
                renderer.material.color = new Color(0.067f, 0.078f, 0.075f);
            else if (n.Contains("skin") || n.Contains("head") || n.Contains("ear") || n.Contains("jaw") || n.Contains("neck") || n.Contains("nose"))
                renderer.material.color = PlayerCharacterPalette.Skin;
        }
    }

    static void SetLayer(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = layer;
    }

    static Material MakeMaterial(Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
            ?? Shader.Find("Standard");
        var mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.color = color;
        if (emission > 0f && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }
        return mat;
    }
}
