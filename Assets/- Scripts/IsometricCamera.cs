using UnityEngine;

public class IsometricCameraFollow : MonoBehaviour
{
    public Transform player; // Assign the Player GameObject here
    public Vector3 offset = new Vector3(0, 10, -10); // Adjust this based on your scene
    public float smoothSpeed = 5f; // Adjust smoothness

    void LateUpdate()
    {
        if (player != null)
        {
            Vector3 targetPosition = player.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
    }
}
