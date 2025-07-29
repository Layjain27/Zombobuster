using UnityEngine;
using System.Collections.Generic; // Make sure this is included if you use it for enemy prefabs later

[CreateAssetMenu(fileName = "NewTowerSettings", menuName = "Tower/Tower Settings")]
public class TowerSettings : ScriptableObject
{
    [Header("Enemy Spawn Settings")]
    public int maxEnemiesToSpawn = 20; // Total enemies the tower can spawn before stopping
    public int maxEnemiesPerWave = 5; // Max number of enemies per wave
    public float enemySpawnInterval = 5f; // Time between enemy spawns

    // Add other shared settings here if needed, e.g., default enemy prefabs
    // public List<GameObject> defaultEnemyPrefabs;
}