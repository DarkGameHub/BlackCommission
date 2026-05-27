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
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return "先创建主机再出车";
            if (!NetworkManager.Singleton.IsHost) return "等待房主发车";
            if (computer.HasSelectedDemoTask) return $"上车出发: {computer.DemoTaskTitle}";
            return "先在电脑锁定委托";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        OfficeComputer computer = GetComputer();
        if (computer == null) return;
        if (!computer.HasSelectedDemoTask) return;
        computer.LaunchSelectedMissionFromVehicle(player);
    }

    public void OnInteractEnd(PlayerController player) { }

    OfficeComputer GetComputer()
    {
        if (cachedComputer != null) return cachedComputer;
        cachedComputer = Object.FindAnyObjectByType<OfficeComputer>();
        return cachedComputer;
    }
}
