using UnityEngine;

public class AutoPickupItem : MonoBehaviour
{
    public float pickupRadius = 2f;
    public string itemName; // Only use "Hellstone" or "Souls"

    private Transform player;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= pickupRadius)
        {
            if (itemName == "Divine Dew") return; // Block Divine Dew from auto-pickup
            Pickup();
        }
    }

    private void Pickup()
    {
        Debug.Log($"{itemName} picked up by player!");

        // TODO: Add to inventory (Souls/Hellstone logic here)

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
