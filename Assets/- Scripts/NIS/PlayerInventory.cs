using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private Dictionary<string, int> items = new Dictionary<string, int>();

    public int hellstoneCount = 0;

    public void AddHellstone(int amount)
    {
        hellstoneCount += amount;
        AddItem("Hellstone", amount); // keep dictionary in sync
    }

    public void RemoveHellstone(int amount)
    {
        hellstoneCount = Mathf.Max(0, hellstoneCount - amount);
        RemoveItem("Hellstone", amount); // keep dictionary in sync
    }

    public void AddItem(string itemName, int amount)
    {
        if (items.ContainsKey(itemName))
            items[itemName] += amount;
        else
            items[itemName] = amount;

        // If the item is hellstone, sync the counter
        if (itemName == "Hellstone")
            hellstoneCount = items[itemName];
    }

    public bool HasItem(string itemName, int requiredAmount)
    {
        return items.ContainsKey(itemName) && items[itemName] >= requiredAmount;
    }

    public bool RemoveItem(string itemName, int amount)
    {
        if (HasItem(itemName, amount))
        {
            items[itemName] -= amount;
            if (items[itemName] <= 0)
                items.Remove(itemName);

            if (itemName == "Hellstone")
                hellstoneCount = items.ContainsKey(itemName) ? items[itemName] : 0;

            return true;
        }
        return false;
    }

    public void PrintInventory()
    {
        foreach (var item in items)
        {
            Debug.Log(item.Key + ": " + item.Value);
        }
    }
}
