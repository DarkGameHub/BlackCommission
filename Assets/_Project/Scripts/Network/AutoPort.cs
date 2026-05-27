using System.Net;
using System.Net.Sockets;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// Picks a free UDP port before NetworkManager starts, so we never hit "port in use".
/// </summary>
[RequireComponent(typeof(UnityTransport))]
public class AutoPort : MonoBehaviour
{
    [SerializeField] ushort basePort = 7778;
    [SerializeField] int searchRange = 200;
    [SerializeField] bool assignOnAwake = false;

    void Awake()
    {
        if (!assignOnAwake) return;

        var transport = GetComponent<UnityTransport>();
        AssignFreePort(transport, basePort, searchRange);
    }

    public static ushort AssignFreePort(UnityTransport transport, ushort basePort = 7778, int searchRange = 200)
    {
        ushort port = FindFreePort(basePort, searchRange);
        transport.SetConnectionData("127.0.0.1", port);
        Debug.Log($"[AutoPort] Using UDP port {port}");
        return port;
    }

    public static ushort FindFreePort(ushort start, int searchRange = 200)
    {
        int range = Mathf.Clamp(searchRange, 1, ushort.MaxValue - start);

        for (int i = 0; i < range; i++)
        {
            ushort port = (ushort)(start + i);
            if (IsUdpPortFree(port))
                return port;
        }

        return start;
    }

    static bool IsUdpPortFree(ushort port)
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ExclusiveAddressUse = true;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, false);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            return true;
        }
        catch
        {
            return false;
        }
    }
}
