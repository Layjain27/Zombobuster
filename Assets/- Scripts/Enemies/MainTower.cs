// Filename: MainTower.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// This line ensures the TowerHealth script is automatically added.
[RequireComponent(typeof(TowerHealth))]
public class MainTower : MonoBehaviour
{
    [Header("General Tower Settings")]
    public float spawnRadius = 3f;

    [Header("Shared Tower Configuration")]
    [Tooltip("Assign a ScriptableObject containing shared tower settings.")]
    public TowerSettings towerSettings;

    [Header("Wave Management")]
    public List<WaveDefinition> waves;
    private int currentWaveIndex = 0;

    public static WaveDefinition currentWaveDefinition;

    private int zombiesSpawnedInCurrentWaveGlobal = 0;
    private float waveStartTime;
    private List<MiniTower> activeMiniTowers;

    // --- NEW: Reference to health component ---
    private TowerHealth towerHealth;

    // --- NEW: Public method for MiniTowers to report their destruction ---
    public void ReportMiniTowerDestroyed(MiniTower destroyedTower)
    {
        if (activeMiniTowers.Contains(destroyedTower))
        {
            activeMiniTowers.Remove(destroyedTower);
            Debug.Log($"<color=yellow>{destroyedTower.name} was destroyed. {activeMiniTowers.Count} MiniTowers remain.</color>");
        }
    }

    private void Awake()
    {
        // --- NEW: Get health component and subscribe to its OnDeath event ---
        towerHealth = GetComponent<TowerHealth>();
        towerHealth.OnDeath += HandleDeath;

        if (FindObjectsByType<MainTower>(FindObjectsSortMode.None).Length == 1)
        {
            GameMetrics.ResetMetrics();
            Debug.Log("GameMetrics (totalActiveEnemies) reset by MainTower.");
        }
    }

    // --- NEW: This method is called when the MainTower's health reaches zero ---
    // In MainTower.cs

    private void HandleDeath()
    {
        // 1. Unsubscribe from the event to prevent any errors.
        towerHealth.OnDeath -= HandleDeath;

        // 2. Log the game over event.
        Debug.Log("<color=red>GAME OVER! The Main Tower has been destroyed.</color>");

        // 3. Stop the MainTower from spawning any new waves.
        StopAllCoroutines();

        // 4. NEW: Trigger the destruction of all remaining MiniTowers.
        // We loop backwards because the activeMiniTowers list will be modified as each tower is destroyed.
        for (int i = activeMiniTowers.Count - 1; i >= 0; i--)
        {
            // Check if the tower in the list actually exists before trying to destroy it.
            if (activeMiniTowers[i] != null)
            {
                // Get its health component...
                TowerHealth miniTowerHealth = activeMiniTowers[i].GetComponent<TowerHealth>();
                if (miniTowerHealth != null)
                {
                    // ...and deal fatal damage to trigger its own full death sequence (sounds, effects, etc.).
                    miniTowerHealth.TakeDamage(float.MaxValue);
                }
            }
        }

        // 5. REMOVED: The line that pauses the game. The game will now continue.
        // Time.timeScale = 0f; 

        // You can still activate a "Game Over" UI canvas here if you want the player to see it
        // while the remaining enemies are on screen.
        // For example: if (gameOverCanvas) gameOverCanvas.SetActive(true);
    }

    private void Start()
    {
        Debug.Log($"<color=orange>{gameObject.name} Start called (MainTower).</color>");

        if (towerSettings == null)
        {
            Debug.LogError($"<color=red>Tower Settings ScriptableObject is NOT assigned to {gameObject.name}!</color>", this);
            enabled = false;
            return;
        }

        if (waves == null || waves.Count == 0)
        {
            Debug.LogError($"{gameObject.name}: Main Tower has no Wave Definitions assigned!", this);
            enabled = false;
            return;
        }

        activeMiniTowers = FindObjectsByType<MiniTower>(FindObjectsSortMode.None).ToList();
        if (activeMiniTowers.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}: No MiniTowers found. Only MainTower will spawn.");
        }
        else
        {
            Debug.Log($"{gameObject.name}: Found {activeMiniTowers.Count} MiniTowers.");
        }

        StartCoroutine(ManageWaves());
    }

    // ... The rest of your MainTower script (ManageWaves, SpawnEnemiesForSubWave, etc.) remains unchanged ...
    // ... (Paste your existing methods here) ...
    private IEnumerator ManageWaves()
    {
        Debug.Log($"{gameObject.name}: ManageWaves coroutine started.");
        while (currentWaveIndex < waves.Count)
        {
            currentWaveDefinition = waves[currentWaveIndex];
            waveStartTime = Time.time;
            zombiesSpawnedInCurrentWaveGlobal = 0;
            Debug.Log($"<color=green>{gameObject.name}: Main Tower starting Wave: {currentWaveDefinition.waveName}</color>");

            int subWavesSpawned = 0;
            while (subWavesSpawned < currentWaveDefinition.subWaveCount &&
                   zombiesSpawnedInCurrentWaveGlobal < currentWaveDefinition.maxZombiesForThisWave &&
                   Time.time < waveStartTime + currentWaveDefinition.waveDuration)
            {
                float gapTime = currentWaveDefinition.gapBetweenSubWaves;
                Debug.Log($"{gameObject.name}: Main Tower waiting {gapTime}s for sub-wave {subWavesSpawned + 1}/{currentWaveDefinition.subWaveCount}.");
                yield return new WaitForSeconds(gapTime);
                SpawnEnemiesForSubWave(currentWaveDefinition);
                subWavesSpawned++;
            }

            float timeRemainingInWave = (waveStartTime + currentWaveDefinition.waveDuration) - Time.time;
            if (timeRemainingInWave > 0)
            {
                Debug.Log($"{gameObject.name}: Main Tower waiting for remaining {timeRemainingInWave:F2}s for wave to end.");
                yield return new WaitForSeconds(timeRemainingInWave);
            }

            Debug.Log($"<color=green>Wave {currentWaveDefinition.waveName} Ended. Total spawned: {zombiesSpawnedInCurrentWaveGlobal}</color>");
            currentWaveIndex++;

            float gapBetweenMainWaves = currentWaveDefinition.gapBetweenEachWave;

            if (currentWaveIndex < waves.Count)
            {
                Debug.Log($"{gameObject.name}: Waiting {gapBetweenMainWaves}s before next wave.");
                yield return new WaitForSeconds(gapBetweenMainWaves);
            }
            else
            {
                Debug.Log("<color=blue>All waves processed.</color>");
            }
        }
        Debug.Log("<color=blue>All waves complete!</color>");
    }

    private void SpawnEnemiesForSubWave(WaveDefinition waveDef)
    {
        if (zombiesSpawnedInCurrentWaveGlobal >= waveDef.maxZombiesForThisWave) return;
        if (GameMetrics.totalActiveEnemies >= GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES) return;

        int enemiesToSpawnThisSubWave = waveDef.zombiesPerSubWave;
        enemiesToSpawnThisSubWave = Mathf.Min(enemiesToSpawnThisSubWave, waveDef.maxZombiesForThisWave - zombiesSpawnedInCurrentWaveGlobal);
        enemiesToSpawnThisSubWave = Mathf.Min(enemiesToSpawnThisSubWave, GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES - GameMetrics.totalActiveEnemies);

        if (enemiesToSpawnThisSubWave <= 0) return;

        Debug.Log($"<color=green>{gameObject.name}: Instructing towers to spawn {enemiesToSpawnThisSubWave} enemies.</color>");

        if (activeMiniTowers.Count > 0)
        {
            int towerCount = activeMiniTowers.Count;
            int currentTowerIndex = 0;
            for (int i = 0; i < enemiesToSpawnThisSubWave; i++)
            {
                if (GameMetrics.totalActiveEnemies >= GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES ||
                    zombiesSpawnedInCurrentWaveGlobal >= waveDef.maxZombiesForThisWave)
                {
                    break;
                }
                MiniTower tower = activeMiniTowers[currentTowerIndex];
                if (tower != null)
                {
                    tower.SpawnEnemiesAtThisTower(waveDef.enemyPrefab, waveDef.zombieHP, 1);
                    zombiesSpawnedInCurrentWaveGlobal++;
                }
                currentTowerIndex = (currentTowerIndex + 1) % towerCount;
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: No active MiniTowers. MainTower spawning directly.");
            if (waveDef.enemyPrefab == null) return;
            for (int i = 0; i < enemiesToSpawnThisSubWave; i++)
            {
                if (GameMetrics.totalActiveEnemies >= GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES ||
                    zombiesSpawnedInCurrentWaveGlobal >= waveDef.maxZombiesForThisWave)
                {
                    break;
                }
                Vector3 spawnPosition = GetRandomSpawnPosition(spawnRadius);
                GameObject newEnemy = Instantiate(waveDef.enemyPrefab, spawnPosition, Quaternion.identity);
                GroundedEnemy enemyScript = newEnemy.GetComponent<GroundedEnemy>();
                if (enemyScript != null)
                {
                    enemyScript.SetHealth(waveDef.zombieHP);
                    enemyScript.OnDeath += () => GameMetrics.totalActiveEnemies--;
                    GameMetrics.totalActiveEnemies++;
                }
                else
                {
                    Destroy(newEnemy);
                }
                zombiesSpawnedInCurrentWaveGlobal++;
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