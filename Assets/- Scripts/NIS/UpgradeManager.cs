using UnityEngine;
using System;

public class UpgradeManager : MonoBehaviour
{
    [Header("Player Weapons (Base Stats)")]
    public WeaponStatsSO pistolBase;
    public WeaponStatsSO rifleBase;
    public WeaponStatsSO shotgunBase;
    public WeaponStatsSO meleeBase;

    [HideInInspector] public WeaponStatsSO pistolStats;
    [HideInInspector] public WeaponStatsSO rifleStats;
    [HideInInspector] public WeaponStatsSO shotgunStats;
    [HideInInspector] public WeaponStatsSO meleeStats;

    [Header("Upgrade Tiers")]
    public UpgradeTierSO[] upgradeTiers;

    [Header("Upgrade Costs")]
    public int[] soulsCost;
    public int[] hellstoneCost;
    public int[] divineDewCost;

    [Header("Player Inventory")]
    public PlayerInventory inventory;

    public int currentTier = 0;
    public static event Action<int> OnWeaponUpgraded;

    private void Awake()
    {
        // Instantiate runtime weapon stats
        pistolStats = Instantiate(pistolBase);
        rifleStats = Instantiate(rifleBase);
        shotgunStats = Instantiate(shotgunBase);
        meleeStats = Instantiate(meleeBase);
    }

    public bool TryUpgradeWeapons()
    {
        if (currentTier >= upgradeTiers.Length)
        {
            Debug.Log("Already max tier!");
            return false;
        }

        int nextTier = currentTier;

        // Check resources
        if (inventory.souls < soulsCost[nextTier] ||
            inventory.hellstone < hellstoneCost[nextTier] ||
            inventory.divineDew < divineDewCost[nextTier])
        {
            Debug.Log("Not enough resources!");
            return false;
        }

        // Deduct resources
        inventory.souls -= soulsCost[nextTier];
        inventory.hellstone -= hellstoneCost[nextTier];
        inventory.divineDew -= divineDewCost[nextTier];

        currentTier++;

        ApplyUpgradeTier();

        Debug.Log($"Weapons upgraded to Tier {currentTier}");
        OnWeaponUpgraded?.Invoke(currentTier);
        return true;
    }

    private void ApplyUpgradeTier()
    {
        float totalDamage = 0f;
        float totalMag = 0f;
        float totalReload = 0f;

        for (int i = 0; i < currentTier; i++)
        {
            totalDamage += upgradeTiers[i].damagePercent;
            totalMag += upgradeTiers[i].magPercent;
            totalReload += upgradeTiers[i].reloadPercent;
        }

        WeaponStatsSO[] allWeapons = { pistolStats, rifleStats, shotgunStats, meleeStats };

        foreach (var weapon in allWeapons)
        {
            weapon.ResetStats();
            weapon.ApplyUpgrade(totalDamage, totalMag, totalReload);
        }
    }
}
