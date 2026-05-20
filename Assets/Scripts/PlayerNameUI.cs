using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class PlayerNameUI : MonoBehaviour
{
    public static PlayerNameUI Instance { get; private set; }

    [Header("UI Elements")]
    public TMP_InputField nameInput;
    public Button confirmButton;
    public GameObject namePanel;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Set up button listener
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmName);
        }

        // Show the panel by default
        if (namePanel != null)
        {
            namePanel.SetActive(true);
        }
    }

    public void OnConfirmName()
    {
        string playerName = nameInput.text.Trim();
        
        // Validate name
        if (string.IsNullOrEmpty(playerName))
        {
            playerName = "Player" + Random.Range(1000, 9999);
        }

        // Store the name in the network manager
        if (NetworkManager.singleton is CustomNetworkManager networkManager)
        {
            networkManager.SetPlayerName(playerName);
        }

        // Hide the panel
        if (namePanel != null)
        {
            namePanel.SetActive(false);
        }
    }
} 