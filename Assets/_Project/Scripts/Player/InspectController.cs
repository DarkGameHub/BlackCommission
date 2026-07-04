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
    ScavengeItemDefinition currentDef;
    GUIStyle detailStyle, personStyle, hintStyle;
    Texture2D detailBg;

    // 低头握持: camera pitches down while inspecting (spec Transitions, -8~-12°).
    // TODO ReducedMotion: zero the pitch once the accessibility toggle exists (settings spec).
    const float InspectPitchDown = 10f;
    const float PitchBlendSeconds = 0.3f;
    float pitchBlend;
    Quaternion camBaseRotation;
    bool inspectToggleLatched;

    static ScavengeItemDefinition[] defCatalog;

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

        // A11y Toggle mode (spec Accessibility, MUST): F flips a latch instead of requiring
        // a continuous hold; the latch reads as "hold" to the session, so armed-latch,
        // interrupts and downed exits behave identically.
        bool hold;
        if (AccessibilityPrefs.InspectToggleMode)
        {
            if (!blocked && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                inspectToggleLatched = !inspectToggleLatched;
            hold = inspectToggleLatched;
        }
        else
        {
            inspectToggleLatched = false;
            hold = !blocked && Keyboard.current != null && Keyboard.current.fKey.isPressed;
        }
        bool interrupt = session.IsActive && ReadInterrupt();
        ScavengeItem target = ResolveInspectable();

        InspectCommand cmd = session.Tick(new InspectInput(hold, interrupt, downed, target != null));

        switch (cmd)
        {
            case InspectCommand.Enter: BeginInspect(target); break;
            case InspectCommand.Exit:  EndInspect();          break;
            default:                   if (session.IsActive) { RotateRelic(); BlendHeadPitch(); } break;
        }
    }

    // Ease the camera into the heads-down hold (~0.3s, spec Transitions); mouse-look is
    // suspended while inspecting, so this controller owns the camera rotation here.
    // Reduced Motion zeroes the pitch (spec Accessibility) — hand animation only.
    void BlendHeadPitch()
    {
        if (cameraTransform == null) return;
        float pitch = AccessibilityPrefs.ReducedMotion ? 0f : InspectPitchDown;
        pitchBlend = Mathf.MoveTowards(pitchBlend, 1f, Time.deltaTime / PitchBlendSeconds);
        cameraTransform.localRotation = camBaseRotation
            * Quaternion.Euler(pitch * Mathf.SmoothStep(0f, 1f, pitchBlend), 0f, 0f);
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
        currentDef = ResolveDefinition(item);
        LocalInspecting = true;
        IsInspecting.Value = true;
        BuildRelicView(item);

        pitchBlend = 0f;
        if (cameraTransform != null) camBaseRotation = cameraTransform.localRotation;
    }

    void EndInspect()
    {
        inspectToggleLatched = false;
        currentItem = null;
        currentDef = null;
        LocalInspecting = false;
        IsInspecting.Value = false;
        if (relicView != null) { Destroy(relicView); relicView = null; }
        // 急放 (~0.1s feel): snap the pitch back; mouse-look resumes next frame.
        if (cameraTransform != null) cameraTransform.localRotation = camBaseRotation;
    }

    // Definition lookup by item id — carries displayName/tier/targetPersonId/inspectDetail
    // for the detail label. Host/solo resolves fine; clients need ItemId replication first
    // (follow-up: itemId is host-stamped, not yet synced).
    static ScavengeItemDefinition ResolveDefinition(ScavengeItem item)
    {
        if (item == null || string.IsNullOrEmpty(item.ItemId)) return null;
        defCatalog ??= Resources.LoadAll<ScavengeItemDefinition>("Scavenge/Items");
        foreach (var def in defCatalog)
            if (def != null && def.id == item.ItemId) return def;
        return null;
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

    // Zone-Detail (spec Layout): screen-space anchored right of the relic — rotation never
    // spins the text. Paper ink on a dark semi-transparent "torn archive label" (撕边档案标签,
    // alpha ~0.55) so the line stays WCAG-readable over a dark scene. inspectDetail is stored
    // as raw zh text for now; the localization pass swaps it to a string-table key (spec Loc).
    void OnGUI()
    {
        if (!IsOwner || !session.IsActive) return;
        EnsureDetailStyles();
        if (detailStyle == null) return;

        string title = currentDef != null && !string.IsNullOrWhiteSpace(currentDef.displayName)
            ? currentDef.displayName
            : null;
        bool relic = currentDef != null && currentDef.tier == ScavengeTier.Relic;
        string body = relic && !string.IsNullOrWhiteSpace(currentDef.inspectDetail)
            ? currentDef.inspectDetail
            : "看不出值多少。";   // 层一 salvage: value stays unreadable in hand (two-tier §1)

        float w = 300f;
        float x = Screen.width * 0.58f;
        float y = Screen.height * 0.40f;
        float titleH = title != null ? 26f : 0f;
        float bodyH = detailStyle.CalcHeight(new GUIContent(body), w - 28f);
        var label = new Rect(x, y, w, titleH + bodyH + 22f);

        DrawTornLabel(label);
        if (title != null)
            GUI.Label(new Rect(label.x + 16f, label.y + 8f, w - 30f, 22f), title, personStyle);
        GUI.Label(new Rect(label.x + 16f, label.y + 10f + titleH, w - 28f, bodyH + 4f), body, detailStyle);

        // Zone-Hint: operation micro-hint, bottom center (fades from attention, not from screen).
        GUI.Label(new Rect(0f, Screen.height - 46f, Screen.width, 22f),
            "拖动旋转看不同面  ·  松开 F 放下", hintStyle);
    }

    // Horizontal strips with a jittered left edge — a cheap torn-paper silhouette that reads
    // as an archive label rather than a UI card.
    void DrawTornLabel(Rect r)
    {
        float[] tear = { 0f, 5f, 2f, 7f, 1f, 4f };
        float stripH = r.height / tear.Length;
        for (int i = 0; i < tear.Length; i++)
            GUI.DrawTexture(new Rect(r.x - tear[i], r.y + i * stripH, r.width + tear[i], stripH + 1f), detailBg);
    }

    void EnsureDetailStyles()
    {
        if (detailStyle != null) return;
        if (GUI.skin == null || GUI.skin.label == null) return;

        detailBg = new Texture2D(1, 1);
        detailBg.SetPixel(0, 0, new Color(0.045f, 0.05f, 0.055f, 0.55f));
        detailBg.Apply();

        detailStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true,
            normal = { textColor = BlackCommissionUiTheme.OldPaper }
        };
        personStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = new Color(0.78f, 0.72f, 0.58f, 0.95f) }
        };
        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.84f, 0.80f, 0.68f, 0.75f) }
        };
        MvpFontProvider.ApplyToStyle(detailStyle);
        MvpFontProvider.ApplyToStyle(personStyle);
        MvpFontProvider.ApplyToStyle(hintStyle);
    }
}
