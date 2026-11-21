using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform target;

    [Header("Distancia")]
    [SerializeField] private float distance = 4.5f;
    [SerializeField] private float targetHeight = 1.4f;

    [Header("Rotacion de camara")]
    [SerializeField] private float mouseSensitivity = 2.5f;
    [SerializeField] private float initialPitch = 12f;
    [SerializeField] private float minPitch = -15f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Suavizado")]
    [SerializeField] private float smoothTime = 0.06f;

    private float yaw;
    private float pitch;
    private Vector3 smoothVelocity;
    private ThirdPersonCameraCollision collisionResolver;
    private bool initialized;

    private void Awake()
    {
        collisionResolver = GetComponent<ThirdPersonCameraCollision>();
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("ThirdPersonCamera: falta asignar el Player en Target.");
            enabled = false;
            return;
        }

        yaw = target.eulerAngles.y;
        pitch = Mathf.Clamp(initialPitch, minPitch, maxPitch);
        LockCursor();
    }

    private void LateUpdate()
    {
        HandleCursor();

        if (target == null)
            return;

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        target.rotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 pivot = target.position + Vector3.up * targetHeight;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = pivot + orbitRotation * Vector3.back * distance;

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref smoothVelocity,
            smoothTime
        );

        if (collisionResolver != null)
        {
            transform.position = collisionResolver.ResolvePosition(
                target,
                pivot,
                smoothedPosition,
                out bool obstructed
            );

            if (obstructed)
                smoothVelocity = Vector3.zero;
        }
        else
        {
            transform.position = smoothedPosition;
        }

        Vector3 lookDirection = pivot - transform.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(
                lookDirection,
                Vector3.up
            );
        }
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
