using UnityEngine;
using UnityEngine.InputSystem; // For Keyboard input (new Input System)

public class GunUpgradeNPC : MonoBehaviour
{
    [Header("Detection Settings")]
    public Transform playerTransform;
    public float detectionRange = 3f;

    [Header("UI Prompt")]
    public GameObject upgradePromptUI; // Assign in Inspector

    [Header("Player Gun Reference")]
    public PlayerGun playerGun; // Reference to player's gun script

    private bool playerInRange = false;

    private void Start()
    {
        if (upgradePromptUI != null)
            upgradePromptUI.SetActive(false); // Hide on start
    }

    private void Update()
    {
        if (playerTransform == null || playerGun == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= detectionRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                upgradePromptUI?.SetActive(true);
            }

            // Detect E key press using new Input System
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                Debug.Log("E key pressed - upgrading gun.");
                playerGun.Upgrade();
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                upgradePromptUI?.SetActive(false);
            }
        }
    }
}
