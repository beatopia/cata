using UnityEngine;
using System.Collections.Generic;
using Mirror;

public class SkinManager : NetworkBehaviour
{
    public static SkinManager Instance { get; private set; }

    [SerializeField] private List<RuntimeAnimatorController> availableSkins;
    private Dictionary<uint, RuntimeAnimatorController> playerSkins = new();
    private HashSet<RuntimeAnimatorController> activeSkins = new();
    private Dictionary<string, RuntimeAnimatorController> skinLookup = new();

    // SyncVar to track assigned skins
    [SyncVar(hook = nameof(OnAssignedSkinsChanged))]
    private string assignedSkinsJson = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Create lookup dictionary for skins
            foreach (var skin in availableSkins)
            {
                if (skin != null)
                {
                    skinLookup[skin.name] = skin;
                }
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnAssignedSkinsChanged(string oldValue, string newValue)
    {
        if (string.IsNullOrEmpty(newValue)) return;

        // Parse the JSON string to get the skin assignments
        var assignments = JsonUtility.FromJson<SkinAssignments>(newValue);
        if (assignments == null) return;

        // Update local skin assignments
        foreach (var assignment in assignments.assignments)
        {
            if (skinLookup.TryGetValue(assignment.skinName, out var skin))
            {
                // Find the player
                PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                foreach (var player in players)
                {
                    if (player.netId == assignment.playerId)
                    {
                        player.UpdateSkin(skin);
                        break;
                    }
                }
            }
        }
    }

    [Server]
    public void AssignRandomSkin(uint playerId)
    {
        if (availableSkins.Count == 0) return;

        Debug.Log($"Assigning skin for player {playerId}. Current active skins: {activeSkins.Count}");

        // Get list of available skins (not currently in use)
        List<RuntimeAnimatorController> availableSkinsList = new();
        foreach (var skin in availableSkins)
        {
            if (!activeSkins.Contains(skin))
            {
                availableSkinsList.Add(skin);
                Debug.Log($"Available skin found: {skin.name}");
            }
        }

        // If all skins are taken, use any skin
        if (availableSkinsList.Count == 0)
        {
            Debug.LogWarning("All skins are taken, using any available skin");
            availableSkinsList = new List<RuntimeAnimatorController>(availableSkins);
        }

        // Select random skin from available ones
        int randomIndex = Random.Range(0, availableSkinsList.Count);
        RuntimeAnimatorController selectedSkin = availableSkinsList[randomIndex];
        
        Debug.Log($"Selected skin for player {playerId}: {selectedSkin.name}");
        
        // Update tracking
        if (playerSkins.ContainsKey(playerId))
        {
            activeSkins.Remove(playerSkins[playerId]);
            Debug.Log($"Removed old skin {playerSkins[playerId].name} from active skins");
        }
        playerSkins[playerId] = selectedSkin;
        activeSkins.Add(selectedSkin);
        
        // Update the SyncVar with all current assignments
        UpdateAssignedSkinsSyncVar();
    }

    private void UpdateAssignedSkinsSyncVar()
    {
        var assignments = new SkinAssignments();
        assignments.assignments = new List<SkinAssignment>();

        foreach (var kvp in playerSkins)
        {
            assignments.assignments.Add(new SkinAssignment
            {
                playerId = kvp.Key,
                skinName = kvp.Value.name
            });
        }

        assignedSkinsJson = JsonUtility.ToJson(assignments);
    }

    [Server]
    public void RemovePlayerSkin(uint playerId)
    {
        Debug.Log($"Removing skin for player {playerId}");
        if (playerSkins.TryGetValue(playerId, out RuntimeAnimatorController skin))
        {
            activeSkins.Remove(skin);
            playerSkins.Remove(playerId);
            Debug.Log($"Removed skin {skin.name} from active skins. Remaining: {activeSkins.Count}");
            
            // Update the SyncVar after removing the skin
            UpdateAssignedSkinsSyncVar();
        }
    }

    public RuntimeAnimatorController GetPlayerSkin(uint playerId)
    {
        return playerSkins.TryGetValue(playerId, out RuntimeAnimatorController skin) ? skin : null;
    }
}

[System.Serializable]
public class SkinAssignments
{
    public List<SkinAssignment> assignments = new List<SkinAssignment>();
}

[System.Serializable]
public class SkinAssignment
{
    public uint playerId;
    public string skinName;
} 