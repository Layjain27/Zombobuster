// IDamageable.cs
using UnityEngine; // Required for Vector3 in TakeDamage method

public interface IDamageable
{
    // This method will be implemented by anything that can take damage.
    // 'amount' is how much damage is taken.
    // 'hitPoint' is the world position where the damage occurred (useful for effects).
    void TakeDamage(float amount, Vector3 hitPoint);
}