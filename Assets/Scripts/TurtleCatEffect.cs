using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class TurtleCatEffect : NetworkBehaviour
{
    private PlayerController player;
    private float originalMoveSpeed;
    private const float SPEED_MULTIPLIER = 0.5f; // 50% slower
    private const int HEALTH_BOOST = 50; // 50 more health

    [SyncVar]
    private bool isActive = false;

    [SyncVar]
    private uint playerNetId;

    public void Initialize(PlayerController playerController)
    {
        if (!isServer) return;
        
        player = playerController;
        playerNetId = playerController.netId;
        
        // Store original values
        originalMoveSpeed = player.moveSpeed;
        
        // Apply effects
        isActive = true;
        ApplyEffects();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (player == null && playerNetId != 0)
        {
            // Find the player by netId
            foreach (var conn in NetworkServer.connections.Values)
            {
                if (conn.identity != null && conn.identity.netId == playerNetId)
                {
                    player = conn.identity.GetComponent<PlayerController>();
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (player != null)
        {
            // Apply speed effect on all clients
            if (isActive)
            {
                player.moveSpeed = originalMoveSpeed * SPEED_MULTIPLIER;
            }
            else
            {
                player.moveSpeed = originalMoveSpeed;
            }
        }
    }

    [Server]
    private void ApplyEffects()
    {
        if (player != null)
        {
            // Increase health (only on server)
            player.Heal(HEALTH_BOOST);
            RpcApplyEffects();
        }
    }

    [ClientRpc]
    private void RpcApplyEffects()
    {
        if (player != null)
        {
            // Apply visual effects or other client-side changes here
            Debug.Log($"TurtleCat effect applied to player {player.netId}");
        }
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            // Restore original values
            player.moveSpeed = originalMoveSpeed;
        }
    }
} 