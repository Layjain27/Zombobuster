// Filename: IDamageable.cs
using UnityEngine;

public interface IDamageable
{
    /// <summary>
    /// A contract for any object that can take damage.
    /// </summary>
    /// <param name="damageAmount">The amount of damage to inflict.</param>
    void TakeDamage(float damageAmount);
}