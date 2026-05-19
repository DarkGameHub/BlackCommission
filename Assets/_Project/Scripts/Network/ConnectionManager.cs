using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobby;
using Unity.Services.Lobby.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Manages host/join flow using Unity Relay (no port forwarding needed) + Lobby.
/// Call HostGame() to create a room, JoinGame(code) to join one.
/// </summary>
public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance { get; private set; }

    const string KEY_RELAY_CODE = "RelayCode";
    const int MAX_PLAYERS = 4;

    Lobby currentLobby;
    string relayJoinCode;

    public event Action<string> OnLobbyCodeGenerated;
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
        await InitializeServices();
    }

    async Task InitializeServices()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"[Connection] Signed in as {AuthenticationService.Instance.PlayerId}");
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
            // 1. Create Relay allocation (supports up to MAX_PLAYERS including host)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MAX_PLAYERS - 1);
            relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // 2. Create Lobby so friends can find the room
            CreateLobbyOptions opts = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) }
                }
            };
            currentLobby = await LobbyService.Instance.CreateLobbyAsync("外包事故组", MAX_PLAYERS, opts);

            Debug.Log($"[Connection] Lobby created. Join code: {relayJoinCode}");
            OnLobbyCodeGenerated?.Invoke(relayJoinCode);

            // 3. Configure transport and start as host
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            NetworkManager.Singleton.StartHost();
            OnConnected?.Invoke();
        }
        catch (Exception e)
        {
            OnError?.Invoke($"建房失败: {e.Message}");
            Debug.LogError($"[Connection] Host failed: {e}");
        }
    }

    public async void JoinGame(string joinCode)
    {
        try
        {
            // 1. Join via Relay code
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            // 2. Configure transport and start as client
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
            OnConnected?.Invoke();
        }
        catch (Exception e)
        {
            OnError?.Invoke($"加入失败，检查代码是否正确: {e.Message}");
            Debug.LogError($"[Connection] Join failed: {e}");
        }
    }

    public async void LeaveLobby()
    {
        if (currentLobby == null) return;
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
            currentLobby = null;
        }
        catch { }
    }

    void OnApplicationQuit()
    {
        LeaveLobby();
    }
}
