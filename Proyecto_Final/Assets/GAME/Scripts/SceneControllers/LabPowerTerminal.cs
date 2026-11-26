using UnityEngine;

/// <summary>Terminal electrico que requiere el fusible del laboratorio.</summary>
public class LabPowerTerminal : Interactable
{
    [SerializeField] private LaboratorioController sceneController;
    private bool activated;

    protected override void Awake()
    {
        base.Awake();
        if (sceneController == null)
            sceneController = FindFirstObjectByType<LaboratorioController>();
    }

    private void Start()
    {
        activated = sceneController != null && sceneController.IsPowerRestored;
    }

    public override bool CanBeFocused => !activated;

    public override string GetInteractionPrompt()
    {
        if (sceneController == null)
            return "TERMINAL ELECTRICO";

        return sceneController.HasFuse
            ? "[E] INSTALAR FUSIBLE Y RESTAURAR ENERGIA"
            : "TERMINAL SIN ENERGIA - ENCUENTRA EL FUSIBLE";
    }

    public override void Interact()
    {
        if (activated)
            return;

        if (sceneController == null)
        {
            Debug.LogError("LabPowerTerminal: falta LaboratorioController.", this);
            return;
        }

        if (!sceneController.TryRestorePower())
            return;

        activated = true;
        base.Interact();
        Debug.Log("Laboratorio: energia restaurada.", this);
    }
}
