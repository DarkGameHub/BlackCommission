using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Always-on "your mic is live" indicator + first-run voice consent notice.
///
/// Black Commission runs proximity voice OPEN-MIC by default (speaking is a deliberate
/// risk — the Echo Mold eavesdrops and replays your voice, see design/gdd/monster-echo-mold.md),
/// so the player must be able to tell at a glance that their microphone is hot. This is a
/// self-bootstrapping IMGUI overlay matching <see cref="SettingsOverlay"/>'s IMGUI style —
/// no scene wiring required. It only READS <see cref="ProximityVoiceChat"/> telemetry; it
/// never drives the mic. The consent gate (shown once, the first time the player is in a
/// voice-capable session) addresses the Echo Mold privacy open question (record/replay of
/// real voice). Mute control still lives in the Settings overlay.
/// </summary>
public class VoiceMicIndicator : MonoBehaviour
{
    const string ConsentKey = "AS.Voice.ConsentShown";

    static VoiceMicIndicator instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("MVP_VoiceMicIndicator");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<VoiceMicIndicator>();
    }

    static bool ConsentShown
    {
        get => PlayerPrefs.GetInt(ConsentKey, 0) != 0;
        set { PlayerPrefs.SetInt(ConsentKey, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    // BC palette (lo-fi): amber accent for "live", dim steel for idle, stamp-ish red for muted.
    static readonly Color Amber = new Color(1f, 0.69f, 0.16f);
    static readonly Color Dim = new Color(0.58f, 0.58f, 0.55f);
    static readonly Color MutedColor = new Color(0.78f, 0.27f, 0.22f);
    static readonly Color Panel = new Color(0.06f, 0.07f, 0.08f, 0.86f);
    static readonly Color Paper = new Color(0.86f, 0.84f, 0.78f);

    GUIStyle labelStyle;
    GUIStyle modalTitle;
    GUIStyle modalBody;
    Texture2D pixel;

    static bool InSession => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    void EnsureStyles()
    {
        if (pixel == null)
        {
            pixel = new Texture2D(1, 1);
            pixel.SetPixel(0, 0, Color.white);
            pixel.Apply();
        }
        labelStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft
        };
        modalTitle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true
        };
        modalBody ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true
        };
    }

    void OnGUI()
    {
        EnsureStyles();

        // First-run consent gate — only once we are actually in a voice-capable session.
        if (InSession && !ConsentShown)
        {
            DrawConsentModal();
            return;
        }

        if (!InSession || !ProximityVoiceChat.VoiceEnabled) return;
        DrawIndicator();
    }

    void Fill(Rect r, Color c)
    {
        Color prev = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, pixel);
        GUI.color = prev;
    }

    void DrawIndicator()
    {
        bool muted = ProximityVoiceChat.Muted;
        bool ptt = ProximityVoiceChat.PushToTalk;
        bool hot = ProximityVoiceChat.IsMicHot;
        bool hasMic = ProximityVoiceChat.MicrophoneAvailable;
        float level = ProximityVoiceChat.InputLevel;

        string label;
        Color color;
        if (!hasMic) { label = MvpLocale.T("mic_no_device"); color = MutedColor; }
        else if (muted) { label = MvpLocale.T("mic_muted"); color = MutedColor; }
        else if (hot) { label = MvpLocale.T("mic_live"); color = Amber; }
        else if (ptt) { label = MvpLocale.T("mic_ptt_ready"); color = Dim; }
        else { label = MvpLocale.T("mic_ready"); color = Dim; }

        // Top-right corner: top-center belongs to the dispatch ticket strip (they used to
        // overlap during boarding/transit), and the HUD zone map keeps this corner free.
        const float w = 172f, h = 30f;
        float x = Screen.width - w - 24f;
        // Keep the badge below the BC-DOS header line. At 16:9/Free Aspect the previous
        // 24 px offset sat directly over the page's MEM OK text and looked like overflow.
        float y = 70f;
        var box = new Rect(x, y, w, h);
        Fill(box, Panel);

        // Status dot — blinks while genuinely live (broadcast "on-air" convention).
        float dotA = (hot && !muted && hasMic) ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f) : 1f;
        var dotRect = new Rect(x + 10f, y + h * 0.5f - 5f, 10f, 10f);
        Color dc = color; dc.a = dotA;
        Fill(dotRect, dc);

        Color prev = GUI.color;
        GUI.color = color;
        GUI.Label(new Rect(x + 28f, y, w - 34f, h), label, labelStyle);
        GUI.color = prev;

        // Live input level meter directly under the badge.
        if (hot && !muted && hasMic)
        {
            var track = new Rect(x, y + h, w, 4f);
            Fill(track, new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(x, y + h, w * Mathf.Clamp01(level), 4f), color);
        }
    }

    void DrawConsentModal()
    {
        const float w = 480f, h = 236f;
        var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.6f));
        Fill(r, Panel);
        Fill(new Rect(r.x, r.y, r.width, 3f), Amber); // accent rule

        Color prev = GUI.color;
        GUI.color = Amber;
        GUI.Label(new Rect(r.x + 20, r.y + 18, r.width - 40, 30), MvpLocale.T("voice_consent_title"), modalTitle);
        GUI.color = Paper;
        GUI.Label(new Rect(r.x + 26, r.y + 58, r.width - 52, 108), MvpLocale.T("voice_consent_body"), modalBody);
        GUI.color = prev;

        float bw = (r.width - 64f) * 0.5f;
        if (GUI.Button(new Rect(r.x + 22, r.y + h - 54, bw, 36), MvpLocale.T("voice_consent_keep")))
            ConsentShown = true;
        if (GUI.Button(new Rect(r.x + 42 + bw, r.y + h - 54, bw, 36), MvpLocale.T("voice_consent_mute")))
        {
            ProximityVoiceChat.Muted = true;
            ConsentShown = true;
        }
    }
}
