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
    bool usingCharacterModel;
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
        // Must cover the camera frustum (~4.7 x 4.1 at this distance) or the uncovered
        // edges show the near-black clear colour as a border.
        backdrop.transform.localScale = new Vector3(6.2f, 5.2f, 1f);
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
        // No shadows in the preview: with only a vertical backdrop (no floor) the
        // directional shadow projects onto the back card as a misaligned blob.
        keyLight.shadows = LightShadows.None;
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

        // Character is ~1.8m tall, feet at local Y=0. Camera at mid-body height; closer
        // + narrower FOV than before so the body fills more of the frame (bigger read).
        var camGo = new GameObject("PreviewCamera");
        camGo.transform.SetParent(stageRoot, false);
        camGo.transform.localPosition = new Vector3(0f, 0.95f, -3.9f);
        camGo.transform.localRotation = Quaternion.Euler(2f, 0f, 0f);
        previewCamera = camGo.AddComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        // Match the backdrop wash so any sliver outside it doesn't read as a black border.
        previewCamera.backgroundColor = new Color(0.040f, 0.063f, 0.055f, 1f);
        previewCamera.orthographic = false;
        previewCamera.fieldOfView = 28f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 14f;
        previewCamera.cullingMask = 1 << PreviewLayer;
        previewCamera.targetTexture = renderTexture;
        previewCamera.allowHDR = false;
        previewCamera.depth = -50;
    }

    void InstantiateWorker()
    {
        // Prefer the new generated character mesh; fall back to the old worker, then a capsule.
        if (TryInstantiateModel(PlayerCharacterModels.Get(PlayerCharacterPalette.SavedIndex), "PreviewCharacter"))
        {
            usingCharacterModel = true;
            TintPreviewModel(PlayerCharacterModels.TintFor(PlayerCharacterPalette.SavedIndex));
            return;
        }
        if (TryInstantiateModel("GeneratedArt/ASV4_WorkerCheapOutsourcedUniform", "PreviewWorker"))
            return;

        CreateFallbackCapsule();
    }

    bool TryInstantiateModel(string resourceName, string instanceName)
    {
        if (string.IsNullOrEmpty(resourceName)) return false;
        var prefab = Resources.Load<GameObject>(resourceName);
        if (prefab == null) return false;

        modelInstance = Instantiate(prefab, modelMount);
        modelInstance.name = instanceName;
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
        return true;
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

        // One shared mesh, six colour-ways: reuse the model and multiply by the slot tint.
        if (usingCharacterModel)
        {
            if (modelInstance == null)
                TryInstantiateModel(PlayerCharacterModels.Get(index), "PreviewCharacter");
            TintPreviewModel(PlayerCharacterModels.TintFor(index));
            return;
        }

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

    void TintPreviewModel(Color tint)
    {
        if (modelInstance == null) return;
        foreach (var renderer in modelInstance.GetComponentsInChildren<Renderer>())
        {
            Material mat = renderer.material;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            else mat.color = tint;
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
