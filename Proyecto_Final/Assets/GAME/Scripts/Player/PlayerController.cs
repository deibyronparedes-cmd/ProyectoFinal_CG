using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float gravity = -20f;

    [Header("Camara Cinemachine")]
    [SerializeField] private Transform cameraTarget;
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 60f;

    private CharacterController controller;
    private Animator animator;

    private float verticalVelocity;
    private float yaw;
    private float pitch = 12f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        animator = GetComponentInChildren<Animator>();

        if (cameraTarget == null)
        {
            cameraTarget = transform.Find("CameraTarget");
        }

        if (cameraTarget == null)
        {
            Debug.LogError(
                "No se encontro CameraTarget dentro de Player."
            );
        }
    }

    private void Start()
    {
        yaw = transform.eulerAngles.y;

        if (cameraTarget != null)
        {
            pitch = cameraTarget.localEulerAngles.x;

            if (pitch > 180f)
                pitch -= 360f;
        }

        LockCursor();
    }

    private void Update()
    {
        HandleCursor();

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            HandleCameraRotation();
        }

        HandleMovement();
    }

    private void HandleCameraRotation()
    {
        if (cameraTarget == null)
            return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * mouseSensitivity;

        transform.rotation = Quaternion.Euler(
            0f,
            yaw,
            0f
        );
        pitch -= mouseY * mouseSensitivity;

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );

        cameraTarget.localRotation =
            Quaternion.Euler(pitch, 0f, 0f);
    }


    private void HandleMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }

        Vector3 direction =
            transform.forward * vertical +
            transform.right * horizontal;

        direction = Vector3.ClampMagnitude(
            direction,
            1f
        );

        bool isMoving = direction.sqrMagnitude > 0.01f;

        bool isRunning = isMoving &&
            (Input.GetKey(KeyCode.LeftShift) ||
             Input.GetKey(KeyCode.RightShift));

        float currentSpeed = isRunning
            ? runSpeed
            : moveSpeed;

        if (animator != null)
        {
            float animationSpeed = 0f;

            if (isMoving)
            {
                animationSpeed = isRunning ? 1f : 0.5f;
            }

            animator.SetFloat(
                "Speed",
                animationSpeed,
                0.1f,
                Time.deltaTime
            );
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 movement = direction * currentSpeed;

        movement.y = verticalVelocity;

        controller.Move(
            movement * Time.deltaTime
        );
    }

    private void HandleCursor()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked &&
            Input.GetMouseButtonDown(0))
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}