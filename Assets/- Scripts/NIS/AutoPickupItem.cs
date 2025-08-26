using UnityEngine;

public class AutoPickupItem : MonoBehaviour
{
    public float pickupRadius = 2f;
    public string itemName; // Souls, Hellstone, DivineDew

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
            // Block auto-pickup for Divine Dew
            if (itemName == "DivineDew") return;

            Pickup();
        }
    }

    private void Pickup()
    {
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.AddItem(itemName, 1);
            Debug.Log($"{itemName} picked up by player!");
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
