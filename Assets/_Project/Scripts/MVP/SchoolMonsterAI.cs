using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NavMeshAgent))]
public class SchoolMonsterAI : NetworkBehaviour
{
    [Header("Detection")]
    [SerializeField] float detectionRadius = 10f;
    [SerializeField] float loseRadius = 16f;
    [SerializeField] float patrolSpeed = 1.8f;
    [SerializeField] float chaseSpeed = 4.2f;

    [Header("Attack")]
    [SerializeField] float attackRange = 1.4f;
    [SerializeField] float attackDamage = 25f;
    [SerializeField] float attackCooldown = 1.5f;

    [Header("Patrol")]
    [SerializeField] Transform[] patrolPoints;

    public enum MonsterState { Patrolling, Chasing, Stunned, Distracted }

    NetworkVariable<MonsterState> state = new(MonsterState.Patrolling,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    NavMeshAgent agent;
    Transform target;
    int patrolIndex;
    float nextAttackTime;
    float stunnedUntil;
    float distractedUntil;
    Vector3 distractionPosition;

    public bool IsChasing => state.Value == MonsterState.Chasing;
    public bool IsStunned => state.Value == MonsterState.Stunned;
    public bool IsDistracted => state.Value == MonsterState.Distracted;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            agent.enabled = false;
            return;
        }

        agent.speed = patrolSpeed;
        GoToNextPatrolPoint();
    }

    void Update()
    {
        if (!IsServer) return;

        switch (state.Value)
        {
            case MonsterState.Chasing:
                UpdateChase();
                break;
            case MonsterState.Stunned:
                UpdateStunned();
                break;
            case MonsterState.Distracted:
                UpdateDistracted();
                break;
            default:
                UpdatePatrol();
                break;
        }
    }

    void UpdatePatrol()
    {
        agent.speed = patrolSpeed;
        Transform nearest = FindNearestPlayer(detectionRadius);
        if (nearest != null)
        {
            target = nearest;
            state.Value = MonsterState.Chasing;
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0 && agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.5f)
            GoToNextPatrolPoint();
    }

    void UpdateChase()
    {
        if (target == null)
        {
            TryRetargetOrPatrol();
            return;
        }

        if (target.TryGetComponent<PlayerHealth>(out var targetHealth) && targetHealth.IsDowned.Value)
        {
            TryRetargetOrPatrol();
            return;
        }

        if (Vector3.Distance(transform.position, target.position) > loseRadius)
        {
            TryRetargetOrPatrol();
            return;
        }

        agent.speed = chaseSpeed;
        if (agent.isOnNavMesh)
            agent.SetDestination(target.position);

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackCooldown;
            if (target.TryGetComponent<PlayerHealth>(out var hitHealth))
                hitHealth.TakeDamage(attackDamage);
        }
    }

    void TryRetargetOrPatrol()
    {
        target = FindNearestPlayer(detectionRadius);
        if (target != null)
        {
            state.Value = MonsterState.Chasing;
            return;
        }

        state.Value = MonsterState.Patrolling;
        GoToNextPatrolPoint();
    }

    void UpdateStunned()
    {
        if (Time.time < stunnedUntil) return;
        agent.isStopped = false;
        state.Value = MonsterState.Patrolling;
        GoToNextPatrolPoint();
    }

    void UpdateDistracted()
    {
        agent.speed = patrolSpeed;
        if (agent.isOnNavMesh)
            agent.SetDestination(distractionPosition);

        if (Time.time < distractedUntil) return;
        state.Value = MonsterState.Patrolling;
        GoToNextPatrolPoint();
    }

    public void Stun(float duration)
    {
        if (!IsServer) return;
        stunnedUntil = Time.time + Mathf.Max(0.1f, duration);
        target = null;
        state.Value = MonsterState.Stunned;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    public void Distract(Vector3 position, float duration)
    {
        if (!IsServer) return;
        distractionPosition = position;
        distractedUntil = Time.time + Mathf.Max(0.1f, duration);
        target = null;
        state.Value = MonsterState.Distracted;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(distractionPosition);
        }
    }

    public static bool TryStunNearest(Vector3 position, float radius, float duration)
    {
        SchoolMonsterAI[] monsters = FindObjectsByType<SchoolMonsterAI>(FindObjectsSortMode.None);
        SchoolMonsterAI nearest = null;
        float nearestDistance = radius;

        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsServer) continue;
            float distance = Vector3.Distance(position, monster.transform.position);
            if (distance <= nearestDistance)
            {
                nearest = monster;
                nearestDistance = distance;
            }
        }

        if (nearest == null) return false;
        nearest.Stun(duration);
        return true;
    }

    public static bool TryDistractNearest(Vector3 position, float radius, float duration)
    {
        SchoolMonsterAI[] monsters = FindObjectsByType<SchoolMonsterAI>(FindObjectsSortMode.None);
        SchoolMonsterAI nearest = null;
        float nearestDistance = radius;

        foreach (var monster in monsters)
        {
            if (monster == null || !monster.IsServer) continue;
            float distance = Vector3.Distance(position, monster.transform.position);
            if (distance <= nearestDistance)
            {
                nearest = monster;
                nearestDistance = distance;
            }
        }

        if (nearest == null) return false;
        nearest.Distract(position, duration);
        return true;
    }

    Transform FindNearestPlayer(float radius)
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        Transform nearest = null;
        float nearestDistance = radius;

        foreach (var player in players)
        {
            if (player == null) continue;
            if (player.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value) continue;
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= nearestDistance)
            {
                nearest = player.transform;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0 || !agent.isOnNavMesh) return;
        agent.SetDestination(patrolPoints[patrolIndex].position);
        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }
}
