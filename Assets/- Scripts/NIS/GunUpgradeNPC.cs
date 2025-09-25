using UnityEngine;
using UnityEngine.InputSystem;

public class GunUpgradeNPC : MonoBehaviour
{
    [Header("UI Prompt")]
    public GameObject upgradePromptUI;

    [Header("Weapon System Reference")]
    public IsometricWeaponSystem weaponSystem;

    [Header("Upgrade Costs")]
    public int[] soulsCost = { 50, 100, 200 };
    public int[] hellstoneCost = { 5, 10, 20 };
    public int[] divineDewCost = { 0, 0, 1 };

    [Header("Upgrade Tiers")]
    public UpgradeTierSO[] upgradeTiers;
    public int currentTier = 0;
    private const int maxTier = 3;

    [Header("Detection Settings")]
    public float interactDistance = 4f;
    public Camera playerCamera; // Assign your main camera here in Inspector

    private bool canUpgrade = false;

    private void Start()
    {
        if (upgradePromptUI != null)
            upgradePromptUI.SetActive(false);

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        DetectNPCWithRaycast();
        HandleInteraction();
    }

    private void DetectNPCWithRaycast()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                if (!canUpgrade)
                {
                    canUpgrade = true;
                    upgradePromptUI?.SetActive(true);
                }
                return;
            }
        }

        // If not hitting NPC
        if (canUpgrade)
        {
            canUpgrade = false;
            upgradePromptUI?.SetActive(false);
        }
    }

    private void HandleInteraction()
    {
        if (canUpgrade && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("E pressed - trying to upgrade...");
            TryUpgradeWeapon();
        }
    }

    private void TryUpgradeWeapon()
    {
        if (currentTier >= maxTier)
        {
            Debug.Log("Already max weapon tier!");
            return;
        }

        int nextTier = currentTier;

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

        weaponSystem.currentAmmo = weaponSystem.ActiveWeapon.MaxAmmo;

        Debug.Log("Weapon upgraded to Tier " + currentTier);
    }

    private void ApplyUpgradeTier()
    {
        WeaponStatsSO[] allWeapons =
        {
            weaponSystem.meleeStats,
            weaponSystem.pistolStats,
            weaponSystem.shotgunStats,
            weaponSystem.rifleStats
        };

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
