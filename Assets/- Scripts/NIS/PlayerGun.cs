using UnityEngine;

public class PlayerGun : MonoBehaviour
{
    public int gunLevel = 1;

    public void Upgrade()
    {
        gunLevel++;
        Debug.Log("Gun upgraded to level: " + gunLevel);
        // Add gun stat updates here later
    }
}
