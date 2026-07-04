using System.Collections.Generic;
using BlackCommission.Monsters;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Civic Idol (市政圣像) — a beatified municipal statue that only moves while
/// unobserved. Design: <c>design/gdd/monster-civic-idol.md</c>.
///
/// <para>The freeze-when-watched rule is the public-domain "living statue" genre
/// archetype; the rules, numbers, code, and re-skinned CC0 visual here are Black
/// Commission's own. Server-authoritative like every monster brain:</para>
/// <list type="bullet">
///   <item><b>Watched</b> = any living player's horizontal view cone contains the idol
///   AND a physics line of sight exists (shelves/walls break it). Camera pitch is
///   owner-local and never synced, so the cone is judged from body yaw only —
///   deliberately generous to players. The cone math is the pure, EditMode-tested
///   <see cref="IdolGazeLogic"/>; this component layers the raycast on top.</item>
///   <item><b>Frozen</b> (any watcher, plus a short unfreeze grace against raycast
///   flicker) stops the NavMeshAgent and pauses the Animator on every peer via a
///   server-written <see cref="NetworkVariable{T}"/> — all clients see the statue
///   halt mid-stride in the same pose. A frozen idol never deals damage: one
///   teammate staring holds it harmless while the others work.</item>
///   <item>Unwatched, it stalks the nearest valid player fast, and swings heavy
///   (<see cref="PlayerHealth.TakeDamage"/>) once in reach.</item>
/// </list>
///
/// <para>Authority mirrors the project idiom: runs on the server, or locally when no
/// <see cref="NetworkManager"/> is listening (offline PreviewWalker walkthroughs).</para>
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkObject))]
public class CivicIdol : NetworkBehaviour
{
    public enum State { Dormant = 0, Stalk = 1, Attack = 2, Dead = 3 }

    [Header("Gaze (freeze-when-watched — IdolGazeLogic is the tested core)")]
    [Tooltip("A player farther than this cannot hold it frozen (and it stays dormant anyway).")]
    [SerializeField] float watchMaxRange = 45f;
    [Tooltip("Half-angle of the horizontal watch cone. ~50° ≈ on-screen for a 16:9 first-person camera.")]
    [SerializeField] float viewHalfAngleDeg = 50f;
    [Tooltip("Seconds it stays frozen after the last watcher looks away (raycast-flicker guard).")]
    [SerializeField] float unfreezeGrace = 0.35f;
    [SerializeField] float gazeScanInterval = 0.1f;
    [Tooltip("Watcher eye height above the player root (standing camera estimate).")]
    [SerializeField] float eyeHeight = 1.55f;
    [Tooltip("Line-of-sight probe point above the idol root.")]
    [SerializeField] float headHeight = 1.5f;

    [Header("Hunt")]
    [Tooltip("A valid player inside this range wakes the statue (Dormant → Stalk).")]
    [SerializeField] float senseRadius = 25f;
    [Tooltip("Target farther than this is dropped → back to Dormant.")]
    [SerializeField] float loseRadius = 32f;
    [Tooltip("Unwatched stalk speed — above player walk (4), it closes distance in glances.")]
    [SerializeField] float stalkSpeed = 4.6f;

    [Header("Damage (heavy melee — GDD §Formulas)")]
    [SerializeField] float attackRange = 1.7f;
    [SerializeField] float dmgPerHit = 40f;
    [SerializeField] float hitInterval = 0.8f;

    /// <summary>Host-authoritative pose so clients animate the reveal identically.</summary>
    public NetworkVariable<int> PoseState = new((int)State.Dormant,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Host-authoritative freeze flag — every peer pauses the Animator on it.</summary>
    public NetworkVariable<bool> Frozen = new(true,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    static readonly int PoseParam = Animator.StringToHash("Pose");

    NavMeshAgent agent;
    Animator animator;
    Light eyeLight;
    State state = State.Dormant;

    PlayerController target;
    float lastWatchedTime = -999f;
    bool watchedNow;
    float gazeTimer;
    float playerScanTimer;
    float hitTimer;
    bool frozen = true;

    readonly List<PlayerController> players = new();

    /// <summary>Server, or offline (no live session) — the project's authority idiom.</summary>
    bool HasAuthority =>
        IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = stalkSpeed;
            agent.angularSpeed = 720f;   // snap-turns are free: no one ever sees it turn
            agent.acceleration = 48f;
            agent.autoBraking = true;
        }
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        foreach (var l in GetComponentsInChildren<Light>(true))
            if (l.name == "SealEyes") { eyeLight = l; break; }
    }

    public override void OnNetworkSpawn()
    {
        PoseState.OnValueChanged += OnPoseChanged;
        Frozen.OnValueChanged += OnFrozenChanged;
        ApplyPose((State)PoseState.Value);
        ApplyFrozen(Frozen.Value);
    }

    public override void OnNetworkDespawn()
    {
        PoseState.OnValueChanged -= OnPoseChanged;
        Frozen.OnValueChanged -= OnFrozenChanged;
    }

    // Offline preview path: OnNetworkSpawn never fires when no NetworkManager is listening.
    void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            ApplyPose(state);
            ApplyFrozen(frozen);
        }
    }

    void Update()
    {
        if (!HasAuthority || state == State.Dead) return;
        if (agent == null || !agent.isOnNavMesh) return;

        playerScanTimer -= Time.deltaTime;
        if (playerScanTimer <= 0f) { RescanPlayers(); playerScanTimer = 1f; }

        gazeTimer -= Time.deltaTime;
        if (gazeTimer <= 0f)
        {
            gazeTimer = gazeScanInterval;
            watchedNow = AnyWatcher();
            if (watchedNow) lastWatchedTime = Time.time;
        }
        SetFrozen(IdolGazeLogic.ShouldFreeze(watchedNow, lastWatchedTime, Time.time, unfreezeGrace));

        agent.isStopped = frozen;
        if (frozen && agent.isOnNavMesh) agent.velocity = Vector3.zero;

        switch (state)
        {
            case State.Dormant: TickDormant(); break;
            case State.Stalk: TickStalk(); break;
            case State.Attack: TickAttack(); break;
        }
    }

    // ─── States ────────────────────────────────────────────────────────────

    void TickDormant()
    {
        // Waking is allowed while watched — the eye-light snapping on under a
        // player's stare is the "it noticed you" beat; it still cannot move.
        if (PickNearestTarget(out PlayerController p, out float dist) && dist <= senseRadius)
        {
            target = p;
            SetState(State.Stalk);
            // Bestiary: the eye-light "it noticed you" beat is the encounter (host/solo local).
            MonsterBestiaryProgress.MarkEncountered(MonsterBestiaryProgress.CivicIdol);
        }
    }

    void TickStalk()
    {
        if (!TargetValid(target) && !PickNearestTarget(out target, out _))
        {
            GoDormant();
            return;
        }

        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist > loseRadius) { GoDormant(); return; }

        if (dist <= attackRange)
        {
            hitTimer = 0f;
            SetState(State.Attack);
            return;
        }

        if (!frozen) agent.SetDestination(target.transform.position);
    }

    void TickAttack()
    {
        if (!TargetValid(target)) { GoDormant(); return; }

        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist > attackRange * 1.4f)   // hysteresis so it doesn't flicker at the edge
        {
            SetState(State.Stalk);
            return;
        }

        if (frozen) return;   // a watched statue is harmless — someone staring saves you

        agent.SetDestination(transform.position);
        Vector3 look = target.transform.position - transform.position; look.y = 0f;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(look), 12f * Time.deltaTime);

        hitTimer += Time.deltaTime;
        if (hitTimer >= hitInterval)
        {
            hitTimer -= hitInterval;
            if (target.TryGetComponent(out PlayerHealth hp))
            {
                hp.TakeDamage(dmgPerHit);   // server-only inside; no-op offline
                if (hp.IsDowned.Value)
                    GoDormant();   // dropped the prey → stand still until the next visitor
            }
        }
    }

    void GoDormant()
    {
        target = null;
        SetState(State.Dormant);
    }

    /// <summary>Authority-only: kill the statue (plays the crumble/death clip).</summary>
    public void Kill()
    {
        if (!HasAuthority || state == State.Dead) return;
        target = null;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        SetState(State.Dead);
        SetFrozen(false);   // let the death clip play
    }

    // ─── Gaze ───────────────────────────────────────────────────────────────

    /// <summary>
    /// True if any living player currently pins the idol: horizontal view cone
    /// (pure <see cref="IdolGazeLogic"/>) + unobstructed line of sight. Hidden
    /// players still count as watchers (a locker slat is a window); downed ones
    /// don't — their camera is on the floor.
    /// </summary>
    bool AnyWatcher()
    {
        Vector3 head = transform.position + Vector3.up * headHeight;
        foreach (var pc in players)
        {
            if (pc == null) continue;
            if (pc.TryGetComponent(out PlayerHealth hp) && hp.IsDowned.Value) continue;

            Vector3 eye = pc.transform.position + Vector3.up * eyeHeight;
            Vector3 fwd = pc.transform.forward;
            if (!IdolGazeLogic.IsWithinViewCone(eye.x, eye.z, fwd.x, fwd.z,
                    head.x, head.z, watchMaxRange, viewHalfAngleDeg)) continue;
            if (HasLineOfSight(eye, head)) return true;
        }
        return false;
    }

    /// <summary>Player bodies and monsters never block sight; level geometry does.</summary>
    bool HasLineOfSight(Vector3 eye, Vector3 head)
    {
        Vector3 dir = head - eye;
        float dist = dir.magnitude;
        if (dist < 0.5f) return true;
        var hits = Physics.RaycastAll(eye, dir / dist, dist - 0.4f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.collider.transform.root == transform.root) continue;
            if (h.collider.GetComponentInParent<PlayerController>() != null) continue;
            if (h.collider.GetComponentInParent<CivicIdol>() != null) continue;
            if (h.collider.GetComponentInParent<EchoMold>() != null) continue;
            return false;
        }
        return true;
    }

    // ─── Targeting ──────────────────────────────────────────────────────────

    void RescanPlayers()
    {
        players.Clear();
        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in all) players.Add(pc);
    }

    bool PickNearestTarget(out PlayerController best, out float bestDist)
    {
        best = null;
        bestDist = float.MaxValue;
        foreach (var pc in players)
        {
            if (!TargetValid(pc)) continue;
            float dist = Vector3.Distance(transform.position, pc.transform.position);
            if (dist < bestDist)
            {
                best = pc;
                bestDist = dist;
            }
        }
        return best != null;
    }

    static bool TargetValid(PlayerController pc)
    {
        if (pc == null) return false;
        if (pc.IsHiddenFromMonsters) return false;
        if (pc.TryGetComponent(out PlayerHealth hp) && hp.IsDowned.Value) return false;
        return true;
    }

    // ─── Pose / freeze sync ─────────────────────────────────────────────────

    void SetState(State next)
    {
        state = next;
        if (IsSpawned && IsServer) PoseState.Value = (int)next;
        ApplyPose(next);   // authority/offline see it immediately; clients via OnPoseChanged
    }

    void SetFrozen(bool next)
    {
        if (frozen == next) return;
        frozen = next;
        if (IsSpawned && IsServer) Frozen.Value = next;
        ApplyFrozen(next);
    }

    void OnPoseChanged(int _, int next) => ApplyPose((State)next);
    void OnFrozenChanged(bool _, bool next) => ApplyFrozen(next);

    void ApplyPose(State s)
    {
        if (animator != null) animator.SetInteger(PoseParam, (int)s);
        // Stamp-red eyes burn while it has prey — the project's threat-telegraph language.
        if (eyeLight != null) eyeLight.enabled = s == State.Stalk || s == State.Attack;
    }

    void ApplyFrozen(bool f)
    {
        if (animator != null) animator.speed = f ? 0f : 1f;
    }
}
