using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public List<ItemData> items = new List<ItemData>();

    public void AddItem(ItemData item)
    {
        items.Add(item);
        Debug.Log("Item stored: " + item.itemName);
    }

    public void ClearInventory()
    {
        items.Clear();
        Debug.Log("Inventory cleared");
    }

    public int ItemCount => items.Count;
}
