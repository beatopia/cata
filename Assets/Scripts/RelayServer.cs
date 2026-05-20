using UnityEngine;
using Mirror;
using kcp2k;
using System.Net;
using System.Net.NetworkInformation;

public class RelayServer : MonoBehaviour
{
    public ushort port = 7777;
    public bool startOnAwake = true;
    private KcpTransport transport;
    private string localIP;
    private string publicIP;

    void Awake()
    {
        // Get or add KCP transport
        transport = GetComponent<KcpTransport>();
        if (transport == null)
        {
            transport = gameObject.AddComponent<KcpTransport>();
        }

        // Configure transport
        NetworkManager.singleton.transport = transport;
        transport.Port = port;

        // Get local IP
        localIP = GetLocalIPAddress();
    }

    void Start()
    {
        if (startOnAwake)
        {
            StartServer();
        }
    }

    public void StartServer()
    {
        NetworkServer.Listen(port);
        Debug.Log($"Relay server started on port {port}");
        Debug.Log($"Local IP: {localIP}");
        Debug.Log($"To use with ngrok, run: ngrok tcp {port}");
        Debug.Log("Then use the ngrok address (without tcp://) in your game's server address");
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    void OnGUI()
    {
        if (NetworkServer.active)
        {
            int y = 10;
            GUI.Label(new Rect(10, y, 400, 20), $"Relay Server Running on port {port}");
            y += 20;
            GUI.Label(new Rect(10, y, 400, 20), $"Local IP: {localIP}");
            y += 20;
            GUI.Label(new Rect(10, y, 400, 20), $"Connected Clients: {NetworkServer.connections.Count}");
            y += 20;
            GUI.Label(new Rect(10, y, 400, 20), "To use with ngrok:");
            y += 20;
            GUI.Label(new Rect(10, y, 400, 20), $"1. Run: ngrok tcp {port}");
            y += 20;
            GUI.Label(new Rect(10, y, 400, 20), "2. Use the ngrok address (without tcp://) in your game");
        }
    }
} 