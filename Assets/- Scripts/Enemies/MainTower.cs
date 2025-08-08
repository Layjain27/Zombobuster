// Filename: MainTower.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(TowerHealth))]
public class MainTower : MonoBehaviour
{
    [Header("General Tower Settings")]
    public float spawnRadius = 3f;

    [Header("Gameplay Settings")]
    [Tooltip("Percentage of max health the Main Tower loses when a Mini Tower is destroyed.")]
    [Range(0f, 100f)]
    public float damagePercentOnMiniTowerLoss = 5f;

    // --- NEW: Public variable for the enemy debuff percentage ---
    [Tooltip("The stacking percentage of health reduction enemies receive when a Mini Tower is destroyed.")]
    [Range(0f, 25f)]
    public float enemyDebuffPercentPerTowerLoss = 5f;

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

    private TowerHealth towerHealth;

    public void ReportMiniTowerDestroyed(MiniTower destroyedTower)
    {
        if (activeMiniTowers.Contains(destroyedTower))
        {
            activeMiniTowers.Remove(destroyedTower);
            Debug.Log($"<color=yellow>{destroyedTower.name} was destroyed. {activeMiniTowers.Count} MiniTowers remain.</color>");

            // 1. Damage the Main Tower
            if (towerHealth != null)
            {
                float damageMultiplier = damagePercentOnMiniTowerLoss / 100f;
                float damageToDeal = towerHealth.MaxHealth * damageMultiplier;
                towerHealth.TakeDamage(damageToDeal);
            }

            // 2. --- UPDATED: Use the public variable to increase the enemy debuff ---
            GameMetrics.enemyHealthDebuffPercentage += enemyDebuffPercentPerTowerLoss;
            GameMetrics.enemyHealthDebuffPercentage = Mathf.Clamp(GameMetrics.enemyHealthDebuffPercentage, 0f, 95f);
            Debug.Log($"<color=purple>Enemy debuff increased! All upcoming enemies will spawn with {GameMetrics.enemyHealthDebuffPercentage}% reduced health.</color>");
        }
    }

    // ... (The rest of the MainTower script remains unchanged) ...
    private void Awake()
    {
        towerHealth = GetComponent<TowerHealth>();
        towerHealth.OnDeath += HandleDeath;

        if (FindObjectsByType<MainTower>(FindObjectsSortMode.None).Length == 1)
        {
            GameMetrics.ResetMetrics();
            Debug.Log("GameMetrics (totalActiveEnemies) reset by MainTower.");
        }
    }

    private void HandleDeath()
    {
        towerHealth.OnDeath -= HandleDeath;
        Debug.Log("<color=red>GAME OVER! The Main Tower has been destroyed.</color>");
        StopAllCoroutines();

        for (int i = activeMiniTowers.Count - 1; i >= 0; i--)
        {
            if (activeMiniTowers[i] != null)
            {
                TowerHealth miniTowerHealth = activeMiniTowers[i].GetComponent<TowerHealth>();
                if (miniTowerHealth != null)
                {
                    miniTowerHealth.TakeDamage(float.MaxValue);
                }
            }
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

                yield return StartCoroutine(SpawnEnemiesForSubWave(currentWaveDefinition));

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

    private IEnumerator SpawnEnemiesForSubWave(WaveDefinition waveDef)
    {
        if (zombiesSpawnedInCurrentWaveGlobal >= waveDef.maxZombiesForThisWave) yield break;
        if (GameMetrics.totalActiveEnemies >= GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES) yield break;

        int enemiesToSpawnThisSubWave = waveDef.zombiesPerSubWave;
        enemiesToSpawnThisSubWave = Mathf.Min(enemiesToSpawnThisSubWave, waveDef.maxZombiesForThisWave - zombiesSpawnedInCurrentWaveGlobal);
        enemiesToSpawnThisSubWave = Mathf.Min(enemiesToSpawnThisSubWave, GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES - GameMetrics.totalActiveEnemies);

        if (enemiesToSpawnThisSubWave <= 0) yield break;

        float staggerDuration = waveDef.subWaveStaggerDuration;
        float delayBetweenSpawns = (staggerDuration > 0 && enemiesToSpawnThisSubWave > 1) ? staggerDuration / enemiesToSpawnThisSubWave : 0f;

        Debug.Log($"<color=cyan>{gameObject.name}: Instructing towers to spawn {enemiesToSpawnThisSubWave} enemies over {staggerDuration} seconds.</color>");

        if (activeMiniTowers.Count > 0)
        {
            int currentTowerIndex = 0;
            for (int i = 0; i < enemiesToSpawnThisSubWave; i++)
            {
                if (GameMetrics.totalActiveEnemies >= GameMetrics.GLOBAL_MAX_ACTIVE_ENEMIES ||
                    zombiesSpawnedInCurrentWaveGlobal >= waveDef.maxZombiesForThisWave)
                {
                    break;
                }

                if (activeMiniTowers.Count == 0)
                {
                    Debug.LogWarning("All MiniTowers destroyed mid-spawn. Aborting sub-wave.");
                    break;
                }

                currentTowerIndex %= activeMiniTowers.Count;
                MiniTower tower = activeMiniTowers[currentTowerIndex];
                if (tower != null)
                {
                    tower.SpawnEnemiesAtThisTower(waveDef.enemyPrefab, waveDef.zombieHP, 1);
                    zombiesSpawnedInCurrentWaveGlobal++;
                }
                currentTowerIndex++;

                if (delayBetweenSpawns > 0)
                {
                    yield return new WaitForSeconds(delayBetweenSpawns);
                }
            }
        }
        else // Fallback spawning for the Main Tower
        {
            Debug.LogWarning($"{gameObject.name}: No active MiniTowers. MainTower spawning directly.");
            if (waveDef.enemyPrefab == null) yield break;

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
                    float healthMultiplier = 1.0f - (GameMetrics.enemyHealthDebuffPercentage / 100f);
                    float finalHealth = waveDef.zombieHP * healthMultiplier;
                    enemyScript.SetHealth(finalHealth);

                    enemyScript.OnDeath += () => GameMetrics.totalActiveEnemies--;
                    GameMetrics.totalActiveEnemies++;
                }
                else
                {
                    Debug.LogWarning($"Spawned enemy does not have a GroundedEnemy script!", newEnemy);
                    Destroy(newEnemy);
                }
                zombiesSpawnedInCurrentWaveGlobal++;

                if (delayBetweenSpawns > 0)
                {
                    yield return new WaitForSeconds(delayBetweenSpawns);
                }
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