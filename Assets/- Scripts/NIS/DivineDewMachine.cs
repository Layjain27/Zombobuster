using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DivineDewMachine : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 3f;
    public string playerTag = "Player";

    [Header("Dew Settings")]
    public float rechargeTime = 10f;
    private float rechargeTimer = 0f;
    private bool canGiveDew = true;

    [Header("UI")]
    public Canvas worldSpaceCanvas;
    public TextMeshProUGUI statusText;

    private void Start()
    {
        if (statusText != null)
        {
            statusText.text = "Ready!";
            worldSpaceCanvas.enabled = false; // Hide UI initially
        }
    }

    private void Update()
    {
        bool playerInRange = false;

        // Detect player by tag
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag(playerTag))
            {
                playerInRange = true;

                if (canGiveDew)
                {
                    GiveDewToPlayer(hit.gameObject);
                    canGiveDew = false;
                    rechargeTimer = 0f;
                    statusText.text = "Recharging...";
                }
                break;
            }
        }

        // Show/hide UI based on proximity
        if (worldSpaceCanvas != null)
        {
            worldSpaceCanvas.enabled = playerInRange;
        }

        // Handle recharge timer
        if (!canGiveDew)
        {
            rechargeTimer += Time.deltaTime;
            if (rechargeTimer >= rechargeTime)
            {
                canGiveDew = true;
                statusText.text = "Ready!";
            }
        }
    }

    private void GiveDewToPlayer(GameObject player)
    {
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.AddDivineDew(1);
            Debug.Log("Divine Dew deposited into inventory.");
        }
        else
        {
            Debug.LogWarning("PlayerInventory not found on player!");
        }
    }

    // Optional: visualize detection radius in the editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
