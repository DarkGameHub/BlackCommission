using UnityEngine;

/// <summary>
/// Van-side depart lever for the tower commission: pulling it asks the
/// <see cref="TowerMissionManager"/> to resolve departure (full / partial settlement).
/// Plain MonoBehaviour — authority routing lives in the manager.
/// </summary>
public class TowerVanDepartLever : MonoBehaviour, IInteractable
{
    [SerializeField] TowerMissionManager manager;

    public string InteractHint =>
        manager == null || manager.IsTerminalState ? "" : "发车结算（拉杆）";

    public void OnInteractStart(PlayerController player)
    {
        if (manager == null || manager.IsTerminalState) return;
        AudioManager.Instance?.PlayLever(transform.position);
        manager.RequestDepart();
    }

    public void OnInteractEnd(PlayerController player) { }
}
