using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls water level rise. Server-authoritative: clients read the level
/// to apply visual effects and movement penalties.
/// </summary>
public class WaterLevelManager : NetworkBehaviour
{
    public static WaterLevelManager Instance { get; private set; }

    [Header("Water Rise Settings")]
    [SerializeField] float startHeight = -2.0f;     // well below floor — safe at mission start
    [SerializeField] float maxHeight = 1.2f;        // waist height — dangerous
    [SerializeField] float[] phaseRiseRates = { 0.003f, 0.006f, 0.012f, 0.02f };

    [Header("Zone Heights")]
    [SerializeField] float ankleTrigger = -2.5f;    // slow movement 10%
    [SerializeField] float kneeTrigger = -1.5f;     // slow movement 40%, electrics dangerous
    [SerializeField] float waistTrigger = -0.5f;    // slow movement 60%, can't carry heavy items

    public NetworkVariable<float> CurrentWaterHeight = new(-2.0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            CurrentWaterHeight.Value = startHeight;
    }

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (GameManager.Instance == null) return;

        var phase = GameManager.Instance.CurrentPhase.Value;
        if (phase == GameManager.MissionPhase.Ended) return;

        // Pump repair slows rise significantly
        float pumpFactor = GameManager.Instance.PumpRepaired.Value ? 0.1f : 1f;

        int phaseIdx = phase switch
        {
            GameManager.MissionPhase.Active => 0,
            GameManager.MissionPhase.Escalating => 1,
            GameManager.MissionPhase.Critical => 2,
            GameManager.MissionPhase.ForcedEvac => 3,
            _ => 0
        };

        float riseRate = phaseRiseRates[phaseIdx] * pumpFactor;
        CurrentWaterHeight.Value = Mathf.Min(maxHeight, CurrentWaterHeight.Value + riseRate * Time.deltaTime);
    }

    // Called by PlayerController every frame to get movement speed modifier
    public float GetSpeedModifierForHeight(float playerWorldY)
    {
        float waterHeight = CurrentWaterHeight.Value;
        float depth = waterHeight - playerWorldY;

        if (depth < 0.05f) return 1f;                  // not in water
        if (playerWorldY > waistTrigger) return 0.4f;  // waist deep
        if (playerWorldY > kneeTrigger)  return 0.6f;  // knee deep
        if (playerWorldY > ankleTrigger) return 0.9f;  // ankle deep
        return 1f;
    }

    public bool IsZoneFlooded(float worldY) => CurrentWaterHeight.Value > worldY + 0.1f;

    // 0-1 normalized level for UI display
    public float NormalizedLevel => Mathf.InverseLerp(startHeight, maxHeight, CurrentWaterHeight.Value);
}
