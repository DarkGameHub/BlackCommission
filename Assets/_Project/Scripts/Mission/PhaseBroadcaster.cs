using Unity.Netcode;
using UnityEngine;

public class PhaseBroadcaster : NetworkBehaviour
{
    [SerializeField] float messageDisplayDuration = 5f;

    static readonly string[] PhaseMessages =
    {
        "地下二层排水系统异常。",
        "水位持续上涨，请勿进入低洼区域。",
        "停车场闸门将在3分钟后封闭。",
        "警告！闸门即将关闭，所有人立即撤离！",
    };

    string currentMessage = "";
    float messageTimer;

    GUIStyle broadcastStyle;

    public override void OnNetworkSpawn()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged += OnPhaseChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= OnPhaseChanged;
    }

    void OnPhaseChanged(GameManager.MissionPhase newPhase)
    {
        if (!IsServer) return;

        int index = newPhase switch
        {
            GameManager.MissionPhase.Active => 0,
            GameManager.MissionPhase.Escalating => 1,
            GameManager.MissionPhase.Critical => 2,
            GameManager.MissionPhase.ForcedEvac => 3,
            _ => -1
        };

        if (index >= 0)
            BroadcastMessageClientRpc(index);
    }

    [ClientRpc]
    void BroadcastMessageClientRpc(int index)
    {
        if (index < 0 || index >= PhaseMessages.Length) return;
        currentMessage = PhaseMessages[index];
        messageTimer = messageDisplayDuration;
        AudioManager.Instance?.PlayPhaseBroadcast(index);
    }

    void Update()
    {
        if (messageTimer > 0)
            messageTimer -= Time.deltaTime;
    }

    void OnGUI()
    {
        if (messageTimer <= 0 || string.IsNullOrEmpty(currentMessage)) return;

        if (broadcastStyle == null)
        {
            broadcastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.2f) }
            };
        }

        float alpha = Mathf.Clamp01(messageTimer / 1.5f);
        var prevColor = GUI.color;
        GUI.color = new Color(1, 1, 1, alpha);

        float boxW = 600, boxH = 60;
        float boxY = Screen.height * 0.25f;
        GUI.Label(new Rect((Screen.width - boxW) / 2f, boxY, boxW, boxH), currentMessage, broadcastStyle);

        GUI.color = prevColor;
    }
}
