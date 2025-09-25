using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI; // --- NEW: Required for NavMeshAgent ---
using UnityEngine.UI;

// Ensure the GameObject has these components
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CapsuleCollider))]

public class GroundedEnemy : MonoBehaviour, IDamageable
{
    [Header("Identity")]
    public Faction faction;

    // --- REMOVED: Old Movement Settings ---
    // public float speed = 3f;
    // public float gravity = -9.81f;
    // public float groundCheckDistance = 1.1f;
    // public LayerMask groundLayer;

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
    public float pushBackForce = 5f;
    public float rotationSpeed = 360f;

    [Header("Drop Settings")]
    [SerializeField] private GameObject soulsPrefab;
    [SerializeField] private float soulsDropChance = 1f;

    private Transform player;
    // --- REPLACED: CharacterController with NavMeshAgent ---
    private NavMeshAgent navMeshAgent;
    private CapsuleCollider capsuleCollider;
    private bool isDead = false;
    private bool aggroedByPlayer = false;
    // --- REMOVED: verticalVelocity ---

    public event System.Action OnDeath;

    private void Start()
    {
        faction = Faction.Enemy;

        // --- NEW: Add a check to ensure the watchtower is assigned ---
        if (watchtower == null)
        {
            Debug.LogWarning("Watchtower transform is not assigned on " + gameObject.name + ". The enemy may not move without a default target.", this);
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        else Debug.LogError("Player with tag 'Player' not found!");

        // --- NEW: Get NavMeshAgent and Collider components ---
        navMeshAgent = GetComponent<NavMeshAgent>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // --- NEW: Sync agent height with collider height to prevent sinking ---
        navMeshAgent.height = capsuleCollider.height;

        // --- NEW: Configure NavMeshAgent based on attack range ---
        // The agent will stop just before it reaches the attack range.
        navMeshAgent.stoppingDistance = attackRange * 0.9f;

        currentHealth = maxHealth;

        if (healthBarPrefab)
        {
            GameObject healthBarInstance = Instantiate(healthBarPrefab, transform.position, Quaternion.identity);
            healthBarInstance.transform.SetParent(transform);
            healthCanvas = healthBarInstance.GetComponentInChildren<Canvas>();
            healthBar = healthCanvas.GetComponentInChildren<Slider>();
            if (healthCanvas != null) healthCanvas.gameObject.SetActive(false);
        }
        UpdateHealthBar();
    }

    private void Update()
    {
        if (isDead) return;

        // --- REFACTORED: Simplified logic for post-attack pause ---
        if (postAttackTimer > 0)
        {
            postAttackTimer -= Time.deltaTime;
            navMeshAgent.isStopped = true; // Force the agent to stop during the pause.

            // Face the current target during the post-attack pause
            Transform currentTarget = GetCurrentTarget();
            if (currentTarget != null)
            {
                Vector3 lookDir = currentTarget.position - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
                }
            }
            return; // Exit Update early during the pause
        }

        ChooseTargetAndAttack();
        UpdateHealthBarPosition();
        HandleHealthBarFade();

        if (attackTimer > 0) attackTimer -= Time.deltaTime;
    }

    // --- NEW: Extracted target selection into its own method for clarity ---
    private Transform GetCurrentTarget()
    {
        // Prioritize the player if they are in range or have aggroed the enemy
        if (player != null && (Vector3.Distance(transform.position, player.position) <= detectionRange || aggroedByPlayer))
        {
            return player;
        }
        // Otherwise, return the default watchtower target
        return watchtower;
    }

    private void ChooseTargetAndAttack()
    {
        Transform currentTarget = GetCurrentTarget();

        // If there's no target at all, do nothing.
        if (currentTarget == null)
        {
            navMeshAgent.isStopped = true; // Stop the agent if it has no target
            return;
        }

        // --- REFACTORED: Logic for movement and attacking is now clearer ---
        navMeshAgent.SetDestination(currentTarget.position);
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance <= navMeshAgent.stoppingDistance)
        {
            // In attack range
            navMeshAgent.isStopped = true;
            if (attackTimer <= 0f)
            {
                IDamageable targetHealth = currentTarget.GetComponent<IDamageable>();
                if (targetHealth != null)
                {
                    targetHealth.TakeDamage(attackDamage);
                    attackTimer = attackCooldown;
                    postAttackTimer = postAttackPause;
                }
            }
        }
        else
        {
            // Out of attack range, so move
            navMeshAgent.isStopped = false;
        }
    }

    // --- REMOVED: MoveTowards(), ApplyGravity(), and IsGrounded() methods ---
    // The NavMeshAgent now handles all of this logic.

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        aggroedByPlayer = true;
        UpdateHealthBar();
        healthBarFadeTimer = healthBarFadeDuration;
        if (currentHealth <= 0)
        {
            StartCoroutine(Die());
        }
    }

    public void SetHealth(float health)
    {
        maxHealth = health;
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthBar && healthCanvas)
        {
            healthCanvas.gameObject.SetActive(true);
            healthBar.value = currentHealth / maxHealth;
        }
    }

    private IEnumerator Die()
    {
        isDead = true;

        // --- UPDATED: Disable NavMeshAgent instead of CharacterController ---
        if (navMeshAgent != null) navMeshAgent.enabled = false;

        if (healthCanvas) healthCanvas.gameObject.SetActive(false);

        if (soulsPrefab != null && Random.value <= soulsDropChance)
        {
            Vector3 dropPosition = new Vector3(transform.position.x, 0, transform.position.z);
            Instantiate(soulsPrefab, dropPosition, Quaternion.identity);
        }

        Vector3 pushBackDir = (-transform.forward + Vector3.up).normalized;
        float timer = 1f;
        while (timer > 0)
        {
            transform.position += pushBackDir * pushBackForce * Time.deltaTime;
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
            timer -= Time.deltaTime;
            yield return null;
        }

        OnDeath?.Invoke();
        Destroy(gameObject);
    }

    private void UpdateHealthBarPosition()
    {
        if (healthCanvas && Camera.main != null)
        {
            // --- UPDATED: Use CapsuleCollider for height calculation ---
            float enemyHeight = capsuleCollider.height;
            healthCanvas.transform.position = transform.position + Vector3.up * (enemyHeight + 0.1f);

            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            if (cameraForward != Vector3.zero)
            {
                healthCanvas.transform.rotation = Quaternion.LookRotation(cameraForward);
            }
            float scaleFactor = enemyHeight * 0.008f;
            healthCanvas.transform.localScale = Vector3.one * scaleFactor;
        }
    }

    private void HandleHealthBarFade()
    {
        if (healthCanvas && healthCanvas.gameObject.activeSelf)
        {
            if (healthBarFadeTimer > 0) healthBarFadeTimer -= Time.deltaTime;
            else healthCanvas.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spacingRadius);
    }
} // Filename: AllyController.cs