using UnityEngine;
using System.Collections.Generic;

public class TowerUpgrade : MonoBehaviour
{
    [Tooltip("The current level of the tower.")]
    [Range(1, 3)]
    public int level = 1;

    [Tooltip("Drag your tower GameObjects here, in order of their level (Level 1, Level 2, Level 3).")]
    public List<GameObject> towerModels = new List<GameObject>();

    [Header("Upgrade Radius Settings")]
    [Tooltip("The radius within which the player must be to upgrade the tower.")]
    public float upgradeRadius = 5f; // Adjust this value in the Inspector

    [Tooltip("The tag of the player GameObject.")]
    public string playerTag = "Player"; // Ensure your player has this tag

    private GameObject player; // Reference to the player GameObject

    private void Start()
    {
        if (towerModels.Count == 0)
        {
            Debug.LogError("Tower Models list is empty! Please assign your tower GameObjects in the inspector.", this);
            return;
        }

        // Find the player GameObject in the scene
        player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogError($"No GameObject with tag '{playerTag}' found in the scene! Tower upgrades might not work as expected.", this);
        }

        SetLevel(level);
    }

    private void Update()
    {
        // Check if the 'K' key was pressed down in this frame
        if (Input.GetKeyDown(KeyCode.K))
        {
            TryUpgradeTower(); // Call a new method to handle the check
        }
    }

    /// <summary>
    /// Attempts to upgrade the tower if the player is within the upgrade radius.
    /// </summary>
    public void TryUpgradeTower()
    {
        if (player == null)
        {
            Debug.LogWarning("Cannot upgrade: Player not found.");
            return;
        }

        // Calculate the distance between the tower and the player
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= upgradeRadius)
        {
            // Player is within range, proceed with upgrade
            UpgradeTower();
        }
        else
        {
            Debug.LogWarning($"You are too far from the tower to upgrade it. Distance: {distanceToPlayer:F2}m. Required: {upgradeRadius:F2}m.");
        }
    }

    /// <summary>
    /// Sets the active tower model based on the provided level.
    /// Deactivates all other tower models.
    /// </summary>
    /// <param name="lvl">The target level for the tower (1-based index).</param>
    public void SetLevel(int lvl)
    {
        lvl = Mathf.Clamp(lvl, 1, towerModels.Count);
        level = lvl;

        for (int i = 0; i < towerModels.Count; i++)
        {
            if (towerModels[i] != null)
            {
                towerModels[i].SetActive(i == (lvl - 1));
            }
        }

        Debug.Log($"Tower upgraded to Level {level}.");
    }

    /// <summary>
    /// Upgrades the tower to the next level, if possible.
    /// </summary>
    public void UpgradeTower()
    {
        if (level < towerModels.Count)
        {
            SetLevel(level + 1);
        }
        else
        {
            Debug.LogWarning("Tower is already at max level!");
        }
    }

    /// <summary>
    /// Downgrades the tower to the previous level, if possible.
    /// </summary>
    public void DowngradeTower()
    {
        if (level > 1)
        {
            SetLevel(level - 1);
        }
        else
        {
            Debug.LogWarning("Tower is already at minimum level!");
        }
    }

    // Optional: Draw the upgrade radius in the editor for visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // Choose a color for the radius
        Gizmos.DrawWireSphere(transform.position, upgradeRadius);
    }
}