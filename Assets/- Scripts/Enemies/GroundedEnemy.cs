using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Make sure this is included for UI elements

public class GroundedEnemy : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 3f;
    public float gravity = -9.81f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;

    [Header("Attack Settings")]
    public float attackRange = 1.5f;
    public float attackDamage = 10f;
    public float attackCooldown = 2f;
    private float attackTimer = 0f;

    [Header("Post-Attack Settings")]
    public float postAttackPause = 1f;
    private float postAttackTimer = 0f;

    [Header("Detection Settings")]
    public Transform watchtower; // This will likely be your MainTower or a specific building
    public float detectionRange = 5f;

    [Header("Spacing Settings")]
    public float spacingRadius = 1.2f;
    public LayerMask enemyLayer; // Layer containing other enemies for spacing

    [Header("Health Bar Settings")]
    public GameObject healthBarPrefab; // Drag your EnemyHealthBarCanvas prefab here
    public float maxHealth = 100f; // This will be the base max health from inspector
    private float currentHealth;
    private Slider healthBar;
    private Canvas healthCanvas;
    private float healthBarFadeTimer = 0f;
    public float healthBarFadeDuration = 3f; // How long health bar stays visible after taking damage

    [Header("Death Settings")]
    public float pushBackForce = 5f;
    public float rotationSpeed = 360f; // Speed of rotation when dying

    // Private References
    private Transform player;
    private CharacterController characterController;
    private bool isDead = false;
    private bool aggroedByPlayer = false; // Becomes true if player damages enemy
    private float verticalVelocity = 0f; // For gravity

    // Event for when this enemy dies (e.g., for GameMetrics or Tower)
    public event System.Action OnDeath;

    private void Start()
    {
        // Find the player by tag. Make sure your player GameObject has the "Player" tag.
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player GameObject with tag 'Player' not found! Enemy cannot target player.");
        }

        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController not found on " + gameObject.name + ". This script requires it!");
        }

        // Initialize health. If SetHealth is called later, it will override this.
        currentHealth = maxHealth;

        // Instantiate and set up the health bar
        if (healthBarPrefab)
        {
            // Instantiate the prefab. Position will be updated in UpdateHealthBarPosition().
            GameObject healthBarInstance = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
            // Parent the health bar to this enemy so it moves with it
            healthBarInstance.transform.SetParent(transform);

            healthCanvas = healthBarInstance.GetComponentInChildren<Canvas>();
            healthBar = healthCanvas.GetComponentInChildren<Slider>();

            if (healthCanvas == null) Debug.LogError("HealthBarPrefab is missing a Canvas child.");
            if (healthBar == null) Debug.LogError("HealthBarPrefab is missing a Slider child.");

            // Health bar starts hidden
            if (healthCanvas != null)
            {
                healthCanvas.gameObject.SetActive(false);
            }
        }
        else
        {
            Debug.LogWarning("Health Bar Prefab is not assigned on " + gameObject.name + ". Health bar will not be displayed.");
        }

        // Update health bar initially based on currentHealth / maxHealth
        // This is important if maxHealth is changed by SetHealth before Start finishes.
        UpdateHealthBar();
    }

    private void Update()
    {
        if (isDead) return;

        ApplyGravity(); // Keep enemy on the ground
        ChooseTarget(); // Decide whether to attack player or watchtower
        UpdateHealthBarPosition(); // Keep health bar above enemy and facing camera
        HandleHealthBarFade(); // Manage health bar visibility

        // Update timers
        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

        if (postAttackTimer > 0)
        {
            postAttackTimer -= Time.deltaTime;

            // Smoothly rotate to face the player even during post-attack pause
            if (player != null)
            {
                Vector3 lookDir = player.position - transform.position;
                lookDir.y = 0; // Ignore Y-axis for horizontal rotation
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
                }
            }
            return; // Stop all other actions (movement, new attacks) during pause
        }
    }

    private void ChooseTarget()
    {
        // Prioritize player if within detection range or aggroed
        if (player != null && (Vector3.Distance(transform.position, player.position) <= detectionRange || aggroedByPlayer))
            AttackPlayer();
        else
            AttackWatchtower(); // If player not detected/aggroed, attack watchtower
    }

    private void AttackWatchtower()
    {
        if (watchtower == null || postAttackTimer > 0f) return; // Cannot attack if no watchtower or in post-attack pause

        MoveTowards(watchtower.position); // Move towards the watchtower
    }

    private void AttackPlayer()
    {
        if (player == null) return; // No player to attack

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            // Within attack range
            if (attackTimer <= 0f && postAttackTimer <= 0f) // Check cooldown and post-attack pause
            {
                PlayerNIS playerScript = player.GetComponent<PlayerNIS>();
                if (playerScript != null && !playerScript.IsDead) // Check if player is alive and has PlayerNIS script
                {
                    playerScript.TakeDamage(attackDamage); // Deal damage to player
                    attackTimer = attackCooldown; // Reset attack cooldown
                    postAttackTimer = postAttackPause; // Start post-attack pause
                }
            }
        }
        else if (postAttackTimer <= 0f) // Not in attack range, and not in post-attack pause
        {
            // Move towards player, avoiding other enemies if too close
            if (!IsAttackPositionBlocked())
                MoveTowards(player.position);
            else
                MoveAroundPlayer(); // Circle player if direct path is blocked by other enemies
        }
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        if (characterController == null) return;

        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Only move horizontally

        characterController.Move(direction * speed * Time.deltaTime); // Move using CharacterController

        // Rotate to face movement direction
        if (direction != Vector3.zero) // Avoid looking at (0,0,0) if target is at enemy position
        {
            transform.forward = direction;
        }
    }

    private void MoveAroundPlayer()
    {
        if (player == null || characterController == null) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        // Get a perpendicular vector for strafing
        Vector3 strafeDirection = Vector3.Cross(Vector3.up, directionToPlayer);

        // Randomly choose left or right strafe direction
        Vector3 offset = strafeDirection * (Random.value > 0.5f ? 1 : -1) * spacingRadius;
        Vector3 strafeTarget = player.position + offset; // Target a point around the player

        Vector3 direction = (strafeTarget - transform.position).normalized;
        direction.y = 0;

        characterController.Move(direction * speed * Time.deltaTime);

        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }
    }

    // Checks if the immediate attack position around the player is blocked by another enemy
    private bool IsAttackPositionBlocked()
    {
        // Use OverlapSphere to detect other enemies within spacingRadius
        Collider[] colliders = Physics.OverlapSphere(transform.position, spacingRadius, enemyLayer);
        foreach (var col in colliders)
        {
            if (col != null && col.transform != transform) // Make sure it's not this enemy's own collider
                return true; // Another enemy is too close
        }
        return false;
    }

    private void ApplyGravity()
    {
        if (characterController == null) return;

        if (IsGrounded())
            verticalVelocity = -2f; // Small downward force to keep grounded
        else
            verticalVelocity += gravity * Time.deltaTime; // Apply gravity

        characterController.Move(new Vector3(0, verticalVelocity * Time.deltaTime, 0)); // Apply vertical movement
    }

    private bool IsGrounded()
    {
        // Raycast down from the enemy's feet to check for ground
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    // Public method for taking damage (can be called by player attacks, towers etc.)
    // MODIFIED: Added Vector3 hitPoint parameter to match expected overload
    public void TakeDamage(float damage, Vector3 hitPoint)
    {
        if (isDead) return;

        currentHealth -= damage;
        aggroedByPlayer = true; // Enemy is now aggroed on the player

        // Show and update health bar
        UpdateHealthBar(); // Call the helper method
        healthBarFadeTimer = healthBarFadeDuration; // Reset fade timer

        // You can use hitPoint here if you want to add effects at the hit location
        // e.g., Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);

        if (currentHealth <= 0)
        {
            StartCoroutine(Die()); // Start death routine
        }
    }

    // Method to set the enemy's health (used by spawn points/towers)
    public void SetHealth(float health)
    {
        maxHealth = health;
        currentHealth = maxHealth;
        UpdateHealthBar(); // Update the health bar to reflect the new max health and full current health
    }

    // Helper method to update the health bar's visual state
    private void UpdateHealthBar()
    {
        if (healthBar && healthCanvas)
        {
            healthCanvas.gameObject.SetActive(true); // Ensure health bar is active
            healthBar.value = currentHealth / maxHealth;
        }
    }

    private IEnumerator Die()
    {
        isDead = true;
        // Disable character controller and collider to stop movement and interactions
        if (characterController != null) characterController.enabled = false;

        // Hide health bar immediately on death
        if (healthCanvas)
            healthCanvas.gameObject.SetActive(false);

        // Calculate pushback direction (away from player, slightly upwards)
        Vector3 pushBackDir = (-transform.forward + Vector3.up).normalized;
        float timer = 1f; // Duration of death animation/pushback

        // Apply pushback and rotation effect
        while (timer > 0)
        {
            transform.position += pushBackDir * pushBackForce * Time.deltaTime;
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
            timer -= Time.deltaTime;
            yield return null;
        }

        // Invoke the OnDeath event (e.g., for GameMetrics to decrement active enemies)
        OnDeath?.Invoke(); // Null-conditional operator for event invocation

        // Destroy the enemy GameObject
        Destroy(gameObject);
    }

    // Updates the position and rotation of the health bar to float above the enemy and face the camera
    private void UpdateHealthBarPosition()
    {
        if (healthCanvas && Camera.main != null)
        {
            // Position above the enemy, considering enemy's height
            float enemyHeight = characterController.bounds.extents.y * 2; // Full height of the character controller
            healthCanvas.transform.position = transform.position + Vector3.up * (enemyHeight + 0.1f); // 0.1f is a small offset

            // Make the health bar face the main camera
            // Only consider X and Z rotation for facing, keep Y-axis upright relative to world
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0; // Flatten the vector to prevent health bar from tilting up/down with camera
            if (cameraForward != Vector3.zero)
            {
                healthCanvas.transform.rotation = Quaternion.LookRotation(cameraForward);
            }

            // Scale the health bar based on enemy height for consistent visual size
            float scaleFactor = enemyHeight * 0.008f; // Adjust 0.008f to make the health bar the desired visual size
            healthCanvas.transform.localScale = Vector3.one * scaleFactor;
        }
    }

    // Manages the health bar fading out after a duration if no damage is taken
    private void HandleHealthBarFade()
    {
        if (healthCanvas && healthCanvas.gameObject.activeSelf) // Only process if active
        {
            if (healthBarFadeTimer > 0)
                healthBarFadeTimer -= Time.deltaTime;
            else
                healthCanvas.gameObject.SetActive(false); // Hide if timer runs out
        }
    }

    // Visual aids for debugging in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange); // Visualize detection range

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange); // Visualize attack range

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spacingRadius); // Visualize spacing radius
    }
}