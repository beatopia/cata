using UnityEngine;

public class BleedEffect : MonoBehaviour
{
    public int totalDamage = 15;
    public float duration = 3f;
    private float tickInterval = 1f;
    private int ticks;
    private int damagePerTick;
    private float timer;
    private PlayerController player;

    public void Initialize(PlayerController target)
    {
        player = target;
        ticks = Mathf.RoundToInt(duration / tickInterval);
        damagePerTick = Mathf.RoundToInt((float)totalDamage / ticks);
        timer = 0f;
    }

    void Update()
    {
        if (player == null) { Destroy(this); return; }
        timer += Time.deltaTime;
        if (timer >= tickInterval && ticks > 0)
        {
            player.TakeDamage(damagePerTick, Vector2.zero);
            ticks--;
            timer = 0f;
        }
        if (ticks <= 0)
        {
            Destroy(this);
        }
    }
} 