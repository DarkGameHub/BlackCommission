using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SchoolMonsterTraceItem : MonoBehaviour, IInteractable
{
    [SerializeField] string traceName = "怪物毛发样本";

    bool locallyHidden;

    public string InteractHint
    {
        get
        {
            if (MonsterBestiaryProgress.IsHomeworkDebtCollectorUnlocked) return "";
            return MonsterBestiaryProgress.HasEncounteredHomeworkDebtCollector
                ? $"采集踪迹: {traceName}"
                : "可疑踪迹: 先遭遇异常后再采样";
        }
    }

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Update()
    {
        bool shouldHide = MonsterBestiaryProgress.IsHomeworkDebtCollectorUnlocked;
        if (shouldHide == locallyHidden) return;

        locallyHidden = shouldHide;
        SetVisualsActive(!shouldHide);
    }

    public void OnInteractStart(PlayerController player)
    {
        if (player != null && player.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
            return;

        if (MonsterBestiaryProgress.TryCollectHomeworkDebtCollectorTrace())
        {
            locallyHidden = true;
            SetVisualsActive(false);
        }
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
