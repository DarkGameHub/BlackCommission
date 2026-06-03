using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WrongHomeworkItem : MonoBehaviour, IInteractable
{
    [SerializeField] string itemName = "相似作业本";
    [SerializeField] float pickupRadius = 3f;

    bool locallyChecked;

    public string InteractHint
    {
        get
        {
            LostItemMissionManager manager = LostItemMissionManager.Instance;
            if (manager == null) return "";
            if (manager.LostItemCollected.Value) return "";
            if (manager.CurrentPhase.Value != LostItemMissionManager.MissionPhase.Searching) return "";
            if (locallyChecked) return $"已排除: {itemName}";
            return manager.BonusEvidenceCollected.Value
                ? $"核对并排除: {itemName}"
                : $"翻看可疑作业本: {itemName}";
        }
    }

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public void OnInteractStart(PlayerController player)
    {
        if (locallyChecked) return;
        if (player != null && player.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
            return;

        locallyChecked = true;
        LostItemMissionManager manager = LostItemMissionManager.Instance;
        if (manager == null || manager.BonusEvidenceCollected.Value) return;

        manager.RequestWrongHomeworkAttempt(transform.position, pickupRadius);
    }

    public void OnInteractEnd(PlayerController player) { }
}
