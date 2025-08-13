using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private int hellstone;
    private int soul;
    private int divineDew;

    public void AddSoul(int amount) { soul += amount; }
    public void AddHellstone(int amount) { hellstone += amount; }

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

    public void AddDivineDew(int amount)
    {
        divineDew += amount;
        Debug.Log("Divine Dew now: " + divineDew);
    }

}
