using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Camara")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private Animator animator;

    private float verticalVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        animator = GetComponentInChildren<Animator>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (animator == null)
        {
            Debug.LogWarning("No se encontro el Animator del personaje.");
        }
    }

    private void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (cameraTransform == null)
            return;

        float horizontal = 0f;
        float vertical = 0f;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 direction =
            forward * vertical + right * horizontal;

        direction = Vector3.ClampMagnitude(direction, 1f);

        bool isMoving = direction.sqrMagnitude > 0.01f;

        bool isRunning = isMoving &&
            (Input.GetKey(KeyCode.LeftShift) ||
             Input.GetKey(KeyCode.RightShift));

        float currentSpeed = isRunning ? runSpeed : moveSpeed;

        if (isMoving)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );
        }

        // Actualizar las animaciones
        if (animator != null)
        {
            float animationSpeed = 0f;

            if (isMoving)
                animationSpeed = isRunning ? 1f : 0.5f;

            animator.SetFloat(
                "Speed",
                animationSpeed,
                0.1f,
                Time.deltaTime
            );
        }

        // Gravedad
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 movement = direction * currentSpeed;
        movement.y = verticalVelocity;

        controller.Move(movement * Time.deltaTime);
    }
}
