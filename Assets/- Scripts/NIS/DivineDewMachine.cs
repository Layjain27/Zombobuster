using UnityEngine;
using TMPro;

public class DivineDewMachine : MonoBehaviour
{
    [Header("Collect Settings")]
    public string playerTag = "Player";
    public float holdDuration = 2f; // how long player must hold E
    public float rechargeTime = 10f; // cooldown before next use
    public int dewAmount = 1; // how much Divine Dew is given per collection

    [Header("UI Settings")]
    public Vector3 textOffset = new Vector3(0, 2f, 0);

    private TextMeshPro worldText;
    private Camera mainCam;
    private float holdTimer = 0f;
    private float rechargeTimer = 0f;
    private bool canGiveDew = true;
    private bool playerInRange = false;
    private GameObject currentPlayer;

    private void Start()
    {
        mainCam = Camera.main;

        // Auto-create floating text
        GameObject textObj = new GameObject("DivineDewText");
        textObj.transform.SetParent(transform);
        textObj.transform.localPosition = textOffset;

        worldText = textObj.AddComponent<TextMeshPro>();
        worldText.alignment = TextAlignmentOptions.Center;
        worldText.fontSize = 30f;

        // --- Style ---
        worldText.color = Color.black;
        worldText.fontStyle = FontStyles.Bold;
        worldText.outlineWidth = 0.3f;
        worldText.outlineColor = Color.white;

        worldText.text = "Hold E to collect Divine Dew";
        worldText.gameObject.SetActive(false);
    }


    private void Update()
    {
        // Billboard text
        if (worldText != null && mainCam != null && worldText.gameObject.activeSelf)
        {
            worldText.transform.rotation =
                Quaternion.LookRotation(worldText.transform.position - mainCam.transform.position);
        }

        // Recharge cycle
        if (!canGiveDew)
        {
            rechargeTimer += Time.deltaTime;
            if (rechargeTimer >= rechargeTime)
            {
                canGiveDew = true;
                worldText.text = "Hold E to collect Divine Dew";
            }
        }

        // Player holding E
        if (playerInRange && currentPlayer != null && canGiveDew)
        {
            if (Input.GetKey(KeyCode.E))
            {
                holdTimer += Time.deltaTime;
                worldText.text = $"Collecting... {Mathf.Ceil(holdDuration - holdTimer)}s";

                if (holdTimer >= holdDuration)
                {
                    GiveDewToPlayer(currentPlayer);
                    canGiveDew = false;
                    rechargeTimer = 0f;
                    holdTimer = 0f;
                    worldText.text = "Recharging...";
                }
            }
            else if (holdTimer > 0f)
            {
                holdTimer = 0f;
                worldText.text = "Hold E to collect Divine Dew";
            }
        }
    }

    private void GiveDewToPlayer(GameObject player)
    {
        PlayerInventory inventory = player.GetComponent<PlayerInventory>();
        if (inventory != null)
        {
            inventory.AddDivineDew(dewAmount);
            Debug.Log($"Player collected {dewAmount} Divine Dew!");
        }
        else
        {
            Debug.LogWarning("PlayerInventory not found!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            currentPlayer = other.gameObject;
            if (canGiveDew)
                worldText.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            currentPlayer = null;
            holdTimer = 0f;
            worldText.gameObject.SetActive(false);
        }
    }
}
