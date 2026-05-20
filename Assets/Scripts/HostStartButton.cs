using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class HostStartButton : NetworkBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private LobbyManager lobbyManager;

    private void Start()
    {
        if (startButton == null)
        {
            Debug.LogError("Start button reference is missing!");
            return;
        }

        if (lobbyManager == null)
        {
            Debug.LogError("LobbyManager reference is missing!");
            return;
        }

        // Initially hide the button
        startButton.gameObject.SetActive(false);
        startButton.interactable = false;

        // Add click listener
        startButton.onClick.AddListener(OnStartButtonClick);
    }

    private void Update()
    {
        if (!isServer) return;

        // Only show button for host
        bool isHost = isServer && isClient;
        startButton.gameObject.SetActive(isHost);

        if (isHost)
        {
            // Make button interactable only when exactly 4 players have joined
            int currentPlayers = lobbyManager.GetCurrentPlayerCount();
            bool hasEnoughPlayers = currentPlayers == lobbyManager.maxPlayers;
            startButton.interactable = hasEnoughPlayers;
            
            // Debug log to help track player count
            if (currentPlayers != lastPlayerCount)
            {
                Debug.Log($"Player count changed: {currentPlayers}/{lobbyManager.maxPlayers}");
                lastPlayerCount = currentPlayers;
            }
        }
    }

    private void OnStartButtonClick()
    {
        if (!isServer) return;

        // Start the game
        lobbyManager.StartGame();
    }

    private int lastPlayerCount = 0;
} 