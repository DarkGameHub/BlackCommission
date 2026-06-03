using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[Serializable]
public enum MvpHotbarItemId
{
    None,
    Flashlight,
    Battery
}

[Serializable]
public class HotbarSlot
{
    public MvpHotbarItemId itemId;
    public int quantity;

    public bool IsEmpty => itemId == MvpHotbarItemId.None || quantity <= 0;
}

public class PlayerHotbar : NetworkBehaviour
{
    public const int SlotCount = 5;
    public const int WristwatchCost = 150;
    const float OfficeComputerPurchaseDistance = 3.4f;
    const float OfficeGroundStorageDropDistance = 5.2f;

    [SerializeField] HotbarSlot[] slots = new HotbarSlot[SlotCount];

    PlayerInputActions inputActions;
    PlayerController seatedController;

    public NetworkVariable<int> SelectedSlot = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> HasWristwatch = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    bool localWristwatchOwned;
    public bool HasWristwatchOwned => localWristwatchOwned || HasWristwatch.Value;

    void Awake()
    {
        EnsureSlots();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputActions = new PlayerInputActions();
        inputActions.Enable();
        if (CompanyData.Current.WristwatchPurchased)
            localWristwatchOwned = true;
    }

    public override void OnNetworkDespawn()
    {
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
        if (!IsOwner) return;
        if (MvpHud.IsBlockingPanelOpen) return;

        // Seated in the van: you can switch slots and pull items out, but not drop them.
        if (seatedController == null) TryGetComponent(out seatedController);
        bool seated = seatedController != null && seatedController.IsSeated;

        bool usePressed = inputActions != null && inputActions.Player.UseItem.WasPressedThisFrame();
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
            usePressed |= Keyboard.current.hKey.wasPressedThisFrame;
        }

        if (usePressed)
            UseSelectedSlot();

        if (!seated && inputActions != null && inputActions.Player.Drop.WasPressedThisFrame())
            TryDropSelectedSlot();
    }

    public HotbarSlot GetSlot(int index)
    {
        EnsureSlots();
        return IsValidSlot(index) ? slots[index] : null;
    }

    public bool TrySetSlot(int index, MvpHotbarItemId itemId, int quantity)
    {
        EnsureSlots();
        if (!IsValidSlot(index)) return false;
        slots[index].itemId = itemId;
        slots[index].quantity = Mathf.Max(0, quantity);
        return true;
    }

    public void SelectSlot(int index)
    {
        if (!IsSpawned || !IsValidSlot(index)) return;
        SelectedSlot.Value = index;
    }

    public void UseSelectedSlot()
    {
        EnsureSlots();
        int index = SelectedSlot.Value;
        if (!IsValidSlot(index) || slots[index].IsEmpty) return;

        if (TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
            return;

        UseSlotServerRpc(index, slots[index].itemId);
    }

    public bool TryDropSelectedSlot()
    {
        EnsureSlots();
        if (!IsOwner) return false;
        if (TryGetComponent<CarrySystem>(out var carry) && carry.IsCarrying) return false;
        if (SceneManager.GetActiveScene().name != "HQ") return false;

        int index = SelectedSlot.Value;
        if (!IsValidSlot(index) || slots[index].IsEmpty) return false;

        // Find any OfficeComputer in the scene — no range restriction
        OfficeComputer computer = UnityEngine.Object.FindAnyObjectByType<OfficeComputer>();
        if (computer == null) return false;

        MvpHotbarItemId itemId = slots[index].itemId;
        Vector3 dropPos = transform.position + transform.forward * 0.4f + Vector3.up * 0.08f;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            if (!computer.TryStoreDroppedItemServer(itemId, 1)) return false;
            RemoveOneFromSlot(index);
            SpawnDropVisualLocal(dropPos, itemId);
            return true;
        }

        DropSelectedSlotServerRpc(index, itemId, dropPos);
        return true;
    }

    public bool TryPurchaseWristwatch()
    {
        if (!IsOwner) return false;
        if (HasWristwatchOwned) return false;
        if (LostItemMissionManager.Instance != null || MvpPendingReward.HasPending) return false;
        if (!IsNearOfficeComputer()) return false;
        if (CompanyData.Current.Funds < WristwatchCost) return false;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            localWristwatchOwned = true;
            CompanyData.Current.Funds -= WristwatchCost;
            CompanyData.Current.WristwatchPurchased = true;
            CompanyData.Save();
            return true;
        }

        PurchaseWristwatchServerRpc();
        return true;
    }

    public bool TryPurchaseItem(MvpHotbarItemId itemId)
    {
        if (!IsOwner || itemId == MvpHotbarItemId.None) return false;
        if (!CanReceiveItem(itemId, out _)) return false;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            TryPurchaseLocal(itemId);
            return true;
        }

        PurchaseItemServerRpc(itemId);
        return true;
    }

    public bool TryReceiveLocalItem(MvpHotbarItemId itemId, int quantity)
    {
        EnsureSlots();
        if (itemId == MvpHotbarItemId.None || quantity <= 0) return false;
        if (!CanReceiveItem(itemId, out _)) return false;
        bool added = TryAddItem(itemId, quantity);
        if (added && IsSpawned && IsServer)
            SyncHotbarClientRpc(
                GetItemId(0), slots[0].quantity,
                GetItemId(1), slots[1].quantity,
                GetItemId(2), slots[2].quantity,
                GetItemId(3), slots[3].quantity,
                GetItemId(4), slots[4].quantity);
        return added;
    }

    public bool TryReceiveItemFromStorage(MvpHotbarItemId itemId, int quantity)
    {
        EnsureSlots();
        if (!IsOwner) return false;
        if (itemId == MvpHotbarItemId.None || quantity <= 0) return false;
        if (!CanReceiveItem(itemId, out _)) return false;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
            ReceiveItemFromStorageServerRpc(itemId, quantity);

        return TryReceiveLocalItem(itemId, quantity);
    }

    public bool TryRemoveOneFromSlotForStorage(int index, MvpHotbarItemId expectedItemId)
    {
        EnsureSlots();
        if (!IsOwner) return false;
        if (!IsValidSlot(index)) return false;
        HotbarSlot slot = slots[index];
        if (slot.IsEmpty || slot.itemId != expectedItemId) return false;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
            RemoveOneFromSlotForStorageServerRpc(index, expectedItemId);

        RemoveOneFromSlot(index);
        if (IsSpawned && IsServer)
            SyncHotbarClientRpc(
                GetItemId(0), slots[0].quantity,
                GetItemId(1), slots[1].quantity,
                GetItemId(2), slots[2].quantity,
                GetItemId(3), slots[3].quantity,
                GetItemId(4), slots[4].quantity);
        return true;
    }

    public bool GrantItemServer(MvpHotbarItemId itemId, int quantity)
    {
        if (!IsServer) return false;
        EnsureSlots();
        if (itemId == MvpHotbarItemId.None || quantity <= 0) return false;
        if (!CanReceiveItem(itemId, out _)) return false;
        if (!TryAddItem(itemId, quantity)) return false;

        SyncHotbarClientRpc(
            GetItemId(0), slots[0].quantity,
            GetItemId(1), slots[1].quantity,
            GetItemId(2), slots[2].quantity,
            GetItemId(3), slots[3].quantity,
            GetItemId(4), slots[4].quantity);
        return true;
    }

    /// <summary>
    /// Server wipes every slot and syncs the empty hotbar to the owner. Used when a player
    /// is stranded at a mission site on return — their held gear is lost with them.
    /// </summary>
    public void ClearAllServer()
    {
        if (!IsServer) return;
        EnsureSlots();

        bool hadAny = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty) hadAny = true;
            slots[i].itemId = MvpHotbarItemId.None;
            slots[i].quantity = 0;
        }
        if (!hadAny) return;

        SyncHotbarClientRpc(
            GetItemId(0), slots[0].quantity,
            GetItemId(1), slots[1].quantity,
            GetItemId(2), slots[2].quantity,
            GetItemId(3), slots[3].quantity,
            GetItemId(4), slots[4].quantity);
    }

    public bool CanReceiveItem(MvpHotbarItemId itemId, out string reason)
    {
        EnsureSlots();
        reason = "";

        if (itemId == MvpHotbarItemId.None)
        {
            reason = "无效道具。";
            return false;
        }

        if (itemId == MvpHotbarItemId.Flashlight && HasItem(MvpHotbarItemId.Flashlight))
        {
            reason = "已经有一支手电。";
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty)
                return true;
            if (slots[i].itemId == itemId && itemId != MvpHotbarItemId.Flashlight)
                return true;
        }

        reason = "热栏已满。";
        return false;
    }

    public static int GetItemCost(MvpHotbarItemId itemId)
    {
        switch (itemId)
        {
            case MvpHotbarItemId.Flashlight: return 120;
            case MvpHotbarItemId.Battery: return 40;
            default: return 0;
        }
    }

    [ServerRpc]
    void UseSlotServerRpc(int index, MvpHotbarItemId itemId)
    {
        EnsureSlots();
        if (!IsValidSlot(index)) return;
        HotbarSlot slot = slots[index];
        if (slot.IsEmpty || slot.itemId != itemId) return;

        if (TryGetComponent<PlayerHealth>(out var ownHealth) && ownHealth.IsDowned.Value)
            return;

        bool used = false;
        bool shouldConsume = false;
        if (itemId == MvpHotbarItemId.Flashlight &&
            TryGetComponent<FlashlightController>(out var flashlight))
        {
            used = flashlight.TryToggleFromHotbar();
        }
        else if (itemId == MvpHotbarItemId.Battery &&
            TryGetComponent<FlashlightController>(out var fl2))
        {
            used = fl2.TryRecharge();
            shouldConsume = used;
        }

        if (used && shouldConsume)
        {
            slot.quantity = Mathf.Max(0, slot.quantity - 1);
            SlotConsumedClientRpc(index, slot.quantity);
        }
    }

    [ServerRpc]
    void ReceiveItemFromStorageServerRpc(MvpHotbarItemId itemId, int quantity)
    {
        GrantItemServer(itemId, quantity);
    }

    [ServerRpc]
    void RemoveOneFromSlotForStorageServerRpc(int index, MvpHotbarItemId expectedItemId)
    {
        EnsureSlots();
        if (!IsValidSlot(index)) return;
        HotbarSlot slot = slots[index];
        if (slot.IsEmpty || slot.itemId != expectedItemId) return;

        RemoveOneFromSlot(index);
        SyncHotbarClientRpc(
            GetItemId(0), slots[0].quantity,
            GetItemId(1), slots[1].quantity,
            GetItemId(2), slots[2].quantity,
            GetItemId(3), slots[3].quantity,
            GetItemId(4), slots[4].quantity);
    }

    [ServerRpc]
    void DropSelectedSlotServerRpc(int index, MvpHotbarItemId itemId, Vector3 dropPos)
    {
        EnsureSlots();
        if (!IsValidSlot(index)) return;
        if (SceneManager.GetActiveScene().name != "HQ") return;

        HotbarSlot slot = slots[index];
        if (slot.IsEmpty || slot.itemId != itemId) return;
        if (TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value) return;

        OfficeComputer computer = UnityEngine.Object.FindAnyObjectByType<OfficeComputer>();
        if (computer == null) return;
        if (!computer.TryStoreDroppedItemServer(itemId, 1)) return;

        RemoveOneFromSlot(index);
        SyncHotbarClientRpc(
            GetItemId(0), slots[0].quantity,
            GetItemId(1), slots[1].quantity,
            GetItemId(2), slots[2].quantity,
            GetItemId(3), slots[3].quantity,
            GetItemId(4), slots[4].quantity);
        SpawnDropVisualClientRpc(dropPos, (int)itemId);
    }

    [ClientRpc]
    void SpawnDropVisualClientRpc(Vector3 position, int itemIdInt)
    {
        SpawnDropVisualLocal(position, (MvpHotbarItemId)itemIdInt);
    }

    static void SpawnDropVisualLocal(Vector3 position, MvpHotbarItemId itemId)
    {
        Color itemColor = itemId switch
        {
            MvpHotbarItemId.Flashlight => new Color(0.18f, 0.19f, 0.18f),
            MvpHotbarItemId.Battery => new Color(0.73f, 0.50f, 0.16f),
            _ => new Color(0.5f, 0.5f, 0.5f)
        };

        // Use cylinder for flashlight (long shape), cube for battery
        GameObject go = itemId == MvpHotbarItemId.Flashlight
            ? GameObject.CreatePrimitive(PrimitiveType.Cylinder)
            : GameObject.CreatePrimitive(PrimitiveType.Cube);

        go.name = $"DroppedItem_{itemId}";
        go.transform.position = position;
        go.transform.localScale = itemId == MvpHotbarItemId.Flashlight
            ? new Vector3(0.06f, 0.15f, 0.06f)
            : new Vector3(0.07f, 0.12f, 0.07f);
        go.transform.rotation = Quaternion.Euler(
            itemId == MvpHotbarItemId.Flashlight ? 0f : 0f,
            UnityEngine.Random.Range(0f, 360f), 0f);

        var mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard"));
        mat.color = itemColor;
        go.GetComponent<Renderer>().material = mat;

        UnityEngine.Object.Destroy(go.GetComponent<Collider>());

        // Add interactable so player can pick up from drop location
        go.AddComponent<DroppedItemVisual>().Init(itemId);

        // Gentle bob animation via a small point light to make it findable
        var glow = new GameObject("DropGlow").AddComponent<Light>();
        glow.transform.SetParent(go.transform);
        glow.transform.localPosition = Vector3.up * 0.15f;
        glow.type = LightType.Point;
        glow.color = itemColor;
        glow.intensity = 0.35f;
        glow.range = 0.8f;
    }

    [ServerRpc]
    void PurchaseItemServerRpc(MvpHotbarItemId itemId)
    {
        EnsureSlots();
        if (LostItemMissionManager.Instance != null || MvpPendingReward.HasPending) return;
        if (!IsNearOfficeComputer()) return;

        int cost = GetItemCost(itemId);
        if (cost <= 0) return;
        if (CompanyData.Current.Funds < cost) return;
        if (!TryAddItem(itemId, 1)) return;

        CompanyData.Current.Funds -= cost;
        CompanyData.Save();
        SyncHotbarAndFundsClientRpc(
            GetItemId(0), slots[0].quantity,
            GetItemId(1), slots[1].quantity,
            GetItemId(2), slots[2].quantity,
            GetItemId(3), slots[3].quantity,
            GetItemId(4), slots[4].quantity,
            CompanyData.Current.Funds);
    }

    [ServerRpc]
    void PurchaseWristwatchServerRpc()
    {
        if (HasWristwatch.Value) return;
        if (LostItemMissionManager.Instance != null || MvpPendingReward.HasPending) return;
        if (!IsNearOfficeComputer()) return;
        if (CompanyData.Current.Funds < WristwatchCost) return;

        HasWristwatch.Value = true;
        CompanyData.Current.Funds -= WristwatchCost;
        CompanyData.Current.WristwatchPurchased = true;
        CompanyData.Save();
        SyncWristwatchPurchaseClientRpc(CompanyData.Current.Funds);
    }

    [ClientRpc]
    void SlotConsumedClientRpc(int index, int remainingQuantity)
    {
        EnsureSlots();
        if (!IsValidSlot(index)) return;
        slots[index].quantity = remainingQuantity;
        if (remainingQuantity <= 0)
            slots[index].itemId = MvpHotbarItemId.None;
    }

    [ClientRpc]
    void SyncHotbarAndFundsClientRpc(
        int item0, int qty0,
        int item1, int qty1,
        int item2, int qty2,
        int item3, int qty3,
        int item4, int qty4,
        int funds)
    {
        EnsureSlots();
        SetSlotFromNetwork(0, item0, qty0);
        SetSlotFromNetwork(1, item1, qty1);
        SetSlotFromNetwork(2, item2, qty2);
        SetSlotFromNetwork(3, item3, qty3);
        SetSlotFromNetwork(4, item4, qty4);
        CompanyData.Current.Funds = funds;
    }

    [ClientRpc]
    void SyncHotbarClientRpc(
        int item0, int qty0,
        int item1, int qty1,
        int item2, int qty2,
        int item3, int qty3,
        int item4, int qty4)
    {
        EnsureSlots();
        SetSlotFromNetwork(0, item0, qty0);
        SetSlotFromNetwork(1, item1, qty1);
        SetSlotFromNetwork(2, item2, qty2);
        SetSlotFromNetwork(3, item3, qty3);
        SetSlotFromNetwork(4, item4, qty4);
    }

    [ClientRpc]
    void SyncWristwatchPurchaseClientRpc(int funds)
    {
        localWristwatchOwned = true;
        CompanyData.Current.Funds = funds;
    }

    bool TryPurchaseLocal(MvpHotbarItemId itemId)
    {
        if (LostItemMissionManager.Instance != null || MvpPendingReward.HasPending) return false;
        if (!IsNearOfficeComputer()) return false;

        int cost = GetItemCost(itemId);
        if (cost <= 0 || CompanyData.Current.Funds < cost) return false;
        if (!TryAddItem(itemId, 1)) return false;
        CompanyData.Current.Funds -= cost;
        return true;
    }

    void RemoveOneFromSlot(int index)
    {
        if (!IsValidSlot(index)) return;
        slots[index].quantity = Mathf.Max(0, slots[index].quantity - 1);
        if (slots[index].quantity <= 0)
            slots[index].itemId = MvpHotbarItemId.None;
    }

    bool TryAddItem(MvpHotbarItemId itemId, int quantity)
    {
        if (itemId == MvpHotbarItemId.None || quantity <= 0) return false;

        if (itemId == MvpHotbarItemId.Flashlight && HasItem(itemId))
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty && slots[i].itemId == itemId && itemId != MvpHotbarItemId.Flashlight)
            {
                slots[i].quantity += quantity;
                return true;
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].itemId = itemId;
                slots[i].quantity = quantity;
                return true;
            }
        }

        return false;
    }

    public bool HasItem(MvpHotbarItemId itemId)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty && slots[i].itemId == itemId)
                return true;
        }

        return false;
    }

    bool IsNearOfficeComputer()
    {
        OfficeComputer[] computers = FindObjectsByType<OfficeComputer>(FindObjectsSortMode.None);
        foreach (var computer in computers)
        {
            if (computer == null) continue;
            if (Vector3.Distance(transform.position, computer.transform.position) <= OfficeComputerPurchaseDistance)
                return true;
        }

        return false;
    }

    OfficeComputer FindNearestOfficeComputer(float maxDistance)
    {
        OfficeComputer nearest = null;
        float nearestDistance = maxDistance;
        OfficeComputer[] computers = FindObjectsByType<OfficeComputer>(FindObjectsSortMode.None);
        foreach (var computer in computers)
        {
            if (computer == null) continue;
            float distance = Vector3.Distance(transform.position, computer.transform.position);
            if (distance <= nearestDistance)
            {
                nearest = computer;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    int GetItemId(int index) => IsValidSlot(index) ? (int)slots[index].itemId : 0;

    void SetSlotFromNetwork(int index, int itemId, int quantity)
    {
        if (!IsValidSlot(index)) return;
        slots[index].itemId = (MvpHotbarItemId)Mathf.Clamp(itemId, 0, (int)MvpHotbarItemId.Battery);
        slots[index].quantity = Mathf.Max(0, quantity);
        if (slots[index].quantity <= 0)
            slots[index].itemId = MvpHotbarItemId.None;
    }

    void EnsureSlots()
    {
        if (slots == null || slots.Length != SlotCount)
            slots = new HotbarSlot[SlotCount];

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                slots[i] = new HotbarSlot();
        }
    }

    static bool IsValidSlot(int index) => index >= 0 && index < SlotCount;
}
