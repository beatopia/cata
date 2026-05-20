using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class NetworkConnectUIs : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField ipAddressInput;
    public TMP_InputField portInput;
    public Button connectButton;

    void Start()
    {
        // Add listener to the connect button
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(ConnectToServer);
        }
    }

    public void ConnectToServer()
    {
        string ipAddress = ipAddressInput.text;
        ushort port = 0;

        // Try to parse the port input
        if (!ushort.TryParse(portInput.text, out port))
        {
            Debug.LogError("Invalid port number entered!");
            return;
        }

        // Get the NetworkManager instance
        CustomNetworkManager networkManager = NetworkManager.singleton as CustomNetworkManager;

        if (networkManager != null)
        {
            // Set the network address and port
            networkManager.networkAddress = ipAddress;
            networkManager.serverPort = port;

            // Ensure relay is used
            networkManager.useRelay = true;

            Debug.Log($"Attempting to connect to {ipAddress}:{port} using relay.");

            // Start the client connection
            networkManager.StartClient();
        }
        else
        {
            Debug.LogError("CustomNetworkManager not found in the scene.");
        }
    }

    void OnDestroy()
    {
        // Clean up the button listener
        if (connectButton != null)
        {
            connectButton.onClick.RemoveListener(ConnectToServer);
        }
    }
} 