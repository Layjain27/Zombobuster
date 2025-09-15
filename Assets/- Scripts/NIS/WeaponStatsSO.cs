using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStats", menuName = "Weapons/Weapon Stats")]
public class WeaponStatsSO : ScriptableObject
{
    [Header("Base Stats")]
    public string weaponName;
    public float baseDamage;
    public float baseMagSize;      // This replaces maxAmmo
    public float baseReloadTime;   // Seconds
    public float fireRate;         // Shots per second
    public float range;            // Raycast range
    public float sphereRadius;     // For spherecast
    public float meleeRange;       // Melee attack radius
    public int shotgunPellets;     // For shotgun
    public float spread;           // For shotgun spread
    public AudioClip shootSound;
    public AudioClip reloadSound;

    [HideInInspector] public float currentDamage;
    [HideInInspector] public float currentMagSize;
    [HideInInspector] public float currentReloadTime;

    private void OnEnable()
    {
        ResetStats();
    }

    public void ResetStats()
    {
        currentDamage = baseDamage;
        currentMagSize = baseMagSize;
        currentReloadTime = baseReloadTime;
    }

    /// <summary>
    /// Apply upgrade percentages to the weapon stats.
    /// </summary>
    /// <param name="damagePercent">Percentage increase to damage</param>
    /// <param name="magPercent">Percentage increase to magazine size</param>
    /// <param name="reloadPercent">Percentage decrease to reload time</param>
    public void ApplyUpgrade(float damagePercent, float magPercent, float reloadPercent)
    {
        currentDamage = Mathf.Round(baseDamage * (1 + damagePercent / 100f));
        currentMagSize = Mathf.Round(baseMagSize * (1 + magPercent / 100f));
        currentReloadTime = Mathf.Round(baseReloadTime * (1 - reloadPercent / 100f));
    }

    /// <summary>
    /// Max ammo as integer for the IsometricWeaponSystem
    /// </summary>
    public int MaxAmmo => Mathf.RoundToInt(currentMagSize);
}
