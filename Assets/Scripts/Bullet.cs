using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(TrailRenderer))]
public class Bullet : NetworkBehaviour
{
    [Tooltip("Time before bullet is destroyed automatically.")]
    public float destroyAfter = 4f;

    [Tooltip("Damage dealt to a player.")]
    public int damageAmount = 25;

    [SerializeField] private float ignoreTime = 1f;

    private Rigidbody2D rb2D;
    private Collider2D bulletCollider;
    private Collider2D[] shooterColliders;
    private TrailRenderer trailRenderer;
    [SyncVar]
    public uint shooterId;
    [SyncVar]
    public float shotTime;
    [SyncVar]
    private Vector2 initialVelocity;

    [Header("Bullet Physics")]
    [SerializeField] private float downwardForce = 5f; // Increased downward force
    [SerializeField] private float dragCoefficient = 0.2f; // Air resistance
    [SerializeField] private float maxSpeed = 30f; // Maximum bullet speed

    [Header("Trail Effect")]
    [SerializeField] private float trailTime = 0.5f;
    [SerializeField] private float trailWidth = 0.1f;
    [SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0.5f);

    // -----------------------------
    // AUDIO FIELDS
    // -----------------------------
    [Header("Sound Effects")]
    [SerializeField] private AudioClip hitPlayerClip;
    [SerializeField] private AudioClip hitWallClip;
    private AudioSource audioSource;

    // -----------------------------
    // PARTICLES
    // -----------------------------
    [Header("Particles")]
    [SerializeField] private ParticleSystem hitParticles;

    void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        bulletCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        trailRenderer = GetComponent<TrailRenderer>();

        // Configure Rigidbody2D for dynamic physics
        if (rb2D != null)
        {
            rb2D.gravityScale = 0f; // Disable gravity scale
            rb2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb2D.mass = 0.05f; // Lighter mass for more dramatic effect
            rb2D.linearDamping = dragCoefficient; // Add drag for air resistance
            rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb2D.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        // Set up trail renderer
        if (trailRenderer != null)
        {
            trailRenderer.time = trailTime;
            trailRenderer.startWidth = trailWidth;
            trailRenderer.endWidth = trailWidth * 0.5f;
            trailRenderer.startColor = trailColor;
            trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trailRenderer.sortingOrder = 1;
        }

        // Set up collider for wall and player collisions only
        if (bulletCollider != null)
        {
            bulletCollider.isTrigger = false;
            // Make sure the collider is set to collide with walls (Default layer) and players
            bulletCollider.includeLayers = LayerMask.GetMask("Default", "Player");
            // Exclude other bullets
            bulletCollider.excludeLayers = LayerMask.GetMask("Bullet");
        }
    }

    public void Initialize(Vector2 force, uint shooterId)
    {
        this.shooterId = shooterId;
        if (rb2D != null)
        {
            initialVelocity = force;
            rb2D.linearVelocity = force; // Use velocity instead of linearVelocity
            // Set initial rotation based on movement direction
            float angle = Mathf.Atan2(force.y, force.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + 180f);
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (NetworkServer.spawned.TryGetValue(shooterId, out NetworkIdentity shooterIdentity))
        {
            shooterColliders = shooterIdentity.GetComponentsInChildren<Collider2D>();
        }

        if (bulletCollider && shooterColliders != null)
        {
            foreach (var col in shooterColliders)
            {
                Physics2D.IgnoreCollision(bulletCollider, col, true);
            }
            StartCoroutine(RestoreCollisionAfterDelay(ignoreTime));
        }

        Invoke(nameof(DestroyBullet), destroyAfter);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (NetworkClient.spawned.TryGetValue(shooterId, out NetworkIdentity shooterIdentity))
        {
            shooterColliders = shooterIdentity.GetComponentsInChildren<Collider2D>();
        }

        if (bulletCollider && shooterColliders != null)
        {
            foreach (var col in shooterColliders)
            {
                Physics2D.IgnoreCollision(bulletCollider, col, true);
            }
            StartCoroutine(RestoreCollisionAfterDelay(ignoreTime));
        }

        // Ensure bullet has proper velocity on client
        if (rb2D != null && initialVelocity != Vector2.zero)
        {
            rb2D.linearVelocity = initialVelocity; // Use velocity instead of linearVelocity
        }
    }

    private IEnumerator RestoreCollisionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (bulletCollider && shooterColliders != null)
        {
            foreach (var col in shooterColliders)
            {
                Physics2D.IgnoreCollision(bulletCollider, col, false);
            }
        }
    }

    [ClientCallback]
    void Update()
    {
        if (rb2D != null)
        {
            // Apply constant downward force
            rb2D.AddForce(Vector2.down * downwardForce, ForceMode2D.Force);

            // Limit maximum speed
            if (rb2D.linearVelocity.magnitude > maxSpeed)
            {
                rb2D.linearVelocity = rb2D.linearVelocity.normalized * maxSpeed;
            }

            Vector2 vel = rb2D.linearVelocity;
            if (vel.sqrMagnitude > 0.0001f)
            {
                float angle = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle + 180f);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isServer) return;

        // Check if we hit a player
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                // Apply damage with knockback and shooter ID
                Vector2 knockback = rb2D.linearVelocity.normalized * 5f; // 5 units of knockback force
                player.TakeDamage(damageAmount, knockback, shooterId);
            }
        }

        // Play hit effects on all clients
        RpcPlayHitEffects(collision.contacts[0].point, collision.gameObject.CompareTag("Player"));

        // Spawn particle effect as a separate object
        if (hitParticles != null)
        {
            GameObject particleObj = Instantiate(hitParticles.gameObject, collision.contacts[0].point, Quaternion.identity);
            ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
                Destroy(particleObj, ps.main.duration);
            }
        }

        // Play sound effect before destroying
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(collision.gameObject.CompareTag("Player") ? hitPlayerClip : hitWallClip);
        }

        // Start delayed destruction to allow sound to play
        StartCoroutine(DelayedDestroy());
    }

    private IEnumerator DelayedDestroy()
    {
        // Wait for a short time to allow sound to play
        yield return new WaitForSeconds(0.1f);
        NetworkServer.Destroy(gameObject);
    }

    [ClientRpc]
    void RpcDisableBulletVisuals()
    {
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;
        if (TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = false;
        if (rb2D != null) rb2D.linearVelocity = Vector2.zero;
    }

    [ClientRpc]
    void RpcPlayHitEffects(Vector2 position, bool hitPlayer)
    {
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(hitPlayer ? hitPlayerClip : hitWallClip);
        }

        // Screen shake logic
        float shakeIntensity = 0f;

        if (hitPlayer)
        {
            shakeIntensity += 0.05f; // scales past player hit
        }

        float speed = rb2D.linearVelocity.magnitude;
        if (speed >= 5f)
        {
            shakeIntensity += (speed - 5f) * 0.02f; // scales past speed 5
        }

        if (shakeIntensity > 0.01f)
        {
            ScreenShake.Instance?.Shake(shakeIntensity, 0.2f); // duration 0.2s
        }
    }

    [Server]
    void DestroyBullet()
    {
        // Check if the object is still valid before destroying
        if (gameObject != null)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    // New method to set the collider as a trigger
    public void SetIsTrigger(bool isTrigger)
    {
        if (bulletCollider != null)
        {
            bulletCollider.isTrigger = isTrigger;
        }
    }
}
