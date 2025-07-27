// Filename: MiniTower.cs
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(TowerHealth))]
public class MiniTower : MonoBehaviour
{
    [Header("General Mini Tower Settings")]
    public float spawnRadius = 3f;

    private TowerHealth towerHealth;
    private MainTower mainTower;

    private void Awake()
    {
        towerHealth = GetComponent<TowerHealth>();
        towerHealth.OnDeath += HandleDeath;
    }

    void Start()
    {
        // --- UPDATED: Using modern FindFirstObjectByType instead of obsolete FindObjectOfType ---
        mainTower = FindFirstObjectByType<MainTower>();
        if (mainTower == null)
        {
            Debug.LogError("MiniTower could not find a MainTower in the scene!");
        }

        Debug.Log($"<color=orange>{gameObject.name} Start called (MiniTower).</color>");
    }

    private void HandleDeath()
    {
        towerHealth.OnDeath -= HandleDeath;
        if (mainTower != null)
        {
            mainTower.ReportMiniTowerDestroyed(this);
        }
    }

    // The rest of your MiniTower script remains the same...
    public void SpawnEnemiesAtThisTower(GameObject enemyPrefab, float enemyHP, int count)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"{gameObject.name}: Cannot spawn, Enemy Prefab is null.", this);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            if (GameMetrics.totalActiveEnemies >= GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES)
            {
                Debug.LogWarning($"{gameObject.name}: Global max active enemies reached. Stopping spawn.");
                break;
            }

            Vector3 spawnPosition = GetRandomSpawnPosition(spawnRadius);
            GameObject newEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            GroundedEnemy enemyScript = newEnemy.GetComponent<GroundedEnemy>();
            if (enemyScript != null)
            {
                enemyScript.SetHealth(enemyHP);
                enemyScript.OnDeath += () => GameMetrics.totalActiveEnemies--;
                GameMetrics.totalActiveEnemies++;
            }
            else
            {
                Debug.LogWarning($"Spawned enemy does not have a GroundedEnemy script!", newEnemy);
                Destroy(newEnemy);
            }
        }
    }

    private Vector3 GetRandomSpawnPosition(float radius)
    {
        Vector2 randomOffset = Random.insideUnitCircle * radius;
        return new Vector3(transform.position.x + randomOffset.x, transform.position.y, transform.position.z + randomOffset.y);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}