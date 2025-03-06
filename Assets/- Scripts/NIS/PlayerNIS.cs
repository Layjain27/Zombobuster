using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNIS : MonoBehaviour
{
    [Header("Mouse Look Variables")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float maxLookUp = -90f, maxLookDown = 90f;

    private float xRotation = 0f;
    private Vector2 lookInput;

    [Header("Movement Variables")]
    [SerializeField] private CharacterController playerController;
    [SerializeField] private float moveSpeed = 5f;

    private Vector2 moveInput;

    [Header("Jumping Variables")]
    [SerializeField] private float jumpHeight = 2f;

    [Header("Gravity Variables")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private LayerMask groundLayerMask;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float checkSphereRadius = 0.4f;
    [SerializeField] private float groundedDownVelocity = -2f;

    private Vector3 velocity;
    private bool isGrounded;

    private PlayerControls inputActions;

    private void Awake()
    {
        inputActions = new PlayerControls();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Bind Input Actions
        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;

        inputActions.Player.Jump.performed += ctx => Jump();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        Look();
        Move();
        ApplyGravity();
    }

    private void Look()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, maxLookUp, maxLookDown);

        transform.Rotate(Vector3.up * mouseX);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private void Move()
    {
        Vector3 moveAmount = transform.right * moveInput.x + transform.forward * moveInput.y;
        playerController.Move(moveAmount * moveSpeed * Time.deltaTime);
    }

    private void Jump()
    {
        if (isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
        }
    }

    private void ApplyGravity()
    {
        isGrounded = Physics.CheckSphere(groundCheckPoint.position, checkSphereRadius, groundLayerMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = groundedDownVelocity;
        }

        velocity.y += gravity * Time.deltaTime;
        playerController.Move(velocity * Time.deltaTime);
    }
}
