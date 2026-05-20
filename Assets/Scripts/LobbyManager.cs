using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    private Dictionary<NetworkConnectionToClient, GameObject> lobbyPlayers = new();
    public int maxPlayers = 4;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        NetworkManager.singleton.maxConnections = 4;
    }

    public override void OnStopServer()
    {
        ClearLobby();
    }

    public void ClearLobby()
    {
        // Clear all players from the lobby
        lobbyPlayers.Clear();
        Debug.Log("Lobby cleared");
    }

    public void UnregisterPlayer(NetworkConnectionToClient conn)
    {
        if (lobbyPlayers.ContainsKey(conn))
        {
            // If the game was started and a player leaves, reset the game state
            if (GameStateManager.Instance != null && GameStateManager.Instance.gameStarted)
            {
                GameStateManager.Instance.ResetGame();
            }
            
            lobbyPlayers.Remove(conn);
            Debug.Log($"Player {conn.connectionId} unregistered");
        }
    }

    public void RegisterPlayer(NetworkConnectionToClient conn, GameObject playerObj)
    {
        if (lobbyPlayers.ContainsKey(conn))
        {
            Debug.Log($"Player {conn.connectionId} is already registered");
            return;
        }

        lobbyPlayers.Add(conn, playerObj);
        Debug.Log($"Player {conn.connectionId} registered successfully");
    }

    public void OnClickStartMatch()
    {
        if (!NetworkServer.active) return;

        // Prevent late joins
        NetworkManager.singleton.maxConnections = lobbyPlayers.Count;
        
        // Spawn the RoundManager if it doesn't exist
        if (RoundManager.Instance == null)
        {
            // Find the RoundManager prefab in NetworkManager's spawnPrefabs
            GameObject roundManagerPrefab = NetworkManager.singleton.spawnPrefabs.Find(prefab => prefab.GetComponent<RoundManager>() != null);
            if (roundManagerPrefab == null)
            {
                Debug.LogError("RoundManager prefab not found in NetworkManager's spawnPrefabs!");
                return;
            }
            
            GameObject roundManager = Instantiate(roundManagerPrefab);
            NetworkServer.Spawn(roundManager);
            Debug.Log("RoundManager spawned");
        }

        // Ensure GameStateManager exists
        if (GameStateManager.Instance == null)
        {
            Debug.LogError("GameStateManager not found in scene! Please add it to your scene.");
            return;
        }
        
        // Start the round
        RoundManager.Instance.StartRound();
        
        // Start the game through RoundManager
        RoundManager.Instance.StartGame();
        Debug.Log("Game started! Waiting for players to pick their cards...");
    }

    public int GetCurrentPlayerCount()
    {
        return lobbyPlayers.Count;
    }

    [Server]
    public void StartGame()
    {
        if (!NetworkServer.active) return;
        OnClickStartMatch();
    }
}
