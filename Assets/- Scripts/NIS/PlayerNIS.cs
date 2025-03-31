using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNIS : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CharacterController playerController;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 2f;

    [Header("Gravity Settings")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float groundedDownVelocity = -2f;
    [SerializeField] private float checkSphereRadius = 0.4f;

    private Vector3 velocity;
    private bool isGrounded;

    private PlayerControls inputActions;
    private Vector2 moveInput;
    private Vector3 moveDirection;

    private void Awake()
    {
        inputActions = new PlayerControls();

        // Bind movement
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        // Bind Jump
        inputActions.Player.Jump.performed += ctx => Jump();
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        HandleMovement();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        camForward.y = 0;
        camRight.y = 0;
        camForward.Normalize();
        camRight.Normalize();

        moveDirection = camForward * moveInput.y + camRight * moveInput.x;
        playerController.Move(moveDirection * moveSpeed * Time.deltaTime);

        // Optional: Face movement direction
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 720f * Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, checkSphereRadius, groundLayerMask);

        if (isGrounded && velocity.y < 0)
            velocity.y = groundedDownVelocity;

        velocity.y += gravity * Time.deltaTime;
        playerController.Move(velocity * Time.deltaTime);
    }

    private void Jump()
    {
        if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
