// Filename: CampMissionController.cs
using UnityEngine;

// Require the AllySpawner script to be on the same GameObject to ensure it exists.
[RequireComponent(typeof(AllySpawner))]
public class CampMissionController : MonoBehaviour
{
    [Header("Mission Targets")]
    [Tooltip("Drag all the 'Containment Structure' buildings here. They MUST have the TowerHealth script.")]
    [SerializeField] private TowerHealth[] containmentStructures;

    // A private reference to the spawner script on this same object.
    private AllySpawner allySpawner;

    // A flag to ensure we only trigger the zombie horde once.
    private bool isHordeTriggered = false;

    void Awake()
    {
        // Get the AllySpawner component from this same GameObject.
        allySpawner = GetComponent<AllySpawner>();
    }

    void Start()
    {
        // Check if the structures have been assigned in the Inspector.
        if (containmentStructures == null || containmentStructures.Length == 0)
        {
            Debug.LogError("No containment structures assigned to the CampMissionController!");
            return;
        }

        // Subscribe our function to the OnDeath event of EACH containment structure.
        foreach (TowerHealth structure in containmentStructures)
        {
            // The '+=' operator adds our method to the list of methods to be called by the event.
            structure.OnDeath += () => OnStructureDestroyed(structure.transform.position);
        }
    }

    /// <summary>
    /// This method is called whenever one of the subscribed TowerHealth components is destroyed.
    /// </summary>
    /// <param name="destroyedPosition">The position where the structure was destroyed.</param>
    private void OnStructureDestroyed(Vector3 destroyedPosition)
    {
        Debug.Log("<color=green>Containment structure destroyed! Spawning allies.</color>");

        // Tell the spawner to create the allies at the location of the destroyed building.
        allySpawner.SpawnAllies(destroyedPosition);

        // Trigger the zombie horde, but only on the FIRST time a structure is destroyed.
        if (!isHordeTriggered)
        {
            isHordeTriggered = true;
            TriggerZombieHorde();
        }
    }

    private void TriggerZombieHorde()
    {
        Debug.LogWarning("ZOMBIE HORDE TRIGGERED! The noise has attracted them!");
        // --- YOUR ZOMBIE LOGIC GOES HERE ---
        // For example:
        // ZombieSpawner.Instance.StartInvasion();
        // GameManager.SetGameState(GameState.HordeActive);
    }

    // It's good practice to unsubscribe from events when the object is destroyed to prevent errors.
    void OnDestroy()
    {
        foreach (TowerHealth structure in containmentStructures)
        {
            if (structure != null)
            {
                // The '-=' operator unsubscribes the method.
                structure.OnDeath -= () => OnStructureDestroyed(structure.transform.position);
            }
        }
    }
}