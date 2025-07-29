using System.Collections.Generic;
using UnityEngine;

public class HellstoneSpawner : MonoBehaviour
{
    [Header("Hellstone Settings")]
    public GameObject hellstonePrefab;
    public float spawnInterval = 60f;
    public float spawnRadius = 5f;
    public int maxHellstones = 5;

    [Header("Spawn Locations")]
    public List<GameObject> watchTowers;

    private List<GameObject> spawnedStones = new List<GameObject>();
    private float timer;

    private void Start()
    {
        // Spawn initial hellstones
        for (int i = 0; i < maxHellstones; i++)
        {
            TrySpawnHellstone();
        }
    }

    private void Update()
    {
        // Clean up any collected/destroyed stones
        bool anyRemoved = spawnedStones.RemoveAll(stone => stone == null) > 0;

        if (anyRemoved)
        {
            // Only restart timer if any stone was collected
            timer = spawnInterval;
        }

        if (spawnedStones.Count < maxHellstones)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                TrySpawnHellstone();
                timer = spawnInterval;
            }
        }
    }

    private void TrySpawnHellstone()
    {
        if (hellstonePrefab == null || watchTowers.Count == 0)
        {
            Debug.LogWarning("HellstoneSpawner: Missing prefab or watchtowers.");
            return;
        }

        GameObject tower = watchTowers[Random.Range(0, watchTowers.Count)];

        // Find a valid ground position by raycasting downward from above
        Vector3 randomOffset = Random.insideUnitSphere * spawnRadius;
        randomOffset.y = 5f; // start raycast above ground
        Vector3 rayStart = tower.transform.position + randomOffset;

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f))
        {
            Vector3 spawnPos = hit.point;
            GameObject newStone = Instantiate(hellstonePrefab, spawnPos, Quaternion.identity);
            spawnedStones.Add(newStone);
            Debug.Log("Hellstone spawned at: " + spawnPos);
        }
        else
        {
            Debug.LogWarning("No valid ground found near: " + tower.name);
        }
    }

    // Gizmo for spawn radius
    private void OnDrawGizmosSelected()
    {
        if (watchTowers != null)
        {
            Gizmos.color = Color.yellow;
            foreach (GameObject tower in watchTowers)
            {
                if (tower != null)
                {
                    Gizmos.DrawWireSphere(tower.transform.position, spawnRadius);
                }
            }
        }
    }
}
