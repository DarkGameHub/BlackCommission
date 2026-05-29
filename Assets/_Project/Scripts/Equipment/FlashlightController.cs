using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
public class FlashlightController : NetworkBehaviour
{
    [Header("Light")]
    [SerializeField] float maxBattery = 100f;
    // 30 minutes of continuous use: 100 / 1800 ≈ 0.0556
    const float DrainPer30Min = 100f / 1800f;
    float normalDrainRate;

    [Header("Strong Light")]
    [SerializeField] float strongDrainCost = 15f;
    [SerializeField] float strongLightDuration = 0.5f;
    [SerializeField] float strongLightCooldown = 10f;
    [SerializeField] float stunRange = 8f;
    [SerializeField] float stunAngle = 30f;
    [SerializeField] float robotStunDuration = 3f;

    public NetworkVariable<bool> IsOn = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    Light spotLight;
    PlayerInputActions inputActions;
    float battery;
    float cooldownTimer;
    float strongTimer;
    bool strongActive;

    void Awake()
    {
        battery = maxBattery;
        normalDrainRate = DrainPer30Min;
    }

    public override void OnNetworkSpawn()
    {
        spotLight = GetComponentInChildren<Light>();

        IsOn.OnValueChanged += (_, on) =>
        {
            if (spotLight != null) spotLight.enabled = on;
        };

        if (spotLight != null) spotLight.enabled = IsOn.Value;

        if (!IsOwner) return;
        inputActions = new PlayerInputActions();
        inputActions.Enable();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        CleanupInputActions();
    }

    public override void OnDestroy()
    {
        CleanupInputActions();
        base.OnDestroy();
    }

    void CleanupInputActions()
    {
        if (inputActions == null) return;
        inputActions.Player.Disable();
        inputActions.Disable();
        inputActions.Dispose();
        inputActions = null;
    }

    void Update()
    {
        if (IsOwner)
            HandleInput();

        if (IsServer)
            ServerUpdate();
    }

    void HandleInput()
    {
        if (inputActions == null) return;

        if (GetComponent<PlayerHotbar>() != null) return;

        if (inputActions.Player.RadioTalk.WasPressedThisFrame())
            ToggleFlashlightServerRpc();
        if (inputActions.Player.UseItem.WasPressedThisFrame())
            UseStrongLightServerRpc();
    }

    public bool TryToggleFromHotbar()
    {
        if (!IsServer) return false;
        if (battery <= 0 && !IsOn.Value) return false;
        IsOn.Value = !IsOn.Value;
        AudioManager.Instance?.PlayFlashlightClick(transform.position);
        return true;
    }

    public bool TryRecharge()
    {
        if (!IsServer) return false;
        if (battery >= maxBattery) return false;
        battery = maxBattery;
        AudioManager.Instance?.PlayPickup(transform.position);
        return true;
    }

    public float BatteryNormalized => battery / maxBattery;

    [ServerRpc]
    void ToggleFlashlightServerRpc()
    {
        if (battery <= 0 && !IsOn.Value) return;
        IsOn.Value = !IsOn.Value;
    }

    [ServerRpc]
    void UseStrongLightServerRpc()
    {
        if (!IsOn.Value) return;
        if (cooldownTimer > 0) return;
        if (battery < strongDrainCost) return;

        battery -= strongDrainCost;
        strongActive = true;
        strongTimer = strongLightDuration;
        cooldownTimer = strongLightCooldown;

        StrongLightBurstClientRpc();
        DetectRobotsInCone();
    }

    void ServerUpdate()
    {
        if (IsOn.Value)
        {
            battery -= normalDrainRate * Time.deltaTime;
            if (battery <= 0)
            {
                battery = 0;
                IsOn.Value = false;
            }
        }

        if (cooldownTimer > 0)
            cooldownTimer -= Time.deltaTime;

        if (strongActive)
        {
            strongTimer -= Time.deltaTime;
            if (strongTimer <= 0)
                strongActive = false;
        }
    }

    void DetectRobotsInCone()
    {
        var camTransform = GetComponentInChildren<Camera>()?.transform;
        if (camTransform == null) return;

        var colliders = Physics.OverlapSphere(camTransform.position, stunRange);
        foreach (var col in colliders)
        {
            var robot = col.GetComponentInParent<CleaningRobot>();
            if (robot == null) continue;

            Vector3 dirToRobot = (robot.transform.position - camTransform.position).normalized;
            float angle = Vector3.Angle(camTransform.forward, dirToRobot);
            if (angle < stunAngle)
            {
                robot.ApplyFlashlightStun(robotStunDuration);
                AudioManager.Instance?.PlayRobotStunned(robot.transform.position);
            }
        }
    }

    [ClientRpc]
    void StrongLightBurstClientRpc()
    {
        if (spotLight == null) return;
        StartCoroutine(StrongLightVisual());
    }

    System.Collections.IEnumerator StrongLightVisual()
    {
        float origIntensity = spotLight.intensity;
        float origAngle = spotLight.spotAngle;
        spotLight.intensity = 5f;
        spotLight.spotAngle = 90f;
        yield return new WaitForSeconds(strongLightDuration);
        spotLight.intensity = origIntensity;
        spotLight.spotAngle = origAngle;
    }

    public float CooldownRemaining => Mathf.Max(0, cooldownTimer);
}
