using UnityEngine;

public class IsometricCameraFollow : MonoBehaviour
{
    public Transform player; // Assign the Player GameObject here
    public Camera mainCamera; // Assign the Camera
    public Vector3 offset = new Vector3(0, 10, -10); // Adjust this based on your scene
    public float smoothSpeed = 5f; // Adjust smoothness
    public float cursorInfluence = 5f; // How much the cursor affects the camera position

    void LateUpdate()
    {
        if (player == null || mainCamera == null) return;

        // Get mouse position in world space
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Vector3 cursorPosition = hit.point;
            cursorPosition.y = player.position.y; // Keep camera movement on a fixed plane

            // Find target camera position based on player and cursor
            Vector3 targetPosition = player.position + offset + (cursorPosition - player.position) / cursorInfluence;

            // Smoothly move the camera
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
        else
        {
            // Default behavior (if no hit)
            Vector3 targetPosition = player.position + offset;
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
    }
}
