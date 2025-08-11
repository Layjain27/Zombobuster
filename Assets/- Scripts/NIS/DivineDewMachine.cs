using UnityEngine;
using TMPro;

public class DivineDewMachine : MonoBehaviour
{
    [Header("Machine Settings")]
    public float rechargeTime = 10f; // seconds before next dew
    public int dewAmount = 1; // how much to give per collect
    public float detectionRadius = 3f; // range for player detection
    public string playerTag = "Player";

    [Header("UI")]
    public TextMeshProUGUI statusText; // assign TMP text in inspector
    public Vector3 uiOffset = new Vector3(0, 2f, 0); // offset above machine

    private float rechargeTimer;
    private bool isReady = false;
    private Transform player;

    void Start()
    {
        rechargeTimer = rechargeTime;
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false); // hide at start
        }
    }

    void Update()
    {
        // Recharge logic
        if (!isReady)
        {
            rechargeTimer -= Time.deltaTime;
            if (rechargeTimer <= 0f)
            {
                isReady = true;
                rechargeTimer = 0f;
            }
        }

        // Player detection
        if (player != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = isReady ? "Ready to Collect" : "Recharging...";

            if (isReady)
            {
                CollectDew();
            }
        }
        else
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }
        }

        // Keep UI above machine
        if (statusText != null)
        {
            statusText.transform.position = Camera.main.WorldToScreenPoint(transform.position + uiOffset);
        }
    }

    private void CollectDew()
    {
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.AddItem("DivineDew", dewAmount);
            Debug.Log($"Player collected {dewAmount} Divine Dew!");
        }

        isReady = false;
        rechargeTimer = rechargeTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            player = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            player = null;
        }
    }

    // Optional: visualize detection zone in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
