using UnityEngine;
using UnityEngine.SceneManagement;

public class DroppedItemVisual : MonoBehaviour, IInteractable
{
    MvpHotbarItemId itemId;

    public void Init(MvpHotbarItemId id)
    {
        itemId = id;
        var sphere = gameObject.AddComponent<SphereCollider>();
        sphere.isTrigger = true;
        sphere.radius = 0.55f;
    }

    public string InteractHint
    {
        get
        {
            if (SceneManager.GetActiveScene().name != "HQ") return "";
            var computer = Object.FindAnyObjectByType<OfficeComputer>();
            if (computer == null) return "";
            int count = computer.GetDroppedItemCount(itemId);
            if (count <= 0) return "";
            string name = itemId == MvpHotbarItemId.Flashlight
                ? MvpLocale.T("flashlight")
                : MvpLocale.T("battery");
            return $"拾取: {name} x{count}";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        var computer = Object.FindAnyObjectByType<OfficeComputer>();
        computer?.TryTakeDroppedItem(itemId);
        Destroy(gameObject);
    }

    public void OnInteractEnd(PlayerController player) { }
}
