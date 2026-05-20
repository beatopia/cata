using UnityEngine;
using Mirror;

public class Caterpillar : NetworkBehaviour
{
    public float health = 1;
    public float moveSpeed = 3.0f;
    private Transform target;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    [SyncVar(hook = nameof(OnFlipChanged))] private bool isFlipped;
    [SyncVar] private Vector2 networkPosition;

    public float Health
    {
        set
        {
            health = value;
            if (health <= 0)
            {
                Defeated();
            }
        }
    }

    public override void OnStartServer()
    {
        InvokeRepeating(nameof(FindNearestPlayer), 0f, 1f);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isServer) return; // Ensure only the server handles damage

 

        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                
            }
        }
    }

    void FixedUpdate()
    {
        if (!isServer)
        {
            transform.position = Vector2.Lerp(transform.position, networkPosition, Time.deltaTime * 2f);
            return;
        }

        if (target != null)
        {
            MoveTowardsTarget();
        }
    }

    [Server]
    void MoveTowardsTarget()
    {
        if (target == null) return;

        Vector2 direction = (target.position - transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed * Time.fixedDeltaTime;

        networkPosition = transform.position;
        bool shouldFlip = direction.x < 0;
        if (isFlipped != shouldFlip)
        {
            isFlipped = shouldFlip;
        }
    }

    void OnFlipChanged(bool oldValue, bool newValue)
    {
        spriteRenderer.flipX = newValue;
    }

    [Server]
    void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float shortestDistance = Mathf.Infinity;
        Transform nearestPlayer = null;

        foreach (GameObject player in players)
        {
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearestPlayer = player.transform;
            }
        }

        target = nearestPlayer;
    }

    [Server]
    public void Defeated()
    {
        NetworkServer.Destroy(gameObject);
    }
}