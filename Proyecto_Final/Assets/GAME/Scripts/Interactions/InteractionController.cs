using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float interactionDistance = 1.5f;

    private Interactable currentInteractable;
    private int interactionMask;

    private void Awake()
    {
        if (player == null)
            player = transform;

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

   
        int playerLayer = LayerMask.NameToLayer("Player");

        interactionMask = playerLayer >= 0
            ? ~(1 << playerLayer)
            : Physics.DefaultRaycastLayers;
    }

    private void Update()
    {
        DetectInteractable();

        if (currentInteractable == null)
            return;

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        if (Input.GetMouseButtonDown(0) ||
            Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log(
                "Interactuando con: " + currentInteractable.gameObject.name
            );

            currentInteractable.Interact();
        }
    }

    private void DetectInteractable()
    {
        if (playerCamera == null || player == null)
        {
            currentInteractable = null;
            return;
        }


        Vector3 rayOrigin =
            player.position + Vector3.up * 0.5f;

        Vector3 rayDirection = playerCamera.forward;
        rayDirection.y = 0f;

        if (rayDirection.sqrMagnitude < 0.001f)
        {
            currentInteractable = null;
            return;
        }

        rayDirection.Normalize();

        Ray ray = new Ray(rayOrigin, rayDirection);

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            interactionMask,
            QueryTriggerInteraction.Ignore))
        {
            currentInteractable =
                hit.collider.GetComponentInParent<Interactable>();
        }
        else
        {
            currentInteractable = null;
        }

        Debug.DrawRay(
            rayOrigin,
            rayDirection * interactionDistance,
            currentInteractable != null ? Color.green : Color.red
        );
    }
}
