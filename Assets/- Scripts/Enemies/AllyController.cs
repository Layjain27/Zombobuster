using UnityEngine;
using UnityEngine.AI;

// Enum to define the ally's current command state
public enum AllyState { Following, HoldingPosition }

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(TowerHealth))]
public class AllyController : MonoBehaviour
{
    [Header("AI State")]
    private AllyState currentState = AllyState.Following;

    [Header("AI Settings")]
    private NavMeshAgent agent;
    private Transform playerTransform;
    [Tooltip("How close the ally should get to the player before stopping.")]
    public float playerFollowDistance = 3f;

    [Header("Targeting")]
    public LayerMask enemyLayer;
    public float detectionRadius = 20f;
    private Transform currentTarget;

    [Header("Attacking")]
    public float attackRange = 15f;
    public float attackRate = 1f;
    public float attackDamage = 10f;
    private float nextAttackTime = 0f;

    private TowerHealth health;

    // Optional: expose following state for external checks
    public bool IsFollowing => currentState == AllyState.Following;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<TowerHealth>();
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Start()
    {
        health.OnDeath += HandleDeath;
    }

    void Update()
    {
        if (health.CurrentHealth <= 0) return;

        FindClosestTarget();

        if (currentTarget != null)
        {
            EngageTarget();
        }
        else
        {
            switch (currentState)
            {
                case AllyState.Following:
                    FollowPlayer();
                    break;
                case AllyState.HoldingPosition:
                    HoldPosition();
                    break;
            }
        }
    }

    // Toggle follow/hold state (called by PlayerNIS)
    public void ToggleFollowState()
    {
        currentState = (currentState == AllyState.Following) ? AllyState.HoldingPosition : AllyState.Following;

        if (currentState == AllyState.HoldingPosition)
            agent.isStopped = true;
    }

    // --- AI Behavior ---
    private void EngageTarget()
    {
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        if (distanceToTarget <= attackRange)
        {
            agent.isStopped = true;
            transform.LookAt(currentTarget);

            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(currentTarget.position);
        }
    }

    private void FollowPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer > playerFollowDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(playerTransform.position);
        }
        else
        {
            agent.isStopped = true;
        }
    }

    private void HoldPosition()
    {
        agent.isStopped = true;
    }

    void FindClosestTarget()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, enemyLayer);
        float closestDistance = Mathf.Infinity;
        Transform closestEnemy = null;

        foreach (var hitCollider in hitColliders)
        {
            float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = hitCollider.transform;
            }
        }

        currentTarget = closestEnemy;
    }

    void Attack()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, attackRange))
        {
            IDamageable damageable = hit.collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
            }
        }
    }

    void HandleDeath()
    {
        agent.isStopped = true;
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, playerFollowDistance);
    }
}
