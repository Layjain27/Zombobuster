using System.Collections.Generic;
using UnityEngine;

public class AllyManager : MonoBehaviour
{
    public static AllyManager Instance;
    public List<AllyController> allies = new List<AllyController>();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterAlly(AllyController ally)
    {
        if (!allies.Contains(ally))
            allies.Add(ally);
    }

    public void UnregisterAlly(AllyController ally)
    {
        if (allies.Contains(ally))
            allies.Remove(ally);
    }

    public int GetAllyIndex(AllyController ally)
    {
        return allies.IndexOf(ally);
    }

    public int GetAllyCount()
    {
        return allies.Count;
    }
}
