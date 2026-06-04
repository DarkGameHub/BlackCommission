using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Breath/oxygen for diving. The owner drains oxygen while its head is underwater
/// (PlayerController.IsSubmergedLocal) and regenerates it above water. When oxygen is
/// gone the owner asks the server to apply drowning damage through the existing
/// PlayerHealth pipeline, so the standard downed/revive flow handles the rest.
///
/// Oxygen is mirrored over the network (owner-write) only so peers could read it later;
/// the HUD bar is drawn locally by the owner.
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerOxygen : NetworkBehaviour
{
    [Header("Oxygen")]
    [SerializeField] float maxOxygen = 100f;
    [SerializeField] float drainPerSecond = 12f;        // while submerged
    [SerializeField] float regenPerSecond = 28f;        // while above water
    [SerializeField] float drownDamagePerSecond = 12f;  // once oxygen is depleted

    public NetworkVariable<float> Oxygen = new(100f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public float MaxOxygen => maxOxygen;
    public float Normalized => maxOxygen > 0f ? Mathf.Clamp01(Oxygen.Value / maxOxygen) : 0f;

    PlayerController controller;
    PlayerHealth health;
    float drownAccumulator;

    // HUD
    static Texture2D barTex;
    GUIStyle labelStyle;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        TryGetComponent(out health);
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
            Oxygen.Value = maxOxygen;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (health != null && health.IsDowned.Value)
        {
            drownAccumulator = 0f;
            return;
        }

        bool submerged = controller != null && controller.IsSubmergedLocal;

        float oxygen = Oxygen.Value;
        oxygen += (submerged ? -drainPerSecond : regenPerSecond) * Time.deltaTime;
        oxygen = Mathf.Clamp(oxygen, 0f, maxOxygen);
        Oxygen.Value = oxygen;

        if (submerged && oxygen <= 0f)
        {
            drownAccumulator += drownDamagePerSecond * Time.deltaTime;
            if (drownAccumulator >= 1f)
            {
                float damage = Mathf.Floor(drownAccumulator);
                drownAccumulator -= damage;
                ApplyDrownDamage(damage);
            }
        }
        else
        {
            drownAccumulator = 0f;
        }
    }

    void ApplyDrownDamage(float damage)
    {
        if (IsServer)
            health?.TakeDamage(damage);
        else
            DrownServerRpc(damage);
    }

    [ServerRpc]
    void DrownServerRpc(float damage)
    {
        health?.TakeDamage(damage);
    }

    // ─── Owner HUD: oxygen bar (only while diving or recovering breath) ───

    void OnGUI()
    {
        if (!IsOwner) return;
        if (health != null && health.IsDowned.Value) return;
        if (MvpHud.IsBlockingPanelOpen || VanTransitOverlay.IsActive) return;

        float fill = Normalized;
        bool diving = controller != null && controller.IsSubmergedLocal;
        if (!diving && fill >= 0.999f) return;   // hide when topped up on land

        EnsureBarTexture();
        if (labelStyle == null)
        {
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };
        }

        float w = 240f;
        float h = 16f;
        float x = Screen.width * 0.5f - w * 0.5f;
        float y = Screen.height - 96f;

        // Frame + background
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, w + 4f, h + 4f), barTex);

        // Fill — cyan, flashing red when nearly out of air
        Color fillColor = fill <= 0.25f
            ? Color.Lerp(new Color(0.9f, 0.15f, 0.12f), new Color(0.2f, 0.7f, 1f), Mathf.PingPong(Time.time * 3f, 1f))
            : new Color(0.3f, 0.78f, 1f);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x, y, w * fill, h), barTex);

        GUI.color = Color.white;
        GUI.Label(new Rect(x, y - 22f, w, 20f), $"O₂  {Mathf.CeilToInt(fill * 100f)}%", labelStyle);
        GUI.color = Color.white;
    }

    static void EnsureBarTexture()
    {
        if (barTex != null) return;
        barTex = new Texture2D(1, 1);
        barTex.SetPixel(0, 0, Color.white);
        barTex.Apply();
    }
}
