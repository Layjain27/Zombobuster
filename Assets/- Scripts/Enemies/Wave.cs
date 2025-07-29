// WaveDefinition.cs
using UnityEngine;
using System.Collections.Generic; // Make sure this is included if you have lists/dictionaries

[CreateAssetMenu(fileName = "NewWaveDefinition", menuName = "Wave System/Wave Definition")]
public class WaveDefinition : ScriptableObject
{
    [Header("Wave Info")]
    public string waveName = "New Wave";
    public GameObject enemyPrefab; // The specific enemy prefab for this wave
    public float zombieHP = 100f; // HP for enemies in this wave

    [Header("Wave Spawning Parameters")]
    public int subWaveCount = 5; // Number of "bursts" of enemies within this wave
    public int zombiesPerSubWave = 6; // Number of zombies to spawn in each sub-wave burst
    public int maxZombiesForThisWave = 30; // Total zombies to spawn for this ENTIRE wave across all sub-waves and towers

    [Header("Wave Timing")]
    public float gapBetweenSubWaves = 2f; // Time between each sub-wave burst
    public float waveDuration = 300f; // Total time this wave lasts (e.g., 300 seconds = 5 minutes)

    // NEW: This is the missing field!
    public float gapBetweenEachWave = 10f; // Time between the end of THIS wave and the start of the NEXT wave

    // You might add more properties like:
    // public float enemySpeedMultiplier = 1f;
    // public List<GameObject> miniBossPrefabs;
    // public float bonusMoneyOnCompletion;
}