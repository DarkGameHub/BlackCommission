using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public enum MvpHotbarItemId
{
    None,
    Medkit,
    Decoy,
    StunSpray,
    Flashlight
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
    const float OfficeComputerPurchaseDistance = 3.4f;

    [SerializeField] HotbarSlot[] slots = new HotbarSlot[SlotCount];
    [SerializeField] float medkitHealAmount = 30f;
    [SerializeField] float stunSprayRadius = 6f;
    [SerializeField] float stunSprayDuration = 2.5f;
    [SerializeField] float decoyRadius = 12f;
    [SerializeField] float decoyDuration = 4f;

    PlayerInputActions inputActions;

    public NetworkVariable<int> SelectedSlot = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    void Awake()
    {
        EnsureSlots();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputActions = new PlayerInputActions();
        inputActions.Enable();
    }

    public override void OnNetworkDespawn()
    {
        inputActions?.Disable();
        inputActions = null;
    }

    void OnDestroy()
    {
        inputActions?.Disable();
        inputActions = null;
    }

    void Update()
    {
        if (!IsOwner) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
        }

        if (inputActions != null && inputActions.Player.UseItem.WasPressedThisFrame())
            UseSelectedSlot();
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

    public void TryPurchaseItem(MvpHotbarItemId itemId)
    {
        if (!IsOwner || itemId == MvpHotbarItemId.None) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            TryPurchaseLocal(itemId);
            return;
        }

        PurchaseItemServerRpc(itemId);
    }

    public static int GetItemCost(MvpHotbarItemId itemId)
    {
        switch (itemId)
        {
            case MvpHotbarItemId.Medkit:
                return 80;
            case MvpHotbarItemId.Decoy:
                return 60;
            case MvpHotbarItemId.StunSpray:
                return 120;
            case MvpHotbarItemId.Flashlight:
                return 100;
            default:
                return 0;
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
        if (itemId == MvpHotbarItemId.Medkit && TryGetComponent<PlayerHealth>(out var health))
        {
            health.Heal(medkitHealAmount);
            used = true;
            shouldConsume = true;
        }
        else if (itemId == MvpHotbarItemId.StunSpray)
        {
            used = SchoolMonsterAI.TryStunNearest(transform.position, stunSprayRadius, stunSprayDuration);
            shouldConsume = used;
        }
        else if (itemId == MvpHotbarItemId.Decoy)
        {
            used = SchoolMonsterAI.TryDistractNearest(transform.position, decoyRadius, decoyDuration);
            shouldConsume = used;
        }
        else if (itemId == MvpHotbarItemId.Flashlight &&
            TryGetComponent<FlashlightController>(out var flashlight))
        {
            used = flashlight.TryToggleFromHotbar();
        }

        if (used && shouldConsume)
        {
            slot.quantity = Mathf.Max(0, slot.quantity - 1);
            SlotConsumedClientRpc(index, slot.quantity);
        }
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
        SyncHotbarAndFundsClientRpc(
            GetItemId(0), slots[0].quantity,
            GetItemId(1), slots[1].quantity,
            GetItemId(2), slots[2].quantity,
            GetItemId(3), slots[3].quantity,
            GetItemId(4), slots[4].quantity,
            CompanyData.Current.Funds);
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

    bool TryAddItem(MvpHotbarItemId itemId, int quantity)
    {
        if (itemId == MvpHotbarItemId.None || quantity <= 0) return false;

        if (itemId == MvpHotbarItemId.Flashlight && HasItem(MvpHotbarItemId.Flashlight))
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

    bool HasItem(MvpHotbarItemId itemId)
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

    int GetItemId(int index) => IsValidSlot(index) ? (int)slots[index].itemId : 0;

    void SetSlotFromNetwork(int index, int itemId, int quantity)
    {
        if (!IsValidSlot(index)) return;
        slots[index].itemId = (MvpHotbarItemId)Mathf.Clamp(itemId, 0, (int)MvpHotbarItemId.Flashlight);
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
