using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeTier", menuName = "Weapons/Upgrade Tier")]
public class UpgradeTierSO : ScriptableObject
{
    [Header("Tier Values (in %)")]
    public float damagePercent;   // e.g. +10
    public float magPercent;      // e.g. +10
    public float reloadPercent;   // e.g. -10
}
