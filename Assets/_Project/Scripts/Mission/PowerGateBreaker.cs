using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative breaker panel for the tower power gate
/// (design/levels/abandoned-tower-earth-coast-01.md P-01: hold-interact 3.0 s in
/// F1_S3_PowerRoom → building-wide power-on, F2 security shutter lifts).
///
/// Authority model (mirrors TowerMissionManager): the host ticks the pure
/// <see cref="PowerGateLogic"/> and owns the synced state; clients only send
/// hold-start/hold-stop intents. <see cref="PowerRestored"/> and
/// <see cref="RestoreProgress"/> are NetworkVariables, so late joiners and host
/// migration receive the gate state automatically (GDD edge case).
///
/// Scene wiring: assign the closed-shutter / pre-power objects to
/// <see cref="disableWhenRestored"/> and the open-shutter / post-power lights to
/// <see cref="enableWhenRestored"/>. Place on the breaker prop in the POWER room
/// (RoomSlotRole.PowerGate slot in the v3 whitebox).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class PowerGateBreaker : NetworkBehaviour, IInteractable
{
    [Header("Tuning (GDD: power-gate hold time = 3.0s)")]
    [SerializeField] float requiredHoldSeconds = 3f;
    [Tooltip("A holder farther than this from the breaker is dropped by the server's per-tick validation.")]
    [SerializeField] float maxHoldDistance = 3.5f;

    [Header("Scene State Swap")]
    [Tooltip("Activated when power is restored: lifted shutter, post-power cold lights, powered signage.")]
    [SerializeField] GameObject[] enableWhenRestored;
    [Tooltip("Deactivated when power is restored: closed shutter blocker, pre-power darkness rigs.")]
    [SerializeField] GameObject[] disableWhenRestored;
    [SerializeField] AudioSource powerOnAudio;

    public NetworkVariable<float> RestoreProgress = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> PowerRestored = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Raised on every peer when the restored state flips on (audio/lighting hooks).</summary>
    public static event System.Action<PowerGateBreaker> OnPowerRestored;

    readonly HashSet<ulong> holdingClientIds = new();
    PowerGateLogic logic;
    bool localSoloHolding;
    bool restoredEffectsFired;
    float lastObservedProgress;
    float nextCrackleTime;

    public string InteractHint
    {
        get
        {
            if (PowerRestored.Value) return "";
            float normalized = requiredHoldSeconds > 0f
                ? Mathf.Clamp01(RestoreProgress.Value / requiredHoldSeconds)
                : 0f;
            return normalized > 0.01f
                ? $"按住恢复供电 {Mathf.RoundToInt(normalized * 100f)}%"
                : "按住恢复供电（断路器）";
        }
    }

    void Awake()
    {
        logic = new PowerGateLogic(requiredHoldSeconds);
    }

    public override void OnNetworkSpawn()
    {
        PowerRestored.OnValueChanged += HandleRestoredChanged;
        // Late joiner / scene reload: apply whatever the host says is current.
        ApplyRestoredState(PowerRestored.Value, silent: true);
        if (PowerRestored.Value) logic.ApplyRestored();
    }

    public override void OnNetworkDespawn()
    {
        PowerRestored.OnValueChanged -= HandleRestoredChanged;
    }

    void Update()
    {
        TickChargingAudio();

        if (!HasGateAuthority) return;
        if (logic.IsRestored) return;

        PruneInvalidHolders();
        bool held = holdingClientIds.Count > 0 || localSoloHolding;

        bool completed = logic.Tick(Time.deltaTime, held);
        if (!IsNetworked || IsServer)
            RestoreProgress.Value = logic.Progress;

        if (completed)
        {
            holdingClientIds.Clear();
            localSoloHolding = false;
            PowerRestored.Value = true;       // HandleRestoredChanged fires on all peers
            // Offline play may not raise OnValueChanged; the effects latch makes this safe.
            ApplyRestoredState(true, silent: false);
        }
    }

    // Every peer watches the synced progress and fizzes locally while it climbs —
    // the hold is audible to teammates standing nearby, not just the holder.
    void TickChargingAudio()
    {
        float progress = RestoreProgress.Value;
        bool charging = progress > lastObservedProgress + 0.0001f;
        lastObservedProgress = progress;
        if (!charging || PowerRestored.Value) return;
        if (Time.time < nextCrackleTime) return;
        nextCrackleTime = Time.time + 0.38f;
        AudioManager.Instance?.PlayBreakerCrackle(transform.position);
    }

    // ─── IInteractable (owner-side intents) ───

    public void OnInteractStart(PlayerController player)
    {
        if (PowerRestored.Value) return;
        if (!IsNetworked)
        {
            localSoloHolding = true;
            return;
        }
        SetHoldingServerRpc(true);
    }

    public void OnInteractEnd(PlayerController player)
    {
        if (!IsNetworked)
        {
            localSoloHolding = false;
            return;
        }
        if (PowerRestored.Value) return;
        SetHoldingServerRpc(false);
    }

    [ServerRpc(RequireOwnership = false)]
    void SetHoldingServerRpc(bool holding, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (holding) holdingClientIds.Add(clientId);
        else holdingClientIds.Remove(clientId);
    }

    // ─── server-side state ───

    // A holder who disconnected, went down, or walked away must not keep charging the
    // hold from beyond the grave; validated every tick on the authority.
    void PruneInvalidHolders()
    {
        if (!IsNetworked || holdingClientIds.Count == 0) return;

        var net = NetworkManager.Singleton;
        holdingClientIds.RemoveWhere(clientId =>
        {
            if (!net.ConnectedClients.TryGetValue(clientId, out var client)) return true;
            NetworkObject po = client.PlayerObject;
            if (po == null) return true;
            if (po.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value) return true;
            return Vector3.Distance(po.transform.position, transform.position) > maxHoldDistance;
        });
    }

    void HandleRestoredChanged(bool previous, bool restored)
    {
        if (restored && !logic.IsRestored) logic.ApplyRestored();
        ApplyRestoredState(restored, silent: !restored || previous == restored);
    }

    void ApplyRestoredState(bool restored, bool silent)
    {
        foreach (GameObject go in enableWhenRestored)
            if (go != null) go.SetActive(restored);
        foreach (GameObject go in disableWhenRestored)
            if (go != null) go.SetActive(!restored);

        if (restored && !silent && !restoredEffectsFired)
        {
            restoredEffectsFired = true;
            if (powerOnAudio != null) powerOnAudio.Play();
            AudioManager.Instance?.PlayPowerRestored(transform.position);
            // The debt shutters slam open where they stood (positions stay readable
            // on the now-inactive objects).
            foreach (GameObject go in disableWhenRestored)
                if (go != null) AudioManager.Instance?.PlayShutterSlam(go.transform.position);
            OnPowerRestored?.Invoke(this);
        }
    }

    bool IsNetworked => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    bool HasGateAuthority => IsServer || !IsNetworked;
}
