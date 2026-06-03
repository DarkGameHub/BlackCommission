using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OfficeCabinetStorage : MonoBehaviour, IInteractable
{
    public const int SlotCount = 8;
    const string SaveFileName = "cabinet.json";
    const string LegacyPrefsKey = "AS.OfficeCabinetStorage.v1"; // imported once, then removed

    static CabinetSaveData data;

    public string InteractHint => "打开补给柜";

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        EnsureLoaded();
    }

    public void OnInteractStart(PlayerController player)
    {
        if (player != null && player.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
            return;

        MvpHud.OpenCabinet(this);
    }

    public void OnInteractEnd(PlayerController player) { }

    public HotbarSlot GetSlot(int index)
    {
        EnsureLoaded();
        if (!IsValidSlot(index)) return null;
        return data.slots[index];
    }

    public bool TryStoreFromHotbar(PlayerHotbar hotbar, int hotbarSlotIndex, out string message)
    {
        EnsureLoaded();
        message = "";
        if (hotbar == null)
        {
            message = "没有找到本地玩家热栏。";
            return false;
        }

        HotbarSlot source = hotbar.GetSlot(hotbarSlotIndex);
        if (source == null || source.IsEmpty)
        {
            message = "这个热栏格是空的。";
            return false;
        }

        MvpHotbarItemId itemId = source.itemId;
        int targetIndex = FindStoreSlot(itemId);
        if (targetIndex < 0)
        {
            message = "补给柜已经满了。";
            return false;
        }

        if (!hotbar.TryRemoveOneFromSlotForStorage(hotbarSlotIndex, itemId))
        {
            message = "没能从热栏取出物品。";
            return false;
        }

        AddOneToCabinetSlot(targetIndex, itemId);
        Save();
        message = $"已存入: {GetItemLabel(itemId)}。";
        return true;
    }

    public bool TryTakeToHotbar(PlayerHotbar hotbar, int cabinetSlotIndex, out string message)
    {
        EnsureLoaded();
        message = "";
        if (!IsValidSlot(cabinetSlotIndex))
        {
            message = "无效柜格。";
            return false;
        }

        HotbarSlot slot = data.slots[cabinetSlotIndex];
        if (slot == null || slot.IsEmpty)
        {
            message = "这个柜格是空的。";
            return false;
        }

        if (hotbar == null)
        {
            message = "没有找到本地玩家热栏。";
            return false;
        }

        MvpHotbarItemId itemId = slot.itemId;
        if (!hotbar.CanReceiveItem(itemId, out string reason))
        {
            message = reason;
            return false;
        }

        if (!hotbar.TryReceiveItemFromStorage(itemId, 1))
        {
            message = "热栏暂时无法接收。";
            return false;
        }

        slot.quantity = Mathf.Max(0, slot.quantity - 1);
        if (slot.quantity <= 0)
            slot.itemId = MvpHotbarItemId.None;
        Save();
        message = $"已取出: {GetItemLabel(itemId)}。";
        return true;
    }

    public static string GetItemLabel(MvpHotbarItemId itemId)
    {
        return itemId switch
        {
            MvpHotbarItemId.Flashlight => "手电筒",
            MvpHotbarItemId.Battery => "电池",
            _ => "空"
        };
    }

    int FindStoreSlot(MvpHotbarItemId itemId)
    {
        if (itemId == MvpHotbarItemId.None) return -1;

        if (itemId != MvpHotbarItemId.Flashlight)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                HotbarSlot slot = data.slots[i];
                if (slot != null && !slot.IsEmpty && slot.itemId == itemId && slot.quantity < 99)
                    return i;
            }
        }

        for (int i = 0; i < SlotCount; i++)
        {
            HotbarSlot slot = data.slots[i];
            if (slot == null || slot.IsEmpty)
                return i;
        }

        return -1;
    }

    void AddOneToCabinetSlot(int index, MvpHotbarItemId itemId)
    {
        HotbarSlot slot = data.slots[index];
        if (slot == null)
        {
            slot = new HotbarSlot();
            data.slots[index] = slot;
        }

        if (slot.IsEmpty)
        {
            slot.itemId = itemId;
            slot.quantity = 1;
            return;
        }

        slot.quantity += 1;
    }

    static void EnsureLoaded()
    {
        if (data != null && data.slots != null && data.slots.Length == SlotCount) return;

        data = SaveIO.ReadJson<CabinetSaveData>(SaveFileName);

        // One-time import of the old PlayerPrefs cabinet so existing players keep stored gear.
        if (data == null)
        {
            string legacy = PlayerPrefs.GetString(LegacyPrefsKey, "");
            if (!string.IsNullOrEmpty(legacy))
            {
                try { data = JsonUtility.FromJson<CabinetSaveData>(legacy); } catch { data = null; }
                PlayerPrefs.DeleteKey(LegacyPrefsKey);
                PlayerPrefs.Save();
            }
        }

        if (data == null)
            data = new CabinetSaveData();

        if (data.slots == null || data.slots.Length != SlotCount)
            data.slots = new HotbarSlot[SlotCount];

        for (int i = 0; i < data.slots.Length; i++)
        {
            if (data.slots[i] == null)
                data.slots[i] = new HotbarSlot();
        }
    }

    static void Save()
    {
        EnsureLoaded();
        SaveIO.WriteJson(SaveFileName, data);
    }

    static bool IsValidSlot(int index) => index >= 0 && index < SlotCount;

    [Serializable]
    class CabinetSaveData
    {
        public HotbarSlot[] slots = new HotbarSlot[SlotCount];
    }
}
