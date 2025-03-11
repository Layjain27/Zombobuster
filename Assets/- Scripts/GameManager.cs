using UnityEngine;
using System.Collections.Generic;

public enum ZombieType
{
    Walker,
    Runner,
    Tank,
    Crawler,
    Spitter,
    Exploder,
    Armored
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public List<Zombie> activeZombies = new List<Zombie>();
    public int totalZombiesKilled = 0;

    [Header("Zombie Prefabs")]
    public List<GameObject> zombiePrefabs; // Assign different zombie prefabs in Inspector

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterZombie(Zombie zombie)
    {
        activeZombies.Add(zombie);
    }

    public void ZombieKilled(Zombie zombie)
    {
        if (activeZombies.Contains(zombie))
        {
            activeZombies.Remove(zombie);
            totalZombiesKilled++;
            Debug.Log("Total Zombies Killed: " + totalZombiesKilled);
        }
    }

    public void SpawnZombie(Vector3 position, Quaternion rotation, int zombieIndex)
    {
        if (zombieIndex >= 0 && zombieIndex < zombiePrefabs.Count)
        {
            GameObject newZombie = Instantiate(zombiePrefabs[zombieIndex], position, rotation);
            RegisterZombie(newZombie.GetComponent<Zombie>());
        }
        else
        {
            Debug.LogWarning("Invalid zombie index.");
        }
    }
}

public class Zombie : MonoBehaviour
{
    public string zombieName;
    public int hitpoints;
    public ZombieType zombieType;
    public ZombieType assignedZombieType; // Assign in Inspector

    void Start()
    {
        zombieType = assignedZombieType; // Use assigned type
        SetZombieStats();
        GameManager.Instance.RegisterZombie(this);
    }

    void SetZombieStats()
    {
        switch (zombieType)
        {
            case ZombieType.Walker:
                hitpoints = 50;
                break;
            case ZombieType.Runner:
                hitpoints = 40;
                break;
            case ZombieType.Tank:
                hitpoints = 200;
                break;
            case ZombieType.Crawler:
                hitpoints = 30;
                break;
            case ZombieType.Spitter:
                hitpoints = 60;
                break;
            case ZombieType.Exploder:
                hitpoints = 80;
                break;
            case ZombieType.Armored:
                hitpoints = 150;
                break;
        }
    }

    public void TakeDamage(int damage, string hitArea, WeaponType weaponType)
    {
        float multiplier = 1f;
        if (hitArea == "Head")
        {
            multiplier = 2f;
            if (weaponType == WeaponType.Pistol && zombieType == ZombieType.Walker)
            {
                hitpoints = 0;
            }
        }
        else // Combines Torso and Legs
        {
            multiplier = 1.25f; // Adjusted for balance
        }

        int finalDamage = Mathf.RoundToInt(damage * multiplier);
        hitpoints -= finalDamage;

        if (hitpoints <= 0)
        {
            Debug.Log(zombieName + " is eliminated!");
            GameManager.Instance.ZombieKilled(this);
            Destroy(gameObject);
        }
        else
        {
            Debug.Log(zombieName + " took " + finalDamage + " damage. Remaining HP: " + hitpoints);
        }
    }
}

public enum WeaponType
{
    Pistol,
    AssaultRifle,
    Shotgun,
    LMG,
    SMG
}
