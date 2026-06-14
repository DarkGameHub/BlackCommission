using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerFirstPersonRig : NetworkBehaviour
{
    [SerializeField] Vector3 rigLocalPosition = new(0f, -0.34f, 0.46f);
    [SerializeField] Vector3 fpModelOffset = new(0f, -1.35f, 0.32f);
    [SerializeField] Vector3 fpModelRotation = new(72f, 0f, 0f);
    [SerializeField] Vector3 fpModelScale = new(0.55f, 1.1f, 1.1f);

    PlayerHotbar hotbar;
    PlayerController controller;
    Transform cameraTransform;
    Transform heldItemRoot;
    GameObject rigRoot;
    GameObject thirdPersonRoot;
    GameObject fpCharacterVisual;
    GameObject tpCharacterVisual;
    GameObject flashlightModel;
    GameObject watchModel;
    GameObject thirdPersonWatchModel;

    GameObject nameplateRoot;
    TextMeshPro nameplateText;
    string lastNameplateValue;

    Vector3 tpVisualDefaultPos;
    Vector3 tpVisualDefaultRot;
    int lastAppliedGesture;

    Material skinMaterial;
    Material sleeveMaterial;
    Material darkMaterial;
    Material redMaterial;
    Material flashlightMaterial;
    Material lightMaterial;
    Material watchMaterial;
    Material watchScreenMaterial;
    Renderer bodyRenderer;
    bool bodyRendererWasEnabled;

    public override void OnNetworkSpawn()
    {
        hotbar = GetComponent<PlayerHotbar>();
        controller = GetComponent<PlayerController>();
        HideCapsuleBodyMesh();

        // Build third person visual for others. 
        // Local owner can also build it but usually hides it to avoid clipping.
        BuildThirdPersonVisual();

        if (IsOwner)
        {
            BuildRig();
            // Hide the WHOLE third-person body from the owner. This MUST cover all three build
            // paths in BuildThirdPersonVisual — the generated-worker and primitive fallbacks
            // leave tpCharacterVisual null, so the old tpCharacterVisual-only hide missed them
            // and the owner's own body kept rendering below the camera AND casting a floor shadow
            // that followed the player ("阴影随人物移动", PM 2026-06-13). Disabling the renderers
            // stops both the unseen render and that shadow.
            if (thirdPersonRoot != null)
                foreach (var r in thirdPersonRoot.GetComponentsInChildren<Renderer>(true))
                    r.enabled = false;
        }
        else
            BuildNameplate();
    }

    public override void OnNetworkDespawn()
    {
        RestoreLocalBodyMesh();
        if (rigRoot != null)
            Destroy(rigRoot);
        if (thirdPersonRoot != null)
            Destroy(thirdPersonRoot);
        if (nameplateRoot != null)
            Destroy(nameplateRoot);
    }

    public override void OnDestroy()
    {
        RestoreLocalBodyMesh();
        base.OnDestroy();
    }

    void LateUpdate()
    {
        if (!IsOwner)
        {
            if (hotbar != null && thirdPersonWatchModel != null)
                thirdPersonWatchModel.SetActive(hotbar.HasWristwatch.Value);

            float moveSpeed = controller != null ? controller.NetworkMoveSpeed.Value : 0f;
            ApplyThirdPersonAnimation(moveSpeed);

            UpdateNameplate();
            return;
        }

        if (hotbar == null || heldItemRoot == null) return;

        HotbarSlot selected = hotbar.GetSlot(hotbar.SelectedSlot.Value);
        MvpHotbarItemId itemId = selected == null || selected.IsEmpty
            ? MvpHotbarItemId.None
            : selected.itemId;

        SetActiveItem(itemId);

        if (watchModel != null)
            watchModel.SetActive(hotbar.HasWristwatchOwned);

        ApplyFirstPersonBob();
    }

    void ApplyFirstPersonBob()
    {
        if (rigRoot == null || controller == null) return;
        float speed = controller.NetworkMoveSpeed.Value;
        
        float t = Time.time;
        if (speed < 0.05f)
        {
            // Idle breathing bob on items
            float idleBob = Mathf.Sin(t * 1.5f) * 0.005f;
            rigRoot.transform.localPosition = new Vector3(0, idleBob, 0);
            return;
        }

        // Walk / Run bobbing for items
        float freq = speed > 0.7f ? 12f : 8f;
        float bobY = Mathf.Sin(t * freq) * (speed > 0.7f ? 0.022f : 0.012f);
        float bobX = Mathf.Sin(t * freq * 0.5f) * (speed > 0.7f ? 0.012f : 0.006f);
        
        rigRoot.transform.localPosition = new Vector3(bobX, Mathf.Abs(bobY), 0f);
    }

    // World-space floating name tag shown above other players' heads.
    void BuildNameplate()
    {
        nameplateRoot = new GameObject("Nameplate");
        nameplateRoot.transform.SetParent(transform, false);
        nameplateRoot.transform.localPosition = new Vector3(0f, 2.15f, 0f);

        nameplateText = nameplateRoot.AddComponent<TextMeshPro>();
        nameplateText.alignment = TextAlignmentOptions.Center;
        nameplateText.fontSize = 2.6f;
        nameplateText.fontStyle = FontStyles.Bold;
        nameplateText.enableWordWrapping = false;
        nameplateText.color = new Color(0.86f, 0.83f, 0.70f);
        nameplateText.outlineWidth = 0.22f;
        nameplateText.outlineColor = new Color32(8, 10, 9, 255);
        var font = MvpTmpFontProvider.GetFontAsset();
        if (font != null) nameplateText.font = font;
        nameplateText.rectTransform.sizeDelta = new Vector2(4f, 1f);
        nameplateText.text = "";
    }

    void UpdateNameplate()
    {
        if (nameplateRoot == null || nameplateText == null) return;

        if (controller != null)
        {
            string current = controller.DisplayName.Value.ToString();
            if (current != lastNameplateValue)
            {
                lastNameplateValue = current;
                nameplateText.text = current;
                // Tint the tag with this player's vest colour for quick identification.
                var colors = PlayerCharacterPalette.Get(controller.CharacterIndex.Value);
                nameplateText.color = Color.Lerp(colors.vest, Color.white, 0.45f);
            }
        }

        // Billboard: copying the camera rotation keeps world-space TMP readable
        // (and not back-facing) regardless of viewing angle.
        Camera cam = Camera.main;
        if (cam != null)
            nameplateRoot.transform.rotation = cam.transform.rotation;
    }

    void BuildRig()
    {
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam == null) return;

        cameraTransform = cam.transform;
        cam.nearClipPlane = Mathf.Min(cam.nearClipPlane, 0.03f);

        if (rigRoot != null) Destroy(rigRoot);
        rigRoot = new GameObject("MVP_FirstPersonRig");
        rigRoot.transform.SetParent(cameraTransform, false);
        rigRoot.transform.localPosition = Vector3.zero;
        rigRoot.transform.localRotation = Quaternion.identity;

        EnsureMaterials();
        
        watchModel = CreateWristwatch(rigRoot.transform);
        watchModel.SetActive(false);

        heldItemRoot = new GameObject("HeldItemRoot").transform;
        heldItemRoot.SetParent(rigRoot.transform, false);
        heldItemRoot.localPosition = new Vector3(0.24f, -0.22f, 0.35f); // Standard floating item position
        heldItemRoot.localRotation = Quaternion.Euler(5f, -8f, 0f);

        flashlightModel = CreateFlashlight(heldItemRoot);
        SetActiveItem(MvpHotbarItemId.None);

        // First-person view models must NOT cast world shadows. The rig is parented to the
        // camera, so any shadow they throw slides across the room as the player moves/looks —
        // exactly the "阴影随人物移动" the PM saw (2026-06-13). Kill shadow casting on the rig.
        foreach (var rend in rigRoot.GetComponentsInChildren<Renderer>(true))
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void BuildThirdPersonVisual()
{
        if (thirdPersonRoot != null) return;

        EnsureMaterials();

        PlayerController ctrl = GetComponent<PlayerController>();
        int charIndex = ctrl != null ? ctrl.CharacterIndex.Value : 0;
        var colors = PlayerCharacterPalette.Get(charIndex);
        Material vestMat = MakeMaterial(colors.vest);
        Material helmetMat = MakeMaterial(colors.helmet);

        thirdPersonRoot = new GameObject("MVP_PlayerCharacterModel");
        thirdPersonRoot.transform.SetParent(transform, false);
        thirdPersonRoot.transform.localPosition = Vector3.zero;
        thirdPersonRoot.transform.localRotation = Quaternion.identity;

        // Preferred path: a real generated character mesh (already textured and
        // normalized by CharacterModelImporter), so we don't recolor it.
        if (TryCreateCharacterVisual(thirdPersonRoot.transform, charIndex))
        {
            thirdPersonWatchModel = CreateThirdPersonWristwatch(thirdPersonRoot.transform);
            thirdPersonWatchModel.SetActive(hotbar != null && hotbar.HasWristwatch.Value);
            return;
        }

        if (TryCreateGeneratedWorkerVisual(thirdPersonRoot.transform))
        {
            ApplyCharacterColorsToGenerated(thirdPersonRoot, colors);
            thirdPersonWatchModel = CreateThirdPersonWristwatch(thirdPersonRoot.transform);
            thirdPersonWatchModel.SetActive(hotbar != null && hotbar.HasWristwatch.Value);
            return;
        }

        CreatePrimitive(PrimitiveType.Cube, "UniformTorso", thirdPersonRoot.transform,
            new Vector3(0f, 1.05f, 0f), new Vector3(0.48f, 0.78f, 0.28f),
            Quaternion.identity, sleeveMaterial);
        CreatePrimitive(PrimitiveType.Sphere, "Head", thirdPersonRoot.transform,
            new Vector3(0f, 1.58f, 0f), new Vector3(0.32f, 0.34f, 0.32f),
            Quaternion.identity, skinMaterial);
        CreatePrimitive(PrimitiveType.Cube, "LeftArm", thirdPersonRoot.transform,
            new Vector3(-0.36f, 1.1f, 0.02f), new Vector3(0.13f, 0.62f, 0.13f),
            Quaternion.Euler(0f, 0f, 10f), sleeveMaterial);
        CreatePrimitive(PrimitiveType.Cube, "RightArm", thirdPersonRoot.transform,
            new Vector3(0.36f, 1.1f, 0.02f), new Vector3(0.13f, 0.62f, 0.13f),
            Quaternion.Euler(0f, 0f, -10f), sleeveMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Vest", thirdPersonRoot.transform,
            new Vector3(0f, 1.18f, -0.12f), new Vector3(0.50f, 0.36f, 0.04f),
            Quaternion.identity, vestMat);
        CreatePrimitive(PrimitiveType.Cube, "Helmet", thirdPersonRoot.transform,
            new Vector3(0f, 1.78f, 0f), new Vector3(0.36f, 0.1f, 0.34f),
            Quaternion.identity, helmetMat);
        CreatePrimitive(PrimitiveType.Cube, "DebtBadge", thirdPersonRoot.transform,
            new Vector3(0.15f, 1.24f, -0.16f), new Vector3(0.14f, 0.08f, 0.02f),
            Quaternion.identity, redMaterial);
        CreatePrimitive(PrimitiveType.Cube, "CheapBackpack", thirdPersonRoot.transform,
            new Vector3(0f, 1.05f, 0.2f), new Vector3(0.38f, 0.48f, 0.12f),
            Quaternion.identity, darkMaterial);
        thirdPersonWatchModel = CreateThirdPersonWristwatch(thirdPersonRoot.transform);
        thirdPersonWatchModel.SetActive(hotbar != null && hotbar.HasWristwatch.Value);
    }

    static void ApplyCharacterColorsToGenerated(GameObject root, PlayerCharacterPalette.CharacterColors colors)
    {
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            string n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("uniform") || n.Contains("torso") || n.Contains("arm") || n.Contains("leg") || n.Contains("fabric"))
                r.material.color = colors.uniform;
            else if (n.Contains("vest") || n.Contains("safety"))
                r.material.color = colors.vest;
            else if (n.Contains("helmet") || n.Contains("hat") || n.Contains("hardhat"))
                r.material.color = colors.helmet;
        }
    }

    bool TryCreateCharacterVisual(Transform parent, int charIndex)
    {
        string resourceName = PlayerCharacterModels.Get(charIndex);
        if (string.IsNullOrEmpty(resourceName)) return false;

        GameObject prefab = Resources.Load<GameObject>(resourceName);
        if (prefab == null) return false;

        GameObject visual = Instantiate(prefab, parent);
        visual.name = "AS_CharacterVisual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        RemoveVisualColliders(visual);
        ConfigureVisualRenderers(visual);
        TintCharacterVisual(visual, charIndex);

        tpCharacterVisual = visual;
        tpVisualDefaultPos = visual.transform.localPosition;
        tpVisualDefaultRot = visual.transform.localEulerAngles;

        return true;
    }

    // One shared mesh, six colour-ways: multiply the base texture by the slot tint.
static void TintCharacterVisual(GameObject visual, int charIndex)
    {
        Color tint = PlayerCharacterModels.TintFor(charIndex);
        foreach (Renderer r in visual.GetComponentsInChildren<Renderer>())
        {
            Material mat = r.material; // instance copy, safe to recolor
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            else mat.color = tint;
        }
    }

    bool TryCreateGeneratedWorkerVisual(Transform parent)
    {
        GameObject prefab = Resources.Load<GameObject>("GeneratedArt/ASV4_WorkerCheapOutsourcedUniform");
        if (prefab == null) return false;

        GameObject visual = Instantiate(prefab, parent);
        visual.name = "ASV4_WorkerCheapOutsourcedUniform_Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        RemoveVisualColliders(visual);
        ConfigureVisualRenderers(visual);
        return true;
    }

    static void RemoveVisualColliders(GameObject visual)
    {
        foreach (var collider in visual.GetComponentsInChildren<Collider>())
            Object.Destroy(collider);
    }

    static void ConfigureVisualRenderers(GameObject visual)
    {
        foreach (var renderer in visual.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            ApplyWornMaterial(renderer);
        }
    }

    // Art-bible: every surface reads as worn public-facility kit — high roughness, no
    // metallic, no chrome. Pushes the worker model toward the semi-realistic weathered
    // reference (design/ux/mockups/character) instead of a clean product render.
    static void ApplyWornMaterial(Renderer renderer)
    {
        foreach (Material m in renderer.materials)
        {
            if (m == null) continue;
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.12f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_SpecularHighlights")) m.SetFloat("_SpecularHighlights", 0f);
        }
    }

    void HideCapsuleBodyMesh()
    {
        bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer == null) return;

        bodyRendererWasEnabled = bodyRenderer.enabled;
        bodyRenderer.enabled = false;
    }

    void RestoreLocalBodyMesh()
    {
        if (bodyRenderer == null) return;
        bodyRenderer.enabled = bodyRendererWasEnabled;
        bodyRenderer = null;
    }

    GameObject CreateFlashlight(Transform parent)
    {
        var root = new GameObject("Held_Flashlight");
        root.transform.SetParent(parent, false);

        GameObject prefab = Resources.Load<GameObject>("GeneratedArt/ASV4_Item_Flashlight");
        if (prefab != null)
        {
            var model = Instantiate(prefab, root.transform);
            model.transform.localPosition = new Vector3(0f, 0f, 0.12f);
            model.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            model.transform.localScale = Vector3.one;
            foreach (Collider c in model.GetComponentsInChildren<Collider>()) Destroy(c);
            return root;
        }

        // Fallback primitives
        CreatePrimitive(PrimitiveType.Cylinder, "Body", root.transform, Vector3.zero,
            new Vector3(0.07f, 0.22f, 0.07f), Quaternion.Euler(90f, 0f, 0f), flashlightMaterial);
        CreatePrimitive(PrimitiveType.Cylinder, "Lens", root.transform, new Vector3(0f, 0f, -0.14f),
            new Vector3(0.09f, 0.035f, 0.09f), Quaternion.Euler(90f, 0f, 0f), lightMaterial);
        return root;
    }

    GameObject CreateWristwatch(Transform parent)
    {
        var root = new GameObject("Worn_Wristwatch");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(-0.24f, -0.15f, 0.28f);
        root.transform.localRotation = Quaternion.Euler(18f, -12f, 4f);

        CreatePrimitive(PrimitiveType.Cube, "CheapBand", root.transform, Vector3.zero,
            new Vector3(0.15f, 0.035f, 0.075f), Quaternion.identity, watchMaterial);
        CreatePrimitive(PrimitiveType.Cube, "ClockFace", root.transform, new Vector3(0f, 0.026f, 0f),
            new Vector3(0.11f, 0.018f, 0.058f), Quaternion.identity, watchScreenMaterial);
        CreatePrimitive(PrimitiveType.Cube, "ClockScratch", root.transform, new Vector3(0.02f, 0.038f, -0.004f),
            new Vector3(0.055f, 0.006f, 0.008f), Quaternion.identity, lightMaterial);
        return root;
    }

    GameObject CreateThirdPersonWristwatch(Transform parent)
    {
        var root = new GameObject("ThirdPerson_Wristwatch");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(-0.36f, 0.86f, -0.09f);
        root.transform.localRotation = Quaternion.identity;

        CreatePrimitive(PrimitiveType.Cube, "VisibleBand", root.transform, Vector3.zero,
            new Vector3(0.17f, 0.055f, 0.035f), Quaternion.identity, watchMaterial);
        CreatePrimitive(PrimitiveType.Cube, "VisibleClockFace", root.transform, new Vector3(0f, 0f, -0.022f),
            new Vector3(0.1f, 0.042f, 0.02f), Quaternion.identity, watchScreenMaterial);
        return root;
    }

    GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent,
        Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;
        if (go.TryGetComponent<Collider>(out var collider))
            Destroy(collider);
        if (go.TryGetComponent<Renderer>(out var renderer))
            renderer.sharedMaterial = material;
        return go;
    }

    void SetActiveItem(MvpHotbarItemId itemId)
    {
        if (flashlightModel != null) flashlightModel.SetActive(itemId == MvpHotbarItemId.Flashlight);
    }

    void ApplyThirdPersonAnimation(float moveSpeed)
    {
        if (tpCharacterVisual == null) return;

        float t = Time.time;

        if (moveSpeed < 0.05f)
        {
            // Idle breathing — gentle rock and vertical pulse
            float breathe = Mathf.Sin(t * 1.4f);
            tpCharacterVisual.transform.localPosition = tpVisualDefaultPos + new Vector3(0, breathe * 0.012f, 0);
            tpCharacterVisual.transform.localRotation = Quaternion.Euler(tpVisualDefaultRot + new Vector3(breathe * 0.8f, 0f, 0f));
        }
        else
        {
            // Walk / sprint procedural movement
            float freq = moveSpeed > 0.7f ? 12f : 8f;
            float bobY = Mathf.Sin(t * freq) * (moveSpeed > 0.7f ? 0.05f : 0.025f);
            float tiltX = (moveSpeed > 0.7f ? 12f : 6f) + Mathf.Sin(t * freq) * 2f;
            float rollZ = Mathf.Sin(t * freq * 0.5f) * (moveSpeed > 0.7f ? 4f : 2f);

            tpCharacterVisual.transform.localPosition = tpVisualDefaultPos + new Vector3(0, Mathf.Abs(bobY), 0);
            tpCharacterVisual.transform.localRotation = Quaternion.Euler(tiltX, 0, rollZ);
        }
    }

    void EnsureMaterials()
    {
        PlayerController ctrl = GetComponent<PlayerController>();
        int charIndex = ctrl != null ? ctrl.CharacterIndex.Value : 0;
        var colors = PlayerCharacterPalette.Get(charIndex);

        skinMaterial = MakeMaterial(PlayerCharacterPalette.Skin);
        sleeveMaterial = MakeMaterial(colors.uniform);
        darkMaterial = MakeMaterial(new Color(0.03f, 0.035f, 0.04f));
        redMaterial = MakeMaterial(new Color(0.85f, 0.08f, 0.06f));
        flashlightMaterial = MakeMaterial(new Color(0.18f, 0.19f, 0.18f));
        lightMaterial = MakeMaterial(new Color(1f, 0.9f, 0.35f));
        watchMaterial = MakeMaterial(new Color(0.025f, 0.03f, 0.03f));
        watchScreenMaterial = MakeMaterial(new Color(0.08f, 0.75f, 0.32f));
    }

    static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
            ?? Shader.Find("Standard");
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        return material;
    }
}
