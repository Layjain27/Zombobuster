using UnityEngine;

public class AutoPickupItem : MonoBehaviour
{
    public float pickupRadius = 2f;
    public string itemName; // "Hellstone", "Souls"
    public int amount = 1;

    private Transform player;
    private PlayerInventory playerInventory;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj)
        {
            player = playerObj.transform;
            playerInventory = playerObj.GetComponent<PlayerInventory>();
        }
    }

    private void Update()
    {
        if (player == null || playerInventory == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= pickupRadius)
        {
            playerInventory.AddItem(itemName, amount);
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
