using UnityEngine;

public class InteractionController : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private UIManager uiManager;

    private Interactable currentInteractable;
    private int interactionMask;

    private void Awake()
    {
        if (player == null)
            player = transform;

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        int playerLayer = LayerMask.NameToLayer("Player");
        interactionMask = playerLayer >= 0
            ? ~(1 << playerLayer)
            : Physics.DefaultRaycastLayers;
    }

    private void Update()
    {
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        if (Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked)
        {
            ChangeCurrentInteractable(null);
            return;
        }

        DetectInteractable();

        if (currentInteractable == null)
            return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            if (!currentInteractable.CanInteract)
                return;

            Debug.Log("Interactuando con: " + currentInteractable.gameObject.name);
            currentInteractable.Interact();
        }
    }


    private void DetectInteractable()
    {
        if (playerCamera == null || player == null)
        {
            ChangeCurrentInteractable(null);
            return;
        }

        Vector3 rayOrigin = playerCamera.position;
        Vector3 rayDirection = playerCamera.forward;

        if (rayDirection.sqrMagnitude < 0.001f)
        {
            ChangeCurrentInteractable(null);
            return;
        }

        rayDirection.Normalize();

        Ray ray = new Ray(rayOrigin, rayDirection);

        Interactable detected = null;

        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionDistance,
            interactionMask,
            QueryTriggerInteraction.Ignore))
        {
            detected = hit.collider.GetComponentInParent<Interactable>();

            if (detected != null && !detected.CanBeFocused)
                detected = null;
        }

        ChangeCurrentInteractable(detected);

        if (currentInteractable != null && uiManager != null)
        {
            uiManager.ShowInteractionPrompt(
                currentInteractable.GetInteractionPrompt()
            );
        }

        Debug.DrawRay(
            rayOrigin,
            rayDirection * interactionDistance,
            currentInteractable != null ? Color.cyan : Color.red
        );
    }

    private void ChangeCurrentInteractable(Interactable newInteractable)
    {
        if (currentInteractable == newInteractable)
        {
            if (newInteractable == null && uiManager != null)
                uiManager.HideInteractionPrompt();
            return;
        }

        if (currentInteractable != null)
            currentInteractable.SetFocused(false);

        currentInteractable = newInteractable;

        if (currentInteractable != null)
        {
            currentInteractable.SetFocused(true);
            if (uiManager != null)
                uiManager.ShowInteractionPrompt(currentInteractable.GetInteractionPrompt());
        }
        else if (uiManager != null)
        {
            uiManager.HideInteractionPrompt();
        }
    }

    private void OnDisable()
    {
        ChangeCurrentInteractable(null);
    }
}
