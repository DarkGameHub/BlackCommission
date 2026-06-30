using BlackCommission.Scavenge;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// First-person "hold up &amp; examine" inspection of a scavenge relic
/// (design/ux/item-inspection.md). Owner-only view layer: hold F while aiming at an
/// inspectable item to raise it to eye level, suspend mouse-look (the mouse rotates the
/// relic instead — "can't look around" is the vulnerability, decision ②), and read its
/// visual traces.
///
/// CONTRACT (spec AC4): inspection NEVER reads or writes item value — it only changes the
/// player's cognition/connection. The world is NOT paused; any move/sprint/attack/flashlight/
/// hotbar input, or going down, instantly cancels (the vulnerable escape valve, decision ②).
/// <see cref="IsInspecting"/> is server-auth so teammates see the heads-down hold pose
/// (B3, "cover me while I read this"); rotation/look stays purely local.
///
/// MVP placeholder: clones the aimed item's mesh at the eye anchor so hold/rotate is
/// Play-testable before real relic models + the inspectDetail UI exist. Held-relic entry,
/// tier/isInspectable gating, the inspectDetail text panel + label backing, low-head camera
/// pitch (-8~-12°), and the Toggle a11y mode are follow-ups (see spec).
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class InspectController : NetworkBehaviour
{
    [SerializeField] float rotateSpeed = 0.25f;
    [SerializeField] Vector3 inspectAnchorLocal = new(0f, -0.12f, 0.42f);
    [SerializeField] float placeholderHoldSize = 0.22f;

    /// <summary>Server-auth; drives the third-person heads-down pose for teammates (B3).</summary>
    public readonly NetworkVariable<bool> IsInspecting =
        new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    /// <summary>Local-owner gate read by <see cref="PlayerCameraController"/> to suspend
    /// mouse-look while inspecting (the mouse rotates the relic instead).</summary>
    public static bool LocalInspecting { get; private set; }

    readonly InspectSession session = new();

    PlayerInteraction interaction;
    PlayerHealth health;
    Transform cameraTransform;
    Transform inspectAnchor;
    GameObject relicView;
    ScavengeItem currentItem;
    GUIStyle detailStyle;
    Texture2D detailBg;

    void Awake()
    {
        interaction = GetComponent<PlayerInteraction>();
        health = GetComponent<PlayerHealth>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) { enabled = false; return; }

        Camera cam = GetComponentInChildren<Camera>(true);
        if (cam != null)
        {
            cameraTransform = cam.transform;
            inspectAnchor = new GameObject("InspectAnchor").transform;
            inspectAnchor.SetParent(cameraTransform, false);
            inspectAnchor.localPosition = inspectAnchorLocal;
            inspectAnchor.localRotation = Quaternion.identity;
        }
    }

    public override void OnNetworkDespawn() => Cleanup();

    public override void OnDestroy()
    {
        Cleanup();
        base.OnDestroy();
    }

    void Cleanup()
    {
        if (IsOwner) LocalInspecting = false;
        if (relicView != null) { Destroy(relicView); relicView = null; }
    }

    void Update()
    {
        if (!IsOwner) return;

        bool downed = health != null && health.IsDowned.Value;
        bool blocked = VanTransitOverlay.IsActive || MvpHud.IsBlockingPanelOpen
                       || MainMenuUI.IsGameplayInputBlockedByMenu;

        bool hold = !blocked && Keyboard.current != null && Keyboard.current.fKey.isPressed;
        bool interrupt = session.IsActive && ReadInterrupt();
        ScavengeItem target = ResolveInspectable();

        InspectCommand cmd = session.Tick(new InspectInput(hold, interrupt, downed, target != null));

        switch (cmd)
        {
            case InspectCommand.Enter: BeginInspect(target); break;
            case InspectCommand.Exit:  EndInspect();          break;
            default:                   if (session.IsActive) RotateRelic(); break;
        }
    }

    // MVP: any aimed ScavengeItem. tier/isInspectable gating + held-relic entry are follow-ups.
    ScavengeItem ResolveInspectable()
        => interaction != null ? interaction.CurrentTarget as ScavengeItem : null;

    // Any move / sprint / attack / flashlight / hotbar input = instant interrupt (decision ②).
    static bool ReadInterrupt()
    {
        // Note: F is the inspect key itself, so it is NOT an interrupt here.
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed
            || kb.dKey.isPressed || kb.spaceKey.isPressed || kb.leftShiftKey.isPressed
            || kb.digit1Key.isPressed || kb.digit2Key.isPressed
            || kb.digit3Key.isPressed || kb.digit4Key.isPressed || kb.digit5Key.isPressed))
            return true;
        Mouse m = Mouse.current;
        return m != null && m.leftButton.isPressed; // attack
    }

    void BeginInspect(ScavengeItem item)
    {
        currentItem = item;
        LocalInspecting = true;
        IsInspecting.Value = true;
        BuildRelicView(item);
    }

    void EndInspect()
    {
        currentItem = null;
        LocalInspecting = false;
        IsInspecting.Value = false;
        if (relicView != null) { Destroy(relicView); relicView = null; }
    }

    void RotateRelic()
    {
        if (relicView == null || cameraTransform == null || Mouse.current == null) return;
        Vector2 d = Mouse.current.delta.ReadValue();
        relicView.transform.Rotate(cameraTransform.up, -d.x * rotateSpeed, Space.World);
        relicView.transform.Rotate(cameraTransform.right, d.y * rotateSpeed, Space.World);
    }

    // View-only clone at the eye anchor. The world item is never picked up, moved, or read
    // for value — inspection is decoupled from the economy (contract).
    void BuildRelicView(ScavengeItem item)
    {
        if (inspectAnchor == null) return;
        if (relicView != null) Destroy(relicView);

        Mesh mesh = null;
        Material mat = null;
        if (item != null)
        {
            MeshFilter mf = item.GetComponentInChildren<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
            Renderer r = item.GetComponentInChildren<Renderer>();
            if (r != null) mat = r.sharedMaterial;
        }

        if (mesh != null)
        {
            relicView = new GameObject("RelicView");
            relicView.transform.SetParent(inspectAnchor, false);
            relicView.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = relicView.AddComponent<MeshRenderer>();
            if (mat != null) mr.sharedMaterial = mat;
            Vector3 size = mesh.bounds.size;
            float max = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (max > 0.0001f) relicView.transform.localScale = Vector3.one * (placeholderHoldSize / max);
        }
        else
        {
            relicView = GameObject.CreatePrimitive(PrimitiveType.Cube);
            relicView.name = "RelicView";
            if (relicView.TryGetComponent<Collider>(out Collider c)) Destroy(c);
            relicView.transform.SetParent(inspectAnchor, false);
            relicView.transform.localScale = Vector3.one * placeholderHoldSize;
        }

        relicView.transform.localPosition = Vector3.zero;
        relicView.transform.localRotation = Quaternion.identity;
        foreach (Renderer rr in relicView.GetComponentsInChildren<Renderer>())
            rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // Placeholder inspectDetail panel: screen-space (Zone-Detail), paper text on a dark
    // "torn archive label" backing (撕边档案标签) for dark-scene contrast (spec Layout + a11y).
    // The real localized one-liner + targetPersonId reveal replaces the body once
    // ScavengeItemDefinition carries inspectDetail/targetPersonId.
    void OnGUI()
    {
        if (!IsOwner || !session.IsActive) return;
        if (detailStyle == null)
        {
            detailBg = new Texture2D(1, 1);
            detailBg.SetPixel(0, 0, new Color(0.04f, 0.045f, 0.05f, 0.7f));
            detailBg.Apply();
            detailStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = BlackCommissionUiTheme.OldPaper, background = detailBg },
                padding = new RectOffset(12, 12, 10, 10)
            };
            MvpFontProvider.ApplyToStyle(detailStyle);
        }

        string body = currentItem != null
            ? $"检视中\n{currentItem.ItemId}\n（视觉痕迹 · 占位）"
            : "检视中";
        GUI.Label(new Rect(Screen.width * 0.60f, Screen.height * 0.40f, 260f, 96f), body, detailStyle);
    }
}
