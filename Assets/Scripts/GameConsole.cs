using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Mirror;

public class GameConsole : MonoBehaviour
{
    public static GameConsole Instance { get; private set; }

    [SerializeField] internal TextMeshProUGUI consoleText;
    [SerializeField] internal ScrollRect scrollRect;
    [SerializeField] internal int maxMessages = 50;

    private Queue<string> messages = new Queue<string>();

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

    public void AddMessage(string message)
    {
        messages.Enqueue(message);
        
        // Keep only the last maxMessages messages
        while (messages.Count > maxMessages)
        {
            messages.Dequeue();
        }

        // Update the console text
        UpdateConsoleText();
    }

    private void UpdateConsoleText()
    {
        if (consoleText != null)
        {
            consoleText.text = string.Join("\n", messages.ToArray());
            
            // Scroll to bottom
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
}
