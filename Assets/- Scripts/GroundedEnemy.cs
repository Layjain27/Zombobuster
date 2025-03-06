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

    private Transform player;
    private CharacterController characterController;
    private bool isDead = false;
    private float verticalVelocity = 0f;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (isDead) return;

        ApplyGravity();
        MoveTowardsPlayer();
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Prevent movement in the air

        characterController.Move(direction * speed * Time.deltaTime);
        transform.forward = direction; // Rotate to face the player
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

        Destroy(gameObject);
    }
}
