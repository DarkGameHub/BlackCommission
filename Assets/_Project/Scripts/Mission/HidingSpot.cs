using UnityEngine;

public class HidingSpot : MonoBehaviour, IInteractable
{
    [SerializeField] Transform hidePoint;

    public string InteractHint => "躲进柜子（按住）";

    void Awake()
    {
        if (hidePoint != null) return;

        var point = new GameObject("HidePoint");
        point.transform.SetParent(transform, false);
        point.transform.localPosition = new Vector3(0f, 0f, -0.45f);
        point.transform.localRotation = Quaternion.identity;
        hidePoint = point.transform;
    }

    public void OnInteractStart(PlayerController player)
    {
        if (player == null || hidePoint == null) return;
        player.SetHiddenFromMonsters(true, hidePoint.position, hidePoint.rotation);
    }

    public void OnInteractEnd(PlayerController player)
    {
        if (player == null) return;
        player.SetHiddenFromMonsters(false, player.transform.position, player.transform.rotation);
    }
}
