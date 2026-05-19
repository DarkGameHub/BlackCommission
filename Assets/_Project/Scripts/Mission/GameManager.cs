using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Central mission state machine. Runs on server, broadcasts state to all clients.
/// Phases: Preparation → Active → Escalating → Critical → Extraction → Ended
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Mission Timing")]
    [SerializeField] float phase1Duration = 300f;  // 0-5 min: controllable
    [SerializeField] float phase2Duration = 300f;  // 5-10 min: deteriorating
    [SerializeField] float phase3Duration = 300f;  // 10-15 min: chaos
    [SerializeField] float forceEvacTime = 900f;   // 15 min: forced evacuation

    public enum MissionPhase { Preparation, Active, Escalating, Critical, ForcedEvac, Ended }

    public NetworkVariable<MissionPhase> CurrentPhase = new(MissionPhase.Preparation,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> MissionTimer = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Objectives
    public NetworkVariable<int> SurvivorsRescued = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> PumpRepaired = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> EvacuationComplete = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public event Action<MissionPhase> OnPhaseChanged;
    public event Action OnMissionComplete;
    public event Action OnMissionFailed;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        CurrentPhase.OnValueChanged += (_, newPhase) => OnPhaseChanged?.Invoke(newPhase);

        if (IsServer)
            StartMission();
    }

    void StartMission()
    {
        MissionTimer.Value = 0f;
        CurrentPhase.Value = MissionPhase.Active;
    }

    void Update()
    {
        if (!IsServer) return;
        if (CurrentPhase.Value == MissionPhase.Ended) return;

        MissionTimer.Value += Time.deltaTime;
        UpdatePhase();
    }

    void UpdatePhase()
    {
        float t = MissionTimer.Value;

        MissionPhase target = t < phase1Duration ? MissionPhase.Active
                            : t < phase1Duration + phase2Duration ? MissionPhase.Escalating
                            : t < forceEvacTime ? MissionPhase.Critical
                            : MissionPhase.ForcedEvac;

        if (target != CurrentPhase.Value)
            CurrentPhase.Value = target;
    }

    // Called by EvacuationPoint when all required objectives are met and players exit
    public void TriggerEvacuation(int survivorsCarried, int evidenceCount)
    {
        if (!IsServer) return;
        EvacuationComplete.Value = true;
        CurrentPhase.Value = MissionPhase.Ended;

        NotifyEvacuationClientRpc(survivorsCarried, evidenceCount);
    }

    [ClientRpc]
    void NotifyEvacuationClientRpc(int survivors, int evidence)
    {
        OnMissionComplete?.Invoke();
        // SettlementManager picks up from here
        SettlementManager.Instance?.BeginSettlement(survivors, evidence);
    }

    public void SurvivorRescued()
    {
        if (!IsServer) return;
        SurvivorsRescued.Value++;
    }

    public void PumpFixed()
    {
        if (!IsServer) return;
        PumpRepaired.Value = true;
    }

    // Can primary objective still be completed?
    public bool CanComplete => SurvivorsRescued.Value >= 1 && PumpRepaired.Value;

    public float TimeRemainingToForcedEvac => Mathf.Max(0, forceEvacTime - MissionTimer.Value);
}
