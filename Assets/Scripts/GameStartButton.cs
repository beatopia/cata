using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class GameStartButton : NetworkBehaviour
{
    private Button button;
    private LobbyManager lobbyManager;
    private GameStateManager gameStateManager;

    void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Get the button component
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("No Button component found on GameStartButton!");
            return;
        }

        // Get the lobby manager
        lobbyManager = FindFirstObjectByType<LobbyManager>();
        if (lobbyManager == null)
        {
            Debug.LogError("No LobbyManager found in scene!");
            return;
        }

        // Get the game state manager
        gameStateManager = GameStateManager.Instance;
        if (gameStateManager == null)
        {
            Debug.LogError("No GameStateManager found in scene!");
            return;
        }

        // Add click listener
        button.onClick.AddListener(OnButtonClick);
    }

    void Update()
    {
        if (button == null || lobbyManager == null || gameStateManager == null) return;

        // Check if this is the host
        bool isHost = isServer && isClient;
        
        // Get current player count from LobbyManager
        int currentPlayers = lobbyManager.GetCurrentPlayerCount();
        
        // Show and enable button only for host when we have exactly 4 players and game hasn't started
        bool shouldShow = isHost && !gameStateManager.gameStarted;
        bool shouldBeInteractable = shouldShow && currentPlayers == lobbyManager.maxPlayers;

        // Update button visibility and interactability
        button.gameObject.SetActive(shouldShow);
        button.interactable = shouldBeInteractable;
    }

    void OnButtonClick()
    {
        if (lobbyManager != null)
        {
            // Start the game through the lobby manager
            lobbyManager.StartGame();
            
            // Trigger the game start event through GameStateManager
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.StartGame();
            }
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
} 