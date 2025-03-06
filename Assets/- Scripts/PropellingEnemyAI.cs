using UnityEngine;
using System.Collections;

public class PropellingEnemyAI : MonoBehaviour
{
    public float speed = 3f;
    public float rotationSpeed = 5f;
    public float knockbackForce = 5f;
    public float deathRotationSpeed = 360f;
    private Transform player;
    private bool isDead = false;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (isDead || player == null) return;

        // Move towards player
        Vector3 direction = (player.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // Rotate to face player
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    public void TakeDamage()
    {
        if (isDead) return;

        isDead = true;
        StartCoroutine(Die());
    }

    private IEnumerator Die()
    {
        float timer = 1f; // Time before enemy is destroyed
        Vector3 knockbackDirection = -transform.forward + Vector3.up; // Push back and up
        float rotationAmount = 0f;

        while (timer > 0)
        {
            transform.position += knockbackDirection * knockbackForce * Time.deltaTime;
            rotationAmount += deathRotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.forward, rotationAmount);
            timer -= Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
