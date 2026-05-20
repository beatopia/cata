using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;

public class CardPicker : NetworkBehaviour
{
    [Header("Card Pools")]
    public List<PowerupCard> commonCards;
    public List<PowerupCard> uncommonCards;
    public List<PowerupCard> rareCards;
    public List<PowerupCard> epicCards;
    public List<PowerupCard> legendaryCards;

    [Header("UI Stuff")]
    public GameObject cardPrefab; // prefab with an Image component
    public Transform cardParent; // empty object to hold the cards

    [Header("UI References")]
    public Canvas cardPickerCanvas; // Reference to the main canvas
    public GameObject blackoutOverlay; // Reference to the blackout overlay
    public GameObject waitingMessage;  // Reference to the waiting message
    public TextMeshProUGUI waitingText; // Reference to the waiting text

    private List<GameObject> cardGameObjects = new List<GameObject>();
    private List<PlayerController> players = new List<PlayerController>();

    private static CardPicker _instance;
    public static CardPicker Instance => _instance;

    private CustomNetworkManager networkManager;
    private bool isUnlocked = false;
    private bool isWaitingForPlayers = false;

    private int lastUpdateFrame = -1;
    private List<PlayerController> cachedPlayers = new List<PlayerController>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        networkManager = FindFirstObjectByType<CustomNetworkManager>();
    }

    private void Start()
    {
        if (waitingText)
        {
            waitingText.text = "Waiting for players...";
            Debug.Log("Initial text set in Start");
        }

        // Register with GameStateManager
        if (GameStateManager.Instance != null)
        {
            Debug.Log("CardPicker: Registering with GameStateManager");
            GameStateManager.Instance.OnGameStart += OnGameStart;
        }
        else
        {
            Debug.LogError("CardPicker: GameStateManager.Instance is null!");
        }

        // Initially hide the canvas
        if (cardPickerCanvas != null)
        {
            cardPickerCanvas.gameObject.SetActive(false);
            Debug.Log("CardPicker: Initially hiding canvas");
        }
        else
        {
            Debug.LogError("CardPicker: cardPickerCanvas is null!");
        }
    }

    private void OnGameStart()
    {
        Debug.Log("CardPicker: OnGameStart called");
        if (isServer)
        {
            Debug.Log("CardPicker: Server showing picker");
            // Wait a short time to ensure all players are spawned
            Invoke(nameof(ServerShowPicker), 0.5f);
        }
    }

    [Server]
    private void ServerShowPicker()
    {
        Debug.Log("CardPicker: ServerShowPicker called");
        RpcShowPicker();
    }

    private void Update()
    {
        if (isWaitingForPlayers)
        {
            UpdatePlayerList();
            
            // Check if all players have picked and we haven't unlocked yet
            if (!isUnlocked && players.Count > 0)
            {
                bool allPicked = AreAllPlayersPicked();
                Debug.Log($"Update check - All players picked: {allPicked}, isUnlocked: {isUnlocked}");
                
                if (allPicked)
                {
                    Debug.Log("All players have picked in Update, unlocking UI");
                    UnlockUI();
                }
                else if (isServer) // Only server updates the waiting state for all clients
                {
                    // Update waiting state while waiting
                    int totalPlayers = NetworkManager.singleton.numPlayers;
                    int pickedCount = CountPickedPlayers();
                    RpcShowWaitingState(pickedCount, totalPlayers);
                }
            }
        }
    }

    private void UpdatePlayerList()
    {
        // If we've already updated this frame, use the cached list
        if (lastUpdateFrame == Time.frameCount)
        {
            players.Clear();
            players.AddRange(cachedPlayers);
            return;
        }

        // Clear the list and rebuild it with valid players
        players.Clear();
        cachedPlayers.Clear();
        
        // Get all players from NetworkServer
        var addedIds = new HashSet<uint>(); // Track added player IDs
        
        // First try to get players from NetworkServer
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn != null && conn.identity != null)
            {
                var player = conn.identity.GetComponent<PlayerController>();
                if (player != null && player.gameObject != null && !addedIds.Contains(player.netId))
                {
                    players.Add(player);
                    cachedPlayers.Add(player);
                    addedIds.Add(player.netId);
                    Debug.Log($"Added player {player.netId} to the list, hasPicked: {player.hasPicked}, isLocalPlayer: {player.isLocalPlayer}, isServer: {player.isServer}, activeInHierarchy: {player.gameObject.activeInHierarchy}");
                }
            }
        }

        // Then try to find any remaining players in the scene
        var allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in allPlayers)
        {
            if (player != null && player.gameObject != null && !addedIds.Contains(player.netId))
            {
                players.Add(player);
                cachedPlayers.Add(player);
                addedIds.Add(player.netId);
                Debug.Log($"Added player {player.netId} to the list, hasPicked: {player.hasPicked}, isLocalPlayer: {player.isLocalPlayer}, isServer: {player.isServer}, activeInHierarchy: {player.gameObject.activeInHierarchy}");
            }
        }
        
        lastUpdateFrame = Time.frameCount;
        Debug.Log($"Updated player list. Total players: {players.Count}");
        foreach (var player in players)
        {
            Debug.Log($"Player in list: {player.netId}, hasPicked: {player.hasPicked}, isLocalPlayer: {player.isLocalPlayer}, isServer: {player.isServer}, activeInHierarchy: {player.gameObject.activeInHierarchy}");
        }
    }

    [ClientRpc]
    public void RpcShowPicker()
    {
        Debug.Log("CardPicker: RpcShowPicker called");
        ShowPicker();
    }

    [ClientRpc]
    private void RpcShowWaitingState(int pickedCount, int totalPlayers)
    {
        Debug.Log($"RPC: Showing waiting state: {pickedCount}/{totalPlayers} players picked");
        ShowWaitingState(pickedCount, totalPlayers);
    }

    private void ShowWaitingState(int pickedCount, int totalPlayers)
    {
        Debug.Log($"Showing waiting state: {pickedCount}/{totalPlayers} players picked");
        
        // Make sure canvas is active
        if (cardPickerCanvas != null)
        {
            cardPickerCanvas.gameObject.SetActive(true);
        }

        // Show blackout overlay
        if (blackoutOverlay != null)
        {
            blackoutOverlay.SetActive(true);
            blackoutOverlay.transform.SetSiblingIndex(0);
        }

        // Get the local player
        PlayerController localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerController>();
        bool isLocalPlayerPicked = localPlayer != null && localPlayer.hasPicked;

        // Only show waiting message if the local player has picked
        if (isLocalPlayerPicked)
        {
            // Show waiting message
            if (waitingMessage != null)
            {
                waitingMessage.SetActive(true);
                waitingMessage.transform.SetSiblingIndex(1);
            }

            // Update waiting text
            if (waitingText != null)
            {
                // Check if the local player has full wins
                bool hasFullWins = false;
                if (isServer)
                {
                    hasFullWins = RoundManager.Instance.HasFullWins(localPlayer.netId);
                }
                else
                {
                    // On client, we can't check HasFullWins directly
                    // Instead, we'll use the hasPicked state as an indicator
                    hasFullWins = localPlayer.hasPicked;
                }

                if (hasFullWins)
                {
                    waitingText.text = "You have a full win! Waiting for other players...";
                    waitingText.color = Color.yellow;
                }
                else
                {
                    waitingText.text = $"Waiting for other players... ({pickedCount}/{totalPlayers})";
                    waitingText.color = Color.white;
                }
            }
        }
        else
        {
            // Hide waiting message if local player hasn't picked yet
            if (waitingMessage != null)
            {
                waitingMessage.SetActive(false);
            }
        }

        // Keep game paused
        Time.timeScale = 0f;
    }

    public void ShowPicker()
    {
        Debug.Log("CardPicker: ShowPicker called");
        isUnlocked = false;
        isWaitingForPlayers = true;
        
        // Reset all players' picked state
        if (isServer)
        {
            ServerResetAllPlayersPickedState();
        }

        // Check if local player has full wins and mark them as picked if they do
        PlayerController localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerController>();
        if (localPlayer != null)
        {
            bool hasFullWins = false;
            if (isServer)
            {
                hasFullWins = RoundManager.Instance.HasFullWins(localPlayer.netId);
            }
            else
            {
                // On client, we can't check HasFullWins directly
                // Instead, we'll use the hasPicked state as an indicator
                hasFullWins = localPlayer.hasPicked;
            }

            if (hasFullWins)
            {
                Debug.Log($"Player {localPlayer.netId} has full wins, automatically marking as picked");
                localPlayer.CmdSetHasPicked(true);
            }
        }

        // Show canvas and blackout overlay, but hide the waiting message
        if (cardPickerCanvas != null)
        {
            cardPickerCanvas.gameObject.SetActive(true);
        }
        if (blackoutOverlay != null)
        {
            blackoutOverlay.SetActive(true);
            blackoutOverlay.transform.SetSiblingIndex(0);
        }
        if (waitingMessage != null)
        {
            waitingMessage.SetActive(false);
        }

        // Pick cards
        PickCards();
    }

    [Server]
    private void ServerResetAllPlayersPickedState()
    {
        UpdatePlayerList();
        foreach (var player in players)
        {
            if (player != null && player.gameObject != null)
            {
                player.SetHasPicked(false);
                Debug.Log($"Server reset hasPicked for player {player.netId}, isLocalPlayer: {player.isLocalPlayer}, isServer: {player.isServer}");
            }
        }
        Debug.Log($"Reset picked state for {players.Count} players");
    }

    private void PickCards()
    {
        // Clear any existing cards first
        RemoveAllCards();

        // Get the local player
        PlayerController localPlayer = NetworkClient.localPlayer?.GetComponent<PlayerController>();
        if (localPlayer == null)
        {
            Debug.LogWarning("No local player found when trying to pick cards");
            return;
        }

        // If the local player has full wins, don't show any cards
        bool hasFullWins = false;
        if (isServer)
        {
            hasFullWins = RoundManager.Instance.HasFullWins(localPlayer.netId);
        }
        else
        {
            // On client, we can't check HasFullWins directly
            // Instead, we'll use the hasPicked state as an indicator
            hasFullWins = localPlayer.hasPicked;
        }

        if (hasFullWins)
        {
            Debug.Log($"Player {localPlayer.netId} has full wins, not showing cards");
            return;
        }

        // Only instantiate UI elements on the client side
        if (!isServer)
        {
            // Use a HashSet to track used cards and ensure no duplicates
            HashSet<string> usedCardIds = new HashSet<string>();
            
            for (int i = 0; i < 3; i++)
            {
                PowerupCard card;
                // Keep trying to get a card until we find one that hasn't been used
                do
                {
                    card = GetRandomCard();
                } while (usedCardIds.Contains(card.cardId));
                
                // Add the card ID to our used set
                usedCardIds.Add(card.cardId);

                GameObject cardGO = Instantiate(cardPrefab, cardParent);
                cardGO.GetComponent<Image>().sprite = card.frontImage;

                // Set up the card click listener
                Button cardButton = cardGO.GetComponent<Button>();
                cardButton.onClick.AddListener(() => OnCardClicked(card));

                cardGameObjects.Add(cardGO);

                CardDisplay cardDisplay = cardGO.AddComponent<CardDisplay>();
                cardDisplay.card = card;
            }
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdNotifyCardChosen(uint playerId, string effectId)
    {
        Debug.Log($"CardPicker: CmdNotifyCardChosen called for player {playerId} with effect {effectId}");

        if (GameManager.Instance == null)
        {
            Debug.LogError("CardPicker: GameManager.Instance is null!");
            return;
        }

        Debug.Log($"CardPicker: Notifying GameManager of card selection for player {playerId}");
        GameManager.Instance.CardChosen(playerId, effectId);
    }

    public void OnCardClicked(PowerupCard clickedCard)
    {
        Debug.Log($"Card clicked: {clickedCard.title} with effectId {clickedCard.effectId}");

        // Store the clicked card ID to the player using a command
        PlayerController player = NetworkClient.localPlayer?.GetComponent<PlayerController>();
        if (player != null && player.isLocalPlayer)
        {
            // Check if player has full wins
            bool hasFullWins = false;
            if (NetworkServer.active)
            {
                hasFullWins = RoundManager.Instance.HasFullWins(player.netId);
            }
            else
            {
                // On client, we can't check HasFullWins directly
                // Instead, we'll use the hasPicked state as an indicator
                hasFullWins = player.hasPicked;
            }

            if (hasFullWins)
            {
                Debug.Log($"Player {player.netId} has full wins and cannot select a card");
                return;
            }

            Debug.Log($"Local player found: {player.netId}, isLocalPlayer: {player.isLocalPlayer}, isServer: {player.isServer}");
            player.StoreCardID(clickedCard.effectId);
            player.CmdSetHasPicked(true); // Set hasPicked to true when card is selected
            Debug.Log($"Stored card {clickedCard.effectId} for player {player.netId}");

            // Notify server about card selection
            CmdNotifyCardChosen(player.netId, clickedCard.effectId);
        }
        else
        {
            Debug.LogWarning("Local player not found when trying to store card!");
        }

        // Update player list and show waiting state BEFORE removing cards
        UpdatePlayerList();
        int totalPlayers = NetworkManager.singleton.numPlayers;
        int pickedCount = CountPickedPlayers();

        // If we're the server, tell all clients to show waiting state
        if (isServer)
        {
            RpcShowWaitingState(pickedCount, totalPlayers);
        }
        else
        {
            ShowWaitingState(pickedCount, totalPlayers);
        }

        // Remove the cards after showing the waiting state
        RemoveAllCards();

        // For single player, unlock immediately
        if (networkManager != null && networkManager.IsSinglePlayer())
        {
            Debug.Log("Single player mode detected, unlocking immediately");
            UnlockUI();
        }
    }

    private int CountPickedPlayers()
    {
        int count = 0;
        foreach (var player in players)
        {
            if (player != null && player.gameObject != null && player.hasPicked)
            {
                count++;
                Debug.Log($"Player {player.netId} has picked a card, isLocalPlayer: {player.isLocalPlayer}, isServer: {player.isServer}");
            }
        }
        return count;
    }

    private bool AreAllPlayersPicked()
    {
        UpdatePlayerList(); // Make sure we have the latest player list
        int totalPlayers = players.Count;
        int pickedCount = CountPickedPlayers();
        
        Debug.Log($"Checking if all players picked: {pickedCount}/{totalPlayers} players have picked");
        
        if (totalPlayers == 0)
        {
            Debug.LogWarning("No players found in the list!");
            return false;
        }

        // Log each player's state
        foreach (var player in players)
        {
            if (player != null)
            {
                Debug.Log($"Player {player.netId} hasPicked: {player.hasPicked}, isLocalPlayer: {player.isLocalPlayer}, isServer: {player.isServer}, isActiveAndEnabled: {player.gameObject.activeInHierarchy}");
            }
        }

        bool allPicked = players.All(p => p != null && p.gameObject != null && p.hasPicked);
        Debug.Log($"All players picked check result: {allPicked}");
        
        return allPicked;
    }

    private void UnlockUI()
    {
        Debug.Log("CardPicker: Unlocking UI");
        isUnlocked = true;
        isWaitingForPlayers = false;
        
        // Hide waiting message and blackout overlay
        if (waitingMessage != null)
        {
            waitingMessage.SetActive(false);
            Debug.Log("Waiting message hidden");
        }
        
        if (blackoutOverlay != null)
        {
            blackoutOverlay.SetActive(false);
            Debug.Log("Blackout overlay hidden");
        }
        
        // Only the server should trigger the game continuation
        if (isServer)
        {
            // Double check that all players have picked before continuing
            UpdatePlayerList();
            if (AreAllPlayersPicked())
            {
                Debug.Log("Server calling RpcContinueGame - all players have picked");
                RpcContinueGame();
            }
            else
            {
                Debug.LogWarning("Server attempted to continue game but not all players have picked!");
                isUnlocked = false;
                isWaitingForPlayers = true;
            }
        }
    }

    [ClientRpc]
    private void RpcContinueGame()
    {
        Debug.Log("CardPicker: Continuing game on client");
        
        // Reset time scale first
        Time.timeScale = 1f;
        Debug.Log("Time scale reset to 1");
        
        // Hide the entire canvas
        if (cardPickerCanvas != null)
        {
            cardPickerCanvas.gameObject.SetActive(false);
            Debug.Log("Card picker canvas hidden");
        }
        else
        {
            Debug.LogWarning("Card picker canvas reference is missing!");
        }

        // Enable movement for all players
        foreach (var player in players)
        {
            if (player != null && player.gameObject != null)
            {
                player.SetCanMove(true);
                Debug.Log($"Enabled movement for player {player.netId}");
            }
        }

        // Only the server should notify GameStateManager
        if (isServer)
        {
            Debug.Log("Server notifying GameStateManager of card picking completion");
            GameStateManager.Instance.CompleteCardPicking();
        }
    }

    public void RemoveAllCards()
    {
        foreach (var cardGO in cardGameObjects)
        {
            if (cardGO != null)
            {
                Destroy(cardGO);
            }
        }
        cardGameObjects.Clear();
    }

    PowerupCard GetRandomCard()
    {
        float roll = Random.value;

        if (roll < 0.02f)
            return GetRandomFromList(legendaryCards);
        else if (roll < 0.10f)
            return GetRandomFromList(epicCards);
        else if (roll < 0.25f)
            return GetRandomFromList(rareCards);
        else if (roll < 0.50f)
            return GetRandomFromList(uncommonCards);
        else
            return GetRandomFromList(commonCards);
    }

    PowerupCard GetRandomFromList(List<PowerupCard> list)
    {
        return list[Random.Range(0, list.Count)];
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnGameStart -= OnGameStart;
        }
    }
}
