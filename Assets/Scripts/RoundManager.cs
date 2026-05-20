using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RoundManager : NetworkBehaviour
{
    public static RoundManager Instance;

    public const int MAX_FULL_WINS = 3;

    [SyncVar] private bool roundInProgress = false;

    private List<PlayerController> alivePlayers = new();
    private Dictionary<uint, int> halfWins = new();
    private Dictionary<uint, int> fullWins = new();
    private uint lastFullWinPlayerId = 0; // Track the most recent player to get a full win

    [Header("Spawn Corners")]
    public Transform[] cornerSpawnPoints;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnLastFullWinPlayerChanged(uint oldValue, uint newValue)
    {
        Debug.Log($"Last full win player changed from {oldValue} to {newValue}");
        // Force update the UI when the last full win player changes
        UpdateScoreUI();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Initialize full wins dictionary on client
        fullWins = new Dictionary<uint, int>();
    }

    [Server]
    public void StartGame()
    {
        StartCoroutine(BeginInitialCardPick());
    }

    [Server]
    IEnumerator BeginInitialCardPick()
    {
        yield return new WaitForSeconds(2f);
        
        // Skip card picker for host
        if (isServer && isClient)
        {
            Debug.Log("Skipping initial card pick for host");
            GameStateManager.Instance.StartGame();
            yield break;
        }
        
        GameStateManager.Instance.StartGame();
    }

    [Server]
    public void RegisterPlayer(PlayerController player)
    {
        if (!alivePlayers.Contains(player))
        {
            alivePlayers.Add(player);
            player.SetAlive(true);
            
            // Assign a random skin to the player
            if (SkinManager.Instance != null)
            {
                SkinManager.Instance.AssignRandomSkin(player.netId);
            }
        }
    }

    [Server]
    public void UnregisterPlayer(PlayerController player)
    {
        if (!alivePlayers.Contains(player)) return;

        alivePlayers.Remove(player);
        player.SetAlive(false);
        player.DespawnPlayer();

        Debug.Log($"Player {player.netId} was unregistered. {alivePlayers.Count} players remaining.");

        if (alivePlayers.Count == 1 && roundInProgress)
        {
            var winner = alivePlayers[0];
            Debug.Log($"Round winner detected: Player {winner.netId}");
            winner.DespawnPlayer();
            roundInProgress = false;
            
            // Award the win first
            AwardHalfWin(winner);
            
            // Show card picker for the winner
            if (CardPicker.Instance != null)
            {
                // Reset the winner's hasPicked state
                winner.hasPicked = false;
                winner.SetHasPicked(false);
                
                // Disable damage during card picking
                if (GameStateManager.Instance != null)
                {
                    GameStateManager.Instance.damageEnabled = false;
                }
                
                CardPicker.Instance.RpcShowPicker();
                
                // Wait for card picking to complete before continuing
                StartCoroutine(WaitForCardPickAndContinue(winner));
            }
            else
            {
                Debug.LogWarning("CardPicker.Instance is null! Skipping card pick for winner.");
                StartCoroutine(NextRoundDelay());
            }
        }
    }

    [Server]
    private IEnumerator WaitForCardPickAndContinue(PlayerController winner)
    {
        Debug.Log($"Waiting for player {winner.netId} to pick a card...");
        
        // Wait until the winner has picked their card
        while (!winner.hasPicked)
        {
            yield return null;
        }

        Debug.Log($"Player {winner.netId} has picked their card, continuing...");

        // Re-enable damage after card picking
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.damageEnabled = true;
        }

        // Start next round instead of awarding win again
        StartCoroutine(NextRoundDelay());
    }

    [Server]
    void AwardHalfWin(PlayerController winner)
    {
        Debug.Log($"AwardHalfWin called for player {winner.netId}");
        
        uint id = winner.netId;
        
        // Initialize dictionaries if needed
        if (!halfWins.ContainsKey(id))
        {
            halfWins[id] = 0;
            Debug.Log($"Initialized half wins for player {id}");
        }
        
        if (!fullWins.ContainsKey(id))
        {
            fullWins[id] = 0;
            Debug.Log($"Initialized full wins for player {id}");
        }
        
        // Log current half wins before incrementing
        Debug.Log($"Current half wins for player {id}: {halfWins[id]}");
        
        halfWins[id]++;
        Debug.Log($"Player {id} earned a half win! Total half wins: {halfWins[id]}");

        // Send message to GameConsole
        if (GameConsole.Instance != null)
        {
            string message = $"Player {id} earned a half win! ({halfWins[id]}/2)";
            GameConsole.Instance.AddMessage(message);
        }

        bool fullRoundWon = false;

        if (halfWins[id] >= 2)
        {
            Debug.Log($"Player {id} has reached 2 half wins, converting to full win");
            // Log the half wins before clearing them
            Debug.Log($"Converting {halfWins[id]} half wins to a full win for player {id}");
            
            // Store the current full wins count before clearing
            int currentFullWins = fullWins.ContainsKey(id) ? fullWins[id] : 0;
            
            // Only clear half wins for all players
            halfWins.Clear();
            
            // Only update full wins for the current player
            fullWins[id] = currentFullWins + 1;
            fullRoundWon = true;
            
            // Update the last player to get a full win
            lastFullWinPlayerId = id;
            
            Debug.Log($"Player {id} earned a full win! Total full wins: {fullWins[id]}/{MAX_FULL_WINS}");

            // Send message to GameConsole
            if (GameConsole.Instance != null)
            {
                string message = $"Player {id} earned a full win! ({fullWins[id]}/{MAX_FULL_WINS})";
                GameConsole.Instance.AddMessage(message);
            }

            if (fullWins[id] >= MAX_FULL_WINS)
            {
                Debug.Log($"GAME OVER! Player {id} has won the game with {fullWins[id]} full wins!");
                // Send game over message to GameConsole
                if (GameConsole.Instance != null)
                {
                    string message = $"GAME OVER! Player {id} has won the game!";
                    GameConsole.Instance.AddMessage(message);
                }
                return;
            }

            // Reset full win restrictions for all players when someone gets a full win
            ResetFullWinRestrictions();
        }

        // Log current standings
        Debug.Log("Current Standings:");
        foreach (var kvp in fullWins)
        {
            Debug.Log($"Player {kvp.Key}: {kvp.Value} full wins");
        }
        foreach (var kvp in halfWins)
        {
            Debug.Log($"Player {kvp.Key}: {kvp.Value} half wins");
        }

        UpdateScoreUI();

        if (fullRoundWon)
        {
            if (CardPicker.Instance == null)
            {
                Debug.LogError("CardPicker.Instance is null! Cannot start loser card pick.");
                StartCoroutine(NextRoundDelay());
                return;
            }

            List<PlayerController> losers = new();
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null && conn.identity.netId != id)
                {
                    var pc = conn.identity.GetComponent<PlayerController>();
                    losers.Add(pc);
                }
            }
            Debug.Log($"Starting card pick for {losers.Count} losers after full win by player {id}");

            // Reset the winner's hasPicked state
            winner.hasPicked = false;
            winner.SetHasPicked(false);
            
            // Disable damage during card picking
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.damageEnabled = false;
            }
            
            CardPicker.Instance.RpcShowPicker();
            
            // Wait for card picking to complete before continuing
            StartCoroutine(WaitForCardPickAndContinue(winner));
        }
        else
        {
            Debug.Log($"Starting next round after half win by player {id}");
            StartCoroutine(NextRoundDelay());
        }
    }

    [Server]
    IEnumerator NextRoundDelay()
    {
        yield return new WaitForSeconds(2f);
        DespawnAllPlayers();
        yield return new WaitForSeconds(1f);
        
        Debug.Log("Starting new round after delay...");
        
        // Respawn all players at their spawn points
        List<PlayerController> players = GetAllPlayers();
        for (int i = 0; i < players.Count; i++)
        {
            var pc = players[i];
            Vector3 spawnPos = cornerSpawnPoints[i % cornerSpawnPoints.Length].position;
            pc.SpawnPlayer(spawnPos);
            RegisterPlayer(pc);
        }
        
        StartRound();
    }

    [Server]
    public void StartRound()
    {
        if (cornerSpawnPoints == null || cornerSpawnPoints.Length == 0)
        {
            Debug.LogError("No corner spawn points set in RoundManager! Please set up spawn points in the Unity Inspector.");
            return;
        }

        Debug.Log("New round starting! Players are being spawned...");
        roundInProgress = true;
        alivePlayers.Clear();

        // Reset all players' hasPicked state
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                var pc = conn.identity.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.hasPicked = false;
                    pc.SetHasPicked(false);
                }
            }
        }

        List<PlayerController> players = GetAllPlayers();
        for (int i = 0; i < players.Count; i++)
        {
            var pc = players[i];
            Vector3 spawnPos = cornerSpawnPoints[i % cornerSpawnPoints.Length].position;
            pc.SpawnPlayer(spawnPos);
            RegisterPlayer(pc);
        }
    }

    [Server]
    void DespawnAllPlayers()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                PlayerController pc = conn.identity.GetComponent<PlayerController>();
                pc.DespawnPlayer();
            }
        }
    }

    [Server]
    List<PlayerController> GetAllPlayers()
    {
        List<PlayerController> result = new();
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
                result.Add(conn.identity.GetComponent<PlayerController>());
        }
        return result;
    }

    [Server]
    public void ResetFullWinRestrictions()
    {
        // Store the current full wins counts
        Dictionary<uint, int> tempFullWins = new Dictionary<uint, int>(fullWins);
        
        // Clear the full wins dictionary
        fullWins.Clear();
        
        // Restore the full wins counts
        foreach (var kvp in tempFullWins)
        {
            fullWins[kvp.Key] = kvp.Value;
        }
        
        // Reset all players' hasPicked state
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity != null)
            {
                var pc = conn.identity.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.SetHasPicked(false);
                }
            }
        }
        
        UpdateScoreUI();
    }

    [Server]
    public bool HasFullWins(uint playerId)
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("HasFullWins called when server was not active");
            return false;
        }
        // A player can't pick cards if they are the most recent player to get a full win
        return playerId == lastFullWinPlayerId;
    }

    [Server]
    void UpdateScoreUI()
    {
        if (RoundUIManager.Instance == null) return;

        List<uint> ids = new();
        List<int> scores = new();

        foreach (var kvp in fullWins)
        {
            ids.Add(kvp.Key);
            scores.Add(kvp.Value);
        }

        // Add the last full win player to the UI if they're not already included
        if (lastFullWinPlayerId != 0 && !ids.Contains(lastFullWinPlayerId))
        {
            ids.Add(lastFullWinPlayerId);
            scores.Add(fullWins.ContainsKey(lastFullWinPlayerId) ? fullWins[lastFullWinPlayerId] : 0);
        }

        RoundUIManager.Instance.RpcUpdateScores(ids, scores);
    }

    [Server]
    public void ResetRound()
    {
        // Reset round state
        roundInProgress = false;
        alivePlayers.Clear();
        halfWins.Clear();
        fullWins.Clear();
        lastFullWinPlayerId = 0;
        
        // Reset any other round-specific state
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.gameStarted = false;
            GameStateManager.Instance.gameStartedA = false;
            GameStateManager.Instance.damageEnabled = false;
        }
        
        Debug.Log("Round state reset");
    }
}
