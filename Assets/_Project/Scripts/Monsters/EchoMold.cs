using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Echo Mold (回声菌) — host-authoritative voice-mimicry fungal hunter.
/// Design: <c>design/gdd/monster-echo-mold.md</c>.
///
/// <para><b>First implementation pass (this file).</b> Delivers the testable core:</para>
/// <list type="bullet">
///   <item>A <see cref="State"/> machine — Roam / Lure / Hunt / Attack / Dead — that
///   ticks only on the authority.</item>
///   <item>"Attention" target selection from each player's synced movement tier
///   (<see cref="PlayerController.NetworkMoveSpeed"/>: idle / crouch / walk / sprint) —
///   the GDD's <c>w_run</c> / <c>w_walk</c> terms. The <c>w_voice</c> term (real VOIP
///   capture + spatialized replay) is the GDD's flagged technical spike (Open Q#1,
///   MEDIUM risk) and is intentionally left as a clean seam — see
///   <see cref="VoiceActivity"/> and <see cref="BroadcastLure"/>.</item>
///   <item>NavMesh movement; contact damage routed through
///   <see cref="PlayerHealth.TakeDamage"/> (which owns the down / failure truth).</item>
///   <item>Drives an <see cref="Animator"/> (EM_Idle / EM_Hunt / EM_Attack / EM_Death)
///   and syncs the pose to clients via <see cref="PoseState"/> so every peer sees the
///   same deceptive-idle ↔ open-hunt reveal (no client decides its own pose).</item>
/// </list>
///
/// <para>Authority mirrors the project idiom: runs on the server, or locally when
/// no <see cref="NetworkManager"/> is listening (offline PreviewWalker walkthroughs),
/// so the creature is testable both in a hosted session and in offline preview.</para>
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkObject))]
public class EchoMold : NetworkBehaviour
{
    public enum State { Roam = 0, Lure = 1, Hunt = 2, Attack = 3, Dead = 4 }

    [Header("Attention weights (GDD §Formulas — systems-designer tuning pending)")]
    [Tooltip("w_voice — talking on the radio (real VOIP capture is the deferred spike; 0 until wired).")]
    [SerializeField] float wVoice = 1.0f;
    [Tooltip("w_run — sprinting players.")]
    [SerializeField] float wRun = 0.5f;
    [Tooltip("w_walk — walking players.")]
    [SerializeField] float wWalk = 0.15f;

    [Header("Ranges (metres)")]
    [Tooltip("Players beyond this are ignored entirely.")]
    [SerializeField] float senseRadius = 22f;
    [Tooltip("A nearby attentive player within this range escalates Roam/Lure → Hunt.")]
    [SerializeField] float huntTriggerRange = 14f;
    [SerializeField] float attackRange = 1.5f;
    [Tooltip("Distance past which a Hunt target is considered fully lost → Roam.")]
    [SerializeField] float loseTargetRange = 26f;
    [Tooltip("Seconds with no attention/contact before Hunt gives up.")]
    [SerializeField] float loseTargetGrace = 5f;

    [Header("Movement (GDD: moveSpeed slightly below player walk = 4)")]
    [SerializeField] float roamSpeed = 1.2f;
    [SerializeField] float huntSpeed = 3.2f;
    [SerializeField] float roamRepathInterval = 5f;
    [SerializeField] float roamRadius = 10f;

    [Header("Lure (deceptive broadcast — VOIP replay deferred, Open Q#1)")]
    [SerializeField] float lureInterval = 25f;
    [SerializeField] float decoyMinDist = 12f;
    [Tooltip("How long it sits at a decoy point baiting before falling back to Roam.")]
    [SerializeField] float lureHoldSeconds = 8f;

    [Header("Damage (references PlayerHealth; GDD §Formulas)")]
    [SerializeField] float dmgPerTick = 8f;
    [SerializeField] float dmgTick = 0.6f;

    /// <summary>Host-authoritative pose so clients animate the reveal identically.</summary>
    public NetworkVariable<int> PoseState = new((int)State.Roam,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    static readonly int PoseParam = Animator.StringToHash("Pose");
    static readonly int SpeedParam = Animator.StringToHash("Speed");

    NavMeshAgent agent;
    Animator animator;
    State state = State.Roam;
    Vector3 lastAnimPos;

    PlayerController target;
    float nextRoamRepath;
    float nextLureTime;
    float lureUntil;
    float damageTimer;
    float lastContactTime;
    float playerScanTimer;

    readonly List<PlayerController> players = new();

    /// <summary>Server, or offline (no live session) — the project's authority idiom.</summary>
    bool HasAuthority =>
        IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = roamSpeed;
            agent.autoBraking = true;
        }
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        lastAnimPos = transform.position;
    }

    /// <summary>Feeds horizontal world speed into the Animator's Speed float on every peer.
    /// Clients move via NetworkTransform (no live agent velocity there), so speed is derived
    /// from the transform delta; drives the Idle↔Walk locomotion blend for humanoid rigs.</summary>
    void DriveLocomotionAnim()
    {
        if (animator == null) return;
        Vector3 pos = transform.position;
        float speed = Mathf.Min(Vector3.Distance(pos, lastAnimPos) / Mathf.Max(Time.deltaTime, 0.0001f), 5f);
        lastAnimPos = pos;
        animator.SetFloat(SpeedParam, speed);
    }

    public override void OnNetworkSpawn()
    {
        PoseState.OnValueChanged += OnPoseChanged;
        ApplyPose((State)PoseState.Value);
        if (HasAuthority) BeginAuthority();
    }

    public override void OnNetworkDespawn()
    {
        PoseState.OnValueChanged -= OnPoseChanged;
        EndAuthority();
    }

    // Offline preview path: OnNetworkSpawn never fires when no NetworkManager is listening.
    void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            BeginAuthority();
            ApplyPose(state);
        }
    }

    void OnDestroy() => EndAuthority();

    bool authorityWired;

    void BeginAuthority()
    {
        if (authorityWired) return;
        authorityWired = true;
        nextLureTime = Time.time + lureInterval;
        SetState(State.Roam);
    }

    void EndAuthority()
    {
        if (!authorityWired) return;
        authorityWired = false;
    }

    void Update()
    {
        DriveLocomotionAnim(); // all peers — before the authority gate
        if (!HasAuthority || state == State.Dead) return;
        if (agent == null || !agent.isOnNavMesh) return;

        playerScanTimer -= Time.deltaTime;
        if (playerScanTimer <= 0f) { RescanPlayers(); playerScanTimer = 1f; }

        switch (state)
        {
            case State.Roam: TickRoam(); break;
            case State.Lure: TickLure(); break;
            case State.Hunt: TickHunt(); break;
            case State.Attack: TickAttack(); break;
        }
    }

    // ─── States ────────────────────────────────────────────────────────────

    void TickRoam()
    {
        agent.speed = roamSpeed;

        if (PickTarget(out PlayerController p, out float dist) && dist <= huntTriggerRange)
        {
            StartHunt(p);
            return;
        }

        if (Time.time >= nextLureTime && AnyAttentiveListener())
        {
            StartLure();
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.25f
            && Time.time >= nextRoamRepath)
        {
            nextRoamRepath = Time.time + roamRepathInterval;
            if (RandomNavPoint(transform.position, roamRadius, out Vector3 dest))
                agent.SetDestination(dest);
        }
    }

    void StartLure()
    {
        SetState(State.Lure);
        lureUntil = Time.time + lureHoldSeconds;
        nextLureTime = Time.time + lureInterval;

        // Move AWAY to a decoy point and "throw" a teammate's voice from there. The real
        // spatialized VOIP replay is the deferred spike (Open Q#1); for now we relocate
        // and fire the seam so audio/telemetry can hook in later.
        Vector3 decoy = transform.position;
        if (target != null)
        {
            Vector3 away = (transform.position - target.transform.position).normalized;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitSphere;
            away.y = 0f;
            RandomNavPoint(transform.position + away.normalized * decoyMinDist, 4f, out decoy);
        }
        else if (!RandomNavPoint(transform.position, roamRadius, out decoy))
        {
            decoy = transform.position;
        }
        agent.speed = huntSpeed;
        agent.SetDestination(decoy);
        BroadcastLure(decoy);
    }

    void TickLure()
    {
        // Bait broke: someone wandered close → drop the act and Hunt.
        if (PickTarget(out PlayerController p, out float dist) && dist <= huntTriggerRange)
        {
            StartHunt(p);
            return;
        }
        if (Time.time >= lureUntil)
            SetState(State.Roam);
    }

    void StartHunt(PlayerController p)
    {
        target = p;
        lastContactTime = Time.time;
        SetState(State.Hunt);
    }

    void TickHunt()
    {
        if (!TargetValid(target))
        {
            // Re-acquire if someone else is making noise; otherwise give up.
            if (PickTarget(out PlayerController p, out _)) target = p;
            else { GiveUp(); return; }
        }

        agent.speed = huntSpeed;
        agent.SetDestination(target.transform.position);

        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist <= attackRange)
        {
            SetState(State.Attack);
            damageTimer = 0f;
            return;
        }

        if (AttentionOf(target) > 0f || dist <= huntTriggerRange) lastContactTime = Time.time;
        if (dist > loseTargetRange && Time.time - lastContactTime > loseTargetGrace)
            GiveUp();
    }

    void TickAttack()
    {
        if (!TargetValid(target)) { GiveUp(); return; }

        float dist = Vector3.Distance(transform.position, target.transform.position);
        if (dist > attackRange * 1.4f)   // hysteresis so it doesn't flicker at the edge
        {
            SetState(State.Hunt);
            return;
        }

        // Hold position and face the prey.
        agent.SetDestination(transform.position);
        Vector3 look = target.transform.position - transform.position; look.y = 0f;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(look), 8f * Time.deltaTime);

        damageTimer += Time.deltaTime;
        if (damageTimer >= dmgTick)
        {
            damageTimer -= dmgTick;
            if (target.TryGetComponent(out PlayerHealth hp))
                hp.TakeDamage(dmgPerTick);    // server-only inside; no-op offline (no real players)
        }

        if (target.TryGetComponent(out PlayerHealth h) && h.IsDowned.Value)
            GiveUp();   // dropped the prey → back to roaming / luring the rest
    }

    void GiveUp()
    {
        target = null;
        SetState(State.Roam);
        nextRoamRepath = 0f;
    }

    /// <summary>Authority-only: kill the creature and play the death pose (future: spores burst).</summary>
    public void Kill()
    {
        if (!HasAuthority || state == State.Dead) return;
        target = null;
        if (agent != null && agent.isOnNavMesh) agent.isStopped = true;
        SetState(State.Dead);
    }

    // ─── Attention / targeting ──────────────────────────────────────────────

    void RescanPlayers()
    {
        players.Clear();
        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var pc in all) players.Add(pc);
    }

    bool PickTarget(out PlayerController best, out float bestDist)
    {
        best = null;
        bestDist = float.MaxValue;
        float bestScore = 0f;
        foreach (var pc in players)
        {
            if (!TargetValid(pc)) continue;
            float dist = Vector3.Distance(transform.position, pc.transform.position);
            if (dist > senseRadius) continue;
            float score = AttentionOf(pc);
            if (score <= 0f) continue;
            // Prefer the loudest; break ties by proximity.
            if (score > bestScore || (Mathf.Approximately(score, bestScore) && dist < bestDist))
            {
                bestScore = score;
                best = pc;
                bestDist = dist;
            }
        }
        return best != null;
    }

    /// <summary>GDD §Formulas: attention(p) = w_voice·voice + w_run·run + w_walk·walk.</summary>
    float AttentionOf(PlayerController pc)
    {
        float tier = pc.NetworkMoveSpeed.Value;       // 0 idle / 0.25 crouch / 0.5 walk / 1 sprint
        bool running = tier >= 0.9f;
        bool walking = tier >= 0.4f && tier < 0.9f;   // crouch (0.25) reads as ~silent
        float score = wRun * (running ? 1f : 0f) + wWalk * (walking ? 1f : 0f);
        score += wVoice * VoiceActivity(pc);
        return score;
    }

    /// <summary>
    /// SEAM (GDD Open Q#1 — deferred spike): real per-player VOIP transmit state, sampled
    /// server-side. <c>ProximityVoiceChat</c> relays every packet through the host
    /// (<c>HandleUplinkVoiceMessage(senderClientId,…)</c>), so a server-side "last spoke at"
    /// probe can feed this without the client deciding anything. Returns 0 until that probe
    /// is wired so the first pass hunts purely on movement noise.
    /// </summary>
    float VoiceActivity(PlayerController pc) => 0f;

    bool AnyAttentiveListener()
    {
        foreach (var pc in players)
            if (TargetValid(pc) && Vector3.Distance(transform.position, pc.transform.position) <= senseRadius
                && AttentionOf(pc) > 0f)
                return true;
        return false;
    }

    static bool TargetValid(PlayerController pc)
    {
        if (pc == null) return false;
        if (pc.IsHiddenFromMonsters) return false;
        if (pc.TryGetComponent(out PlayerHealth hp) && hp.IsDowned.Value) return false;
        return true;
    }

    /// <summary>
    /// Samples a random point within <paramref name="radius"/> of <paramref name="origin"/>
    /// and snaps it onto the NavMesh. Returns false (with <paramref name="result"/> = origin)
    /// if no navigable point is found after a few attempts, so callers can skip the move.
    /// </summary>
    static bool RandomNavPoint(Vector3 origin, float radius, out Vector3 result)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 candidate = origin + Random.insideUnitSphere * radius;
            candidate.y = origin.y;
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = origin;
        return false;
    }

    // ─── Lure broadcast seam ────────────────────────────────────────────────

    /// <summary>
    /// SEAM (GDD §4 + Open Q#1): replay a captured teammate clip (or a fallback line) from
    /// <paramref name="decoyPoint"/> via <c>ProximityVoiceChat</c>'s spatialized pipeline,
    /// passed through the learnable distortion <c>tell</c>. Stubbed for the first pass.
    /// </summary>
    void BroadcastLure(Vector3 decoyPoint)
    {
        // TODO(Open Q#1 spike): host picks a clip + player voice, plays it at decoyPoint on
        // all peers through the VOIP replay path with the distortion filter (the `tell`).
    }

    // ─── Pose sync / animation ──────────────────────────────────────────────

    void SetState(State next)
    {
        state = next;
        if (IsSpawned && IsServer) PoseState.Value = (int)next;
        ApplyPose(next);   // authority/offline see it immediately; clients via OnPoseChanged
    }

    void OnPoseChanged(int _, int next) => ApplyPose((State)next);

    void ApplyPose(State s)
    {
        if (animator != null) animator.SetInteger(PoseParam, (int)s);
    }
}
