using Unity.Netcode;
using UnityEngine;
using BlackCommission.Scavenge;

/// <summary>
/// The van's shared cargo bay on a scavenging mission site — the deposit gate of the
/// scavenging loop (quick-spec §3). A player sets a <see cref="ScavengeItem"/> DOWN inside this
/// trigger volume (carrying it next to the van does not count, exactly like the tower's eco-
/// column <c>VAN_CargoZone</c>); the host then tries to load it through the shared
/// <see cref="VanCargoManifest"/>. If the item's weight fits the remaining capacity it is
/// stowed (recorded for settlement + despawned from the world); if the van is full, the item
/// is left on the ground and the team must decide what to leave behind — the weight limit is
/// the loop's primary tension lever, not a timer.
///
/// Server-authoritative: only the host scans, loads, and despawns. The current load is
/// replicated (<see cref="LoadUnits"/>/<see cref="Capacity"/>/<see cref="ItemCount"/>) so every
/// peer can draw the cargo ticket-strip, and a late joiner sees the correct count immediately.
/// The recorded manifest lives host-side and feeds settlement via <see cref="SettleCargo"/>.
///
/// Scene wiring (run by a level/editor tool, not committed into a scene here): one GameObject
/// with a trigger BoxCollider sized to the open cargo bed + a NetworkObject + this component.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class ScavengeCargoZone : NetworkBehaviour
{
    [Header("Bay")]
    [Tooltip("Trigger volume of the open cargo bed. Defaults to a BoxCollider on this object.")]
    [SerializeField] BoxCollider zone;

    [Tooltip("Shared capacity in weight units; -1 reads ScavengingConfig.vanWeightCapacity (spec 12).")]
    [SerializeField] int capacityOverride = -1;

    [Tooltip("How often (seconds) the host scans the bay for set-down items.")]
    [SerializeField] float scanInterval = 0.25f;

    /// <summary>Units currently loaded — replicated for the cargo ticket-strip.</summary>
    public NetworkVariable<int> LoadUnits = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Shared capacity in units — replicated so clients can render remaining slots.</summary>
    public NetworkVariable<int> Capacity = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Number of items stowed — replicated for the ticket-strip count.</summary>
    public NetworkVariable<int> ItemCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Scene-singleton: non-null means a scavenging cargo bay is active in the loaded scene.</summary>
    public static ScavengeCargoZone Instance { get; private set; }

    VanCargoManifest manifest;
    float scanTimer;
    GUIStyle stripStyle;

    bool HasAuthority =>
        IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

    bool IsNetworked =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

    void Awake()
    {
        if (zone == null) zone = GetComponent<BoxCollider>();
    }

    void OnEnable() => Instance = this;

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    public override void OnNetworkSpawn()
    {
        if (!HasAuthority) return;
        EnsureManifest();
    }

    void EnsureManifest()
    {
        if (manifest != null) return;
        int capacity = capacityOverride >= 0 ? capacityOverride : ResolveConfiguredCapacity();
        manifest = new VanCargoManifest(capacity);
        Capacity.Value = manifest.Capacity;
        LoadUnits.Value = manifest.LoadUnits;
        ItemCount.Value = manifest.Count;
    }

    static int ResolveConfiguredCapacity()
    {
        var config = Resources.Load<ScavengingConfig>("Config/ScavengingConfig");
        return config != null ? config.vanWeightCapacity : 12;
    }

    void Update()
    {
        if (!HasAuthority || zone == null) return;
        // Offline (no host) the network carry path never runs, so there is nothing to scan;
        // skip to avoid touching unspawned NetworkObjects.
        if (!IsNetworked) return;

        EnsureManifest();
        scanTimer += Time.deltaTime;
        if (scanTimer < scanInterval) return;
        scanTimer = 0f;
        ScanForDeposits();
    }

    // Host-only: load any item that has been set down (not being carried) inside the bay.
    void ScanForDeposits()
    {
        if (manifest.IsFull) return; // nothing can fit; re-checked per item below would be wasted

        Bounds bounds = zone.bounds;
        var items = FindObjectsByType<ScavengeItem>(FindObjectsSortMode.None);
        foreach (var item in items)
        {
            if (item == null) continue;
            if (item.IsBeingCarried.Value) continue;                 // still in hand — not deposited
            if (!bounds.Contains(item.transform.position)) continue; // not in the bay
            TryStow(item);
            if (manifest.IsFull) break;
        }
    }

    void TryStow(ScavengeItem item)
    {
        // Client preference (scavenging-core-loop §3.4): items in the run client's favoured
        // categories settle at the preference multiplier (Free Salvage favours nothing). Condition
        // stays Good until the condition-by-environment pass (a deeper milestone).
        bool favoured = MvpMissionRuntime.ActiveTask != null &&
                        MvpMissionRuntime.ActiveTask.FavoursCategoryId((int)item.Category);
        var settlementItem = new SettlementItem(item.ItemId, item.BaseValue, ItemCondition.Good, favoured);
        if (!manifest.TryLoad(settlementItem, item.Weight))
            return; // won't fit — leave it on the ground (team decides what stays)

        LoadUnits.Value = manifest.LoadUnits;
        ItemCount.Value = manifest.Count;

        Vector3 pos = item.transform.position;
        var netObj = item.NetworkObject;
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
        else
            Destroy(item.gameObject);

        AudioManager.Instance?.PlayStore(pos);   // "存入" — stowing INTO the van, not picking up
        Debug.Log($"[ScavengeCargoZone] Stowed '{item.ItemId}' ({item.Weight}, {item.Weight.Units()}u) — " +
                  $"load {manifest.LoadUnits}/{manifest.Capacity}, {manifest.Count} item(s).");
    }

    /// <summary>Host-side: the recorded cargo manifest (settlement input). Empty before spawn.</summary>
    public System.Collections.Generic.IReadOnlyList<SettlementItem> Manifest =>
        manifest != null ? manifest.Items : System.Array.Empty<SettlementItem>();

    /// <summary>
    /// Host-side: settle the loaded cargo through the configured calculator (quick-spec §4).
    /// The eventual scavenging mission manager calls this at departure to produce the per-item
    /// reveal + run total. Returns an empty result if called before spawn / off the host.
    /// </summary>
    public SettlementResult SettleCargo()
    {
        if (manifest == null)
            return new SettlementResult(System.Array.Empty<SettlementLine>(), 0);
        var config = Resources.Load<ScavengingConfig>("Config/ScavengingConfig");
        var calculator = config != null
            ? config.CreateSettlementCalculator()
            : new ScavengeSettlementCalculator();
        return calculator.Settle(manifest.ToSettlementItems());
    }

    /// <summary>Host-side: empty the van for a new run.</summary>
    public void ResetCargo()
    {
        if (!HasAuthority) return;
        EnsureManifest();
        manifest.Reset();
        LoadUnits.Value = manifest.LoadUnits;
        ItemCount.Value = manifest.Count;
    }

    // ─── On-site cargo ticket-strip (every peer; reads the replicated load) ───

    void OnGUI()
    {
        if (Capacity.Value <= 0) return;          // not spawned / no capacity configured
        if (VanTransitOverlay.IsActive) return;   // the transit cabin owns the screen during the ride

        EnsureStripStyle();
        if (stripStyle == null) return;

        int load = LoadUnits.Value;
        int cap = Capacity.Value;
        int remaining = Mathf.Max(0, cap - load);

        const float w = 232f, h = 30f;
        // Match MvpHud's resolution-adaptive HUD canvas: draw in 1080-tall reference space and
        // scale to the real screen via GUI.matrix so the strip keeps its proportion at any resolution.
        float scale = MvpHud.UiScale > 0f ? MvpHud.UiScale : 1f;
        float rw = Screen.width / scale;
        const float rh = 1080f;
        Matrix4x4 savedMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        var rect = new Rect((rw - w) * 0.5f, rh - h - 18f, w, h);

        // Aged-paper slip with a civic-teal rule — same grammar as the dispatch ticket strip.
        GUI.DrawTexture(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.Shadow));
        GUI.DrawTexture(rect, BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.OldPaper));
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreen));

        // Slot pips: filled units vs remaining, capped so a big van doesn't overflow the strip.
        int pips = Mathf.Min(cap, 24);
        float pipW = (rect.width - 96f) / Mathf.Max(1, pips);
        for (int i = 0; i < pips; i++)
        {
            int unit = Mathf.RoundToInt((i + 1) * (float)cap / pips);
            bool filled = unit <= load;
            var pip = new Rect(rect.x + 12f + i * pipW, rect.y + 19f, Mathf.Max(2f, pipW - 2f), 6f);
            GUI.DrawTexture(pip, BlackCommissionUiTheme.MakeTex(filled
                ? BlackCommissionUiTheme.RustWarning
                : new Color(0.18f, 0.17f, 0.13f, 0.30f)));
        }

        bool full = remaining <= 0;
        stripStyle.normal.textColor = full
            ? BlackCommissionUiTheme.RustWarning
            : new Color(0.10f, 0.095f, 0.075f, 1f);
        string label = full ? $"舱位已满 {load}/{cap}" : $"舱位 {load}/{cap}";
        GUI.Label(new Rect(rect.xMax - 92f, rect.y + 4f, 86f, 22f), label, stripStyle);

        GUI.matrix = savedMatrix;
    }

    void EnsureStripStyle()
    {
        if (stripStyle != null) return;
        if (GUI.skin == null || GUI.skin.label == null) return;
        stripStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
        };
        MvpFontProvider.ApplyToStyle(stripStyle);
    }
}
