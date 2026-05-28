using UnityEngine;

public class SchoolEntranceDoor : MonoBehaviour, IInteractable
{
    [SerializeField] bool opened;
    Collider doorCollider;
    Renderer[] renderers;

    public string InteractHint => opened ? "" : "推开学校正门: 进入任务区";

    void Awake()
    {
        doorCollider = GetComponent<Collider>();
        renderers = GetComponentsInChildren<Renderer>();
        ApplyOpenState();
    }

    public void OnInteractStart(PlayerController player)
    {
        if (opened) return;
        opened = true;
        ApplyOpenState();
    }

    public void OnInteractEnd(PlayerController player) { }

    void ApplyOpenState()
    {
        if (doorCollider != null)
            doorCollider.enabled = !opened;

        if (renderers == null) return;
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = !opened;
        }
    }
}
