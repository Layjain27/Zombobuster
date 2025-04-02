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

    [Header("Combat Settings")]
    public float pushBackForce = 5f;
    public float rotationSpeed = 360f;
    public Transform watchtower;
    public float detectionRange = 5f;

    [Header("Health Bar Settings")]
    public GameObject healthBarPrefab;
    public float maxHealth = 100f;
    private float currentHealth;
    private Slider healthBar;
    private Canvas healthCanvas;
    private float healthBarFadeTimer = 0f;
    public float healthBarFadeDuration = 3f;

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

        // Instantiate health bar
        if (healthBarPrefab)
        {
            GameObject healthBarInstance = Instantiate(healthBarPrefab, transform.position + Vector3.up * 2f, Quaternion.identity);
            healthCanvas = healthBarInstance.GetComponentInChildren<Canvas>();
            healthBar = healthCanvas.GetComponentInChildren<Slider>();
            healthCanvas.gameObject.SetActive(false); // Hide initially
        }
    }

    private void Update()
    {
        if (isDead) return;

        ApplyGravity();
        ChooseTarget();
        UpdateHealthBarPosition();
        HandleHealthBarFade();
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
        if (watchtower == null) return;
        MoveTowards(watchtower.position);
    }

    private void AttackPlayer()
    {
        MoveTowards(player.position);
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0;
        characterController.Move(direction * speed * Time.deltaTime);
        transform.forward = direction;
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

        // Disable character controller collision
        characterController.detectCollisions = false;

        // Disable health bar
        if (healthCanvas)
            healthCanvas.gameObject.SetActive(false);

        // Knockback effect
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
            healthCanvas.transform.position = transform.position + Vector3.up * 2f;
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
}
