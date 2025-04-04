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
    public float pushBackForce = 5f;
    public float rotationSpeed = 360f;

    private Transform player;
    private CharacterController characterController;
    private bool isDead = false;
    private bool aggroedByPlayer = false;
    private float verticalVelocity = 0f;

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

        ApplyGravity();
        ChooseTarget();
        UpdateHealthBarPosition();
        HandleHealthBarFade();

        if (attackTimer > 0)
            attackTimer -= Time.deltaTime;

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
    }

    private void ChooseTarget()
    {
        if (Vector3.Distance(transform.position, player.position) <= detectionRange || aggroedByPlayer)
            AttackPlayer();
        else
            AttackWatchtower();
    }

    private void AttackWatchtower()
    {
        if (watchtower == null || postAttackTimer > 0f) return;
        MoveTowards(watchtower.position);
    }

    private void AttackPlayer()
    {
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
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        characterController.Move(direction * speed * Time.deltaTime);
        transform.forward = direction;
    }

    private void MoveAroundPlayer()
    {
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
        if (IsGrounded())
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;

        characterController.Move(new Vector3(0, verticalVelocity * Time.deltaTime, 0));
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    public void TakeDamage(float damage)
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

        if (currentHealth <= 0)
        {
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        isDead = true;
        characterController.detectCollisions = false;

        if (healthCanvas)
            healthCanvas.gameObject.SetActive(false);

        Vector3 pushBackDir = (-transform.forward + Vector3.up).normalized;
        float timer = 1f;

        while (timer > 0)
        {
            characterController.Move(pushBackDir * pushBackForce * Time.deltaTime);
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
            float enemyHeight = characterController.bounds.extents.y * 2;
            healthCanvas.transform.position = transform.position + Vector3.up * (enemyHeight + 0.1f);

            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            healthCanvas.transform.rotation = Quaternion.LookRotation(cameraForward);

            float scaleFactor = enemyHeight * 0.008f;
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
    }
}
