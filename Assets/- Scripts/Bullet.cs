using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifetime = 2f; // Time before the bullet is destroyed

    private void Start()
    {
        Destroy(gameObject, lifetime); // Destroy bullet after a certain time
    }

    private void OnTriggerEnter(Collider other)
    {
        // Handle collision with enemies or obstacles
        Destroy(gameObject); // Destroy bullet on hit
    }
}