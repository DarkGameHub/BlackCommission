using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Main-menu carrier per design/ux/main-menu.md (Approved 2026-06-11): the menu is the
/// game picture itself. A fixed camera sits inside the real HQ office aimed at the desk
/// CRT; the five actions render as terminal-green rows on a world-space canvas glued to
/// the CRT glass; the day's commission hangs beside the monitor as a paper note driven
/// by real task data. Entering the game pushes the camera from the desk to the local
/// player's head and hands off — no black screen, no cut.
///
/// MainMenuUI keeps owning connection logic and the modal sub-pages; this class owns
/// only the 3D presentation (menu camera, CRT rows, job note, push-in transition).
/// Composition can be art-directed by placing a "MenuCameraAnchor" object in the HQ
/// scene — when present its pose is used verbatim instead of the computed default.
/// </summary>
public class CrtMenuStage : MonoBehaviour
{
    const float PushInSeconds = 0.85f;
    const float BootFlashSeconds = 0.5f;

    // Ink on aged paper (job note); CRT colors come from the shared theme.
    static readonly Color Ink = new(0.098f, 0.075f, 0.051f, 1f);
    static readonly Color CrtBgTint = new(0.010f, 0.026f, 0.014f, 0.95f);

    MainMenuUI owner;
    Transform screenAnchor;          // MVP_OfficeComputer — its position IS the screen centre
    Camera menuCam;
    AudioListener menuListener;
    GameObject crtCanvasGo;
    CanvasGroup rowsGroup;
    TMP_Text statusLine;
    Image bootFlash;
    float bootFlashUntil = -1f;

    readonly List<TMP_Text> rowTexts = new();
    readonly List<Image> rowBacks = new();
    readonly List<bool> rowSelectable = new();
    readonly List<System.Action> rowActions = new();
    int selected;
    bool pushing;
    bool wasListening;
    float parkedRenderScale = -1f; // retro 0.5 scale parked while the menu camera is live

    /// <summary>True while the menu camera owns the picture (pre-game or during push-in).</summary>
    public bool CameraLive => menuCam != null;

    /// <summary>
    /// Creates the stage if the HQ office computer exists in the loaded scene.
    /// Returns null otherwise so MainMenuUI can fall back to its screen-space layout.
    /// </summary>
    public static CrtMenuStage TryCreate(MainMenuUI menu)
    {
        var anchorGo = GameObject.Find("MVP_OfficeComputer");
        if (anchorGo == null) return null;

        var go = new GameObject("CrtMenuStage");
        var stage = go.AddComponent<CrtMenuStage>();
        stage.owner = menu;
        stage.screenAnchor = anchorGo.transform;
        stage.Build();
        return stage;
    }

    void Build()
    {
        bool listening = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (!listening)
            BuildMenuCamera();
        BuildCrtCanvas();
        BuildJobNote();

        if (!listening)
        {
            // CRT power-on flash (spec: 进入菜单 = CRT 开机闪).
            bootFlashUntil = Time.unscaledTime + BootFlashSeconds;
            owner.UiPlaySelect();
        }

        SetListeningVisuals(listening);
        wasListening = listening;
    }

    // ─── Camera ───────────────────────────────────────────────────────────

    void BuildMenuCamera()
    {
        // A leftover enabled camera ("HQMenuCamera", dead script attached) lives in
        // the HQ scene from an earlier menu experiment. With the baked-PNG backdrop
        // gone it would render for nothing — park it while the stage owns the shot.
        var legacyCam = GameObject.Find("HQMenuCamera");
        if (legacyCam != null && legacyCam.TryGetComponent(out Camera legacy))
            legacy.enabled = false;

        var camGo = new GameObject("MenuCamera");
        camGo.transform.SetParent(transform, false);

        var anchorOverride = GameObject.Find("MenuCameraAnchor");
        if (anchorOverride != null)
        {
            camGo.transform.SetPositionAndRotation(
                anchorOverride.transform.position, anchorOverride.transform.rotation);
        }
        else
        {
            // Default composition per spec: CRT in the left third, slight downward
            // angle (seated at the desk), office depth filling the right of frame.
            // The screen faces -Z in the HQ scene, so the camera looks +Z.
            Vector3 screenCenter = screenAnchor.position;
            Vector3 camPos = screenCenter + new Vector3(0.26f, 0.18f, -0.92f);
            Vector3 lookAt = screenCenter + new Vector3(0.40f, -0.04f, 0f);
            camGo.transform.position = camPos;
            camGo.transform.rotation = Quaternion.LookRotation((lookAt - camPos).normalized, Vector3.up);
        }

        menuCam = camGo.AddComponent<Camera>();
        menuCam.fieldOfView = 42f;
        menuCam.nearClipPlane = 0.04f;
        menuCam.depth = 95f; // above any freshly spawned player camera until hand-off
        // Menu reads at full clarity: no grain/vignette/outline and no retro render
        // scale on the opening shot (PM 2026-06-12: 开场太糊没必要). The LC stack
        // returns with the player camera at hand-off.
        var urpData = menuCam.GetUniversalAdditionalCameraData();
        if (urpData != null) urpData.renderPostProcessing = false;
        ParkRetroRenderScale();

        menuListener = camGo.AddComponent<AudioListener>();

        if (crtCanvasGo != null)
        {
            var canvas = crtCanvasGo.GetComponent<Canvas>();
            if (canvas != null) canvas.worldCamera = menuCam;
        }
    }

    void ParkRetroRenderScale()
    {
        var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline
            as UniversalRenderPipelineAsset;
        if (urp == null || parkedRenderScale >= 0f) return;
        if (urp.renderScale >= 0.999f) return; // nothing to park
        parkedRenderScale = urp.renderScale;
        urp.renderScale = 1f;
    }

    void RestoreRetroRenderScale()
    {
        if (parkedRenderScale < 0f) return;
        var urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline
            as UniversalRenderPipelineAsset;
        if (urp != null) urp.renderScale = parkedRenderScale;
        parkedRenderScale = -1f;
    }

    void OnDestroy()
    {
        RestoreRetroRenderScale();
    }

    Camera LocalPlayerCamera()
    {
        foreach (var ctrl in FindObjectsByType<PlayerCameraController>(FindObjectsSortMode.None))
        {
            if (!ctrl.IsOwner) continue;
            var cam = ctrl.GetComponentInChildren<Camera>(true);
            if (cam != null) return cam;
        }
        return null;
    }

    // ─── CRT canvas ───────────────────────────────────────────────────────

    void BuildCrtCanvas()
    {
        crtCanvasGo = new GameObject("CrtScreenCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        crtCanvasGo.transform.SetParent(transform, false);
        var canvas = crtCanvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = menuCam;

        var rt = crtCanvasGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400f, 300f);
        rt.localScale = Vector3.one * 0.001f; // 0.40 × 0.30 m on the glass

        // Hover just in front of the CRT glass; identity rotation because the
        // screen faces -Z and the menu camera looks along +Z.
        crtCanvasGo.transform.position = screenAnchor.position + new Vector3(0f, 0.02f, -0.045f);
        crtCanvasGo.transform.rotation = Quaternion.identity;

        // Phosphor backdrop so the rows read against the dark model glass.
        var bg = AddImage(crtCanvasGo.transform, "PhosphorBg", CrtBgTint);
        StretchFull(bg.rectTransform);
        bg.raycastTarget = false;

        var rows = new GameObject("Rows", typeof(RectTransform), typeof(CanvasGroup));
        rows.transform.SetParent(crtCanvasGo.transform, false);
        StretchFull(rows.GetComponent<RectTransform>());
        rowsGroup = rows.GetComponent<CanvasGroup>();

        var header = AddText(rows.transform, "Header", MvpLocale.T("crt_terminal_header"), 13,
            BlackCommissionUiTheme.CrtGreenDim, TextAlignmentOptions.Left);
        AnchorTop(header.rectTransform, 12f, 24f, 18f);
        header.characterSpacing = 1.5f;

        var divider = AddImage(rows.transform, "Divider", BlackCommissionUiTheme.CrtGreenDim);
        var divRt = divider.rectTransform;
        divRt.anchorMin = new Vector2(0f, 1f);
        divRt.anchorMax = new Vector2(1f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.anchoredPosition = new Vector2(0f, -36f);
        divRt.sizeDelta = new Vector2(-36f, 1f);
        divider.raycastTarget = false;

        bool hasSave = SaveIO.AnySave;
        AddRow(rows.transform, 0, MvpLocale.T("menu_continue") + (hasSave ? "" : " " + MvpLocale.T("crt_no_save")),
            hasSave, () => owner.UiContinueShift());
        AddRow(rows.transform, 1, MvpLocale.T("menu_new_office"), true, () => owner.UiContinueShift());
        AddRow(rows.transform, 2, MvpLocale.T("join_office"), true, () => owner.UiOpenJoin());
        AddRow(rows.transform, 3, MvpLocale.T("menu_settings"), true, () => owner.UiOpenSettings());
        AddRow(rows.transform, 4, MvpLocale.T("menu_shutdown"), true, () => owner.UiQuitConfirm());

        var version = AddText(rows.transform, "Version", "ver 0.1 · BC-DOS", 9,
            BlackCommissionUiTheme.CrtGreenDim, TextAlignmentOptions.Right);
        var vRt = version.rectTransform;
        vRt.anchorMin = new Vector2(0f, 0f);
        vRt.anchorMax = new Vector2(1f, 0f);
        vRt.pivot = new Vector2(0.5f, 0f);
        vRt.anchoredPosition = new Vector2(-18f, 8f);
        vRt.sizeDelta = new Vector2(-36f, 14f);

        // Session status line replaces the rows once a network session is live.
        statusLine = AddText(crtCanvasGo.transform, "StatusLine", MvpLocale.T("crt_online"), 14,
            BlackCommissionUiTheme.CrtGreen, TextAlignmentOptions.Center);
        var sRt = statusLine.rectTransform;
        sRt.anchorMin = new Vector2(0f, 0.5f);
        sRt.anchorMax = new Vector2(1f, 0.5f);
        sRt.pivot = new Vector2(0.5f, 0.5f);
        sRt.anchoredPosition = Vector2.zero;
        sRt.sizeDelta = new Vector2(-30f, 60f);
        statusLine.gameObject.SetActive(false);

        // Boot flash sits on top of everything on the glass.
        bootFlash = AddImage(crtCanvasGo.transform, "BootFlash", Color.clear);
        StretchFull(bootFlash.rectTransform);
        bootFlash.raycastTarget = false;

        if (selected == 0 && !hasSave) selected = 1;
        ApplySelection();
    }

    void AddRow(Transform parent, int index, string title, bool selectable, System.Action action)
    {
        var rowGo = new GameObject($"Row{index}", typeof(RectTransform), typeof(Image), typeof(Button));
        rowGo.transform.SetParent(parent, false);
        var rt = rowGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -52f - index * 40f);
        rt.sizeDelta = new Vector2(-32f, 34f);

        var back = rowGo.GetComponent<Image>();
        back.color = Color.clear; // highlight fill set by ApplySelection
        rowBacks.Add(back);

        var label = AddText(rowGo.transform, "Label", "  " + title, 21,
            BlackCommissionUiTheme.CrtGreen, TextAlignmentOptions.Left);
        StretchFull(label.rectTransform);
        label.rectTransform.offsetMin = new Vector2(8f, 0f);
        label.raycastTarget = false;
        rowTexts.Add(label);
        rowSelectable.Add(selectable);
        rowActions.Add(action);

        var btn = rowGo.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        int i = index;
        btn.onClick.AddListener(() => { if (RowsInteractive() && rowSelectable[i]) { selected = i; ApplySelection(); Activate(); } });

        var trigger = rowGo.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            if (!RowsInteractive() || !rowSelectable[i] || selected == i) return;
            selected = i;
            ApplySelection();
            owner.UiPlayHover();
        });
        trigger.triggers.Add(enter);
    }

    void ApplySelection()
    {
        for (int i = 0; i < rowTexts.Count; i++)
        {
            bool sel = i == selected;
            string title = rowTexts[i].text.TrimStart(' ', '>');
            rowTexts[i].text = (sel ? "> " : "  ") + title;
            if (!rowSelectable[i])
            {
                // Spec: 无存档 ⇒ 「继续营业」墨淡不可选.
                rowTexts[i].color = new Color(0.26f, 0.40f, 0.26f, 0.55f);
                rowBacks[i].color = Color.clear;
            }
            else
            {
                // Selected row = 反白 (inverse video) per spec — two-channel with the > prefix.
                rowTexts[i].color = sel ? new Color(0.02f, 0.05f, 0.02f, 1f) : BlackCommissionUiTheme.CrtGreen;
                rowBacks[i].color = sel ? BlackCommissionUiTheme.CrtGreen : Color.clear;
            }
        }
    }

    void Activate()
    {
        owner.UiPlaySelect();
        rowActions[selected]?.Invoke();
    }

    // ─── Job note (新委托招贴, data-driven) ───────────────────────────────

    void BuildJobNote()
    {
        var task = Resources.Load<OfficeTaskDefinition>("Tasks/TowerEarthCoast_01");
        if (task == null) return;

        var noteGo = new GameObject("JobNoteCanvas", typeof(Canvas));
        noteGo.transform.SetParent(transform, false);
        noteGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var rt = noteGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160f, 200f);
        rt.localScale = Vector3.one * 0.001f;

        // Pinned beside the monitor housing, tilted like a hasty sticky note.
        noteGo.transform.position = screenAnchor.position + new Vector3(0.37f, -0.02f, -0.05f);
        noteGo.transform.rotation = Quaternion.Euler(-3f, -12f, 5f);

        var paper = AddImage(noteGo.transform, "Paper", BlackCommissionUiTheme.OldPaper);
        StretchFull(paper.rectTransform);
        paper.raycastTarget = false;

        var headBand = AddImage(noteGo.transform, "HeadBand", BlackCommissionUiTheme.MilitaryGreen);
        var hbRt = headBand.rectTransform;
        hbRt.anchorMin = new Vector2(0f, 1f);
        hbRt.anchorMax = new Vector2(1f, 1f);
        hbRt.pivot = new Vector2(0.5f, 1f);
        hbRt.anchoredPosition = Vector2.zero;
        hbRt.sizeDelta = new Vector2(0f, 26f);
        headBand.raycastTarget = false;

        var headText = AddText(noteGo.transform, "Head", MvpLocale.T("job_note_header"), 12,
            BlackCommissionUiTheme.OldPaper, TextAlignmentOptions.Center);
        AnchorTop(headText.rectTransform, 4f, 18f, 8f);
        headText.fontStyle = FontStyles.Bold;

        var title = AddText(noteGo.transform, "Title", task.title, 14, Ink, TextAlignmentOptions.TopLeft);
        AnchorTop(title.rectTransform, 34f, 64f, 10f);
        title.fontStyle = FontStyles.Bold;
        title.enableWordWrapping = true;

        var client = AddText(noteGo.transform, "Client", MvpLocale.T("job_note_client", task.client), 10,
            new Color(0.35f, 0.30f, 0.22f, 1f), TextAlignmentOptions.TopLeft);
        AnchorTop(client.rectTransform, 102f, 30f, 10f);
        client.enableWordWrapping = true;

        var reward = AddText(noteGo.transform, "Reward",
            MvpLocale.T("job_note_reward", task.moneyReward), 14,
            BlackCommissionUiTheme.RustWarning, TextAlignmentOptions.Left);
        AnchorTop(reward.rectTransform, 140f, 24f, 10f);
        reward.fontStyle = FontStyles.Bold;

        // Stamp-red corner block — paperwork is never unstamped in this office.
        var stamp = AddImage(noteGo.transform, "Stamp", new Color(0.761f, 0.227f, 0.169f, 0.55f));
        var stRt = stamp.rectTransform;
        stRt.anchorMin = new Vector2(1f, 0f);
        stRt.anchorMax = new Vector2(1f, 0f);
        stRt.pivot = new Vector2(1f, 0f);
        stRt.anchoredPosition = new Vector2(-8f, 10f);
        stRt.sizeDelta = new Vector2(44f, 22f);
        stRt.localRotation = Quaternion.Euler(0f, 0f, -8f);
        stamp.raycastTarget = false;
    }

    // ─── Per-frame ────────────────────────────────────────────────────────

    void Update()
    {
        bool listening = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (listening != wasListening)
        {
            SetListeningVisuals(listening);
            wasListening = listening;
            // Session ended while we were already handed off → bring the desk view back.
            if (!listening && menuCam == null && !pushing && LocalPlayerCamera() == null)
                BuildMenuCamera();
        }

        ManageListener();
        UpdateBootFlash();

        if (RowsInteractive())
            HandleKeys();
    }

    bool RowsInteractive()
    {
        return menuCam != null && !pushing && owner != null && owner.MenuRowsAvailable &&
            (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening);
    }

    void HandleKeys()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        int dir = 0;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dir = 1;
        else if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dir = -1;

        if (dir != 0)
        {
            int n = rowTexts.Count;
            for (int step = 1; step <= n; step++)
            {
                int cand = ((selected + dir * step) % n + n) % n;
                if (rowSelectable[cand]) { selected = cand; break; }
            }
            ApplySelection();
            owner.UiPlayHover();
            return;
        }

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            Activate();
    }

    void ManageListener()
    {
        if (menuListener == null) return;
        bool playerListenerLive = false;
        foreach (var ctrl in FindObjectsByType<PlayerCameraController>(FindObjectsSortMode.None))
        {
            if (!ctrl.IsOwner) continue;
            var l = ctrl.GetComponentInChildren<AudioListener>();
            if (l != null && l.enabled) { playerListenerLive = true; break; }
        }
        if (menuListener.enabled == playerListenerLive)
            menuListener.enabled = !playerListenerLive;
    }

    void UpdateBootFlash()
    {
        if (bootFlash == null || bootFlashUntil < 0f) return;
        float remain = bootFlashUntil - Time.unscaledTime;
        if (remain <= 0f)
        {
            bootFlash.color = Color.clear;
            bootFlashUntil = -1f;
            return;
        }
        float k = remain / BootFlashSeconds;
        bootFlash.color = new Color(0.424f, 1f, 0.373f, 0.85f * k * k);
    }

    void SetListeningVisuals(bool listening)
    {
        if (rowsGroup != null)
        {
            rowsGroup.alpha = listening ? 0f : 1f;
            rowsGroup.interactable = !listening;
            rowsGroup.blocksRaycasts = !listening;
        }
        if (statusLine != null)
            statusLine.gameObject.SetActive(listening);
    }

    // ─── Push-in hand-off ─────────────────────────────────────────────────

    /// <summary>
    /// Dolly the menu camera from the desk to the local player's head, then hand off.
    /// Called by MainMenuUI when the player confirms entry (确认到岗 / lobby dismissal).
    /// </summary>
    public void BeginPushIn()
    {
        if (menuCam == null || pushing) return;
        var target = LocalPlayerCamera();
        if (target == null)
        {
            // No player camera to hand to (offline edge) — just drop the desk view.
            Destroy(menuCam.gameObject);
            menuCam = null;
            menuListener = null;
            RestoreRetroRenderScale();
            return;
        }
        StartCoroutine(PushIn(target));
    }

    IEnumerator PushIn(Camera target)
    {
        pushing = true;
        if (menuListener != null) menuListener.enabled = false;

        Vector3 fromPos = menuCam.transform.position;
        Quaternion fromRot = menuCam.transform.rotation;
        float fromFov = menuCam.fieldOfView;
        float t0 = Time.unscaledTime;

        while (Time.unscaledTime - t0 < PushInSeconds)
        {
            if (menuCam == null || target == null) break;
            float k = Mathf.SmoothStep(0f, 1f, (Time.unscaledTime - t0) / PushInSeconds);
            // Track the player's live head pose — they may already be looking around.
            menuCam.transform.position = Vector3.LerpUnclamped(fromPos, target.transform.position, k);
            menuCam.transform.rotation = Quaternion.SlerpUnclamped(fromRot, target.transform.rotation, k);
            menuCam.fieldOfView = Mathf.Lerp(fromFov, target.fieldOfView, k);
            yield return null;
        }

        if (menuCam != null) Destroy(menuCam.gameObject);
        menuCam = null;
        menuListener = null;
        pushing = false;
        RestoreRetroRenderScale(); // the LC retro look belongs to gameplay, not the menu
    }

    // ─── UI primitives ────────────────────────────────────────────────────

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void AnchorTop(RectTransform rt, float yFromTop, float height, float sidePad)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -yFromTop);
        rt.sizeDelta = new Vector2(-sidePad * 2f, height);
    }

    static Image AddImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    static TMP_Text AddText(Transform parent, string name, string body, int fontSize,
        Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = body;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        return text;
    }
}
