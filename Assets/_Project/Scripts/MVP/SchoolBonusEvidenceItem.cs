using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SchoolBonusEvidenceItem : MonoBehaviour, IInteractable
{
    [SerializeField] string evidenceName = "逾期作业登记簿";
    [SerializeField] float pickupRadius = 3f;

    bool locallyHidden;

    public string InteractHint
    {
        get
        {
            var manager = LostItemMissionManager.Instance;
            if (manager != null && manager.BonusEvidenceCollected.Value) return "";
            return $"拍照留证: {evidenceName}";
        }
    }

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Update()
    {
        var manager = LostItemMissionManager.Instance;
        bool shouldHide = manager != null && manager.BonusEvidenceCollected.Value;
        if (shouldHide == locallyHidden) return;

        locallyHidden = shouldHide;
        SetVisualsActive(!shouldHide);
    }

    public void OnInteractStart(PlayerController player)
    {
        if (player != null && player.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
            return;

        LostItemMissionManager.Instance?.RequestCollectBonusEvidence(transform.position, pickupRadius);
    }

    public void OnInteractEnd(PlayerController player) { }

    void SetVisualsActive(bool active)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = active;
        }
    }
}
