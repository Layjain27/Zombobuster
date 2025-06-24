using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TowerUpgrade : MonoBehaviour
{
    // --- Tower Level Configuration ---
    [Header("Tower Level Configuration")]
    [Tooltip("The current level of the tower. This will be updated automatically during upgrades.")]
    [Range(1, 10)] // Adjust the max range if you plan for more than 10 tower levels
    public int currentTowerLevel = 1;

    [Tooltip("Drag your tower GameObjects here, in order of their level (Element 0 = Level 1, Element 1 = Level 2, etc.).")]
    public List<GameObject> towerModels = new List<GameObject>();

    // --- Player Detection Settings ---
    [Header("Player Detection Settings")]
    [Tooltip("The radius within which the player must be to interact (upgrade/preview) with the tower.")]
    public float interactionRadius = 5f;

    [Tooltip("The tag of the player GameObject. Ensure your player GameObject has this tag.")]
    public string playerTag = "Player";

    [Tooltip("The Unity KeyCode to press for upgrading the tower.")]
    public KeyCode upgradeKey = KeyCode.K;

    // --- Ghost Tower Slideshow Settings ---
    [Header("Ghost Tower Slideshow Settings")]
    [Tooltip("The material to use for the ghost preview of the next tower levels.")]
    public Material ghostMaterial;

    [Tooltip("Duration (in seconds) each upcoming ghost tower level is shown during the slideshow.")]
    [Range(0.5f, 5f)] // Sensible range for slideshow speed
    public float slideshowDurationPerLevel = 1.5f;

    // --- Private Internal References & State ---
    private GameObject player;
    private bool isPlayerInRange = false; // Track if player is currently in range
    private GameObject activeTowerModel; // Reference to the currently active (non-ghost) tower model
    private Coroutine currentSlideshowCoroutine; // Reference to the running slideshow coroutine

    // Stores the ORIGINAL materials for ALL renderers across ALL tower models.
    // Key: Renderer component, Value: Original Material for that renderer.
    private Dictionary<Renderer, Material> allOriginalMaterials = new Dictionary<Renderer, Material>();

    // --- Unity Lifecycle Methods ---

    // Awake is called when the script instance is being loaded.
    // Use it to capture original materials before Start().
    private void Awake()
    {
        CaptureAllOriginalMaterials();
    }

    // Start is called once before the first frame update.
    private void Start()
    {
        // Basic validation for tower models list
        if (towerModels.Count == 0)
        {
            Debug.LogError("Tower Models list is empty! Please assign your tower GameObjects in the inspector.", this);
            return;
        }

        // Find the player GameObject by tag
        player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogError($"No GameObject with tag '{playerTag}' found in the scene! Interaction features might not work as expected.", this);
        }

        // Initialize the tower to its starting level
        SetTowerLevel(currentTowerLevel);
    }

    // Update is called once per frame.
    private void Update()
    {
        if (player == null) return; // Exit if player object isn't found

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
        bool newPlayerInRangeState = (distanceToPlayer <= interactionRadius);

        // Detect state change (player entered/left range)
        if (newPlayerInRangeState != isPlayerInRange)
        {
            isPlayerInRange = newPlayerInRangeState; // Update state

            if (isPlayerInRange)
            {
                StartGhostSlideshow();
            }
            else
            {
                StopGhostSlideshow();
            }
        }

        // Check for upgrade input only if player is in range
        if (isPlayerInRange && Input.GetKeyDown(upgradeKey))
        {
            AttemptUpgradeTower(); // Call method to try upgrading
        }
    }

    // OnDrawGizmosSelected is called when the object is selected in the editor.
    // Useful for visualizing interaction radius.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // Choose a color for the radius visualization
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }

    // --- Core Tower Management Methods ---

    /// <summary>
    /// Captures the original materials of all renderers across all tower models.
    /// This is called once during Awake() to remember each model's default look.
    /// </summary>
    private void CaptureAllOriginalMaterials()
    {
        allOriginalMaterials.Clear(); // Clear any previous captures

        foreach (GameObject model in towerModels)
        {
            if (model != null)
            {
                // Get all renderers, including those on inactive children
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        allOriginalMaterials[renderer] = renderer.sharedMaterial;
                    }
                }
            }
        }

        if (allOriginalMaterials.Count == 0 && towerModels.Count > 0)
        {
            Debug.LogWarning("No renderers or materials found on tower models. Ghosting and material restoration might not work correctly. " +
                             "Ensure your 'Tower Models' GameObjects have MeshRenderer components with materials assigned.", this);
        }
    }

    /// <summary>
    /// Sets the active tower model to a specific level and ensures it uses its original materials.
    /// Deactivates all other tower models.
    /// </summary>
    /// <param name="targetLvl">The target level for the tower (1-based index).</param>
    public void SetTowerLevel(int targetLvl)
    {
        StopGhostSlideshow(); // Always stop slideshow when setting a new permanent level

        // Ensure all tower models revert to their original materials before activating one
        ResetAllTowerModelsToOriginalMaterials();

        // Clamp the target level to be within valid bounds
        targetLvl = Mathf.Clamp(targetLvl, 1, towerModels.Count);
        currentTowerLevel = targetLvl; // Update the public current level

        activeTowerModel = null; // Clear reference to old active model

        for (int i = 0; i < towerModels.Count; i++)
        {
            if (towerModels[i] != null)
            {
                // The list is 0-indexed, so level 1 corresponds to index 0, level 2 to index 1, etc.
                bool isActive = (i == (targetLvl - 1));
                towerModels[i].SetActive(isActive); // Set active/inactive

                if (isActive)
                {
                    activeTowerModel = towerModels[i]; // Store reference to the currently active model
                    // Materials are already handled by ResetAllTowerModelsToOriginalMaterials() above
                }
            }
        }

        Debug.Log($"Tower set to Level {currentTowerLevel}.");
    }

    /// <summary>
    /// Attempts to upgrade the tower to the next level if possible.
    /// Called when the upgrade key is pressed and player is in range.
    /// </summary>
    public void AttemptUpgradeTower()
    {
        if (currentTowerLevel < towerModels.Count)
        {
            SetTowerLevel(currentTowerLevel + 1); // Increment level and update model
            Debug.Log("Tower upgraded!");
        }
        else
        {
            Debug.LogWarning("Tower is already at max level!");
            StopGhostSlideshow(); // Ensure slideshow is off if player attempts to upgrade max level
            // Explicitly ensure the max level tower has its original material
            if (activeTowerModel != null) ApplyOriginalMaterial(activeTowerModel);
        }
    }

    /// <summary>
    /// Downgrades the tower to the previous level if possible. (Optional method)
    /// </summary>
    public void DowngradeTower()
    {
        StopGhostSlideshow(); // Stop slideshow if downgrading
        if (currentTowerLevel > 1)
        {
            SetTowerLevel(currentTowerLevel - 1);
            Debug.Log("Tower downgraded!");
        }
        else
        {
            Debug.LogWarning("Tower is already at minimum level!");
        }
    }

    // --- Ghost Slideshow Methods ---

    /// <summary>
    /// Starts the coroutine for the ghost tower slideshow.
    /// Only runs if not at max level and ghost material is assigned.
    /// </summary>
    private void StartGhostSlideshow()
    {
        // Don't show ghost if tower is already at max level or no ghost material
        if (currentTowerLevel >= towerModels.Count || ghostMaterial == null)
        {
            if (currentTowerLevel >= towerModels.Count)
                Debug.Log("Tower is already at max level, no ghost slideshow.");
            else if (ghostMaterial == null)
                Debug.LogWarning("Ghost Material is not assigned! Cannot show ghost slideshow.");
            return;
        }

        // Temporarily deactivate the current actual tower model
        if (activeTowerModel != null)
        {
            activeTowerModel.SetActive(false);
        }

        // Stop any existing slideshow before starting a new one
        if (currentSlideshowCoroutine != null)
        {
            StopCoroutine(currentSlideshowCoroutine);
        }
        currentSlideshowCoroutine = StartCoroutine(GhostSlideshowCoroutine());
        Debug.Log("Ghost tower slideshow started.");
    }

    /// <summary>
    /// Stops the ghost tower slideshow coroutine and restores the actual tower's appearance.
    /// </summary>
    private void StopGhostSlideshow()
    {
        if (currentSlideshowCoroutine != null)
        {
            StopCoroutine(currentSlideshowCoroutine);
            currentSlideshowCoroutine = null;
            Debug.Log("Ghost tower slideshow stopped.");
        }

        // Deactivate all tower models before reactivating the correct one
        foreach (GameObject model in towerModels)
        {
            if (model != null) model.SetActive(false);
        }

        // Ensure all models have their original materials restored
        ResetAllTowerModelsToOriginalMaterials();

        // Reactivate the true current tower model
        if (activeTowerModel != null)
        {
            activeTowerModel.SetActive(true);
        }
    }

    /// <summary>
    /// Coroutine to cycle through upcoming tower levels as ghosts, with timed delays.
    /// </summary>
    private IEnumerator GhostSlideshowCoroutine()
    {
        while (true) // Loop indefinitely while coroutine is running
        {
            // Cycle through levels from current+1 to max level
            // 'i' here represents the 0-based index of the tower model in the list
            for (int i = currentTowerLevel; i < towerModels.Count; i++)
            {
                // Ensure all models are off and their materials are original before showing the next ghost
                ResetAllTowerModelsToOriginalMaterials(); // Reset materials
                foreach (GameObject model in towerModels) // Deactivate all
                {
                    if (model != null) model.SetActive(false);
                }

                GameObject previewModel = towerModels[i]; // Get the next model for preview
                if (previewModel != null)
                {
                    previewModel.SetActive(true); // Activate it
                    ApplyGhostMaterial(previewModel); // Apply ghost material
                    Debug.Log($"Showing ghost preview of Level {i + 1}.");
                }
                yield return new WaitForSeconds(slideshowDurationPerLevel); // Wait for duration
            }

            // After showing all upcoming levels, yield once more before looping
            yield return null;
        }
    }

    // --- Material Management Helper Methods ---

    /// <summary>
    /// Applies the ghost material to all renderers of a given GameObject.
    /// Uses 'renderer.material' to create a unique instance for temporary ghosting.
    /// </summary>
    /// <param name="targetObject">The GameObject whose materials should be ghosted.</param>
    private void ApplyGhostMaterial(GameObject targetObject)
    {
        if (ghostMaterial == null) return;

        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null) // Ensure renderer is not null
            {
                renderer.material = ghostMaterial; // Creates a new material instance
            }
        }
    }

    /// <summary>
    /// Applies the ORIGINAL material(s) to all renderers of a given GameObject.
    /// Uses 'renderer.sharedMaterial' to revert to the original material asset.
    /// </summary>
    /// <param name="targetObject">The GameObject whose materials should be reset.</param>
    private void ApplyOriginalMaterial(GameObject targetObject)
    {
        Renderer[] renderers = targetObject.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null && allOriginalMaterials.TryGetValue(renderer, out Material originalMat))
            {
                renderer.sharedMaterial = originalMat; // Reverts to the original shared material asset
            }
        }
    }

    /// <summary>
    /// Iterates through all tower models and resets their materials to their individually captured originals.
    /// This is crucial for cleanup after ghosting or setting a new level.
    /// </summary>
    private void ResetAllTowerModelsToOriginalMaterials()
    {
        foreach (GameObject model in towerModels)
        {
            if (model != null)
            {
                ApplyOriginalMaterial(model);
            }
        }
    }
}