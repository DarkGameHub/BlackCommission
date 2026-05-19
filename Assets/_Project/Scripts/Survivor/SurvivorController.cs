using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider))]
public class SurvivorController : NetworkBehaviour, IInteractable
{
    public enum InjuryLevel { Light, Heavy }

    [Header("Setup")]
    [SerializeField] InjuryLevel injuryLevel = InjuryLevel.Light;
    [SerializeField] float followDistance = 2f;
    [SerializeField] float panicRadius = 3f;
    [SerializeField] float panicCooldown = 8f;

    public NetworkVariable<bool> IsRescued = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsPanicking = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    NavMeshAgent agent;
    Transform escortTarget;
    float panicTimer;

    public InjuryLevel Injury => injuryLevel;
    public bool CanWalk => injuryLevel == InjuryLevel.Light;

    public string InteractHint => injuryLevel == InjuryLevel.Light
        ? "安抚幸存者 (轻伤)"
        : "救援幸存者 (重伤)";

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) { agent.enabled = false; return; }
        agent.enabled = true;
        if (!CanWalk) agent.enabled = false;
    }

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (IsRescued.Value) return;

        UpdatePanic();
        if (CanWalk && !IsPanicking.Value) UpdateFollow();
    }

    void UpdateFollow()
    {
        if (escortTarget == null) return;
        if (!agent.isOnNavMesh) return;
        float dist = Vector3.Distance(transform.position, escortTarget.position);
        if (dist > followDistance)
            agent.SetDestination(escortTarget.position);
        else
            agent.ResetPath();
    }

    void UpdatePanic()
    {
        if (panicTimer > 0) { panicTimer -= Time.deltaTime; return; }

        bool robotNearby = Physics.CheckSphere(transform.position, panicRadius, LayerMask.GetMask("Robot"));
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
        if (!agent.isOnNavMesh) return;
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

    public void OnInteractStart(PlayerController player)
    {
        if (IsRescued.Value) return;
        if (injuryLevel == InjuryLevel.Light)
        {
            var netObj = player.GetComponent<NetworkObject>();
            if (netObj != null)
                CalmSurvivorServerRpc(new NetworkObjectReference(netObj));
        }
        else
        {
            RescueServerRpc();
        }
    }

    public void OnInteractEnd(PlayerController player) { }

    [ServerRpc(RequireOwnership = false)]
    public void CalmSurvivorServerRpc(NetworkObjectReference playerRef)
    {
        if (!CanWalk) return;
        if (!playerRef.TryGet(out NetworkObject playerNet)) return;
        escortTarget = playerNet.transform;
        IsPanicking.Value = false;
        CalmedClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void RescueServerRpc()
    {
        MarkRescued();
    }

    [ClientRpc]
    void CalmedClientRpc()
    {
        AudioManager.Instance?.PlaySurvivorCalm(transform.position);
    }

    public void MarkRescued()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        IsRescued.Value = true;
        agent.enabled = false;
        GameManager.Instance?.SurvivorRescued();
        HideRescuedClientRpc();
    }

    [ClientRpc]
    void HideRescuedClientRpc()
    {
        gameObject.SetActive(false);
    }
}
