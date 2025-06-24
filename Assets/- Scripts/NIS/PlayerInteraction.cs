using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 2.5f;
    public Transform interactionPoint; // Empty object on chest/head of player
    public LayerMask interactableLayer;
    public GameObject interactionUI; // UI prompt GameObject

    private GameObject currentInteractable;

    void Update()
    {
        DetectInteractable();

        if (currentInteractable != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            PickupItem();
        }
    }

    private void DetectInteractable()
    {
        Ray ray = new Ray(interactionPoint.position, interactionPoint.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactableLayer))
        {
            if (hit.collider.CompareTag("Pickup"))
            {
                currentInteractable = hit.collider.gameObject;
                interactionUI.SetActive(true);
                return;
            }
        }

        // If nothing is detected or not a pickup
        currentInteractable = null;
        interactionUI.SetActive(false);
    }

    private void PickupItem()
    {
        ItemData data = currentInteractable.GetComponent<ItemReference>()?.itemData;

        if (data != null)
        {
            GetComponent<PlayerInventory>().AddItem(data);
        }

        Destroy(currentInteractable);
        currentInteractable = null;
        interactionUI.SetActive(false);
    }

}
