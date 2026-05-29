using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Relay multiplayer connection. Direct LAN hosting remains available in QuickNetworkUI
/// for editor/local testing when services are still initializing or unavailable.
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    const int MaxConnections = 3; // host + 3 clients = 4 players

    public event Action<string> OnJoinCodeReady;
    public event Action OnConnected;
    public event Action<string> OnError;

    Task initTask;
    bool servicesReady;

    public bool ServicesReady => servicesReady;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        initTask = InitServices();
        await initTask;
    }

    async Task InitServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            servicesReady = true;
        }
        catch (Exception e)
        {
            servicesReady = false;
            OnError?.Invoke($"Relay service initialization failed: {e.Message}");
        }
    }

    async Task<bool> EnsureServicesReady()
    {
        if (servicesReady) return true;
        if (initTask == null || initTask.IsCompleted)
            initTask = InitServices();
        await initTask;
        return servicesReady;
    }

    public async void HostGame()
    {
        try
        {
            if (!await EnsureServicesReady())
            {
                OnError?.Invoke("Relay services are not ready. Use Direct Host for local testing.");
                return;
            }

            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));

            NetworkManager.Singleton.StartHost();
            OnJoinCodeReady?.Invoke(joinCode);
            OnConnected?.Invoke();

            Debug.Log($"[Connection] Hosting. Join code: {joinCode}");
        }
        catch (Exception e)
        {
            OnError?.Invoke($"Failed to create Relay room: {e.Message}");
            Debug.LogError(e);
        }
    }

    public async void JoinGame(string joinCode)
    {
        try
        {
            if (!await EnsureServicesReady())
            {
                OnError?.Invoke("Relay services are not ready. Use direct connect or try again.");
                return;
            }

            JoinAllocation alloc = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));

            NetworkManager.Singleton.StartClient();
            OnConnected?.Invoke();

            Debug.Log($"[Connection] Joined game with code: {joinCode}");
        }
        catch (Exception e)
        {
            OnError?.Invoke($"Failed to join room. Check the code: {e.Message}");
            Debug.LogError(e);
        }
    }
}
