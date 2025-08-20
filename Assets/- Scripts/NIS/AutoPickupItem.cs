using UnityEngine;

public class AutoPickupItem : MonoBehaviour
{
    public float pickupRadius = 2f;

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
            PickupSoul();
        }
    }

    private void PickupSoul()
    {
        Debug.Log("Soul picked up by player!");

        // TODO: Add soul to inventory
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.AddItem("Souls", 1); // assumes your PlayerInventory has AddItem(string, int)
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
