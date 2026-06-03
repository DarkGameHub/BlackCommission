using UnityEngine;
using Unity.Netcode;

public class OfficeDepartureVan : MonoBehaviour, IInteractable
{
    OfficeComputer cachedComputer;

    public string InteractHint
    {
        get
        {
            OfficeComputer computer = GetComputer();
            if (computer == null) return "车库电脑离线";
            if (MvpPendingReward.HasPending) return "先去电脑领取结算";
            if (VanTransitOverlay.IsActive) return "司机已经发车";
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return "先创建主机再出车";
            if (!computer.HasSelectedDemoTask) return "先去绿光 COMPUTER 终端锁定委托";
            PlayerController.GetSeatedCounts(out int seated, out int total);
            if (!NetworkManager.Singleton.IsHost) return $"[E]上车  {seated}/{total} 已就座";
            if (total > 0 && seated < total)
                return $"等待全员上车  {seated}/{total}";
            return "[SPACE] 发车";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        // E at the HQ van seats the interacting player inside the cabin. Anyone can board;
        // departure (Space) is gated server-side so the van won't leave until all aboard.
        OfficeComputer computer = GetComputer();
        if (computer == null) return;
        if (!computer.HasSelectedDemoTask) return;
        if (VanTransitOverlay.IsActive && player != null && player.IsSeated) return;

        if (player != null)
            player.RequestSeat();

        string title = computer.DemoTaskTitle;
        string location = computer.DemoTaskLocation;
        VanTransitOverlay.ShowBoarding(title, location, true);
    }

    public void OnInteractEnd(PlayerController player) { }

    OfficeComputer GetComputer()
    {
        if (cachedComputer != null) return cachedComputer;
        cachedComputer = Object.FindAnyObjectByType<OfficeComputer>();
        return cachedComputer;
    }
}
