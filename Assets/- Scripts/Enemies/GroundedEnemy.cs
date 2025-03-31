using UnityEngine;
using System.Collections;

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
    public Transform watchtower; // Assign watchtower in Inspector
    public float detectionRange = 5f; // Player detection range

    private Transform player;
    private CharacterController characterController;
    private bool isDead = false;
    private bool aggroedByPlayer = false;
    private float verticalVelocity = 0f;

    public event System.Action OnDeath; // Event for notifying death

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
        // Aggro on player if detected or attacked
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
        direction.y = 0; // Ignore Y for horizontal movement
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

    // Call this when hit by a bullet
    public void TakeDamage()
    {
        if (isDead) return;

        isDead = true;
        aggroedByPlayer = true;
        StartCoroutine(Die());
    }

    private IEnumerator Die()
    {
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
}
