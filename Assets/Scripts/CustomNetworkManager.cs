using Mirror;
using UnityEngine;
using kcp2k;
using System.Collections.Generic;

public class CustomNetworkManager : NetworkManager
{
    [Header("Player Spawn Settings")]
    public Transform[] spawnPoints;
    public new NetworkManagerMode mode = NetworkManagerMode.Offline;

    [Header("Network Settings")]
    public string serverAddress = "127.0.0.1";
    public ushort serverPort = 7777;
    public bool useRelay = false;

    private KcpTransport kcpTransport;
    private bool isServer => NetworkServer.active;

    // Message queue to store messages that need to be sent to clients
    private Queue<string> messageQueue = new Queue<string>();

    // Dictionary to store player names
    private Dictionary<uint, string> playerNames = new Dictionary<uint, string>();

    public void SetPlayerName(string name)
    {
        if (NetworkClient.localPlayer != null)
        {
            uint playerId = NetworkClient.localPlayer.netId;
            playerNames[playerId] = name;
            Debug.Log($"Set player name for {playerId}: {name}");
        }
    }

    public string GetPlayerName(uint playerId)
    {
        return playerNames.TryGetValue(playerId, out string name) ? name : $"Player {playerId}";
    }

    public override void Awake()
    {
        base.Awake();
        
        // Configure KCP transport
        kcpTransport = GetComponent<KcpTransport>();
        if (kcpTransport == null)
        {
            kcpTransport = gameObject.AddComponent<KcpTransport>();
        }

        // Set KCP transport as the active transport
        transport = kcpTransport;

        // Configure KCP settings for better performance
        kcpTransport.NoDelay = true;
        kcpTransport.Interval = 1;
        kcpTransport.FastResend = 2;
        kcpTransport.SendWindowSize = 4096;
        kcpTransport.ReceiveWindowSize = 4096;
        kcpTransport.Timeout = 10000;
    }

    public new void StartHost()
    {
        if (useRelay)
        {
            Debug.Log("Starting as relay host...");
            NetworkServer.Listen(serverPort);
        }
        else
        {
            Debug.Log("Starting as direct host...");
            base.StartHost();
        }
    }

    public new void StartClient()
    {
        if (useRelay)
        {
            Debug.Log($"Connecting to relay server at {serverAddress}:{serverPort}");
            networkAddress = serverAddress;
            base.StartClient();
        }
        else
        {
            Debug.Log("Connecting directly...");
            base.StartClient();
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("Server started");
        
        // Create the GameConsole
        GameObject consolePrefab = Resources.Load<GameObject>("GameConsole");
        if (consolePrefab != null)
        {
            GameObject console = Instantiate(consolePrefab);
            Debug.Log("GameConsole created on server");
        }
        else
        {
            Debug.LogError("GameConsole prefab not found in Resources folder!");
        }
        
        // Try to find spawn points if none are assigned
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (spawnPointObjects.Length > 0)
            {
                spawnPoints = new Transform[spawnPointObjects.Length];
                for (int i = 0; i < spawnPointObjects.Length; i++)
                {
                    spawnPoints[i] = spawnPointObjects[i].transform;
                }
                Debug.Log($"Found {spawnPoints.Length} spawn points in scene");
            }
            else
            {
                Debug.LogWarning("No spawn points found in scene. Creating default spawn points...");
                CreateDefaultSpawnPoints();
            }
        }
    }

    private void CreateDefaultSpawnPoints()
    {
        // Create 4 spawn points in a square formation
        spawnPoints = new Transform[4];
        float spacing = 5f; // Space between spawn points
        
        for (int i = 0; i < 4; i++)
        {
            GameObject spawnPoint = new GameObject($"SpawnPoint_{i}");
            spawnPoint.tag = "SpawnPoint";
            
            // Position in a square formation
            float x = (i % 2 == 0 ? -1 : 1) * spacing;
            float z = (i < 2 ? -1 : 1) * spacing;
            spawnPoint.transform.position = new Vector3(x, 0, z);
            
            spawnPoints[i] = spawnPoint.transform;
        }
        
        Debug.Log("Created 4 default spawn points in a square formation");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("Client started");

        // Create the GameConsole for clients
        if (!isServer) // Only create on clients, server already has one
        {
            GameObject consolePrefab = Resources.Load<GameObject>("GameConsole");
            if (consolePrefab != null)
            {
                GameObject console = Instantiate(consolePrefab);
                Debug.Log("GameConsole created on client");
            }
            else
            {
                Debug.LogError("GameConsole prefab not found in Resources folder!");
            }
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("Client stopped");
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log($"Player connected: {conn.connectionId}");
        
        if (numPlayers >= 4)
        {
            conn.Disconnect(); // Rejects excess players
            if (GameConsole.Instance != null)
            {
                string message = $"Player {conn.connectionId} was rejected (server full)";
                GameConsole.Instance.AddMessage(message);
                SendConsoleMessageToAll(message);
            }
        }
        else if (GameConsole.Instance != null)
        {
            string message = $"Player {conn.connectionId} has joined";
            GameConsole.Instance.AddMessage(message);
            SendConsoleMessageToAll(message);
        }
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Unregister the player from the lobby manager before disconnecting
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.UnregisterPlayer(conn);
        }
        
        base.OnServerDisconnect(conn);
        Debug.Log($"Player disconnected: {conn.connectionId}");
        
        if (GameConsole.Instance != null)
        {
            string message = $"Player {conn.connectionId} has left";
            GameConsole.Instance.AddMessage(message);
            SendConsoleMessageToAll(message);
        }
    }

    // Send a message to all connected clients
    private void SendConsoleMessageToAll(string message)
    {
        if (!isServer) return;

        // Add message to queue
        messageQueue.Enqueue(message);

        // Send to all clients
        foreach (NetworkConnectionToClient conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.isReady)
            {
                conn.Send(new ConsoleMessage { message = message });
            }
        }
    }

    // Handle incoming console messages from clients
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        NetworkClient.RegisterHandler<ConsoleMessage>(OnConsoleMessage);
    }

    private void OnConsoleMessage(ConsoleMessage message)
    {
        if (GameConsole.Instance != null)
        {
            GameConsole.Instance.AddMessage(message.message);
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Find a spawn point
        Transform spawnPoint = GetNextSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogError("No spawn points available!");
            return;
        }

        // Create the player
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        if (player == null)
        {
            Debug.LogError("Failed to instantiate player!");
            return;
        }

        // Add the player to the connection
        NetworkServer.AddPlayerForConnection(conn, player);
        Debug.Log($"Player added for connection {conn.connectionId} at position {spawnPoint.position}");

        // Register with LobbyManager if it exists
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.RegisterPlayer(conn, player);
        }
        else
        {
            Debug.LogWarning("LobbyManager.Instance is null - player registration skipped");
        }

        // Assign BlackCat skin to the first player, random unique to others
        var pc = player.GetComponent<PlayerController>();
        if (pc != null && SkinManager.Instance != null)
        {
            if (numPlayers == 1)
            {
                // Assign BlackCatAC (index 0)
                var blackCatSkin = SkinManager.Instance.GetType().GetField("availableSkins", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(SkinManager.Instance) as System.Collections.IList;
                if (blackCatSkin != null && blackCatSkin.Count > 0)
                {
                    var skin = blackCatSkin[0] as RuntimeAnimatorController;
                    if (skin != null)
                    {
                        // Get the current collections
                        var playerSkins = SkinManager.Instance.GetType().GetField("playerSkins", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(SkinManager.Instance) as System.Collections.Generic.Dictionary<uint, RuntimeAnimatorController>;
                        var activeSkins = SkinManager.Instance.GetType().GetField("activeSkins", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(SkinManager.Instance) as System.Collections.Generic.HashSet<RuntimeAnimatorController>;
                        
                        if (playerSkins != null && activeSkins != null)
                        {
                            // Remove any existing skin for this player
                            SkinManager.Instance.RemovePlayerSkin(pc.netId);
                            
                            // Assign the new skin
                            playerSkins[pc.netId] = skin;
                            activeSkins.Add(skin);
                            
                            // Update the SyncVar with all current assignments
                            SkinManager.Instance.GetType().GetMethod("UpdateAssignedSkinsSyncVar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(SkinManager.Instance, null);
                            Debug.Log($"Assigned {skin.name} skin to first player");
                        }
                    }
                }
            }
            else
            {
                // For subsequent players, use the normal random skin assignment
                SkinManager.Instance.AssignRandomSkin(pc.netId);
            }
        }
    }

    private Transform GetNextSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned!");
            return null;
        }

        // Simple round-robin spawn point selection
        return spawnPoints[numPlayers % spawnPoints.Length];
    }

    public bool IsSinglePlayer()
    {
        return mode == NetworkManagerMode.Offline;
    }

    public override void OnStopHost()
    {
        // Reset game state
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ResetGame();
        }

        // Clear the lobby when host stops
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.ClearLobby();
        }

        // Reset round manager if it exists
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.ResetRound();
        }
        
        base.OnStopHost();
        Debug.Log("Host stopped and game state reset");
    }
}

// Network message structure for console messages
public struct ConsoleMessage : NetworkMessage
{
    public string message;
}