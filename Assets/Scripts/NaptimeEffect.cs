using UnityEngine;
using Mirror;

public class NaptimeEffect : NetworkBehaviour
{
    private float idleTimer = 0f;
    private const float IDLE_THRESHOLD = 2f;
    private const float HEAL_PERCENTAGE = 0.03f;
    private PlayerController playerController;
    private bool isIdle = false;
    private float lastHealTime = 0f;
    private const float HEAL_INTERVAL = 1f; // Heal every second

    public void Initialize(PlayerController controller)
    {
        playerController = controller;
        Debug.Log($"NaptimeEffect: Initialized for player {playerController.netId}");
    }

    private void Start()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }
        Debug.Log($"NaptimeEffect: Started for player {playerController.netId}");
    }

    private void Update()
    {
        if (!isServer || playerController == null) return;

        // Check if player is idle (not moving and not shooting)
        bool isMoving = playerController.IsMoving;
        bool isShooting = playerController.IsShooting;

        if (!isMoving && !isShooting)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= IDLE_THRESHOLD && !isIdle)
            {
                isIdle = true;
                Debug.Log($"NaptimeEffect: Player {playerController.netId} is now idle");
            }
        }
        else
        {
            if (isIdle)
            {
                Debug.Log($"NaptimeEffect: Player {playerController.netId} is no longer idle");
            }
            idleTimer = 0f;
            isIdle = false;
        }

        // Apply healing when idle
        if (isIdle && Time.time >= lastHealTime + HEAL_INTERVAL)
        {
            float healAmount = playerController.MaxHealth * HEAL_PERCENTAGE;
            playerController.Heal((int)healAmount);
            lastHealTime = Time.time;
            Debug.Log($"NaptimeEffect: Healed player {playerController.netId} for {healAmount} health");
        }
    }
} 