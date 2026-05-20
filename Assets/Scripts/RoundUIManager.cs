using UnityEngine;
using TMPro; // <- REQUIRED
using Mirror;
using System.Collections.Generic;

public class RoundUIManager : NetworkBehaviour
{
    public static RoundUIManager Instance;

    [Header("Score Texts")]
    public TextMeshProUGUI[] playerScoreTexts; // MUST be public

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [ClientRpc]
    public void RpcUpdateScores(List<uint> playerIds, List<int> playerScores)
    {
        for (int i = 0; i < playerIds.Count; i++)
        {
            if (i < playerScoreTexts.Length)
            {
                playerScoreTexts[i].text = $"Player {i + 1}: {playerScores[i]}";
            }
        }
    }
    

}
