using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Cleaning robot AI. Patrols corridors, reacts to sound, tries to "clean" players
/// and fallen survivors (i.e. shoves/drags them). Server-authoritative.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkObject))]
public class CleaningRobot : NetworkBehaviour
{
    [Header("Patrol")]
    [SerializeField] Transform[] patrolPoints;
    [SerializeField] float patrolWaitTime = 2f;

    [Header("Detection")]
    [SerializeField] float hearingRange = 8f;      // react to loud sounds
    [SerializeField] float sightRange = 5f;        // direct line of sight chase
    [SerializeField] float chaseSpeed = 4f;
    [SerializeField] float patrolSpeed = 2f;
    [SerializeField] LayerMask playerLayer;

    [Header("Attack")]
    [SerializeField] float ramForce = 8f;
    [SerializeField] float ramDamage = 30f;
    [SerializeField] float stunSelf = 1.5f;

    enum RobotState { Patrolling, Alerted, Chasing, Ramming, Stunned }
    NetworkVariable<RobotState> state = new(RobotState.Patrolling,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    NavMeshAgent agent;
    Transform chaseTarget;
    int patrolIndex;
    float stateTimer;
    float currentStunDuration;

    // Angry phase multiplier — set by GameManager as phase increases
    public NetworkVariable<float> AggressionMultiplier = new(1f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake() => agent = GetComponent<NavMeshAgent>();

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) { agent.enabled = false; return; }
        // Delay first patrol tick so NavMesh has time to init
        Invoke(nameof(GoToNextPatrolPoint), 0.5f);
    }

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        stateTimer += Time.deltaTime;

        switch (state.Value)
        {
            case RobotState.Patrolling: UpdatePatrol(); break;
            case RobotState.Alerted:   UpdateAlerted(); break;
            case RobotState.Chasing:   UpdateChase(); break;
            case RobotState.Stunned:   UpdateStunned(); break;
        }
    }

    void UpdatePatrol()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (stateTimer > patrolWaitTime)
            {
                GoToNextPatrolPoint();
                stateTimer = 0;
            }
        }

        // Listen for nearby players
        var hit = Physics.OverlapSphere(transform.position, hearingRange, playerLayer);
        if (hit.Length > 0)
            EnterAlert(hit[0].transform);
    }

    void UpdateAlerted()
    {
        if (chaseTarget == null) { SetState(RobotState.Patrolling); return; }
        if (!agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, chaseTarget.position);
        if (dist < sightRange)
        {
            SetState(RobotState.Chasing);
            agent.speed = chaseSpeed * AggressionMultiplier.Value;
        }
        else if (stateTimer > 5f)
        {
            SetState(RobotState.Patrolling);
        }

        agent.SetDestination(chaseTarget.position);
    }

    void UpdateChase()
    {
        if (chaseTarget == null) { SetState(RobotState.Patrolling); return; }
        if (!agent.isOnNavMesh) return;

        agent.SetDestination(chaseTarget.position);
        float dist = Vector3.Distance(transform.position, chaseTarget.position);

        // Ram when close enough
        if (dist < 1.2f)
        {
            StartCoroutine(Ram(chaseTarget));
        }
        else if (dist > hearingRange * 1.5f)
        {
            SetState(RobotState.Patrolling);
        }
    }

    void UpdateStunned()
    {
        if (stateTimer > currentStunDuration)
        {
            SetState(RobotState.Patrolling);
            agent.isStopped = false;
        }
    }

    public void ApplyFlashlightStun(float duration)
    {
        if (!IsServer) return;
        if (state.Value == RobotState.Stunned) return;
        currentStunDuration = duration / AggressionMultiplier.Value;
        agent.isStopped = true;
        SetState(RobotState.Stunned);
    }

    IEnumerator Ram(Transform target)
    {
        SetState(RobotState.Ramming);
        agent.isStopped = true;

        if (target.TryGetComponent<NetworkObject>(out var netObj))
            RamPlayerClientRpc(new NetworkObjectReference(netObj));

        if (target.TryGetComponent<PlayerHealth>(out var ph))
            ph.TakeDamage(ramDamage);

        yield return new WaitForSeconds(0.3f);
        currentStunDuration = stunSelf;
        SetState(RobotState.Stunned);
        stateTimer = 0;

        PlayRamAudioClientRpc();
    }

    [ClientRpc]
    void RamPlayerClientRpc(NetworkObjectReference playerRef)
    {
        if (!playerRef.TryGet(out NetworkObject netObj)) return;
        if (netObj.TryGetComponent<CharacterController>(out _)) return; // handled by physics
        if (netObj.TryGetComponent<Rigidbody>(out var rb))
            rb.AddForce((netObj.transform.position - transform.position).normalized * ramForce, ForceMode.Impulse);
    }

    [ClientRpc]
    void PlayRamAudioClientRpc()
    {
        // Audio system plays robot ram sound + random dialogue line
        AudioManager.Instance?.PlayRobotDialogue(transform.position);
    }

    void EnterAlert(Transform target)
    {
        chaseTarget = target;
        SetState(RobotState.Alerted);
    }

    void SetState(RobotState newState)
    {
        state.Value = newState;
        stateTimer = 0;
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        if (!agent.isOnNavMesh) return;
        agent.speed = patrolSpeed;
        agent.SetDestination(patrolPoints[patrolIndex].position);
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    // Called by noisy events (drop, run, interact)
    public void AlertToSound(Vector3 soundPosition, float volume)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        float dist = Vector3.Distance(transform.position, soundPosition);
        if (dist < hearingRange * volume && state.Value == RobotState.Patrolling)
        {
            agent.SetDestination(soundPosition);
            SetState(RobotState.Alerted);
        }
    }
}
