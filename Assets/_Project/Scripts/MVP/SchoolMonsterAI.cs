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
        EnsureVisualModel();
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

    void EnsureVisualModel()
    {
        if (transform.Find("MVP_MonsterVisualRoot") != null) return;

        Material coat = MakeVisualMaterial(new Color(0.44f, 0.03f, 0.025f));
        Material shirt = MakeVisualMaterial(new Color(0.08f, 0.075f, 0.065f));
        Material paper = MakeVisualMaterial(new Color(0.86f, 0.82f, 0.68f));
        Material skin = MakeVisualMaterial(new Color(0.5f, 0.45f, 0.38f));
        Material eye = MakeVisualMaterial(new Color(0.95f, 0.08f, 0.04f));

        var root = new GameObject("MVP_MonsterVisualRoot");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = new Vector3(0f, -0.65f, 0f);

        CreateVisualPrimitive(PrimitiveType.Cube, "DebtCollector_Coat", root.transform,
            new Vector3(0f, 0.95f, 0f), new Vector3(0.8f, 1.45f, 0.45f),
            Quaternion.identity, coat);
        CreateVisualPrimitive(PrimitiveType.Cube, "DebtCollector_Shirt", root.transform,
            new Vector3(0f, 1.05f, -0.24f), new Vector3(0.42f, 0.95f, 0.05f),
            Quaternion.identity, shirt);
        CreateVisualPrimitive(PrimitiveType.Sphere, "DebtCollector_Head", root.transform,
            new Vector3(0f, 1.85f, -0.02f), new Vector3(0.52f, 0.58f, 0.48f),
            Quaternion.identity, skin);
        CreateVisualPrimitive(PrimitiveType.Cube, "DebtCollector_LeftArm", root.transform,
            new Vector3(-0.55f, 1.02f, -0.05f), new Vector3(0.16f, 1.2f, 0.16f),
            Quaternion.Euler(0f, 0f, 12f), coat);
        CreateVisualPrimitive(PrimitiveType.Cube, "DebtCollector_RightArm", root.transform,
            new Vector3(0.55f, 1.02f, -0.05f), new Vector3(0.16f, 1.2f, 0.16f),
            Quaternion.Euler(0f, 0f, -12f), coat);
        CreateVisualPrimitive(PrimitiveType.Cube, "OverdueLedger", root.transform,
            new Vector3(0.2f, 1.08f, -0.34f), new Vector3(0.42f, 0.28f, 0.04f),
            Quaternion.Euler(8f, -8f, 0f), paper);
        CreateVisualPrimitive(PrimitiveType.Sphere, "LeftEye", root.transform,
            new Vector3(-0.11f, 1.9f, -0.25f), new Vector3(0.07f, 0.04f, 0.03f),
            Quaternion.identity, eye);
        CreateVisualPrimitive(PrimitiveType.Sphere, "RightEye", root.transform,
            new Vector3(0.11f, 1.9f, -0.25f), new Vector3(0.07f, 0.04f, 0.03f),
            Quaternion.identity, eye);

        var eyeLight = new GameObject("EyeWarningLight");
        eyeLight.transform.SetParent(root.transform, false);
        eyeLight.transform.localPosition = new Vector3(0f, 1.9f, -0.38f);
        var light = eyeLight.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.08f, 0.04f);
        light.range = 2.5f;
        light.intensity = 0.9f;
    }

    GameObject CreateVisualPrimitive(PrimitiveType type, string name, Transform parent,
        Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;
        if (go.TryGetComponent<Collider>(out var collider))
            Destroy(collider);
        if (go.TryGetComponent<Renderer>(out var renderer))
            renderer.sharedMaterial = material;
        return go;
    }

    static Material MakeVisualMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
            ?? Shader.Find("Standard");
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        return material;
    }
}
