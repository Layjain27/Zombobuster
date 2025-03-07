using UnityEngine;
using System.Collections;

public class GroundedEnemy : MonoBehaviour
{
    public float speed = 3f;
    public float pushBackForce = 5f;
    public float rotationSpeed = 360f;
    public float gravity = -9.81f;
    public float groundCheckDistance = 1.1f;
    public LayerMask groundLayer;

    public Transform watchtower; // Assign the watchtower in the Inspector
    public float detectionRange = 5f; // Distance at which the player is detected

    private Transform player;
    private CharacterController characterController;
    private bool isDead = false;
    private bool aggroedByPlayer = false;
    private float verticalVelocity = 0f;

    public event System.Action OnDeath; // Event for when enemy dies

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (isDead) return;

        ApplyGravity();
        ChooseTarget();
    }

    private void ChooseTarget()
    {
        // If the player is within range OR the enemy was attacked, switch target to player
        if (Vector3.Distance(transform.position, player.position) <= detectionRange || aggroedByPlayer)
        {
            AttackPlayer();
        }
        else
        {
            AttackWatchtower();
        }
    }

    private void AttackWatchtower()
    {
        if (watchtower == null) return;

        Debug.Log("Enemy attacking the watchtower!");

        MoveTowards(watchtower.position);
    }

    private void AttackPlayer()
    {
        Debug.Log("Enemy attacking the player!");

        MoveTowards(player.position);
    }

    private void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        direction.y = 0; // Prevent movement in the air

        characterController.Move(direction * speed * Time.deltaTime);
        transform.forward = direction; // Rotate to face the target
    }

    private void ApplyGravity()
    {
        if (IsGrounded())
        {
            verticalVelocity = -2f; // Keep it grounded
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        characterController.Move(new Vector3(0, verticalVelocity * Time.deltaTime, 0));
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    public void TakeDamage()
    {
        if (isDead) return;

        isDead = true;
        aggroedByPlayer = true; // Enemy switches to attacking the player
        StartCoroutine(Die());
    }

    private IEnumerator Die()
    {
        Vector3 pushBackDirection = -transform.forward + Vector3.up;
        float timer = 1f;

        while (timer > 0)
        {
            characterController.Move(pushBackDirection * pushBackForce * Time.deltaTime);
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
            timer -= Time.deltaTime;
            yield return null;
        }

        OnDeath?.Invoke(); // Notify spawner that enemy died
        Destroy(gameObject);
    }
}
