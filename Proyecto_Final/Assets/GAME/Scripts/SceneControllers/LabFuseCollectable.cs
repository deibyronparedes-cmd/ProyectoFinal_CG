using UnityEngine;

/// <summary>Objeto recogible del laboratorio: el fusible industrial.</summary>
public class LabFuseCollectable : Interactable
{
    [SerializeField] private string itemId = "FUSIBLE_LABORATORIO";
    [SerializeField] private LaboratorioController sceneController;

    private bool collected;

    protected override void Awake()
    {
        base.Awake();
        if (sceneController == null)
            sceneController = FindFirstObjectByType<LaboratorioController>();
    }

    private void Start()
    {
        // Si el jugador ya lleva el fusible (por una partida cargada), no reaparece en el escenario.
        if (GameManager.Instance != null && GameManager.Instance.HasItem(itemId))
        {
            collected = true;
            gameObject.SetActive(false);
        }
    }

    public override bool CanBeFocused => !collected;

    public override string GetInteractionPrompt()
    {
        return "[E] RECOGER FUSIBLE INDUSTRIAL";
    }

    public override void Interact()
    {
        if (collected)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe GameManager activo.", this);
            return;
        }

        if (!GameManager.Instance.AddItem(itemId))
            return;

        collected = true;
        base.Interact();

        if (sceneController != null)
            sceneController.NotifyFuseCollected();

        gameObject.SetActive(false);
    }
}
