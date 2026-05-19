using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class JobBoard : MonoBehaviour, IInteractable
{
    static bool _panelOpen;
    static int _selectedJob = -1;
    float _msgTimer;
    string _msg;
    GUIStyle _msgStyle;
    Texture2D _msgBg;
    Texture2D _panelBg;
    Texture2D _jobBg;
    Texture2D _jobHover;
    Texture2D _jobSelected;
    GUIStyle _titleStyle;
    GUIStyle _jobStyle;
    GUIStyle _jobActiveStyle;
    GUIStyle _detailStyle;
    GUIStyle _btnStyle;
    GUIStyle _closeStyle;
    bool _stylesReady;

    struct JobData
    {
        public string type;
        public string title;
        public string location;
        public string detail;
        public int pay;
        public string difficulty;
        public string scene;
    }

    static readonly JobData[] Jobs =
    {
        new() { type = "Water", title = "Underground Mall Flood",
            location = "B2 Parking Level", detail = "Flood in underground parking. Rescue survivors, repair pump, evacuate before lockdown.",
            pay = 1700, difficulty = "Medium", scene = "Mall_B2" },
        new() { type = "Fire", title = "Warehouse Fire",
            location = "Industrial District", detail = "Chemical warehouse fire. Locate missing workers and secure hazmat containers.",
            pay = 2200, difficulty = "Hard", scene = "" },
        new() { type = "Collapse", title = "Building Collapse",
            location = "Old Town Residential", detail = "Partial building collapse after earthquake. Search for trapped residents.",
            pay = 1900, difficulty = "Hard", scene = "" },
        new() { type = "Gas", title = "Gas Leak Emergency",
            location = "Subway Station B3", detail = "Gas leak detected in subway tunnels. Evacuate staff and seal the leak.",
            pay = 1500, difficulty = "Easy", scene = "" },
    };

    public string InteractHint
    {
        get
        {
            if (_panelOpen) return "";
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return "Need to connect first";
            if (!NetworkManager.Singleton.IsHost)
                return "Waiting for host";
            return "[E] Job Board";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        if (_panelOpen) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            ShowMessage("Connect first (Start Host)");
            return;
        }
        if (!NetworkManager.Singleton.IsHost)
        {
            ShowMessage("Only host can accept jobs");
            return;
        }
        _panelOpen = true;
        _selectedJob = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnInteractEnd(PlayerController player) { }

    void ShowMessage(string msg)
    {
        _msg = msg;
        _msgTimer = 3f;
    }

    void InitStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _panelBg = MakeTex(new Color(0.06f, 0.06f, 0.1f, 0.95f));
        _jobBg = MakeTex(new Color(0.12f, 0.12f, 0.18f, 1f));
        _jobHover = MakeTex(new Color(0.18f, 0.18f, 0.28f, 1f));
        _jobSelected = MakeTex(new Color(0.15f, 0.3f, 0.55f, 1f));

        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.85f, 0.4f) }
        };
        _jobStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14, alignment = TextAnchor.MiddleLeft,
            normal = { background = _jobBg, textColor = new Color(0.85f, 0.85f, 0.85f) },
            hover = { background = _jobHover, textColor = Color.white },
            active = { background = _jobSelected, textColor = Color.white },
            padding = new RectOffset(12, 12, 8, 8),
            margin = new RectOffset(0, 0, 2, 2)
        };
        _jobActiveStyle = new GUIStyle(_jobStyle)
        {
            normal = { background = _jobSelected, textColor = Color.white },
            fontStyle = FontStyle.Bold
        };
        _detailStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, wordWrap = true,
            normal = { textColor = new Color(0.8f, 0.8f, 0.85f) },
            padding = new RectOffset(8, 8, 4, 4)
        };
        _btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16, fontStyle = FontStyle.Bold,
            normal = { background = MakeTex(new Color(0.2f, 0.5f, 0.2f)), textColor = Color.white },
            hover = { background = MakeTex(new Color(0.25f, 0.6f, 0.25f)), textColor = Color.white },
            active = { background = MakeTex(new Color(0.15f, 0.4f, 0.15f)), textColor = Color.white },
            padding = new RectOffset(16, 16, 8, 8)
        };
        _closeStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            normal = { background = MakeTex(new Color(0.5f, 0.15f, 0.15f)), textColor = Color.white },
            hover = { background = MakeTex(new Color(0.6f, 0.2f, 0.2f)), textColor = Color.white },
            padding = new RectOffset(12, 12, 6, 6)
        };
    }

    static Texture2D MakeTex(Color col)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, col);
        t.Apply();
        return t;
    }

    void OnGUI()
    {
        InitStyles();

        if (_panelOpen)
            DrawJobPanel();

        if (_msgTimer > 0)
        {
            _msgTimer -= Time.deltaTime;
            if (_msgStyle == null)
            {
                _msgBg = MakeTex(new Color(0.1f, 0.1f, 0.15f, 0.9f));
                _msgStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16, alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.yellow, background = _msgBg },
                    padding = new RectOffset(12, 12, 8, 8)
                };
            }
            float w = 400, h = 40;
            GUI.Label(new Rect((Screen.width - w) / 2f, Screen.height * 0.6f, w, h), _msg, _msgStyle);
        }
    }

    void DrawJobPanel()
    {
        float pw = 700, ph = 440;
        float px = (Screen.width - pw) / 2f;
        float py = (Screen.height - ph) / 2f;

        GUI.DrawTexture(new Rect(px, py, pw, ph), _panelBg);

        GUI.Label(new Rect(px, py + 8, pw, 34), "JOB BOARD", _titleStyle);

        // Close button
        if (GUI.Button(new Rect(px + pw - 70, py + 8, 60, 28), "Close", _closeStyle))
        {
            _panelOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Left: job list
        float listX = px + 16, listY = py + 52;
        float listW = 280, itemH = 52;

        for (int i = 0; i < Jobs.Length; i++)
        {
            var job = Jobs[i];
            var style = (i == _selectedJob) ? _jobActiveStyle : _jobStyle;
            string label = $"[{job.type}]  {job.title}\n${job.pay}  |  {job.difficulty}";

            if (GUI.Button(new Rect(listX, listY + i * (itemH + 4), listW, itemH), label, style))
                _selectedJob = i;
        }

        // Right: details
        float detX = px + 316, detY = py + 52;
        float detW = pw - 336;

        if (_selectedJob >= 0 && _selectedJob < Jobs.Length)
        {
            var job = Jobs[_selectedJob];

            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            var infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.6f, 0.7f, 0.8f) }
            };

            GUI.Label(new Rect(detX, detY, detW, 26), job.title, headerStyle);
            detY += 28;
            GUI.Label(new Rect(detX, detY, detW, 20), $"Location: {job.location}", infoStyle);
            detY += 20;
            GUI.Label(new Rect(detX, detY, detW, 20), $"Pay: ${job.pay}  |  Difficulty: {job.difficulty}", infoStyle);
            detY += 28;
            GUI.Label(new Rect(detX, detY, detW, 100), job.detail, _detailStyle);
            detY += 80;

            bool available = !string.IsNullOrEmpty(job.scene);
            string btnText = available ? "ACCEPT JOB" : "COMING SOON";

            GUI.enabled = available;
            if (GUI.Button(new Rect(detX, ph + py - 60, detW, 40), btnText, _btnStyle))
            {
                _panelOpen = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                NetworkManager.Singleton.SceneManager.LoadScene(job.scene, LoadSceneMode.Single);
            }
            GUI.enabled = true;
        }
    }
}
