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

    [SerializeField] HotbarSlot[] slots = new HotbarSlot[SlotCount];
    [SerializeField] float medkitHealAmount = 30f;
    [SerializeField] bool grantMvpStarterItems = true;
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
        GrantStarterItemsIfEmpty();
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
        if (!IsValidSlot(index)) return;
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

    [ClientRpc]
    void SlotConsumedClientRpc(int index, int remainingQuantity)
    {
        EnsureSlots();
        if (!IsValidSlot(index)) return;
        slots[index].quantity = remainingQuantity;
        if (remainingQuantity <= 0)
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

    void GrantStarterItemsIfEmpty()
    {
        if (!grantMvpStarterItems) return;

        bool hasAnyItem = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty)
            {
                hasAnyItem = true;
                break;
            }
        }

        if (hasAnyItem) return;

        slots[0].itemId = MvpHotbarItemId.Medkit;
        slots[0].quantity = 1;
        slots[1].itemId = MvpHotbarItemId.StunSpray;
        slots[1].quantity = 1;
        slots[2].itemId = MvpHotbarItemId.Decoy;
        slots[2].quantity = 1;
        slots[3].itemId = MvpHotbarItemId.Flashlight;
        slots[3].quantity = 1;
    }

    static bool IsValidSlot(int index) => index >= 0 && index < SlotCount;
}
