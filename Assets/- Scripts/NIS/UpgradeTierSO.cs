using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeTier", menuName = "Weapons/UpgradeTier")]
public class UpgradeTierSO : ScriptableObject
{
    [Header("Upgrade Percentages")]
    public float damagePercent = 10f;
    public float magPercent = 10f;
    public float reloadPercent = 10f;
}
