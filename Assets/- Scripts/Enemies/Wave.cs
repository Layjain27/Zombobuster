// Filename: WaveDefinition.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Wave Definition", menuName = "Tower Defense/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [Header("Wave Identification")]
    [Tooltip("A descriptive name for this wave (e.g., 'Wave 1 - Grunts').")]
    public string waveName = "New Wave";

    [Header("Wave Timing and Structure")]
    [Tooltip("The total duration of the wave in seconds. Spawning will stop after this time.")]
    public float waveDuration = 60f;
    [Tooltip("The time in seconds to wait after this wave ends before the next one begins.")]
    public float gapBetweenEachWave = 10f;
    [Tooltip("The number of sub-waves (bursts of enemies) within this main wave.")]
    public int subWaveCount = 3;
    [Tooltip("The time in seconds to wait between each sub-wave.")]
    public float gapBetweenSubWaves = 5f;

    [Header("Enemy Spawning")]
    [Tooltip("The enemy prefab to spawn for this wave.")]
    public GameObject enemyPrefab;
    [Tooltip("The maximum number of enemies that can be spawned in total for this entire wave.")]
    public int maxZombiesForThisWave = 50;
    [Tooltip("The number of enemies to spawn in each individual sub-wave.")]
    public int zombiesPerSubWave = 10;
    [Tooltip("The health points for each enemy spawned in this wave.")]
    public float zombieHP = 100f;

    [Header("Sub-Wave Spawning (Staggering)")]
    [Tooltip("The total time in seconds over which to spawn all enemies in a single sub-wave. A value of 0 will spawn them all at once.")]
    public float subWaveStaggerDuration = 3f;
}
