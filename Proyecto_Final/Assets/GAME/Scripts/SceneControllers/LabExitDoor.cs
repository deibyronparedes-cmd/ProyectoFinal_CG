using UnityEngine;

/// <summary>Salida del laboratorio; solo permite avanzar tras restaurar la energia.</summary>
public class LabExitDoor : Interactable
{
    [SerializeField] private LaboratorioController sceneController;
    private bool used;

    protected override void Awake()
    {
        base.Awake();
        if (sceneController == null)
            sceneController = FindFirstObjectByType<LaboratorioController>();
    }

    public override string GetInteractionPrompt()
    {
        if (sceneController == null || !sceneController.IsPowerRestored)
            return "SALIDA BLOQUEADA - RESTAURA LA ENERGIA";

        return "[E] ABRIR SALIDA A SALA DE CONTROL";
    }

    public override void Interact()
    {
        if (used)
            return;

        if (sceneController == null)
        {
            Debug.LogError("LabExitDoor: falta LaboratorioController.", this);
            return;
        }

        if (!sceneController.IsPowerRestored)
        {
            sceneController.NotifyExitBlocked();
            return;
        }

        used = true;
        base.Interact();
        sceneController.CompleteLaboratory();
    }
}
