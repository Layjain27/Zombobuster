using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Required for .ToList() and other LINQ operations

public class MainTower : MonoBehaviour
{
    [Header("General Tower Settings")]
    public float spawnRadius = 3f; // Used for its own potential spawns if it spawns directly

    [Header("Shared Tower Configuration")]
    [Tooltip("Assign a ScriptableObject containing shared tower settings.")]
    public TowerSettings towerSettings;

    [Header("Wave Management")]
    public List<WaveDefinition> waves;
    private int currentWaveIndex = 0;

    // This will be accessed by MiniTowers to know the current wave
    public static WaveDefinition currentWaveDefinition;

    // MainTower tracks globally spawned for the current wave
    private int zombiesSpawnedInCurrentWaveGlobal = 0;
    private float waveStartTime;

    private List<MiniTower> activeMiniTowers; // Cache active mini towers

    private void Awake()
    {
        // Ensure static variable is reset when the first MainTower comes online
        if (FindObjectsByType<MainTower>(FindObjectsSortMode.None).Length == 1)
        {
            GameMetrics.ResetMetrics();
            Debug.Log("GameMetrics (totalActiveEnemies) reset by MainTower.");
        }
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
            Debug.LogError($"{gameObject.name}: Main Tower has no Wave Definitions assigned! Please assign wave assets.", this);
            enabled = false;
            return;
        }

        // FIX CS0618 Warning: Using FindObjectsByType instead of FindObjectsOfType
        activeMiniTowers = FindObjectsByType<MiniTower>(FindObjectsSortMode.None).ToList();
        if (activeMiniTowers.Count == 0)
        {
            Debug.LogWarning($"{gameObject.name}: No MiniTowers found in the scene. Only MainTower will attempt to spawn (if configured).");
        }
        else
        {
            Debug.Log($"{gameObject.name}: Found {activeMiniTowers.Count} MiniTowers.");
        }

        StartCoroutine(ManageWaves()); // Main tower drives the wave progression
    }

    private IEnumerator ManageWaves()
    {
        Debug.Log($"{gameObject.name}: ManageWaves coroutine started.");
        while (currentWaveIndex < waves.Count)
        {
            currentWaveDefinition = waves[currentWaveIndex]; // This sets the global currentWaveDefinition
            waveStartTime = Time.time;
            zombiesSpawnedInCurrentWaveGlobal = 0; // Reset global counter for the new wave
            Debug.Log($"<color=green>{gameObject.name}: Main Tower starting Wave: {currentWaveDefinition.waveName}</color>");

            int subWavesSpawned = 0;
            while (subWavesSpawned < currentWaveDefinition.subWaveCount &&
                   zombiesSpawnedInCurrentWaveGlobal < currentWaveDefinition.maxZombiesForThisWave && // Check global wave limit
                   Time.time < waveStartTime + currentWaveDefinition.waveDuration)
            {
                float gapTime = currentWaveDefinition.gapBetweenSubWaves;
                Debug.Log($"{gameObject.name}: Main Tower waiting {gapTime}s for sub-wave {subWavesSpawned + 1}/{currentWaveDefinition.subWaveCount}.");
                yield return new WaitForSeconds(gapTime);

                // --- Spawn Logic for this Sub-Wave ---
                SpawnEnemiesForSubWave(currentWaveDefinition);
                subWavesSpawned++;
            }

            // Ensure the full wave duration passes, even if all enemies for this wave are spawned early
            float timeRemainingInWave = (waveStartTime + currentWaveDefinition.waveDuration) - Time.time;
            if (timeRemainingInWave > 0)
            {
                Debug.Log($"{gameObject.name}: Main Tower waiting for remaining {timeRemainingInWave:F2}s for wave to end.");
                yield return new WaitForSeconds(timeRemainingInWave);
            }

            Debug.Log($"<color=green>Wave {currentWaveDefinition.waveName} Ended. Total zombies spawned for this wave: {zombiesSpawnedInCurrentWaveGlobal}</color>");
            currentWaveIndex++; // Move to the next wave

            // Wait for the gap between main waves
            // FIX CS1061 Error: Accessing currentWaveDefinition.gapBetweenEachWave
            float gapBetweenMainWaves = currentWaveDefinition.gapBetweenEachWave;

            if (currentWaveIndex < waves.Count) // Only wait if there are more waves
            {
                Debug.Log($"{gameObject.name}: Waiting {gapBetweenMainWaves}s before starting next wave.");
                yield return new WaitForSeconds(gapBetweenMainWaves);
            }
            else
            {
                Debug.Log("<color=blue>All waves processed. Game might end or enter a final state.</color>");
            }
        }
        Debug.Log("<color=blue>All waves complete! Game End or Victory State.</color>");
    }

    private void SpawnEnemiesForSubWave(WaveDefinition waveDef)
    {
        Debug.Log($"{gameObject.name}: Entering SpawnEnemiesForSubWave for wave: {waveDef.waveName}. " +
                  $"Global spawned this wave: {zombiesSpawnedInCurrentWaveGlobal}/{waveDef.maxZombiesForThisWave}. " +
                  $"Global active: {GameMetrics.totalActiveEnemies}/{GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES}.");

        // First, check global limits *before* calculating how many to spawn
        if (zombiesSpawnedInCurrentWaveGlobal >= waveDef.maxZombiesForThisWave)
        {
            Debug.Log($"{gameObject.name}: Max zombies for current wave ({waveDef.maxZombiesForThisWave}) already reached globally. Skipping sub-wave spawn.");
            return;
        }
        if (GameMetrics.totalActiveEnemies >= GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES)
        {
            Debug.Log($"{gameObject.name}: Global max active enemies ({GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES}) reached. Skipping sub-wave spawn.");
            return;
        }

        int enemiesToSpawnThisSubWave = waveDef.zombiesPerSubWave;

        // Cap enemiesToSpawnThisSubWave by global wave limit and global active limit
        enemiesToSpawnThisSubWave = Mathf.Min(enemiesToSpawnThisSubWave, waveDef.maxZombiesForThisWave - zombiesSpawnedInCurrentWaveGlobal);
        enemiesToSpawnThisSubWave = Mathf.Min(enemiesToSpawnThisSubWave, GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES - GameMetrics.totalActiveEnemies);

        if (enemiesToSpawnThisSubWave <= 0)
        {
            Debug.Log($"{gameObject.name}: Calculated enemiesToSpawnThisSubWave is 0. Skipping. (Remaining for wave: {waveDef.maxZombiesForThisWave - zombiesSpawnedInCurrentWaveGlobal}, Global slots: {GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES - GameMetrics.totalActiveEnemies})");
            return;
        }

        Debug.Log($"<color=green>{gameObject.name}: Instructing towers to spawn {enemiesToSpawnThisSubWave} enemies for wave {waveDef.waveName}.</color>");

        // Distribute spawns among active mini towers or spawn from MainTower if no mini towers
        if (activeMiniTowers.Count > 0)
        {
            int towerCount = activeMiniTowers.Count;
            int currentTowerIndex = 0; // To cycle through towers

            // FIX CS0103 Error: Simplified distribution loop to prevent scope issues
            for (int i = 0; i < enemiesToSpawnThisSubWave; i++) // Loop for each enemy we need to spawn
            {
                // Re-check global limits *before* picking a tower for THIS enemy,
                // as enemies might have died or new ones spawned by other sources.
                if (GameMetrics.totalActiveEnemies >= GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES ||
                    zombiesSpawnedInCurrentWaveGlobal >= waveDef.maxZombiesForThisWave)
                {
                    Debug.Log($"{gameObject.name}: Global limits reached during enemy distribution. Stopping spawn.");
                    break; // Stop spawning if limits are hit
                }

                MiniTower tower = activeMiniTowers[currentTowerIndex];
                if (tower != null)
                {
                    tower.SpawnEnemiesAtThisTower(waveDef.enemyPrefab, waveDef.zombieHP, 1); // Spawn 1 enemy
                    // zombiesSpawnedInCurrentWaveGlobal is incremented inside MiniTower's SpawnEnemiesAtThisTower now too
                    // but we still need to increment it here to reflect global wave progress *from MainTower's perspective*
                    zombiesSpawnedInCurrentWaveGlobal++;
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name}: MiniTower at index {currentTowerIndex} is null or destroyed. Skipping this spawn attempt.");
                }

                currentTowerIndex = (currentTowerIndex + 1) % towerCount; // Move to the next tower
            }
        }
        else // Fallback: MainTower spawns if no MiniTowers are active
        {
            Debug.LogWarning($"{gameObject.name}: No active MiniTowers found. MainTower will spawn directly for this sub-wave.");
            if (waveDef.enemyPrefab == null)
            {
                Debug.LogWarning($"{gameObject.name}: Enemy Prefab is not assigned in the WaveDefinition '{waveDef.name}'. Cannot spawn enemies.", this);
                return;
            }

            for (int i = 0; i < enemiesToSpawnThisSubWave; i++)
            {
                if (GameMetrics.totalActiveEnemies >= GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES ||
                    zombiesSpawnedInCurrentWaveGlobal >= waveDef.maxZombiesForThisWave)
                {
                    Debug.Log($"{gameObject.name}: Main Tower reached limit mid-loop. Breaking spawn loop.");
                    break;
                }

                Vector3 spawnPosition = GetRandomSpawnPosition(spawnRadius);
                GameObject newEnemy = Instantiate(waveDef.enemyPrefab, spawnPosition, Quaternion.identity);

                GroundedEnemy enemyScript = newEnemy.GetComponent<GroundedEnemy>();
                if (enemyScript != null)
                {
                    enemyScript.SetHealth(waveDef.zombieHP);
                    enemyScript.OnDeath += () => GameMetrics.totalActiveEnemies--; // Decrement on death
                    GameMetrics.totalActiveEnemies++;
                    Debug.Log($"Spawned {newEnemy.name} with HP {waveDef.zombieHP} from {gameObject.name} directly. Global active: {GameMetrics.totalActiveEnemies}. Global wave spawned: {zombiesSpawnedInCurrentWaveGlobal + 1}");
                }
                else
                {
                    Debug.LogWarning($"Spawned enemy '{newEnemy.name}' for wave '{waveDef.name}' does not have a GroundedEnemy script!", newEnemy);
                    Destroy(newEnemy);
                }
                zombiesSpawnedInCurrentWaveGlobal++; // Increment global wave counter
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