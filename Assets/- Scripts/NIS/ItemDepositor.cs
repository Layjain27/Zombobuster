using UnityEngine;
using UnityEngine.InputSystem;

public class ItemDepositor : MonoBehaviour
{
    private bool isInDepositZone = false;
    private PlayerInventory inventory;

    [Header("UI Reference")]
    public GameObject depositPromptUI; // Drag your DepositPrompt UI here

    private void Start()
    {
        inventory = GetComponent<PlayerInventory>();
        if (depositPromptUI != null)
            depositPromptUI.SetActive(false); // Hide at start
    }

    private void Update()
    {
        if (isInDepositZone && Keyboard.current.eKey.wasPressedThisFrame)
        {
            DepositItems();
        }
    }

    private void DepositItems()
    {
        int souls = inventory.GetSoulCount();
        int hellstone = inventory.GetHellstoneCount();
        int dew = inventory.GetDivineDewCount();

        if (souls > 0 || hellstone > 0 || dew > 0)
        {
            Debug.Log($"Deposited → Souls: {souls}, Hellstone: {hellstone}, DivineDew: {dew}");
            inventory.ClearInventory();

            // TODO: Add logic for upgrades or machine fueling here
        }
        else
        {
            Debug.Log("No items to deposit!");
        }

        if (depositPromptUI != null)
            depositPromptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DepositZone"))
        {
            isInDepositZone = true;
            if (depositPromptUI != null)
                depositPromptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("DepositZone"))
        {
            isInDepositZone = false;
            if (depositPromptUI != null)
                depositPromptUI.SetActive(false);
        }
    }
}
