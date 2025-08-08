// Filename: WeaponStats.cs

using UnityEngine; // Required for AudioClip

[System.Serializable]
public class WeaponStats
{
    public float fireRate = 1f;
    public int maxAmmo = 10;
    public float reloadTime = 2f;
    public float range = 50f;
    public float meleeRange = 2f;
    public float spread = 0.1f;
    public int shotgunPellets = 6;
    public int damage = 10;
    public float sphereRadius = 0.2f;
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public float knockbackForce = 10f;
}