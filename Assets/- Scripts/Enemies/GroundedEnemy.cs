using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CapsuleCollider))]
public class GroundedEnemy : MonoBehaviour, IDamageable
{
    [Header("Identity")]
    public Faction faction;

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

    [Header("Ally Detection")]
    public LayerMask allyLayer;
    public float allyDetectionRadius = 5f;

    [Header("Player Detection")]
    public float playerDetectionRadius = 3f;

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
    private NavMeshAgent navMeshAgent;
    private CapsuleCollider capsuleCollider;
    private bool isDead = false;

    public event System.Action OnDeath;

    private void Start()
    {
        faction = Faction.Enemy;

        if (watchtower == null)
            Debug.LogWarning("Watchtower transform is not assigned on " + gameObject.name);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        else Debug.LogError("Player with tag 'Player' not found!");

        navMeshAgent = GetComponent<NavMeshAgent>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        navMeshAgent.height = capsuleCollider.height;
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

        if (postAttackTimer > 0)
        {
            postAttackTimer -= Time.deltaTime;
            navMeshAgent.isStopped = true;

            Transform currentTarget = GetCurrentTarget();
            if (currentTarget != null)
            {
                Vector3 lookDir = currentTarget.position - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), 10f * Time.deltaTime);
            }
            return;
        }

        ChooseTargetAndAttack();
        UpdateHealthBarPosition();
        HandleHealthBarFade();

        if (attackTimer > 0) attackTimer -= Time.deltaTime;
    }

    private Transform GetCurrentTarget()
    {
        // 1. Check for closest ally first
        Collider[] allyColliders = Physics.OverlapSphere(transform.position, allyDetectionRadius, allyLayer);
        Transform closestAlly = null;
        float closestDistance = Mathf.Infinity;
        foreach (var col in allyColliders)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestAlly = col.transform;
            }
        }
        if (closestAlly != null) return closestAlly;

        // 2. Default to watchtower
        if (watchtower != null) return watchtower;

        // 3. Player only if within “way” (detection radius)
        if (player != null && Vector3.Distance(transform.position, player.position) <= playerDetectionRadius)
            return player;

        return null; // Nothing to attack
    }

    private void ChooseTargetAndAttack()
    {
        Transform currentTarget = GetCurrentTarget();
        if (currentTarget == null)
        {
            navMeshAgent.isStopped = true;
            return;
        }

        navMeshAgent.SetDestination(currentTarget.position);
        float distance = Vector3.Distance(transform.position, currentTarget.position);

        if (distance <= navMeshAgent.stoppingDistance)
        {
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
            navMeshAgent.isStopped = false;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        UpdateHealthBar();
        healthBarFadeTimer = healthBarFadeDuration;
        if (currentHealth <= 0)
            StartCoroutine(Die());
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
            float enemyHeight = capsuleCollider.height;
            healthCanvas.transform.position = transform.position + Vector3.up * (enemyHeight + 0.1f);

            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;
            if (cameraForward != Vector3.zero)
                healthCanvas.transform.rotation = Quaternion.LookRotation(cameraForward);

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
        Gizmos.DrawWireSphere(transform.position, playerDetectionRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, allyDetectionRadius);
    }
}
