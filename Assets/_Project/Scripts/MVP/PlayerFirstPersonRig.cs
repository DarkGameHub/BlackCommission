using Unity.Netcode;
using UnityEngine;

public class PlayerFirstPersonRig : NetworkBehaviour
{
    [SerializeField] Vector3 rigLocalPosition = new(0f, -0.34f, 0.46f);

    PlayerHotbar hotbar;
    PlayerController controller;
    Transform cameraTransform;
    Transform heldItemRoot;
    GameObject rigRoot;
    GameObject thirdPersonRoot;
    GameObject flashlightModel;
    GameObject watchModel;
    GameObject thirdPersonWatchModel;

    Transform fpLeftHand;
    Transform fpRightHand;
    Transform tpLeftArm;
    Transform tpRightArm;
    Vector3 fpLeftHandDefaultPos;
    Vector3 fpLeftHandDefaultRot;
    Vector3 fpRightHandDefaultPos;
    Vector3 fpRightHandDefaultRot;
    Vector3 tpLeftArmDefaultRot;
    Vector3 tpRightArmDefaultRot;
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

        if (!IsOwner)
            BuildThirdPersonVisual();
        else
            BuildRig();
    }

    public override void OnNetworkDespawn()
    {
        RestoreLocalBodyMesh();
        if (rigRoot != null)
            Destroy(rigRoot);
        if (thirdPersonRoot != null)
            Destroy(thirdPersonRoot);
    }

    public override void OnDestroy()
    {
        RestoreLocalBodyMesh();
        base.OnDestroy();
    }

    void LateUpdate()
    {
        int gestureId = controller != null ? controller.GestureId.Value : 0;

        if (!IsOwner)
        {
            if (hotbar != null && thirdPersonWatchModel != null)
                thirdPersonWatchModel.SetActive(hotbar.HasWristwatch.Value);

            float moveSpeed = controller != null ? controller.NetworkMoveSpeed.Value : 0f;
            if (gestureId != 0)
                ApplyThirdPersonGesture(gestureId);
            else
                ApplyThirdPersonAnimation(moveSpeed);
            return;
        }

        if (hotbar == null || heldItemRoot == null) return;

        HotbarSlot selected = hotbar.GetSlot(hotbar.SelectedSlot.Value);
        MvpHotbarItemId itemId = selected == null || selected.IsEmpty
            ? MvpHotbarItemId.None
            : selected.itemId;

        if (gestureId != 0)
        {
            SetActiveItem(MvpHotbarItemId.None);
            ApplyFirstPersonGesture(gestureId);
        }
        else
        {
            RestoreFirstPersonHands();
            SetActiveItem(itemId);
        }

        if (watchModel != null)
            watchModel.SetActive(hotbar.HasWristwatchOwned);

        if (gestureId == 0)
            ApplyFirstPersonHandBob();
    }

    void ApplyFirstPersonHandBob()
    {
        if (rigRoot == null || controller == null) return;
        float speed = controller.NetworkMoveSpeed.Value;
        if (speed < 0.05f) { rigRoot.transform.localPosition = rigLocalPosition; return; }

        float t = Time.time;
        float freq = speed > 0.7f ? 10f : 6.5f;
        float ampY = speed > 0.7f ? 0.022f : 0.012f;
        float ampX = speed > 0.7f ? 0.012f : 0.006f;

        float bobY = Mathf.Sin(t * freq) * ampY;
        float bobX = Mathf.Sin(t * freq * 0.5f) * ampX;
        rigRoot.transform.localPosition = rigLocalPosition + new Vector3(bobX, bobY, 0f);
    }

    void BuildRig()
    {
        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam == null) return;

        cameraTransform = cam.transform;
        cam.nearClipPlane = Mathf.Min(cam.nearClipPlane, 0.03f);

        if (rigRoot != null) Destroy(rigRoot);
        rigRoot = new GameObject("MVP_FirstPersonHands");
        rigRoot.transform.SetParent(cameraTransform, false);
        rigRoot.transform.localPosition = rigLocalPosition;
        rigRoot.transform.localRotation = Quaternion.identity;

        EnsureMaterials();
        if (!TryCreateGeneratedGloves(rigRoot.transform))
            CreateHands(rigRoot.transform);
        ApplyFirstPersonColors(rigRoot);
        CacheFirstPersonHandTransforms(rigRoot.transform);
        watchModel = CreateWristwatch(rigRoot.transform);
        watchModel.SetActive(false);

        heldItemRoot = new GameObject("HeldItemRoot").transform;
        heldItemRoot.SetParent(rigRoot.transform, false);
        heldItemRoot.localPosition = new Vector3(0.16f, -0.12f, 0.24f);
        heldItemRoot.localRotation = Quaternion.Euler(8f, -10f, -6f);

        flashlightModel = CreateFlashlight(heldItemRoot);
        SetActiveItem(MvpHotbarItemId.None);
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

    void CreateHands(Transform parent)
    {
        CreatePrimitive(PrimitiveType.Cube, "LeftSleeve", parent,
            new Vector3(-0.22f, -0.06f, 0.18f), new Vector3(0.13f, 0.13f, 0.42f),
            Quaternion.Euler(18f, -12f, 4f), sleeveMaterial);
        CreatePrimitive(PrimitiveType.Cube, "RightSleeve", parent,
            new Vector3(0.22f, -0.08f, 0.18f), new Vector3(0.13f, 0.13f, 0.44f),
            Quaternion.Euler(18f, 12f, -4f), sleeveMaterial);
        CreatePrimitive(PrimitiveType.Sphere, "LeftHand", parent,
            new Vector3(-0.23f, -0.1f, 0.42f), new Vector3(0.13f, 0.09f, 0.1f),
            Quaternion.identity, skinMaterial);
        CreatePrimitive(PrimitiveType.Sphere, "RightHand", parent,
            new Vector3(0.22f, -0.12f, 0.43f), new Vector3(0.13f, 0.09f, 0.1f),
            Quaternion.identity, skinMaterial);
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
        root.transform.localPosition = new Vector3(-0.235f, -0.085f, 0.28f);
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

    bool TryCreateGeneratedGloves(Transform parent)
    {
        GameObject prefab = Resources.Load<GameObject>("GeneratedArt/ASV4_FirstPerson_Gloves");
        if (prefab == null) return false;

        GameObject gloves = Instantiate(prefab, parent);
        gloves.name = "ASV4_Gloves";
        gloves.transform.localPosition = new Vector3(0f, -0.20f, 0.08f);
        gloves.transform.localRotation = Quaternion.Euler(58f, 0f, 0f);
        gloves.transform.localScale = Vector3.one * 0.78f;

        foreach (Collider c in gloves.GetComponentsInChildren<Collider>())
            Destroy(c);

        return true;
    }

    void ApplyFirstPersonColors(GameObject root)
    {
        int charIndex = controller != null ? controller.CharacterIndex.Value : 0;
        var colors = PlayerCharacterPalette.Get(charIndex);

        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            string n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("forearm") || n.Contains("cuff") || n.Contains("sleeve") || n.Contains("uniform"))
                r.material.color = colors.uniform;
        }
    }

    void CacheFirstPersonHandTransforms(Transform parent)
    {
        fpLeftHand = parent.Find("LeftHand");
        fpRightHand = parent.Find("RightHand");
        if (fpLeftHand != null)
        {
            fpLeftHandDefaultPos = fpLeftHand.localPosition;
            fpLeftHandDefaultRot = fpLeftHand.localEulerAngles;
        }
        if (fpRightHand != null)
        {
            fpRightHandDefaultPos = fpRightHand.localPosition;
            fpRightHandDefaultRot = fpRightHand.localEulerAngles;
        }
    }

    void CacheThirdPersonArmTransforms()
    {
        if (thirdPersonRoot == null) return;
        tpLeftArm = thirdPersonRoot.transform.Find("LeftArm");
        tpRightArm = thirdPersonRoot.transform.Find("RightArm");
        if (tpLeftArm != null) tpLeftArmDefaultRot = tpLeftArm.localEulerAngles;
        if (tpRightArm != null) tpRightArmDefaultRot = tpRightArm.localEulerAngles;
    }

    void ApplyFirstPersonGesture(int gestureId)
    {
        var pose = PlayerGestures.Get(gestureId);
        if (fpRightHand != null)
        {
            fpRightHand.localPosition = pose.fpRightPos;
            fpRightHand.localEulerAngles = pose.fpRightRot;
        }
        if (fpLeftHand != null)
        {
            fpLeftHand.localPosition = pose.fpLeftPos;
            fpLeftHand.localEulerAngles = pose.fpLeftRot;
        }
        lastAppliedGesture = gestureId;
    }

    void RestoreFirstPersonHands()
    {
        if (lastAppliedGesture == 0) return;
        if (fpRightHand != null)
        {
            fpRightHand.localPosition = fpRightHandDefaultPos;
            fpRightHand.localEulerAngles = fpRightHandDefaultRot;
        }
        if (fpLeftHand != null)
        {
            fpLeftHand.localPosition = fpLeftHandDefaultPos;
            fpLeftHand.localEulerAngles = fpLeftHandDefaultRot;
        }
        lastAppliedGesture = 0;
    }

    void ApplyThirdPersonAnimation(float moveSpeed)
    {
        if (tpLeftArm == null || tpRightArm == null)
            CacheThirdPersonArmTransforms();
        if (tpLeftArm == null || tpRightArm == null) return;

        float t = Time.time;

        if (moveSpeed < 0.05f)
        {
            // Idle breathing — gentle arm rock
            float breathe = Mathf.Sin(t * 1.4f) * 2.5f;
            tpRightArm.localEulerAngles = tpRightArmDefaultRot + new Vector3(breathe, 0f, 0f);
            tpLeftArm.localEulerAngles = tpLeftArmDefaultRot + new Vector3(-breathe, 0f, 0f);
        }
        else
        {
            // Walk / sprint arm swing
            float freq = moveSpeed > 0.7f ? 7.5f : 5f;
            float amp = moveSpeed > 0.7f ? 42f : 26f;
            float forwardLean = moveSpeed > 0.7f ? 12f : 6f;
            float swing = Mathf.Sin(t * freq) * amp;

            tpRightArm.localEulerAngles = tpRightArmDefaultRot + new Vector3(-swing - forwardLean, 0f, 0f);
            tpLeftArm.localEulerAngles = tpLeftArmDefaultRot + new Vector3(swing - forwardLean, 0f, 0f);

            // Body lean — tilt torso slightly forward
            Transform torso = thirdPersonRoot != null ? thirdPersonRoot.transform.Find("UniformTorso") : null;
            if (torso != null)
                torso.localEulerAngles = new Vector3(-forwardLean * 0.5f, 0f, 0f);

            // Head bob on torso
            Transform head = thirdPersonRoot != null ? thirdPersonRoot.transform.Find("Head") : null;
            if (head != null)
            {
                float bobY = Mathf.Abs(Mathf.Sin(t * freq * 0.5f)) * (moveSpeed > 0.7f ? 0.04f : 0.02f);
                head.localPosition = new Vector3(0f, 1.58f + bobY, 0f);
            }
        }

        // Restore torso and head when idle
        if (moveSpeed < 0.05f)
        {
            Transform torso = thirdPersonRoot != null ? thirdPersonRoot.transform.Find("UniformTorso") : null;
            if (torso != null) torso.localEulerAngles = Vector3.zero;
            Transform head = thirdPersonRoot != null ? thirdPersonRoot.transform.Find("Head") : null;
            if (head != null) head.localPosition = new Vector3(0f, 1.58f, 0f);
        }
    }

    void ApplyThirdPersonGesture(int gestureId)
    {
        if (tpLeftArm == null || tpRightArm == null)
            CacheThirdPersonArmTransforms();
        if (tpLeftArm == null || tpRightArm == null) return;

        if (gestureId == 0)
        {
            tpLeftArm.localEulerAngles = tpLeftArmDefaultRot;
            tpRightArm.localEulerAngles = tpRightArmDefaultRot;
            return;
        }

        var pose = PlayerGestures.Get(gestureId);
        tpRightArm.localEulerAngles = pose.tpRightArmRot;
        tpLeftArm.localEulerAngles = pose.tpLeftArmRot;
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
