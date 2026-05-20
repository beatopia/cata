using UnityEngine;

public class MilkDrunkEffect : MonoBehaviour
{
    public float healPerSecond = 5f;
    private PlayerController player;
    private float timer = 0f;

    public void Initialize(PlayerController playerController)
    {
        player = playerController;
    }

    void Update()
    {
        if (player == null) return;
        timer += Time.deltaTime;
        if (timer >= 1f)
        {
            player.Heal(Mathf.RoundToInt(healPerSecond));
            timer = 0f;
        }
    }
} 