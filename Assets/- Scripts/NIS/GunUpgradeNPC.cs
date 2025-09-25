using UnityEngine;
using UnityEngine.InputSystem;

public class GunUpgradeNPC : MonoBehaviour
{
    [Header("Detection Settings")]
    public Transform playerTransform;
    public float detectionRange = 5f; // Increased for easier testing

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

    private int maxTier => upgradeTiers.Length;
    private bool playerInRange = false;

    private void Start()
    {
        if (upgradePromptUI != null)
            upgradePromptUI.SetActive(false);

        // Instantiate weapon stats to avoid modifying original ScriptableObjects
        weaponSystem.meleeStats = Instantiate(weaponSystem.meleeStats);
        weaponSystem.pistolStats = Instantiate(weaponSystem.pistolStats);
        weaponSystem.shotgunStats = Instantiate(weaponSystem.shotgunStats);
        weaponSystem.rifleStats = Instantiate(weaponSystem.rifleStats);
    }

    private void Update()
    {
        if (playerTransform == null || weaponSystem == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= detectionRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                ShowUpgradePrompt(true);
            }

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

        // Debug current resources
        Debug.Log($"Attempting upgrade to Tier {nextTier + 1}. Inventory: Souls {weaponSystem.inventory.souls}, Hellstone {weaponSystem.inventory.hellstone}, DivineDew {weaponSystem.inventory.divineDew}");

        // Check if player has enough resources
        if (weaponSystem.inventory.souls < soulsCost[nextTier] ||
            weaponSystem.inventory.hellstone < hellstoneCost[nextTier] ||
            weaponSystem.inventory.divineDew < divineDewCost[nextTier])
        {
            Debug.Log("Not enough resources to upgrade!");
            return;
        }

        // Deduct resources
        weaponSystem.inventory.souls -= soulsCost[nextTier];
        weaponSystem.inventory.hellstone -= hellstoneCost[nextTier];
        weaponSystem.inventory.divineDew -= divineDewCost[nextTier];

        currentTier++;
        ApplyUpgradeTier();

        // Reset current ammo for the active weapon
        weaponSystem.currentAmmo = weaponSystem.ActiveWeapon.MaxAmmo;

        Debug.Log($"Weapon upgraded to Tier {currentTier}");
    }

    private void ApplyUpgradeTier()
    {
        WeaponStatsSO[] allWeapons = { weaponSystem.meleeStats, weaponSystem.pistolStats, weaponSystem.shotgunStats, weaponSystem.rifleStats };

        float totalDamagePercent = 0;
        float totalMagPercent = 0;
        float totalReloadPercent = 0;

        for (int i = 0; i < currentTier; i++)
        {
            totalDamagePercent += upgradeTiers[i].damagePercent;
            totalMagPercent += upgradeTiers[i].magPercent;
            totalReloadPercent += upgradeTiers[i].reloadPercent;
        }

        foreach (var weapon in allWeapons)
        {
            weapon.ResetStats();
            weapon.ApplyUpgrade(totalDamagePercent, totalMagPercent, totalReloadPercent);
        }

        Debug.Log($"Applied upgrades: Damage {totalDamagePercent}%, Mag {totalMagPercent}%, Reload {totalReloadPercent}%");
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize detection range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
