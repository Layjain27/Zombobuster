using UnityEngine;
using System.Collections;
using UnityEngine.UI;

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
    public Transform watchtower;
    public float detectionRange = 5f;

    [Header("Spacing Settings")]
    public float spacingRadius = 1.2f;
    public LayerMask enemyLayer;

    [Header("Health Bar Settings")]
    public GameObject healthBarPrefab;
    public float maxHealth = 100f;
    private float currentHealth;
    private Slider healthBar;
    private Canvas healthCanvas;
    private float healthBarFadeTimer = 0f;
    public float healthBarFadeDuration = 3f;

    [Header("Death Settings")]
    public float pushBackForce = 5f; // This is for death animation push back
    public float rotationSpeed = 360f;

    [Header("Knockback Settings")] // New Header for enemy-specific knockback
    public float knockbackForceReceiver = 5f; // How strong the knockback is received by this enemy
    public float knockbackDuration = 0.2f; // How long the enemy is actively pushed back
    public float knockbackLerpSpeed = 10f; // How quickly the knockback force decays

    private Transform player;
    private CharacterController characterController;
    private bool isDead = false;
    private bool aggroedByPlayer = false;
    private float verticalVelocity = 0f;

    private Vector3 knockbackVelocity = Vector3.zero; // Current velocity due to knockback
    private bool isKnockedBack = false; // Flag to indicate if enemy is currently being knocked back

    public event System.Action OnDeath;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        characterController = GetComponent<CharacterController>();

        currentHealth = maxHealth;

        if (healthBarPrefab)
        {
            GameObject healthBarInstance = Instantiate(healthBarPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            healthCanvas = healthBarInstance.GetComponentInChildren<Canvas>();
            healthBar = healthCanvas.GetComponentInChildren<Slider>();
            healthCanvas.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Handle knockback movement
        if (isKnockedBack)
        {
            // Apply knockback velocity using CharacterController.Move
            characterController.Move(knockbackVelocity * Time.deltaTime);

            // Decay the knockback velocity over time
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackLerpSpeed * Time.deltaTime);

            // If knockback velocity is very small, consider knockback finished
            // This prevents the enemy from "sliding" indefinitely from a tiny residual force
            if (knockbackVelocity.magnitude < 0.1f)
            {
                isKnockedBack = false;
            }
            // IMPORTANT: If you want other movement/AI to completely stop during knockback,
            // make sure to return early here or manage states within ChooseTarget().
            // For now, we'll let it try to move if the knockback is very weak.
        }

        // Only run normal AI/movement if not knocked back AND not in post-attack pause
        if (postAttackTimer > 0)
        {
            postAttackTimer -= Time.deltaTime;

            // Smoothly rotate to face the player even during pause
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
            }

            return; // Stop all other actions during pause
        }

        // Only apply gravity and choose target if not currently experiencing significant knockback
        // This prevents gravity from interfering too much with knockback and ensures AI pauses
        if (!isKnockedBack)
        {
            ApplyGravity();
            ChooseTarget();
        }

        UpdateHealthBarPosition();
        HandleHealthBarFade();

        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;
    }

    private void ChooseTarget()
    {
        // Don't choose target or move if knocked back
        if (isKnockedBack) return;

        if (Vector3.Distance(transform.position, player.position) <= detectionRange || aggroedByPlayer)
            AttackPlayer();
        else
            AttackWatchtower();
    }

    private void AttackWatchtower()
    {
        if (watchtower == null || postAttackTimer > 0f || isKnockedBack) return; // Added isKnockedBack check
        MoveTowards(watchtower.position);
    }

    private void AttackPlayer()
    {
        if (isKnockedBack) return; // Added isKnockedBack check

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            if (attackTimer <= 0f && postAttackTimer <= 0f)
            {
                PlayerNIS playerScript = player.GetComponent<PlayerNIS>();
                if (playerScript != null && !playerScript.IsDead)
                {
                    playerScript.TakeDamage(attackDamage);
                    attackTimer = attackCooldown;
                    postAttackTimer = postAttackPause;
                }
            }
        }
        else if (postAttackTimer <= 0f)
        {
            if (!IsAttackPositionBlocked())
                MoveTowards(player.position);
            else
                MoveAroundPlayer();
        }
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        // Only move if not knocked back
        if (isKnockedBack) return;

        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        characterController.Move(direction * speed * Time.deltaTime);
        transform.forward = direction;
    }

    private void MoveAroundPlayer()
    {
        // Only move if not knocked back
        if (isKnockedBack) return;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 strafeDirection = Vector3.Cross(Vector3.up, directionToPlayer);
        Vector3 offset = strafeDirection * (Random.value > 0.5f ? 1 : -1) * spacingRadius;
        Vector3 strafeTarget = player.position + offset;

        Vector3 direction = (strafeTarget - transform.position).normalized;
        direction.y = 0;
        characterController.Move(direction * speed * Time.deltaTime);
        transform.forward = direction;
    }

    private bool IsAttackPositionBlocked()
    {
        // No need to check isKnockedBack here, as calling methods already handle it.
        Collider[] colliders = Physics.OverlapSphere(transform.position, spacingRadius, enemyLayer);
        foreach (var col in colliders)
        {
            if (col != null && col.transform != transform)
                return true;
        }
        return false;
    }

    private void ApplyGravity()
    {
        // Gravity is applied normally, but characterController.Move
        // will be overridden by knockbackVelocity if isKnockedBack is true.
        if (IsGrounded())
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        characterController.Move(new Vector3(0, verticalVelocity * Time.deltaTime, 0));
    }

    private bool IsGrounded()
    {
        // Use characterController.isGrounded for reliability with CharacterController
        // You can still keep Raycast for additional ground checks if needed, but isGrounded is primary.
        return characterController.isGrounded || Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    // Modified to accept attacker position for knockback direction
    public void TakeDamage(float damage, Vector3 attackerPosition)
    {
        if (isDead) return;

        currentHealth -= damage;
        aggroedByPlayer = true;

        if (healthBar)
        {
            healthCanvas.gameObject.SetActive(true);
            healthBar.value = currentHealth / maxHealth;
            healthBarFadeTimer = healthBarFadeDuration;
        }

        // --- KNOCKBACK IMPLEMENTATION START ---
        // Calculate knockback direction away from the attacker
        Vector3 knockbackDirection = (transform.position - attackerPosition).normalized;
        knockbackDirection.y = 0; // Keep knockback horizontal for isometric view
        knockbackDirection.Normalize(); // Re-normalize after setting y to 0

        // Set the initial knockback velocity
        // Use knockbackForceReceiver from this script, as it controls how this enemy reacts
        knockbackVelocity = knockbackDirection * knockbackForceReceiver;
        isKnockedBack = true;

        // Reset post-attack timer if currently in one, to allow knockback to interrupt
        postAttackTimer = 0f;
        // --- KNOCKBACK IMPLEMENTATION END ---

        if (currentHealth <= 0)
        {
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        isDead = true;
        // Disable CharacterController to stop its movement and collisions during death animation
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (healthCanvas)
            healthCanvas.gameObject.SetActive(false);

        // This push back is specifically for the death animation
        Vector3 deathPushBackDir = (-transform.forward + Vector3.up).normalized;
        float timer = 1f;

        // If you want the death push back to use a Rigidbody (e.g., ragdoll), you'd enable/add it here.
        // For now, it will apply a simple push using transform.position if CharacterController is disabled.
        // Or you can use a Rigidbody here for better physics based death.
        // If you enable Rigidbody for death, make sure to add it and set isKinematic false.

        while (timer > 0)
        {
            // If CharacterController is disabled, this push will work
            // If CharacterController is enabled (which it shouldn't be here), this won't work well
            transform.position += deathPushBackDir * pushBackForce * Time.deltaTime;
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
            timer -= Time.deltaTime;
            yield return null;
        }

        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    private void UpdateHealthBarPosition()
    {
        if (healthCanvas)
        {
            // characterController.height or bounds.size.y is generally better for full height
            float enemyHeight = characterController.height;
            healthCanvas.transform.position = transform.position + Vector3.up * (enemyHeight + 0.1f);

            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            healthCanvas.transform.rotation = Quaternion.LookRotation(cameraForward);

            float scaleFactor = enemyHeight * 0.008f; // Adjust this factor as needed
            healthCanvas.transform.localScale = Vector3.one * scaleFactor;
        }
    }

    private void HandleHealthBarFade()
    {
        if (healthCanvas && healthCanvas.gameObject.activeSelf)
        {
            if (healthBarFadeTimer > 0)
                healthBarFadeTimer -= Time.deltaTime;
            else
                healthCanvas.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (characterController != null)
        {
            // Draw a sphere for the CharacterController's bottom, useful for ground check visualization
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * (characterController.height / 2 - characterController.radius), characterController.radius);
        }
    }
}