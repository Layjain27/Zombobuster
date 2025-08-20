using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private Dictionary<string, int> items = new Dictionary<string, int>();

    // -----------------------------
    // GENERIC ITEM SYSTEM
    // -----------------------------
    public void AddItem(string itemName, int amount)
    {
        if (!items.ContainsKey(itemName))
            items[itemName] = 0;

        items[itemName] += amount;
        Debug.Log($"Added {amount} {itemName}. Total: {items[itemName]}");
    }

    public bool RemoveItem(string itemName, int amount)
    {
        if (items.ContainsKey(itemName) && items[itemName] >= amount)
        {
            items[itemName] -= amount;
            Debug.Log($"Removed {amount} {itemName}. Remaining: {items[itemName]}");
            return true;
        }
        return false;
    }

    public int GetItemCount(string itemName)
    {
        return items.ContainsKey(itemName) ? items[itemName] : 0;
    }

    public void ClearInventory()
    {
        items.Clear();
        Debug.Log("Inventory cleared.");
    }

    public void PrintInventory()
    {
        foreach (var kvp in items)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value}");
        }
    }

    // -----------------------------
    // HELLSTONE SHORTCUTS
    // -----------------------------
    private const string HellstoneKey = "Hellstone";

    public void AddHellstone(int amount) => AddItem(HellstoneKey, amount);
    public bool SpendHellstone(int amount) => RemoveItem(HellstoneKey, amount);
    public bool HasHellstone(int amount) => GetItemCount(HellstoneKey) >= amount;
    public int GetHellstoneCount() => GetItemCount(HellstoneKey);

    // -----------------------------
    // DIVINE DEW SHORTCUTS
    // -----------------------------
    private const string DivineDewKey = "DivineDew";

    public void AddDivineDew(int amount) => AddItem(DivineDewKey, amount);
    public bool SpendDivineDew(int amount) => RemoveItem(DivineDewKey, amount);
    public bool HasDivineDew(int amount) => GetItemCount(DivineDewKey) >= amount;
    public int GetDivineDewCount() => GetItemCount(DivineDewKey);
}
