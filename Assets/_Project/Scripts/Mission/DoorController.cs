using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class DoorController : NetworkBehaviour, IInteractable
{
    public enum DoorType { Normal, Locked, Shortcut, WaterBlocked }

    [SerializeField] DoorType doorType = DoorType.Normal;
    [SerializeField] float openAngle = 90f;
    [SerializeField] float openSpeed = 3f;
    [SerializeField] float waterBlockThreshold = -1f;

    public NetworkVariable<bool> IsOpen = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsUnlocked = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    Transform pivot;
    float targetAngle;

    void Awake()
    {
        pivot = transform.childCount > 0 ? transform.GetChild(0) : transform;
    }

    public override void OnNetworkSpawn()
    {
        if (doorType == DoorType.Normal)
            IsUnlocked.Value = true;
    }

    void Update()
    {
        targetAngle = IsOpen.Value ? openAngle : 0f;
        float current = pivot.localEulerAngles.y;
        if (current > 180f) current -= 360f;
        float next = Mathf.MoveTowards(current, targetAngle, openSpeed * 100f * Time.deltaTime);
        pivot.localEulerAngles = new Vector3(0, next, 0);
    }

    public string InteractHint
    {
        get
        {
            if (doorType == DoorType.WaterBlocked && IsWaterBlocked())
                return "门被积水堵住了";
            if (doorType == DoorType.Locked && !IsUnlocked.Value)
                return "需要工具箱开锁";
            if (doorType == DoorType.Shortcut && !IsUnlocked.Value)
                return "从另一侧开启";
            return IsOpen.Value ? "关门" : "开门";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        switch (doorType)
        {
            case DoorType.Normal:
                ToggleDoorServerRpc();
                break;

            case DoorType.Locked:
                if (!IsUnlocked.Value)
                {
                    var carry = player.GetComponent<CarrySystem>();
                    if (carry != null && carry.CarriedItem is ToolboxItem)
                        UnlockAndOpenServerRpc();
                }
                else
                    ToggleDoorServerRpc();
                break;

            case DoorType.Shortcut:
                if (!IsUnlocked.Value)
                {
                    Vector3 toPlayer = (player.transform.position - transform.position).normalized;
                    float dot = Vector3.Dot(transform.forward, toPlayer);
                    if (dot < 0)
                        UnlockAndOpenServerRpc();
                }
                else
                    ToggleDoorServerRpc();
                break;

            case DoorType.WaterBlocked:
                if (!IsWaterBlocked())
                    ToggleDoorServerRpc();
                break;
        }
    }

    public void OnInteractEnd(PlayerController player) { }

    [ServerRpc(RequireOwnership = false)]
    void ToggleDoorServerRpc()
    {
        if (doorType == DoorType.WaterBlocked && IsWaterBlocked()) return;
        if (!IsUnlocked.Value && doorType != DoorType.Normal) return;
        IsOpen.Value = !IsOpen.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    void UnlockAndOpenServerRpc()
    {
        IsUnlocked.Value = true;
        IsOpen.Value = true;
    }

    bool IsWaterBlocked()
    {
        if (WaterLevelManager.Instance == null) return false;
        return WaterLevelManager.Instance.CurrentWaterHeight.Value > waterBlockThreshold;
    }
}
