using UnityEngine;

/// <summary>
/// Van-side depart trigger for a scavenging run: interacting asks the
/// <see cref="ScavengeMissionManager"/> to settle the loaded cargo and head back (mirrors the
/// tower's <c>TowerVanDepartLever</c>). Plain MonoBehaviour — authority routing lives in the
/// manager; this only needs a collider so the E-key interaction raycast can hit it.
/// </summary>
public class ScavengeVanDepartTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] ScavengeMissionManager manager;

    ScavengeMissionManager Manager => manager != null ? manager : ScavengeMissionManager.Instance;

    public string InteractHint
    {
        get
        {
            var m = Manager;
            return m == null || m.IsTerminalState ? "" : MvpLocale.Pick("Depart and settle cargo", "发车结算（搬完上车）");
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        var m = Manager;
        if (m == null || m.IsTerminalState) return;
        AudioManager.Instance?.PlayLever(transform.position);
        m.RequestDepart();
    }

    public void OnInteractEnd(PlayerController player) { }
}
