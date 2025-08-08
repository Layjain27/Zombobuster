// Filename: TowerHealth.cs
using UnityEngine;

// An enum to define the affiliation of the game object.
public enum Faction { Player, Enemy }

// By implementing IDamageable, this tower can be attacked by any enemy that targets the interface.
public class TowerHealth : MonoBehaviour, IDamageable
{
    [Header("Tower Identity")]
    [Tooltip("Set whether this tower belongs to the Player or an Enemy.")]
    [SerializeField] public Faction faction;

    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    private float currentHealth;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;

    // An event that other scripts (like GameManager or ScoreManager) can subscribe to.
    public event System.Action OnDeath;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Reduces the tower's health. This method is required by the IDamageable interface.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to inflict.</param>
    public void TakeDamage(float damageAmount)
    {
        // Don't do anything if already dead.
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth); // Ensure health doesn't go below 0

        Debug.Log($"{gameObject.name} ({faction}) took {damageAmount} damage, current health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Invoke the OnDeath event so any interested scripts are notified.
        OnDeath?.Invoke();

        // Add any death effects here (e.g., explosions, sound)
        Debug.Log($"<color=red>{gameObject.name} ({faction}) has been destroyed!</color>");

        // Destroy the tower GameObject
        Destroy(gameObject);
    }
}
