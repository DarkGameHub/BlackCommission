using Unity.Netcode;
using UnityEngine;

/// <summary>
/// World/inspection representation of a dispatch order. The active copy is filed in a hotbar
/// slot rather than occupying the carry hand; this component supplies the readable text and the
/// optional physical mesh. It is never cargo and never reaches settlement.
/// </summary>
public class WorkOrderItem : ScavengeItem
{
    public override bool CanEnterCargo => false;
    public override bool BlocksHotbar => false;

    public string BuildReadableText() => BuildReadableTextForActiveTask();

    public static string BuildReadableTextForActiveTask()
    {
        OfficeTaskDefinition task = MvpMissionRuntime.ActiveTask ?? MvpMissionRuntime.SelectedTask;
        if (task == null)
            return "DISPATCH RECORD UNAVAILABLE";

        bool cargoSecured = ScavengeCargoZone.Instance != null &&
                            ScavengeCargoZone.Instance.ItemCount.Value > 0;
        string objectiveMark = cargoSecured ? "[X]" : "[ ]";
        string localizedScrawl = OfficeTaskText.Scrawl(task);
        string scrawl = string.IsNullOrWhiteSpace(localizedScrawl)
            ? "NO CLIENT ANNOTATION"
            : localizedScrawl;

        return $"WORK ORDER {task.taskId}\n" +
               $"SITE: {OfficeTaskText.Location(task)}\n" +
               $"CLIENT: {OfficeTaskText.Client(task)}\n\n" +
               $"[ ] {OfficeTaskText.Description(task)}\n" +
               $"{objectiveMark} SECURE RETRIEVAL ITEMS IN VAN\n\n" +
               $"CLIENT NOTE: {scrawl}";
    }

    public static void DespawnAllServer()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsListening || !nm.IsServer) return;
        foreach (WorkOrderItem item in Object.FindObjectsByType<WorkOrderItem>(FindObjectsSortMode.None))
            if (item != null && item.NetworkObject != null && item.NetworkObject.IsSpawned)
                item.NetworkObject.Despawn(true);
        foreach (PlayerHotbar hotbar in Object.FindObjectsByType<PlayerHotbar>(FindObjectsSortMode.None))
            if (hotbar != null)
                hotbar.RemoveAllOfTypeServer(MvpHotbarItemId.WorkOrder);
    }
}
