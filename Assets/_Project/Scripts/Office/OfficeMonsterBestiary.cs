using UnityEngine;

[RequireComponent(typeof(Collider))]
public class OfficeMonsterBestiary : MonoBehaviour, IInteractable
{
    public string InteractHint => "View monster bestiary";

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public void OnInteractStart(PlayerController player)
    {
        if (player != null && player.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
            return;

        MvpHud.OpenBestiary(this);
    }

    public void OnInteractEnd(PlayerController player) { }
}
