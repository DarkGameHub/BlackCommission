using UnityEngine;

public class SchoolEntranceDoor : MonoBehaviour, IInteractable
{
    [SerializeField] bool opened;
    Collider[] doorColliders;
    Renderer[] renderers;

    public string InteractHint => opened ? "" : "推开学校正门: 进入任务区";

    void Awake()
    {
        doorColliders = GetComponents<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        ApplyOpenState();
    }

    public void OnInteractStart(PlayerController player)
    {
        if (opened) return;
        LostItemMissionManager manager = LostItemMissionManager.Instance;
        if (manager != null)
        {
            manager.RequestOpenSchoolEntrance();
            return;
        }

        SetOpen(true);
    }

    public void OnInteractEnd(PlayerController player) { }

    public void SetOpen(bool isOpen)
    {
        opened = isOpen;
        ApplyOpenState();
    }

    public static void SetAllOpen(bool isOpen)
    {
        SchoolEntranceDoor[] doors = FindObjectsByType<SchoolEntranceDoor>(FindObjectsSortMode.None);
        foreach (var door in doors)
        {
            if (door != null)
                door.SetOpen(isOpen);
        }
    }

    void ApplyOpenState()
    {
        if (doorColliders != null)
        {
            foreach (var collider in doorColliders)
            {
                if (collider != null)
                    collider.enabled = !opened;
            }
        }

        if (renderers == null) return;
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = !opened;
        }
    }
}
