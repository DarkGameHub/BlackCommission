using Unity.Netcode;
using UnityEngine;

public class PlayerFirstPersonRig : NetworkBehaviour
{
    [SerializeField] Vector3 rigLocalPosition = new(0f, -0.2f, 0.48f);

    PlayerHotbar hotbar;
    Transform cameraTransform;
    Transform heldItemRoot;
    GameObject rigRoot;
    GameObject thirdPersonRoot;
    GameObject medkitModel;
    GameObject sprayModel;
    GameObject decoyModel;
    GameObject flashlightModel;
    GameObject watchModel;
    GameObject thirdPersonWatchModel;

    Material skinMaterial;
    Material sleeveMaterial;
    Material darkMaterial;
    Material medkitMaterial;
    Material redMaterial;
    Material sprayMaterial;
    Material decoyMaterial;
    Material flashlightMaterial;
    Material lightMaterial;
    Material watchMaterial;
    Material watchScreenMaterial;
    Renderer bodyRenderer;
    bool bodyRendererWasEnabled;

    public override void OnNetworkSpawn()
    {
        hotbar = GetComponent<PlayerHotbar>();
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

    void OnDestroy()
    {
        RestoreLocalBodyMesh();
    }

    void LateUpdate()
    {
        if (!IsOwner && hotbar != null && thirdPersonWatchModel != null)
            thirdPersonWatchModel.SetActive(hotbar.HasWristwatch.Value);

        if (!IsOwner || hotbar == null || heldItemRoot == null) return;

        HotbarSlot selected = hotbar.GetSlot(hotbar.SelectedSlot.Value);
        MvpHotbarItemId itemId = selected == null || selected.IsEmpty
            ? MvpHotbarItemId.None
            : selected.itemId;

        SetActiveItem(itemId);
        if (watchModel != null)
            watchModel.SetActive(hotbar.HasWristwatchOwned);
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
        CreateHands(rigRoot.transform);
        watchModel = CreateWristwatch(rigRoot.transform);
        watchModel.SetActive(false);

        heldItemRoot = new GameObject("HeldItemRoot").transform;
        heldItemRoot.SetParent(rigRoot.transform, false);
        heldItemRoot.localPosition = new Vector3(0.16f, -0.12f, 0.24f);
        heldItemRoot.localRotation = Quaternion.Euler(8f, -10f, -6f);

        medkitModel = CreateMedkit(heldItemRoot);
        sprayModel = CreateSpray(heldItemRoot);
        decoyModel = CreateDecoy(heldItemRoot);
        flashlightModel = CreateFlashlight(heldItemRoot);
        SetActiveItem(MvpHotbarItemId.None);
    }

    void BuildThirdPersonVisual()
    {
        if (thirdPersonRoot != null) return;

        EnsureMaterials();
        thirdPersonRoot = new GameObject("MVP_PlayerCharacterModel");
        thirdPersonRoot.transform.SetParent(transform, false);
        thirdPersonRoot.transform.localPosition = Vector3.zero;
        thirdPersonRoot.transform.localRotation = Quaternion.identity;

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
        CreatePrimitive(PrimitiveType.Cube, "DebtBadge", thirdPersonRoot.transform,
            new Vector3(0.15f, 1.24f, -0.16f), new Vector3(0.14f, 0.08f, 0.02f),
            Quaternion.identity, redMaterial);
        CreatePrimitive(PrimitiveType.Cube, "CheapBackpack", thirdPersonRoot.transform,
            new Vector3(0f, 1.05f, 0.2f), new Vector3(0.38f, 0.48f, 0.12f),
            Quaternion.identity, darkMaterial);
        thirdPersonWatchModel = CreateThirdPersonWristwatch(thirdPersonRoot.transform);
        thirdPersonWatchModel.SetActive(hotbar != null && hotbar.HasWristwatch.Value);
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

    GameObject CreateMedkit(Transform parent)
    {
        var root = new GameObject("Held_Medkit");
        root.transform.SetParent(parent, false);
        CreatePrimitive(PrimitiveType.Cube, "Case", root.transform, Vector3.zero,
            new Vector3(0.23f, 0.15f, 0.12f), Quaternion.identity, medkitMaterial);
        CreatePrimitive(PrimitiveType.Cube, "CrossVertical", root.transform, new Vector3(0f, 0f, -0.065f),
            new Vector3(0.035f, 0.11f, 0.01f), Quaternion.identity, redMaterial);
        CreatePrimitive(PrimitiveType.Cube, "CrossHorizontal", root.transform, new Vector3(0f, 0f, -0.07f),
            new Vector3(0.12f, 0.035f, 0.01f), Quaternion.identity, redMaterial);
        return root;
    }

    GameObject CreateSpray(Transform parent)
    {
        var root = new GameObject("Held_StunSpray");
        root.transform.SetParent(parent, false);
        CreatePrimitive(PrimitiveType.Cylinder, "Can", root.transform, Vector3.zero,
            new Vector3(0.08f, 0.2f, 0.08f), Quaternion.Euler(90f, 0f, 0f), sprayMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Nozzle", root.transform, new Vector3(0f, 0.03f, -0.14f),
            new Vector3(0.06f, 0.035f, 0.07f), Quaternion.identity, darkMaterial);
        return root;
    }

    GameObject CreateDecoy(Transform parent)
    {
        var root = new GameObject("Held_Decoy");
        root.transform.SetParent(parent, false);
        CreatePrimitive(PrimitiveType.Sphere, "Bell", root.transform, Vector3.zero,
            new Vector3(0.16f, 0.16f, 0.16f), Quaternion.identity, decoyMaterial);
        CreatePrimitive(PrimitiveType.Cube, "Tag", root.transform, new Vector3(0f, -0.1f, 0f),
            new Vector3(0.06f, 0.05f, 0.02f), Quaternion.identity, redMaterial);
        return root;
    }

    GameObject CreateFlashlight(Transform parent)
    {
        var root = new GameObject("Held_Flashlight");
        root.transform.SetParent(parent, false);
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
        if (medkitModel != null) medkitModel.SetActive(itemId == MvpHotbarItemId.Medkit);
        if (sprayModel != null) sprayModel.SetActive(itemId == MvpHotbarItemId.StunSpray);
        if (decoyModel != null) decoyModel.SetActive(itemId == MvpHotbarItemId.Decoy);
        if (flashlightModel != null) flashlightModel.SetActive(itemId == MvpHotbarItemId.Flashlight);
    }

    void EnsureMaterials()
    {
        skinMaterial = MakeMaterial(new Color(0.76f, 0.56f, 0.42f));
        sleeveMaterial = MakeMaterial(new Color(0.08f, 0.12f, 0.13f));
        darkMaterial = MakeMaterial(new Color(0.03f, 0.035f, 0.04f));
        medkitMaterial = MakeMaterial(new Color(0.92f, 0.92f, 0.85f));
        redMaterial = MakeMaterial(new Color(0.85f, 0.08f, 0.06f));
        sprayMaterial = MakeMaterial(new Color(0.15f, 0.7f, 0.82f));
        decoyMaterial = MakeMaterial(new Color(0.95f, 0.62f, 0.16f));
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
