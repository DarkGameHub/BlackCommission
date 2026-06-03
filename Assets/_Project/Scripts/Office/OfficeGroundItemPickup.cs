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

    static string GetItemLabel(MvpHotbarItemId id) => id switch
    {
        MvpHotbarItemId.Flashlight => MvpLocale.T("flashlight"),
        MvpHotbarItemId.Battery => MvpLocale.T("battery"),
        _ => "物品"
    };
}
