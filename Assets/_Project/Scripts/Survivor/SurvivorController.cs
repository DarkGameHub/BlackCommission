using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// NPC survivor. Two types:
///   Light injury  — can walk when guided, follows player escort
///   Heavy injury  — cannot move, must be carried (drag or 2-player stretcher)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkObject))]
public class SurvivorController : NetworkBehaviour
{
    public enum InjuryLevel { Light, Heavy }

    [Header("Setup")]
    [SerializeField] InjuryLevel injuryLevel = InjuryLevel.Light;
    [SerializeField] float followDistance = 2f;
    [SerializeField] float panicRadius = 3f;     // robot/water causes panic in this radius
    [SerializeField] float panicCooldown = 8f;

    public NetworkVariable<bool> IsRescued = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsPanicking = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    NavMeshAgent agent;
    Transform escortTarget;      // player who calmed this survivor
    float panicTimer;

    public InjuryLevel Injury => injuryLevel;
    public bool CanWalk => injuryLevel == InjuryLevel.Light;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) { agent.enabled = false; return; }
        agent.enabled = true;
        if (!CanWalk) agent.enabled = false;
    }

    void Update()
    {
        if (!IsServer) return;
        if (IsRescued.Value) return;

        UpdatePanic();
        if (CanWalk && !IsPanicking.Value) UpdateFollow();
    }

    void UpdateFollow()
    {
        if (escortTarget == null) return;
        float dist = Vector3.Distance(transform.position, escortTarget.position);
        if (dist > followDistance)
            agent.SetDestination(escortTarget.position);
        else
            agent.ResetPath();
    }

    void UpdatePanic()
    {
        if (panicTimer > 0) { panicTimer -= Time.deltaTime; return; }

        // Check for robots or rising water nearby
        bool robotNearby = Physics.CheckSphere(transform.position, panicRadius,
            LayerMask.GetMask("Robot"));
        bool floodingNearby = WaterLevelManager.Instance != null &&
            WaterLevelManager.Instance.IsZoneFlooded(transform.position.y - 0.5f);

        bool shouldPanic = robotNearby || floodingNearby;
        if (shouldPanic != IsPanicking.Value)
        {
            IsPanicking.Value = shouldPanic;
            if (shouldPanic)
            {
                panicTimer = panicCooldown;
                if (CanWalk) RunInRandomDirection();
                TriggerCalloutClientRpc();
            }
        }
    }

    void RunInRandomDirection()
    {
        Vector3 runDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        Vector3 target = transform.position + runDir * 5f;
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

    [ClientRpc]
    void TriggerCalloutClientRpc()
    {
        AudioManager.Instance?.PlaySurvivorCallout(transform.position);
    }

    // Called by player interaction (E key on this survivor)
    [ServerRpc(RequireOwnership = false)]
    public void CalmSurvivorServerRpc(NetworkObjectReference playerRef)
    {
        if (!CanWalk) return;
        if (!playerRef.TryGet(out NetworkObject playerNet)) return;

        escortTarget = playerNet.transform;
        IsPanicking.Value = false;

        CalmedClientRpc();
    }

    [ClientRpc]
    void CalmedClientRpc()
    {
        AudioManager.Instance?.PlaySurvivorCalm(transform.position);
    }

    public void MarkRescued()
    {
        if (!IsServer) return;
        IsRescued.Value = true;
        agent.enabled = false;
        GameManager.Instance?.SurvivorRescued();
    }
}
