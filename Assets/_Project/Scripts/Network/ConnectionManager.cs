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
/// Relay-only multiplayer connection. No lobby needed for friend groups.
/// Host calls HostGame() → shares the 6-char join code via Discord/chat.
/// Clients call JoinGame(code) to connect.
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    const int MAX_CONNECTIONS = 3; // host + 3 clients = 4 players

    public event Action<string> OnJoinCodeReady;  // fires with join code when host is ready
    public event Action OnConnected;
    public event Action<string> OnError;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        await InitServices();
    }

    async Task InitServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        catch (Exception e)
        {
            OnError?.Invoke($"服务初始化失败: {e.Message}");
        }
    }

    public async void HostGame()
    {
        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(MAX_CONNECTIONS);
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
            OnError?.Invoke($"建房失败: {e.Message}");
            Debug.LogError(e);
        }
    }

    public async void JoinGame(string joinCode)
    {
        try
        {
            JoinAllocation alloc = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(new RelayServerData(alloc, "dtls"));

            NetworkManager.Singleton.StartClient();
            OnConnected?.Invoke();

            Debug.Log($"[Connection] Joined game with code: {joinCode}");
        }
        catch (Exception e)
        {
            OnError?.Invoke($"加入失败，确认代码正确: {e.Message}");
            Debug.LogError(e);
        }
    }
}
