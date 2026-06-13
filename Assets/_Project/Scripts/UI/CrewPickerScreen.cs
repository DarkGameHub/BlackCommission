using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// "Select your agent" — the crew/appearance picker (design/ux/lobby.md amendment).
/// Framed as a Mars labour-review window: a warm, opulent interior looks out at a
/// cold grey window; the candidate (a possible you) stands on a lit review dais and
/// you cycle through the six PlayerCharacterPalette looks. The actual in-game
/// character model is shown — see design/ux/mockups/character for the target look.
///
/// Self-contained: builds its own preview rig far from the live scene, a high-depth
/// camera, warm/cold lights, and the dossier UI. Open()/Close() like SettingsOverlay.
/// Confirm writes PlayerCharacterPalette.SavedIndex (+ the local player's Character
/// index if one exists, so the change shows immediately in a session).
/// </summary>
public class CrewPickerScreen : MonoBehaviour
{
    static CrewPickerScreen instance;
    public static bool IsOpen => instance != null;

    const float StageY = 1000f;   // far above the live scene so nothing overlaps

    Camera cam;
    Transform modelAnchor;
    GameObject currentModel;
    int index;
    float spin;
    float parkedRenderScale = -1f;

    GUIStyle headerStyle, chitTitleStyle, chitBodyStyle, chitFormStyle, hintStyle, stampStyle;
    Texture2D paperTex, headerBandTex, swatchTex;
    bool stylesReady;

    public static void Open()
    {
        if (instance != null) return;
        var go = new GameObject("CrewPickerScreen");
        instance = go.AddComponent<CrewPickerScreen>();
        instance.Build();
    }

    public static void Close()
    {
        if (instance != null) instance.Teardown();
    }

    void Build()
    {
        index = PlayerCharacterPalette.SavedIndex;

        // The preview is rendered sharp regardless of the lo-fi mission scale.
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp != null && urp.renderScale < 0.999f) { parkedRenderScale = urp.renderScale; urp.renderScale = 1f; }

        BuildRoom();
        BuildModel();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void BuildRoom()
    {
        var root = new GameObject("CrewPickerStage");
        root.transform.SetParent(transform, false);
        root.transform.position = new Vector3(0f, StageY, 0f);

        // ── warm opulent interior: a dark warm box behind/around the dais ──
        Material warmWall = FlatMat(new Color(0.16f, 0.10f, 0.06f));
        Material gold = FlatMat(new Color(0.50f, 0.39f, 0.18f));
        Material floorMat = FlatMat(new Color(0.10f, 0.07f, 0.04f));
        AddBox(root.transform, "BackWall", new Vector3(0f, StageY + 1.4f, 2.6f), new Vector3(7f, 4.6f, 0.2f), warmWall);
        AddBox(root.transform, "Floor", new Vector3(0f, StageY - 0.05f, 0.8f), new Vector3(7f, 0.1f, 5f), floorMat);
        AddBox(root.transform, "TrimL", new Vector3(-2.3f, StageY + 1.2f, 1.0f), new Vector3(0.12f, 3.6f, 3.6f), gold);
        AddBox(root.transform, "TrimR", new Vector3(2.3f, StageY + 1.2f, 1.0f), new Vector3(0.12f, 3.6f, 3.6f), gold);

        // ── tall cold window behind the candidate (the bleak Earth outside) ──
        AddBox(root.transform, "WindowFrame", new Vector3(0f, StageY + 1.5f, 2.45f), new Vector3(2.2f, 3.4f, 0.08f), gold);
        var win = AddBox(root.transform, "Window", new Vector3(0f, StageY + 1.5f, 2.5f), new Vector3(2.0f, 3.2f, 0.04f),
            FlatMat(new Color(0.40f, 0.45f, 0.46f)));
        win.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0.34f, 0.39f, 0.40f));
        win.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");

        // ── review dais ──
        AddBox(root.transform, "Dais", new Vector3(0f, StageY + 0.06f, 0f), new Vector3(1.8f, 0.12f, 1.8f),
            FlatMat(new Color(0.14f, 0.10f, 0.06f)));

        modelAnchor = new GameObject("ModelAnchor").transform;
        modelAnchor.SetParent(root.transform, false);
        modelAnchor.position = new Vector3(0f, StageY + 0.12f, 0f);

        // ── lights: warm key from the front-left (interior), cold rim from the window ──
        var key = new GameObject("KeyLight").AddComponent<Light>();
        key.transform.SetParent(root.transform, false);
        key.transform.position = new Vector3(-1.6f, StageY + 2.4f, -1.6f);
        key.transform.rotation = Quaternion.Euler(35f, 45f, 0f);
        key.type = LightType.Directional; key.color = new Color(1f, 0.78f, 0.48f); key.intensity = 1.5f;
        var rim = new GameObject("RimLight").AddComponent<Light>();
        rim.transform.SetParent(root.transform, false);
        rim.transform.rotation = Quaternion.Euler(10f, 200f, 0f);
        rim.type = LightType.Directional; rim.color = new Color(0.62f, 0.70f, 0.78f); rim.intensity = 0.9f;

        // ── preview camera (renders on top of everything) ──
        var camGo = new GameObject("CrewPickerCamera");
        camGo.transform.SetParent(transform, false);
        camGo.transform.position = new Vector3(0f, StageY + 1.05f, -2.7f);
        camGo.transform.LookAt(new Vector3(0f, StageY + 0.95f, 0f));
        cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 36f;
        cam.nearClipPlane = 0.05f;
        cam.depth = 90f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.05f, 0.03f);
        camGo.AddComponent<AudioListener>().enabled = false;
    }

    void BuildModel()
    {
        if (currentModel != null) Destroy(currentModel);

        string resource = PlayerCharacterModels.Get(index);
        GameObject prefab = !string.IsNullOrEmpty(resource) ? Resources.Load<GameObject>(resource) : null;
        if (prefab == null) prefab = Resources.Load<GameObject>("GeneratedArt/ASV4_WorkerCheapOutsourcedUniform");

        if (prefab != null)
        {
            currentModel = Instantiate(prefab, modelAnchor);
            currentModel.transform.localPosition = Vector3.zero;
            currentModel.transform.localRotation = Quaternion.identity;
            currentModel.transform.localScale = Vector3.one;
            foreach (var c in currentModel.GetComponentsInChildren<Collider>()) Destroy(c);
            Color tint = PlayerCharacterModels.TintFor(index);
            foreach (var r in currentModel.GetComponentsInChildren<Renderer>())
            {
                var m = r.material;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint); else m.color = tint;
            }
        }
        else
        {
            // Last-ditch placeholder so the screen still works without the prefab.
            currentModel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            currentModel.transform.SetParent(modelAnchor, false);
            currentModel.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
            Destroy(currentModel.GetComponent<Collider>());
        }
    }

    void Cycle(int dir)
    {
        index = (index + dir + PlayerCharacterModels.ResourceNames.Length) % PlayerCharacterModels.ResourceNames.Length;
        BuildModel();
        if (cam != null) AudioManager.Instance?.PlayComputerBeep(cam.transform.position);
    }

    void Confirm()
    {
        PlayerCharacterPalette.SavedIndex = index;
        foreach (var p in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            if (p.IsOwner) p.CharacterIndex.Value = index;
        AudioManager.Instance?.PlayStamp();
        Teardown();
    }

    void Update()
    {
        if (modelAnchor != null) { spin += Time.unscaledDeltaTime * 14f; modelAnchor.localRotation = Quaternion.Euler(0f, 180f + Mathf.Sin(spin * 0.04f) * 18f, 0f); }

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;
        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) Cycle(-1);
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) Cycle(1);
        else if (kb.eKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame) Confirm();
        else if (kb.escapeKey.wasPressedThisFrame) Teardown();
    }

    void Teardown()
    {
        if (parkedRenderScale >= 0f)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp != null) urp.renderScale = parkedRenderScale;
        }
        if (instance == this) instance = null;
        Destroy(gameObject);
    }

    // ── UI ──
    void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;
        paperTex = Tex(new Color(0.812f, 0.769f, 0.643f, 0.98f));
        headerBandTex = Tex(BlackCommissionUiTheme.MilitaryGreen);   // oxblood now
        swatchTex = Tex(PlayerCharacterPalette.Get(index).vest);
        headerStyle = Label(30, new Color(0.88f, 0.78f, 0.55f), FontStyle.Bold);
        chitTitleStyle = Label(22, new Color(0.15f, 0.12f, 0.10f), FontStyle.Bold);
        chitBodyStyle = Label(16, new Color(0.29f, 0.25f, 0.21f), FontStyle.Normal);
        chitFormStyle = Label(13, new Color(0.66f, 0.75f, 0.71f), FontStyle.Normal);
        hintStyle = Label(18, new Color(0.60f, 0.56f, 0.47f), FontStyle.Normal);
        stampStyle = Label(20, new Color(0.76f, 0.23f, 0.17f), FontStyle.Bold);
    }

    void OnGUI()
    {
        EnsureStyles();
        var pal = PlayerCharacterPalette.Get(index);
        float w = Screen.width, h = Screen.height;

        GUI.Label(new Rect(40f, 28f, w - 80f, 40f), "SELECT YOUR AGENT", headerStyle);
        GUI.Label(new Rect(w - 180f, 32f, 140f, 30f), $"{index + 1} / {PlayerCharacterModels.ResourceNames.Length}",
            new GUIStyle(headerStyle) { alignment = TextAnchor.MiddleRight, fontSize = 22 });

        // dossier chit, bottom-centre
        float cw = 760f, ch = 150f;
        Rect chit = new Rect((w - cw) * 0.5f, h - ch - 70f, cw, ch);
        GUI.DrawTexture(chit, paperTex);
        GUI.DrawTexture(new Rect(chit.x, chit.y, chit.width, 42f), headerBandTex);
        GUI.Label(new Rect(chit.x + 20f, chit.y + 9f, 400f, 26f), "AGENT DOSSIER", new GUIStyle(chitTitleStyle){ normal = { textColor = new Color(0.84f,0.80f,0.68f) }});
        GUI.Label(new Rect(chit.xMax - 130f, chit.y + 12f, 110f, 20f), "FORM BC-04", chitFormStyle);
        swatchTex = Tex(pal.vest);
        GUI.DrawTexture(new Rect(chit.x + 24f, chit.y + 60f, 56f, 56f), swatchTex);
        GUI.Label(new Rect(chit.x + 96f, chit.y + 58f, cw - 120f, 28f), pal.label.ToUpperInvariant(), chitTitleStyle);
        GUI.Label(new Rect(chit.x + 96f, chit.y + 90f, cw - 120f, 24f), "Temp hire · supply-closet kit · hi-vis vest, hard hat, respirator", chitBodyStyle);
        GUI.Label(new Rect(chit.xMax - 150f, chit.y + 96f, 130f, 30f), "ASSIGNED", stampStyle);

        GUI.Label(new Rect(0f, h - 50f, w, 28f),
            "‹ ›  CYCLE      [E] THIS IS ME      [ESC] KEEP CURRENT",
            new GUIStyle(hintStyle) { alignment = TextAnchor.MiddleCenter });
    }

    // ── helpers ──
    static Texture2D Tex(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }
    static GUIStyle Label(int size, Color c, FontStyle fs)
    {
        var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = fs, normal = { textColor = c }, wordWrap = true };
        MvpFontProvider.ApplyToStyle(s);
        return s;
    }
    static Material FlatMat(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        return m;
    }
    static GameObject AddBox(Transform parent, string name, Vector3 worldPos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, true);
        go.transform.position = worldPos;
        go.transform.localScale = size;
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<Renderer>().material = mat;
        return go;
    }
}
