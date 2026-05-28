using UnityEngine;

public class OfficeGroundItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] MvpHotbarItemId itemId = MvpHotbarItemId.None;

    public string InteractHint
    {
        get
        {
            OfficeComputer computer = Object.FindAnyObjectByType<OfficeComputer>();
            int count = computer != null ? computer.GetDroppedItemCount(itemId) : 0;
            return count > 0 ? $"拾取地上存放: {GetItemLabel(itemId)} x{count}" : "";
        }
    }

    public void Configure(MvpHotbarItemId id)
    {
        itemId = id;
    }

    public void OnInteractStart(PlayerController player)
    {
        OfficeComputer computer = Object.FindAnyObjectByType<OfficeComputer>();
        computer?.TryTakeDroppedItem(itemId);
    }

    public void OnInteractEnd(PlayerController player) { }

    static string GetItemLabel(MvpHotbarItemId id)
    {
        switch (id)
        {
            case MvpHotbarItemId.Medkit:
                return "回血药";
            case MvpHotbarItemId.Decoy:
                return "诱饵";
            case MvpHotbarItemId.StunSpray:
                return "定身喷雾";
            case MvpHotbarItemId.Flashlight:
                return "手电";
            default:
                return "物品";
        }
    }
}
