using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class GunUpgradeNPC : MonoBehaviour
{
    [Header("Detection")]
    public Transform playerTransform;
    public float detectionRange = 5f;

    [Header("UI")]
    public GameObject upgradePromptUI; // Parent panel
    public TextMeshProUGUI upgradeText; // Text inside panel

    [Header("Upgrade System")]
    public UpgradeManager upgradeManager;

    private bool playerInRange = false;

    private void Update()
    {
        if (!playerTransform || !upgradeManager) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= detectionRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                upgradePromptUI?.SetActive(true);
            }

            UpdateUpgradeText();

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                bool success = upgradeManager.TryUpgradeWeapons();
                if (success)
                {
                    Debug.Log("Upgrade Successful!");
                }
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

    private void UpdateUpgradeText()
    {
        int tier = upgradeManager.currentTier;

        if (tier >= upgradeManager.upgradeTiers.Length)
        {
            upgradeText.text = "MAX TIER";
            return;
        }

        int souls = upgradeManager.soulsCost[tier];
        int hellstone = upgradeManager.hellstoneCost[tier];
        int divineDew = upgradeManager.divineDewCost[tier];

        upgradeText.text = $"Press E to Upgrade\n" +
                           $"Cost: {souls} Souls, {hellstone} Hellstone, {divineDew} Divine Dew";
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
