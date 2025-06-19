using UnityEngine;
using System.Collections;
// System.Collections.Generic is not directly used here so can be removed if not needed elsewhere

public class MiniTower : MonoBehaviour
{
    [Header("General Mini Tower Settings")]
    public float spawnRadius = 3f;

    // We no longer need towerSettings directly in MiniTower for wave logic,
    // as MainTower will manage it.
    // public TowerSettings towerSettings; 

    // Mini Tower no longer needs to track spawned enemies or run its own spawn loop
    // private int zombiesSpawnedInCurrentWaveByThisTower = 0;
    // private Coroutine miniTowerSpawnLoopCoroutine = null;

    private void Start()
    {
        Debug.Log($"<color=orange>{gameObject.name} Start called (MiniTower).</color>");
        // MiniTower no longer starts its own spawn loop.
        // It will wait for MainTower to instruct it to spawn.
    }

    /// <summary>
    /// Called by MainTower to instruct this MiniTower to spawn a specified number of enemies.
    /// </summary>
    /// <param name="enemyPrefab">The enemy prefab to instantiate.</param>
    /// <param name="enemyHP">The health to set for the spawned enemies.</param>
    /// <param name="count">The number of enemies to spawn at this tower.</param>
    public void SpawnEnemiesAtThisTower(GameObject enemyPrefab, float enemyHP, int count)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot spawn enemies. Enemy Prefab is null.", this);
            return;
        }

        Debug.Log($"{gameObject.name}: Spawning {count} enemies.");

        for (int i = 0; i < count; i++)
        {
            // Add a global active enemies check here too, as MainTower might send too many if it's not perfect.
            if (GameMetrics.totalActiveEnemies >= GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES)
            {
                Debug.LogWarning($"{gameObject.name}: Global max active enemies ({GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES}) reached. Stopping spawn for this MiniTower.");
                break;
            }

            Vector3 spawnPosition = GetRandomSpawnPosition(spawnRadius);
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            GroundedEnemy enemyScript = newEnemy.GetComponent<GroundedEnemy>();
            if (enemyScript != null)
            {
                enemyScript.SetHealth(enemyHP);
                // Subscribe to the enemy's OnDeath event to decrement the global counter
                enemyScript.OnDeath += () => GameMetrics.totalActiveEnemies--;
                GameMetrics.totalActiveEnemies++; // Increment only when successfully spawned and tracked
                Debug.Log($"Spawned {newEnemy.name} with HP {enemyHP} from {gameObject.name}. Global active: {GameMetrics.totalActiveEnemies}");
            }
            else
            {
                Debug.LogWarning($"Spawned enemy '{newEnemy.name}' does not have a GroundedEnemy script!", newEnemy);
                Destroy(newEnemy); // Destroy to prevent untracked enemies
            }
        }
    }

    private Vector3 GetRandomSpawnPosition(float radius)
    {
        Vector2 randomOffset = Random.insideUnitCircle * radius;
        // Make sure the spawn position is generally on the ground, assuming tower base is at Y=0 or similar
        // You might need to add a raycast down from this point to find actual ground level if your terrain is uneven.
        return new Vector3(transform.position.x + randomOffset.x, transform.position.y, transform.position.z + randomOffset.y);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}