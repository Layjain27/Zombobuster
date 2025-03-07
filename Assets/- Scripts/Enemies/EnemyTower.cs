using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyTower : MonoBehaviour
{
    [Header("Enemy Settings")]
    public List<GameObject> enemyPrefabs; // Different enemy types to spawn
    public float spawnRadius = 3f; // Radius around the tower where enemies spawn

    [Header("Spawn Settings")]
    public int maxSpawns = 20; // Total enemies the tower can spawn before stopping
    public int maxSpawnAmount = 5; // Max number of enemies per wave
    public float spawnInterval = 5f; // Time between spawns

    private int totalSpawned = 0; // Tracks total spawned enemies

    private void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        while (totalSpawned < maxSpawns) // Stop when maxSpawns is reached
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnEnemyWave();
        }
    }

    private void SpawnEnemyWave()
    {
        int spawnAmount = Random.Range(1, maxSpawnAmount + 1); // Random between 1 and maxSpawnAmount

        for (int i = 0; i < spawnAmount; i++)
        {
            if (totalSpawned >= maxSpawns) return; // Stop if max spawns reached

            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            Vector3 spawnPosition = GetRandomSpawnPosition();

            Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            totalSpawned++; // Track total spawned enemies
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        return new Vector3(transform.position.x + randomOffset.x, transform.position.y, transform.position.z + randomOffset.y);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius); // Draw spawn radius in the editor
    }

    /*
    // PREVIOUS FUNCTIONALITY (COMMENTED OUT)
    // If you want to limit how many enemies exist at once instead of total spawns:
    private List<GameObject> activeEnemies = new List<GameObject>();
    
    private void SpawnEnemyWave()
    {
        int spawnAmount = Random.Range(1, maxSpawnAmount + 1);

        for (int i = 0; i < spawnAmount; i++)
        {
            if (activeEnemies.Count >= maxEnemies) return; // Limit active enemies

            GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
            Vector3 spawnPosition = GetRandomSpawnPosition();

            GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            activeEnemies.Add(newEnemy);

            newEnemy.GetComponent<GroundedEnemy>().OnDeath += () => activeEnemies.Remove(newEnemy);
        }
    }
    */

}
