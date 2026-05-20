using UnityEngine;
using Mirror;
using System;

public class GameStateManager : NetworkBehaviour
{
    [SyncVar]
    public bool gameStarted = false;
    
    [SyncVar]
    public bool gameStartedA = false;
    
    [SyncVar]
    public bool damageEnabled = false;

    public event Action OnGameStart;
    public event Action OnCardPickingComplete;
    public event Action OnGameReset;

    private static GameStateManager instance;
    public static GameStateManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<GameStateManager>();
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("GameStateManager started on server");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("GameStateManager started on client");
    }

    [Server]
    public void StartGame()
    {
        if (gameStarted) 
        {
            Debug.Log("GameStateManager: Game already started, ignoring StartGame call");
            return;
        }
        
        Debug.Log("GameStateManager: Starting game...");
        gameStarted = true;
        gameStartedA = true;
        damageEnabled = true; // Enable damage immediately when game starts
        
        // Trigger the OnGameStart event
        Debug.Log("GameStateManager: Invoking OnGameStart event");
        OnGameStart?.Invoke();
        
        Debug.Log("GameStateManager: Game started!");
    }

    [Server]
    public void CompleteCardPicking()
    {
        Debug.Log("Server: Completing card picking phase");
        // Enable damage and continue the game
        damageEnabled = true;
        
        // Trigger the OnCardPickingComplete event
        OnCardPickingComplete?.Invoke();
        
        Debug.Log("Card picking complete! Game continuing...");
    }

    [Server]
    public void ResetGame()
    {
        Debug.Log("GameStateManager: Resetting game state");
        gameStarted = false;
        gameStartedA = false;
        damageEnabled = false;
        
        // Trigger the OnGameReset event
        OnGameReset?.Invoke();
        
        Debug.Log("GameStateManager: Game state reset complete");
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
} 