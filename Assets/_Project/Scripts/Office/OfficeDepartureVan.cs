using UnityEngine;
using Unity.Netcode;

public class OfficeDepartureVan : MonoBehaviour, IInteractable
{
    const float BoardingHorizontalPadding = 0.2f;
    const float BoardingBelowPadding = 0.45f;
    const float BoardingHeadroomPadding = 1.25f;

    OfficeComputer cachedComputer;
    BoxCollider cachedBoardingTrigger;

    public string InteractHint
    {
        get
        {
            OfficeComputer computer = GetComputer();
            if (computer == null) return "车库电脑离线";
            if (MvpPendingReward.HasPending) return "先去电脑领取结算";
            if (VanTransitOverlay.IsActive) return "司机已经发车";
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return "先创建主机再出车";
            if (!NetworkManager.Singleton.IsHost) return "等待房主发车";
            if (computer.HasSelectedDemoTask)
            {
                GetBoardingCounts(out int boarded, out int total);
                if (total > 0 && boarded < total)
                    return $"车内 {boarded}/{total}: 等所有队员上车";

                return $"关门开车: {computer.DemoTaskTitle}";
            }
            return "先在电脑锁定委托";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        OfficeComputer computer = GetComputer();
        if (computer == null) return;
        if (!computer.HasSelectedDemoTask) return;
        if (!IsEveryoneBoarded()) return;
        computer.LaunchSelectedMissionFromVehicle(player);
    }

    public void OnInteractEnd(PlayerController player) { }

    OfficeComputer GetComputer()
    {
        if (cachedComputer != null) return cachedComputer;
        cachedComputer = Object.FindAnyObjectByType<OfficeComputer>();
        return cachedComputer;
    }

    bool IsEveryoneBoarded()
    {
        GetBoardingCounts(out int boarded, out int total);
        return total > 0 && boarded >= total;
    }

    void GetBoardingCounts(out int boarded, out int total)
    {
        boarded = 0;
        total = 0;

        Bounds bounds = GetBoardingBounds();
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening)
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            total = player != null ? 1 : 0;
            boarded = player != null && IsPointInsideBoardingArea(player.transform.position, bounds) ? 1 : 0;
            return;
        }

        foreach (var pair in network.ConnectedClients)
        {
            NetworkObject playerObject = pair.Value.PlayerObject;
            if (playerObject == null) continue;

            if (playerObject.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
                continue;

            total++;
            if (IsPointInsideBoardingArea(playerObject.transform.position, bounds))
                boarded++;
        }
    }

    bool IsPointInsideBoardingArea(Vector3 worldPosition, Bounds fallbackBounds)
    {
        if (cachedBoardingTrigger != null)
        {
            Vector3 localPoint = cachedBoardingTrigger.transform.InverseTransformPoint(worldPosition) - cachedBoardingTrigger.center;
            Vector3 halfSize = cachedBoardingTrigger.size * 0.5f;
            return Mathf.Abs(localPoint.x) <= halfSize.x + BoardingHorizontalPadding &&
                   Mathf.Abs(localPoint.z) <= halfSize.z + BoardingHorizontalPadding &&
                   localPoint.y >= -halfSize.y - BoardingBelowPadding &&
                   localPoint.y <= halfSize.y + BoardingHeadroomPadding;
        }

        return worldPosition.x >= fallbackBounds.min.x - BoardingHorizontalPadding &&
               worldPosition.x <= fallbackBounds.max.x + BoardingHorizontalPadding &&
               worldPosition.z >= fallbackBounds.min.z - BoardingHorizontalPadding &&
               worldPosition.z <= fallbackBounds.max.z + BoardingHorizontalPadding &&
               worldPosition.y >= fallbackBounds.min.y - BoardingBelowPadding &&
               worldPosition.y <= fallbackBounds.max.y + BoardingHeadroomPadding;
    }

    Bounds GetBoardingBounds()
    {
        if (cachedBoardingTrigger == null)
            TryGetComponent(out cachedBoardingTrigger);

        if (cachedBoardingTrigger != null)
            return cachedBoardingTrigger.bounds;

        return new Bounds(transform.position, new Vector3(2.8f, 1.9f, 3.5f));
    }
}
