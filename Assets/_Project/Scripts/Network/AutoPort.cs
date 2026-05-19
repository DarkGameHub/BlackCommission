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

    void Awake()
    {
        var transport = GetComponent<UnityTransport>();
        ushort port = FindFreePort(basePort);
        transport.SetConnectionData("127.0.0.1", port);
        Debug.Log($"[AutoPort] Using port {port}");
    }

    static ushort FindFreePort(ushort start)
    {
        for (ushort p = start; p < start + 20; p++)
        {
            try
            {
                var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sock.Bind(new IPEndPoint(IPAddress.Any, p));
                sock.Close();
                return p;
            }
            catch { }
        }
        return start;
    }
}
