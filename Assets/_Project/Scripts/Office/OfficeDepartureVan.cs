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
            if (computer == null) return "Garage terminal offline";
            if (MvpPendingReward.HasPending) return "Collect your settlement at the terminal first";
            if (VanTransitOverlay.IsActive) return "Van already departed";
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return "Create a host session before departing";
            if (!computer.HasSelectedDemoTask) return "Lock a commission at the green COMPUTER terminal first";
            PlayerController.GetSeatedCounts(out int seated, out int total);
            if (!NetworkManager.Singleton.IsHost) return $"[E] Board  {seated}/{total} seated";
            if (total > 0 && seated < total)
                return $"Waiting for all crew to board  {seated}/{total}";
            return "[SPACE] Depart";
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
