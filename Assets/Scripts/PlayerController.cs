using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    // -------------------------------
    // MOVEMENT
    // -------------------------------
    public float moveSpeed = 0.02f;  // Base movement speed
    private Vector2 movementInput;
    private Rigidbody2D rb;
    private bool canMove = true;

    // Add base movement speed variable
    public float baseMoveSpeed;

    // -------------------------------
    // COMBAT
    // -------------------------------
    [Header("Shooting")]
    public Transform firePoint;
    public GameObject bulletPrefab;
    public GameObject backedUpBulletPrefab; // New prefab for Backed Up ability
    public float bulletForce = 20f;
    public float bulletSpread = 15f; // Spread angle in degrees
    private bool canShoot = true;

    [Header("Health & Knockback")]
    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth = 100;
    private const int maxHealth = 100;
    [SerializeField] public float knockbackForce = 5f;
    public float KnockbackForce => knockbackForce;
    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;

    // Add properties for Naptime effect
    public bool IsMoving => isMoving;
    public bool IsShooting => canShoot && currentBullets > 0;

    // -------------------------------
    // HEALTH BAR
    // -------------------------------
    public GameObject healthBarPrefab;
    private HealthBar healthBar;

    // -------------------------------
    // AUDIO
    // -------------------------------
    [Header("Audio")]
    public AudioClip shootClip;
    private AudioSource audioSource;

    // -------------------------------
    // ANIMATION
    // -------------------------------
    private Animator animator;
    [SyncVar(hook = nameof(OnMoveStateChanged))] private bool isMoving = false;
    [SyncVar(hook = nameof(OnDirectionChanged))] private Vector2 lastMoveDirection = Vector2.zero;

    // -------------------------------
    // BULLET SYSTEM
    // -------------------------------
    [Header("Bullet System")]
    [SyncVar(hook = nameof(OnBulletsChanged))]
    private int currentBullets = 1;
    private int maxBullets = 1;
    private bool isReloading = false;
    private float lastShotTime = 0f;
    private Transform bulletUIParent;
    private SpriteRenderer[] bulletUIImages;

    // -------------------------------
    // CARD SYSTEM
    // -------------------------------
    [Header("Card System")]
    [SyncVar]
    public bool hasPicked = false;
    [SyncVar]
    public string selectedCardID = "";
    [SyncVar(hook = nameof(OnHasPounceChanged))]
    private bool hasPounce = false;

    public bool HasPounce => hasPounce;

    // -------------------------------
    // ROLL/DASH SYSTEM
    // -------------------------------
    [Header("Roll/Dash System")]
    [SerializeField] private float dashSpeedMultiplier = 1.5f;
    [SerializeField] private float dashDuration = 1.5f;  // Increased to 1.5 seconds
    [SerializeField] private float dashCooldown = 10f;
    [SerializeField] private float postDashSlowDuration = 3f;  // Increased to 3 seconds
    [SerializeField] private float postDashSlowMultiplier = 0.3f;  // Changed to 30% speed
    private bool isDashing = false;
    private bool isPostDashSlow = false;
    private bool canDash = true;
    private float lastDashTime = 0f;
    private Vector2 preDashVelocity;

    // -------------------------------
    // SKIN SYSTEM
    // -------------------------------
    [Header("Skin System")]
    private RuntimeAnimatorController currentSkin;

    [SyncVar] private uint lastDamageSource = 0; // Track who dealt the last damage

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        // Store the initial base move speed
        baseMoveSpeed = moveSpeed;

        // Configure Rigidbody2D for proper movement
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Set up sprite renderer
        var spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Use standard sprite material
            spriteRenderer.material = new Material(Shader.Find("Sprites/Default"));
            spriteRenderer.sortingOrder = 1;
            
            if (isLocalPlayer)
            {
                Color color = spriteRenderer.color;
                color.a = 0.8f;
                spriteRenderer.color = color;
            }
        }

        // Configure colliders
        var colliders = GetComponents<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.hideFlags = HideFlags.HideInInspector;
            
            // Configure circle collider for hitbox
            if (collider is CircleCollider2D circleCollider)
            {
                circleCollider.isTrigger = false; // Changed to false to allow physical collisions
                circleCollider.includeLayers = LayerMask.GetMask("Bullet", "Player");
                circleCollider.excludeLayers = LayerMask.GetMask("Default");
            }
            // Configure box collider for wall collisions
            else if (collider is BoxCollider2D boxCollider)
            {
                boxCollider.isTrigger = false;
                boxCollider.includeLayers = LayerMask.GetMask("Default");
                boxCollider.excludeLayers = LayerMask.GetMask("Bullet", "Player");
            }
        }

        if (isServer) 
        {
            currentHealth = maxHealth;
            gameObject.SetActive(true);
        }

        if (isClient)
        {
            // Create health bar
            GameObject bar = Instantiate(healthBarPrefab);
            healthBar = bar.GetComponent<HealthBar>();
            healthBar.Init(transform);
            healthBar.SetHealth(currentHealth, maxHealth);

            // Create bullet UI parent
            GameObject bulletUIParent = new GameObject("BulletUI");
            bulletUIParent.transform.SetParent(transform);
            bulletUIParent.transform.localPosition = new Vector3(0.05f, 0.15f, 0);
            this.bulletUIParent = bulletUIParent.transform;

            // Initialize bullets
            currentBullets = maxBullets;
            if (isLocalPlayer)
            {
                CmdSetBullets(maxBullets);
            }
            
            // Create bullet UI
            CreateBulletUI();
        }

        // Ensure the player is visible for all clients
        if (!isLocalPlayer) 
        {
            gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            Vector2 input = Vector2.zero;
            
            // Use both Input System and legacy Input for editor compatibility
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Input.GetKey(KeyCode.W)) input.y += 1;
                if (Keyboard.current.sKey.isPressed || Input.GetKey(KeyCode.S)) input.y -= 1;
                if (Keyboard.current.aKey.isPressed || Input.GetKey(KeyCode.A)) input.x -= 1;
                if (Keyboard.current.dKey.isPressed || Input.GetKey(KeyCode.D)) input.x += 1;

                // Check for dash input
                if ((Keyboard.current.leftShiftKey.isPressed || Input.GetKey(KeyCode.LeftShift)) && canDash && !isDashing)
                {
                    StartDash();
                }
            }
            else
            {
                // Fallback to legacy input if Input System is not available
                if (Input.GetKey(KeyCode.W)) input.y += 1;
                if (Input.GetKey(KeyCode.S)) input.y -= 1;
                if (Input.GetKey(KeyCode.A)) input.x -= 1;
                if (Input.GetKey(KeyCode.D)) input.x += 1;

                // Check for dash input
                if (Input.GetKey(KeyCode.LeftShift) && canDash && !isDashing)
                {
                    StartDash();
                }
            }
            movementInput = input.normalized;

            // Handle shooting with both input systems
            bool shootInput = false;
            if (Mouse.current != null)
            {
                shootInput = Mouse.current.leftButton.wasPressedThisFrame;
            }
            else
            {
                shootInput = Input.GetMouseButtonDown(0);
            }

            if (shootInput)
            {
                TryShoot();
            }
        }

        animator.SetBool("isMoving", isMoving);
        animator.SetFloat("moveX", lastMoveDirection.x);
        animator.SetFloat("moveY", lastMoveDirection.y);
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        HandleMovement();
        // Ensure no rotation at all times
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
    }

    private void HandleMovement()
    {
        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float currentMoveSpeed = moveSpeed;
        
        if (isPostDashSlow)
        {
            currentMoveSpeed *= postDashSlowMultiplier;
        }

        if (isDashing)
        {
            // During dash, use base speed with 50% boost
            Vector2 dashVelocity = movementInput * (moveSpeed * dashSpeedMultiplier);
            rb.linearVelocity = dashVelocity;
        }
        else
        {
            // Normal movement (or slowed movement after dash)
            Vector2 movement = movementInput * currentMoveSpeed;
            movement = Vector2.ClampMagnitude(movement, currentMoveSpeed);
            rb.linearVelocity = movement;
        }

        // Update movement state
        bool newIsMoving = movementInput.sqrMagnitude > 0.01f;
        if (newIsMoving != isMoving || (newIsMoving && movementInput != lastMoveDirection))
        {
            CmdUpdateMovementState(newIsMoving, movementInput);
        }
    }

    private void TryShoot()
    {
        if (!canShoot || currentBullets <= 0 || isReloading) return;

        Vector2 mousePos;
        if (Mouse.current != null && Mouse.current.position != null)
        {
            mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }
        else
        {
            // Fallback to legacy input if Input System mouse position is not available
            mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }

        Vector2 baseShootDir = (mousePos - (Vector2)firePoint.position).normalized;

        PlayShootSound();

        // Handle shooting based on current bullets and max bullets
        if (maxBullets > 1 && currentBullets > 0) // Backed Up ability active
        {
            Debug.Log($"[TryShoot] Player {netId} attempting to shoot Backed Up bullets. Current bullets: {currentBullets}, Max bullets: {maxBullets}");
            // Shoot all available bullets (up to maxBullets)
            float totalSpread = bulletSpread * (maxBullets - 1); // Total spread angle
            float startAngle = -totalSpread / 2f; // Start from negative half of total spread

            for (int i = 0; i < maxBullets; i++) // Always loop 3 times for Backed Up
            {
                // Calculate spread angle for this bullet
                float spreadAngle = startAngle + (totalSpread * i / (maxBullets - 1));
                // Rotate the base direction by the spread angle
                Vector2 spreadDir = RotateVector(baseShootDir, spreadAngle);
                // Command the server to shoot this specific bullet instance
                CmdShoot(spreadDir);
            }

            // Use all bullets at once for Backed Up
            currentBullets = 0;
            CmdSetBullets(0);

        }
        else if (currentBullets > 0) // Normal shooting (maxBullets <= 1)
        {
             Debug.Log($"[TryShoot] Player {netId} attempting to shoot normal bullet. Current bullets: {currentBullets}, Max bullets: {maxBullets}");
            // Shoot one bullet
            CmdShoot(baseShootDir);
            // Decrement bullet count for normal shooting
            currentBullets--;
            CmdSetBullets(currentBullets);
        }

        UpdateBulletUI();

        canShoot = false;
        lastShotTime = Time.time;
        Invoke(nameof(ResetShoot), 0.5f);

        // Start reload only if bullets are depleted or Backed Up was used
        if (currentBullets <= 0) // This check should be enough now
        {
           StartReload();
        }
    }

    private void ResetShoot() => canShoot = true;

    private Vector2 RotateVector(Vector2 vector, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos
        );
    }

    private void PlayShootSound()
    {
        if (audioSource != null && shootClip != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(shootClip);
        }
    }

    [Command]
    private void CmdShoot(Vector2 direction)
    {
        // Determine which prefab to use based on the name received from the client
        GameObject prefabToUse = null;

        if (direction == Vector2.zero) return; // Exit if direction is zero

        if (direction.sqrMagnitude > 0.01f) // Check if direction is valid
        {
            prefabToUse = maxBullets > 1 ? backedUpBulletPrefab : bulletPrefab;
        }
        else
        {
            Debug.LogError($"[CmdShoot] Invalid direction received: {direction}");
            return; // Exit if direction is invalid
        }

        if (prefabToUse == null || firePoint == null) return;

        // Instantiate the bullet on the server
        GameObject bulletObj = Instantiate(prefabToUse, firePoint.position, Quaternion.identity);
        if (bulletObj == null) return;

        // Set bullet layer
        bulletObj.layer = LayerMask.NameToLayer("Bullet");

        var bullet = bulletObj.GetComponent<Bullet>();
        if (bullet == null)
        {
            Destroy(bulletObj);
            return;
        }

        // Set up bullet components
        bullet.shooterId = netId;
        bullet.shotTime = Time.time;

        // Initialize bullet with velocity (using the method from Bullet.cs)
        Vector2 bulletVelocity = direction * bulletForce; // Removed .normalized to maintain force magnitude
        bullet.Initialize(bulletVelocity, netId);

        // Ensure the bullet is spawned immediately on all clients
        NetworkServer.Spawn(bulletObj, connectionToClient);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isServer) return;
        if (!collision.gameObject.CompareTag("Wall")) return;

        // Get the collision normal
        Vector2 normal = collision.contacts[0].normal;
        
        // Add a small buffer to prevent getting stuck on edges
        float buffer = 0.1f;
        Vector2 adjustedNormal = normal + (normal * buffer);
        
        // Stop movement in the direction of the wall with smoother response
        Vector2 velocity = rb.linearVelocity;
        float dot = Vector2.Dot(velocity, normal);
        if (dot < 0)
        {
            // Smoothly reduce velocity in the wall direction
            velocity -= normal * dot * 1.2f; // Slightly increased response
            rb.linearVelocity = velocity;
        }
        
        // Ensure no rotation
        rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isServer) return;
        if (!collision.gameObject.CompareTag("Wall")) return;

        // Continuously check for and resolve any sticking
        Vector2 normal = collision.contacts[0].normal;
        Vector2 velocity = rb.linearVelocity;
        float dot = Vector2.Dot(velocity, normal);
        
        if (dot < 0)
        {
            // Adjust velocity to slide along the wall instead of pushing away
            Vector2 slideDirection = Vector2.Perpendicular(normal);
            float slideAmount = Vector2.Dot(velocity, slideDirection);
            rb.linearVelocity = slideDirection * slideAmount;
        }
    }

    [TargetRpc]
    private void TargetApplyKnockback(NetworkConnection target, Vector2 knockback)
    {
        // Apply knockback directly to velocity
        rb.linearVelocity = knockback;
        rb.angularVelocity = 0f;
    }

    [ClientRpc]
    void RpcRespawn()
    {
        gameObject.SetActive(true);
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
            healthBar.SetHealth(currentHealth, maxHealth);
        }
    }

    [ClientRpc]
    void RpcHandleDeath()
    {
        gameObject.SetActive(false);
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }
    }

    [Command]
    private void CmdUpdateMovementState(bool moving, Vector2 direction)
    {
        isMoving = moving;
        lastMoveDirection = direction;
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        Debug.Log($"[Player {netId}] Health changed: {oldVal} -> {newVal} (delta: {newVal - oldVal})");
        currentHealth = newVal;
        if (healthBar != null)
        {
            Debug.Log($"[Player {netId}] Updating health bar to {currentHealth}/{maxHealth}");
            healthBar.SetHealth(currentHealth, maxHealth);
        }
        else
        {
            Debug.LogWarning($"[Player {netId}] Health bar is null, cannot update UI");
        }
    }

    private void OnMoveStateChanged(bool oldVal, bool newVal)
    {
        isMoving = newVal;
        animator.SetBool("isMoving", isMoving);
    }

    private void OnDirectionChanged(Vector2 oldVal, Vector2 newVal)
    {
        lastMoveDirection = newVal;
        animator.SetFloat("moveX", lastMoveDirection.x);
        animator.SetFloat("moveY", lastMoveDirection.y);
    }

    private void OnDestroy()
    {
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        // Remove the player's skin when they disconnect
        if (isServer && SkinManager.Instance != null)
        {
            SkinManager.Instance.RemovePlayerSkin(netId);
        }
    }

    [Server]
    public void Die()
    {
        // Send elimination message to GameConsole
        if (GameConsole.Instance != null)
        {
            string message;
            if (lastDamageSource != 0)
            {
                message = $"Player {netId} was eliminated by Player {lastDamageSource}!";
            }
            else
            {
                message = $"Player {netId} was eliminated!";
            }
            GameConsole.Instance.AddMessage(message);
        }

        RoundManager.Instance.UnregisterPlayer(this);
        RpcHandleDeath();
    }

    [Server]
    public void Respawn()
    {
        currentHealth = maxHealth;
        RpcRespawn();
    }

    [Server]
    public void DespawnPlayer()
    {
        RpcDespawn();
    }

    [ClientRpc]
    void RpcDespawn()
    {
        gameObject.SetActive(false);
    }

    [Server]
    public void SpawnPlayer(Vector3 position)
    {
        transform.position = position;
        currentHealth = maxHealth;
        RpcRespawn();
    }

    [Server]
    public void SetAlive(bool isAlive)
    {
        // Optional for syncing state or logic if needed
    }

    [Command]
    public void CmdSetBullets(int amount)
    {
        currentBullets = amount;
    }

    [Command]
    public void CmdSetHasPicked(bool value)
    {
        hasPicked = value;
    }

    [Server]
    public void SetHasPicked(bool value)
    {
        hasPicked = value;
    }

    [Command]
    public void StoreCardID(string cardId)
    {
        selectedCardID = cardId;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
    }

    [Server]
    public void SetMaxBullets(int newMax)
    {
        maxBullets = newMax;
        currentBullets = maxBullets;
        
        // Create bullet UI first
        CreateBulletUI();
        
        // Update bullets for all clients
        RpcSetMaxBullets(newMax);
        UpdateBulletUI();
    }

    [ClientRpc]
    private void RpcSetMaxBullets(int newMax)
    {
        maxBullets = newMax;
        currentBullets = maxBullets;
        CreateBulletUI();
        UpdateBulletUI();
    }

    private void CreateBulletUI()
    {
        // Destroy existing bullet UI if it exists
        if (bulletUIImages != null)
        {
            foreach (var img in bulletUIImages)
            {
                if (img != null && img.gameObject != null)
                    Destroy(img.gameObject);
            }
        }

        // Create new bullet UI parent if it doesn't exist
        if (bulletUIParent == null)
        {
            GameObject bulletUIParent = new GameObject("BulletUI");
            bulletUIParent.transform.SetParent(transform);
            bulletUIParent.transform.localPosition = new Vector3(0.05f, 0.15f, 0);
            this.bulletUIParent = bulletUIParent.transform;
        }

        // Create new bullet UI elements
        bulletUIImages = new SpriteRenderer[maxBullets];
        for (int i = 0; i < maxBullets; i++)
        {
            GameObject bulletUI = new GameObject($"BulletUI_{i}");
            bulletUI.transform.SetParent(bulletUIParent);
            bulletUI.transform.localPosition = new Vector3(i * 0.1f - (maxBullets * 0.1f / 2), 0, 0);
            bulletUI.transform.localScale = new Vector3(0.1f, 0.1f, 1f);
            SpriteRenderer spriteRenderer = bulletUI.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateBulletSprite();
            bulletUIImages[i] = spriteRenderer;
        }
    }

    private Sprite CreateBulletSprite()
    {
        // Create a small white circle sprite
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = x - 16;
                float dy = y - 16;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                colors[y * 32 + x] = dist < 14 ? Color.white : Color.clear;
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100);
    }

    private void UpdateBulletUI()
    {
        if (bulletUIImages == null || bulletUIImages.Length == 0)
        {
            CreateBulletUI();
            return;
        }

        for (int i = 0; i < maxBullets; i++)
        {
            if (i < bulletUIImages.Length && bulletUIImages[i] is SpriteRenderer spriteRenderer)
            {
                spriteRenderer.color = i < currentBullets ? new Color(1, 1, 1, 1) : new Color(1, 1, 1, 0);
            }
        }
    }

    private void OnBulletsChanged(int oldVal, int newVal)
    {
        currentBullets = newVal;
        UpdateBulletUI();
    }

    private void StartReload()
    {
        if (isReloading) return;
        isReloading = true;
        Invoke(nameof(FinishReload), 1.5f);
    }

    private void FinishReload()
    {
        currentBullets = maxBullets;
        isReloading = false;
        if (isLocalPlayer)
        {
            CmdSetBullets(maxBullets);
        }
        UpdateBulletUI();
    }

    [Server]
    public void TakeDamage(int damage, Vector2 knockback, uint sourceId = 0)
    {
        if (!GameStateManager.Instance.damageEnabled) return;

        Debug.Log($"[Player {netId}] Taking {damage} damage from player {sourceId}. Current health: {currentHealth} -> {currentHealth - damage}");
        currentHealth = Mathf.Max(0, currentHealth - damage);
        lastDamageSource = sourceId;
        
        // Apply knockback
        Vector2 knockbackForce = knockback.normalized * this.knockbackForce;
        TargetApplyKnockback(connectionToClient, knockbackForce);
        
        // --- SCRATCH BLEED LOGIC ---
        if (sourceId != 0 && NetworkServer.spawned.TryGetValue(sourceId, out var attackerIdentity))
        {
            var attacker = attackerIdentity.GetComponent<PlayerController>();
            if (attacker != null && attacker.GetComponent<ScratchEffectHolder>() != null)
            {
                if (GetComponent<BleedEffect>() == null)
                {
                    var bleed = gameObject.AddComponent<BleedEffect>();
                    bleed.Initialize(this);
                }
            }
        }
        // --- END SCRATCH BLEED LOGIC ---

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    [Server]
    public void EnablePounce()
    {
        Debug.Log($"PlayerController: EnablePounce called for player {netId}");
        hasPounce = true;
        Debug.Log($"PlayerController: Pounce enabled for player {netId}, hasPounce = {hasPounce}");
        // Reset dash cooldown when pounce is enabled
        ResetDashCooldown();
    }

    [Server]
    public void DisablePounce()
    {
        Debug.Log($"PlayerController: DisablePounce called for player {netId}");
        hasPounce = false;
        Debug.Log($"PlayerController: Pounce disabled for player {netId}, hasPounce = {hasPounce}");
    }

    [Command]
    private void CmdEnablePounce()
    {
        Debug.Log($"PlayerController: CmdEnablePounce called for player {netId}");
        hasPounce = true;
        // Reset dash cooldown when pounce is enabled
        ResetDashCooldown();
        Debug.Log($"PlayerController: Pounce enabled for player {netId}, hasPounce = {hasPounce}");
    }

    private void StartDash()
    {
        Debug.Log($"PlayerController: StartDash called for player {netId}, hasPounce = {hasPounce}, canDash = {canDash}, isDashing = {isDashing}");
        // Only allow dashing if player has pounce
        if (!hasPounce || !canDash || isDashing) return;
        
        // Enhanced dash settings for pounce
        dashSpeedMultiplier = 2.0f; // Enhanced speed for pounce
        dashDuration = 0.5f; // Shorter duration for pounce
        postDashSlowDuration = 1.0f; // Shorter slow duration
        
        Debug.Log($"PlayerController: Starting enhanced dash for player {netId}");
        isDashing = true;
        canDash = false;
        lastDashTime = Time.time;
        preDashVelocity = rb.linearVelocity;
        Invoke(nameof(EndDash), dashDuration);
        Invoke(nameof(ResetDashCooldown), dashCooldown);
    }

    private void EndDash()
    {
        isDashing = false;
        isPostDashSlow = true;
        rb.linearVelocity = preDashVelocity;
        Invoke(nameof(EndPostDashSlow), postDashSlowDuration);
    }

    private void EndPostDashSlow()
    {
        isPostDashSlow = false;
    }

    [Server]
    public void ResetDashCooldown()
    {
        canDash = true;
        lastDashTime = 0f;
    }

    public void UpdateSkin(RuntimeAnimatorController newSkin)
    {
        if (newSkin == null)
        {
            Debug.LogWarning($"Cannot update skin for player {netId}: newSkin is null");
            return;
        }

        // Ensure animator is initialized
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError($"Cannot update skin for player {netId}: Animator component not found");
                return;
            }
        }
        
        Debug.Log($"Updating skin for player {netId} to {newSkin.name}");
        currentSkin = newSkin;
        
        // Temporarily disable the animator
        animator.enabled = false;
        
        // Set the new controller
        animator.runtimeAnimatorController = newSkin;
        
        // Re-enable the animator
        animator.enabled = true;
        
        // Force update the animator parameters
        animator.SetBool("isMoving", isMoving);
        animator.SetFloat("moveX", lastMoveDirection.x);
        animator.SetFloat("moveY", lastMoveDirection.y);
        
        // Force the animator to update
        animator.Update(0f);
        
        // Ensure the animator is playing
        animator.Rebind();
        animator.Update(0f);
    }

    private void OnHasPounceChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"PlayerController: hasPounce changed from {oldValue} to {newValue} for player {netId}");
        hasPounce = newValue;
    }

    [Server]
    public void Heal(int amount)
    {
        Debug.Log($"PlayerController: Heal called for player {netId} with amount {amount}");
        Debug.Log($"PlayerController: Current health before heal: {currentHealth}");
        
        if (currentHealth < maxHealth)
        {
            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            Debug.Log($"PlayerController: Health after heal: {currentHealth} (healed {currentHealth - oldHealth})");
            RpcUpdateHealth(currentHealth);
        }
        else
        {
            Debug.Log($"PlayerController: Player {netId} already at max health ({currentHealth}/{maxHealth})");
        }
    }

    [ClientRpc]
    private void RpcUpdateHealth(int newHealth)
    {
        Debug.Log($"[Player {netId}] RpcUpdateHealth called with new health: {newHealth}");
        int oldHealth = currentHealth;
        currentHealth = newHealth;
        Debug.Log($"[Player {netId}] Health updated: {oldHealth} -> {currentHealth}");
        
        if (healthBar != null)
        {
            Debug.Log($"[Player {netId}] Updating health bar to {currentHealth}/{maxHealth}");
            healthBar.SetHealth(currentHealth, maxHealth);
        }
        else
        {
            Debug.LogWarning($"[Player {netId}] Health bar is null, cannot update UI");
        }
    }
}
