// Filename: AllySpawner.cs
using UnityEngine;

public class AllySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [Tooltip("The NPC Ally prefab to spawn.")]
    public GameObject allyPrefab;

    [Tooltip("How many allies to spawn when called.")]
    public int alliesToSpawn = 3;

    [Tooltip("The radius around the spawn point to instantiate allies, to prevent them from overlapping.")]
    public float spawnRadius = 2f;

    /// <summary>
    /// Spawns a configured number of allies at a specific world position.
    /// </summary>
    /// <param name="spawnCenterPosition">The position of the destroyed building.</param>
    public void SpawnAllies(Vector3 spawnCenterPosition)
    {
        if (allyPrefab == null)
        {
            Debug.LogError("Ally Prefab is not assigned in the AllySpawner!");
            return;
        }

        Debug.Log($"Spawning {alliesToSpawn} allies at {spawnCenterPosition}");

        for (int i = 0; i < alliesToSpawn; i++)
        {
            // Calculate a random position within a circle to spread them out
            Vector2 randomPoint = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = spawnCenterPosition + new Vector3(randomPoint.x, 0, randomPoint.y);

            // Instantiate the ally
            Instantiate(allyPrefab, spawnPosition, Quaternion.identity);
        }
    }
}