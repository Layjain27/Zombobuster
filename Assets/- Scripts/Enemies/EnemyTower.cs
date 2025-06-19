using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyTower : MonoBehaviour
{
    [Header("General Tower Settings")]
    public bool isMiniTower = false;
    public float spawnRadius = 3f;

    [Header("Shared Tower Configuration")]
    [Tooltip("Assign a ScriptableObject containing shared tower settings.")]
    public TowerSettings towerSettings;

    [Header("Wave Management (Only for Main Tower)")]
    public List<WaveDefinition> waves;
    private int currentWaveIndex = 0;
    private WaveDefinition currentWaveDefinition; // The currently active wave for all towers to reference

    // --- Global Counters and Limits ---
    public const int GLOBAL_MAX_ACTIVE_ENEMIES = 30; // Total enemies all towers combined can have active at once
    public static int totalActiveEnemies = 0; // Tracks total active enemies in the scene

    // --- Local Tower State ---
    private int zombiesSpawnedInCurrentWaveByThisTower = 0; // Tracks enemies spawned by THIS tower for the current wave
    private float waveStartTime;
    private Coroutine miniTowerSpawnLoopCoroutine = null; // Still needed for mini-towers

    // This static method is called by GroundedEnemy when it dies
    public static void DecrementActiveEnemies()
    {
        if (totalActiveEnemies > 0)
        {
            totalActiveEnemies--;
        }
    }

    private void Start()
    {
        Debug.Log($"<color=orange>{gameObject.name} Start called. IsMiniTower: {isMiniTower}</color>");

        if (towerSettings == null)
        {
            Debug.LogError($"<color=red>Tower Settings ScriptableObject is NOT assigned to {gameObject.name}!</color>", this);
            enabled = false;
            return;
        }

        if (!isMiniTower) // Main Tower Logic
        {
            // Reset global count only if this is the only main tower starting up.
            // Consider using a GameManager for more robust scene-wide resets.
            if (UnityEngine.Object.FindObjectsByType<EnemyTower>(FindObjectsSortMode.None).Length <= 1)
            {
                totalActiveEnemies = 0;
                Debug.Log("Global totalActiveEnemies reset to 0 (First main tower).");
            }

            if (waves == null || waves.Count == 0)
            {
                Debug.LogError($"{gameObject.name}: Main Tower has no Wave Definitions assigned! Please assign wave assets.", this);
                enabled = false;
                return;
            }
            StartCoroutine(ManageWaves()); // Main tower drives the wave progression
            // Removed: StartCoroutine(SpawnMiniTowersRoutine());
        }
        else // Mini Tower Logic
        {
            Debug.Log($"<color=green>{gameObject.name}: Starting Mini Tower logic. Initiating MiniTowerEnemySpawnLoop.</color>");
            miniTowerSpawnLoopCoroutine = StartCoroutine(MiniTowerEnemySpawnLoop());
            if (miniTowerSpawnLoopCoroutine == null)
            {
                Debug.LogError($"<color=red>{gameObject.name}: Failed to start MiniTowerEnemySpawnLoop coroutine!</color>");
            }
        }
    }

    private IEnumerator MiniTowerEnemySpawnLoop()
    {
        // First, wait for the global wave definition to be established by the main tower
        while (GetCurrentActiveWave() == null)
        {
            // Debug.Log($"{gameObject.name}: Mini Tower waiting for current wave definition..."); // Can be very chatty
            yield return null; // Wait a frame
        }

        currentWaveDefinition = GetCurrentActiveWave(); // Get the initial wave definition
        Debug.Log($"{gameObject.name}: Mini Tower found initial wave: {currentWaveDefinition.waveName}");

        while (true) // This loop runs indefinitely while the mini-tower is active
        {
            // Update the wave definition if the main tower has progressed to the next wave
            WaveDefinition newWaveDef = GetCurrentActiveWave();
            if (newWaveDef != null && newWaveDef != currentWaveDefinition)
            {
                currentWaveDefinition = newWaveDef;
                zombiesSpawnedInCurrentWaveByThisTower = 0; // Reset local counter for new wave
                Debug.Log($"{gameObject.name}: Mini Tower detected wave change to: {currentWaveDefinition.waveName}");
            }

            if (currentWaveDefinition != null)
            {
                Debug.Log($"{gameObject.name}: Mini Tower attempting to spawn sub-wave for {currentWaveDefinition.waveName}.");
                SpawnEnemySubWave(currentWaveDefinition);
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: currentWaveDefinition is null inside MiniTowerEnemySpawnLoop. This should not happen if initial wait succeeded.");
            }

            // Wait for the next spawn interval (using the gapBetweenSubWaves from the current wave)
            float waitTime = (currentWaveDefinition != null) ? currentWaveDefinition.gapBetweenSubWaves : towerSettings.enemySpawnInterval;
            yield return new WaitForSeconds(waitTime);
            // Debug.Log($"{gameObject.name}: Mini Tower waiting for {waitTime} seconds before next sub-wave attempt."); // Can be very chatty
        }
    }

    private IEnumerator ManageWaves()
    {
        Debug.Log($"{gameObject.name}: ManageWaves coroutine started.");
        while (currentWaveIndex < waves.Count)
        {
            currentWaveDefinition = waves[currentWaveIndex]; // This sets the global currentWaveDefinition
            waveStartTime = Time.time;
            zombiesSpawnedInCurrentWaveByThisTower = 0; // Reset for the new wave for *this* main tower
            Debug.Log($"{gameObject.name}: Main Tower starting Wave: {currentWaveDefinition.waveName}");

            int subWavesSpawned = 0;
            while (subWavesSpawned < currentWaveDefinition.subWaveCount &&
                   zombiesSpawnedInCurrentWaveByThisTower < currentWaveDefinition.maxZombiesForThisWave &&
                   Time.time < waveStartTime + currentWaveDefinition.waveDuration)
            {
                float gapTime = currentWaveDefinition.gapBetweenSubWaves;
                Debug.Log($"{gameObject.name}: Main Tower waiting {gapTime}s for sub-wave {subWavesSpawned + 1}/{currentWaveDefinition.subWaveCount}.");
                yield return new WaitForSeconds(gapTime);

                SpawnEnemySubWave(currentWaveDefinition);
                subWavesSpawned++;
            }

            // Ensure the full wave duration passes, even if all enemies are spawned early
            float timeRemainingInWave = (waveStartTime + currentWaveDefinition.waveDuration) - Time.time;
            if (timeRemainingInWave > 0)
            {
                Debug.Log($"{gameObject.name}: Main Tower waiting for remaining {timeRemainingInWave}s for wave to end.");
                yield return new WaitForSeconds(timeRemainingInWave);
            }

            Debug.Log($"Wave {currentWaveDefinition.waveName} Ended. Total spawned by THIS main tower: {zombiesSpawnedInCurrentWaveByThisTower}");
            currentWaveIndex++; // Move to the next wave
            yield return new WaitForSeconds(towerSettings.enemySpawnInterval * 2); // Short break between waves
            Debug.Log($"{gameObject.name}: Main Tower moving to next wave or finished.");
        }
        Debug.Log("All waves complete!");
    }

    private WaveDefinition GetCurrentActiveWave()
    {
        // Finds the first active EnemyTower that is NOT a mini-tower
        // and returns its currentWaveDefinition.
        EnemyTower mainTower = UnityEngine.Object.FindFirstObjectByType<EnemyTower>();
        if (mainTower != null && !mainTower.isMiniTower && mainTower.currentWaveDefinition != null)
        {
            return mainTower.currentWaveDefinition;
        }
        return null; // No main tower found or no wave definition set yet
    }

    private void SpawnEnemySubWave(WaveDefinition waveDef)
    {
        Debug.Log($"{gameObject.name}: Entering SpawnEnemySubWave for wave: {waveDef.waveName}. " +
                  $"Current local spawned: {zombiesSpawnedInCurrentWaveByThisTower}/{waveDef.maxZombiesForThisWave}. " +
                  $"Global active: {totalActiveEnemies}/{GLOBAL_MAX_ACTIVE_ENEMIES}.");

        if (zombiesSpawnedInCurrentWaveByThisTower >= waveDef.maxZombiesForThisWave)
        {
            Debug.Log($"{gameObject.name}: Max zombies for current wave ({waveDef.maxZombiesForThisWave}) reached for THIS tower. Skipping spawn.");
            return;
        }
        if (totalActiveEnemies >= GLOBAL_MAX_ACTIVE_ENEMIES)
        {
            Debug.Log($"{gameObject.name}: Global max active enemies ({GLOBAL_MAX_ACTIVE_ENEMIES}) reached. Skipping spawn.");
            return;
        }

        int enemiesToSpawn = waveDef.zombiesPerSubWave;

        // Ensure we don't exceed this wave's total limit (for all towers combined)
        // or the global active limit
        enemiesToSpawn = Mathf.Min(enemiesToSpawn, waveDef.maxZombiesForThisWave - zombiesSpawnedInCurrentWaveByThisTower);
        enemiesToSpawn = Mathf.Min(enemiesToSpawn, GLOBAL_MAX_ACTIVE_ENEMIES - totalActiveEnemies);

        if (enemiesToSpawn <= 0)
        {
            Debug.Log($"{gameObject.name}: Calculated enemiesToSpawn is 0. Skipping. (Remaining for wave: {waveDef.maxZombiesForThisWave - zombiesSpawnedInCurrentWaveByThisTower}, Global slots: {GLOBAL_MAX_ACTIVE_ENEMIES - totalActiveEnemies})");
            return;
        }

        Debug.Log($"{gameObject.name}: Attempting to spawn {enemiesToSpawn} enemies for wave {waveDef.waveName}.");

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            if (totalActiveEnemies >= GLOBAL_MAX_ACTIVE_ENEMIES ||
                zombiesSpawnedInCurrentWaveByThisTower >= waveDef.maxZombiesForThisWave)
            {
                Debug.Log($"{gameObject.name}: Reached limit mid-loop. Breaking spawn loop.");
                break;
            }

            if (waveDef.enemyPrefab == null)
            {
                Debug.LogWarning($"{gameObject.name}: Enemy Prefab is not assigned in the WaveDefinition '{waveDef.name}'. Cannot spawn enemies.", this);
                break;
            }

            Vector3 spawnPosition = GetRandomSpawnPosition(spawnRadius);
            GameObject newEnemy = Instantiate(waveDef.enemyPrefab, spawnPosition, Quaternion.identity);

            // Set HP on the spawned enemy
            GroundedEnemy enemyScript = newEnemy.GetComponent<GroundedEnemy>();
            if (enemyScript != null)
            {
                enemyScript.SetHealth(waveDef.zombieHP);
                Debug.Log($"Spawned {newEnemy.name} with HP {waveDef.zombieHP} from {gameObject.name}. Global active: {totalActiveEnemies + 1}. Local spawned this wave: {zombiesSpawnedInCurrentWaveByThisTower + 1}");
            }
            else
            {
                Debug.LogWarning($"Spawned enemy '{newEnemy.name}' for wave '{waveDef.name}' does not have a GroundedEnemy script!", newEnemy);
            }

            totalActiveEnemies++;
            zombiesSpawnedInCurrentWaveByThisTower++;
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

        // Gizmo for mini-tower spawn radius is removed since it's manual now
        // if (!isMiniTower && miniTowerPrefab != null)
        // {
        //     Gizmos.color = Color.blue;
        //     Gizmos.DrawWireSphere(transform.position, miniTowerSpawnRadius);
        // }
    }
}