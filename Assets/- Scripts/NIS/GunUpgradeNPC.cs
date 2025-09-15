using UnityEngine;
using UnityEngine.InputSystem;

public class GunUpgradeNPC : MonoBehaviour
{
    [Header("Detection Settings")]
    public Transform playerTransform;
    public float detectionRange = 3f;

    [Header("UI Prompt")]
    public GameObject upgradePromptUI;

    [Header("Weapon System Reference")]
    public IsometricWeaponSystem weaponSystem; // Reference to IsometricWeaponSystem

    [Header("Upgrade Costs")]
    public int[] soulsCost = { 50, 100, 200 };
    public int[] hellstoneCost = { 5, 10, 20 };
    public int[] divineDewCost = { 0, 0, 1 };

    [Header("Upgrade Tiers")]
    public UpgradeTierSO[] upgradeTiers; // Assign 3 tiers in Inspector
    public int currentTier = 0;
    private const int maxTier = 3;

    private bool playerInRange = false;

    private void Start()
    {
        if (upgradePromptUI != null)
            upgradePromptUI.SetActive(false);
    }

    private void Update()
    {
        if (playerTransform == null || weaponSystem == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Check if player is within range
        if (distance <= detectionRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                ShowUpgradePrompt(true);
            }

            // Press E to upgrade
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryUpgradeWeapon();
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                ShowUpgradePrompt(false);
            }
        }
    }

    private void ShowUpgradePrompt(bool show)
    {
        if (upgradePromptUI != null)
            upgradePromptUI.SetActive(show);
    }

    private void TryUpgradeWeapon()
    {
        if (currentTier >= maxTier)
        {
            Debug.Log("Already max weapon tier!");
            return;
        }

        int nextTier = currentTier;

        // Check resources in player's inventory
        if (weaponSystem.inventory.souls < soulsCost[nextTier] ||
            weaponSystem.inventory.hellstone < hellstoneCost[nextTier] ||
            weaponSystem.inventory.divineDew < divineDewCost[nextTier])
        {
            Debug.Log("Not enough resources to upgrade!");
            return;
        }

        // Spend resources
        weaponSystem.inventory.souls -= soulsCost[nextTier];
        weaponSystem.inventory.hellstone -= hellstoneCost[nextTier];
        weaponSystem.inventory.divineDew -= divineDewCost[nextTier];

        currentTier++;
        ApplyUpgradeTier();

        // Update current ammo for the active weapon
        weaponSystem.currentAmmo = weaponSystem.ActiveWeapon.MaxAmmo;

        Debug.Log("Weapons upgraded to Tier " + currentTier);
    }

    private void ApplyUpgradeTier()
    {
        WeaponStatsSO[] allWeapons = { weaponSystem.meleeStats, weaponSystem.pistolStats, weaponSystem.shotgunStats, weaponSystem.rifleStats };

        float damagePercent = 0;
        float magPercent = 0;
        float reloadPercent = 0;

        for (int i = 0; i < currentTier; i++)
        {
            damagePercent += upgradeTiers[i].damagePercent;
            magPercent += upgradeTiers[i].magPercent;
            reloadPercent += upgradeTiers[i].reloadPercent;
        }

        foreach (var weapon in allWeapons)
        {
            weapon.ResetStats();
            weapon.ApplyUpgrade(damagePercent, magPercent, reloadPercent);
        }
    }
}
