using UnityEngine;

public class SecurityTerminal : Interactable
{
    [Header("Identificacion")]
    [SerializeField]
    private string terminalId =
        "TERMINAL_RECEPCION";

    [Header("Requisitos")]
    [SerializeField]
    private string requiredItemId =
        "KEYCARD_RECEPCION";

    private bool isActivated = false;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterTerminal(terminalId);

            isActivated =
                GameManager.Instance.IsTerminalActivated(terminalId);
        }
    }

    public override void Interact()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe un GameManager.");
            return;
        }

        if (isActivated ||
            GameManager.Instance.IsTerminalActivated(terminalId))
        {
            Debug.Log("Terminal: sistema ya autorizado.");
            return;
        }

        
        if (!GameManager.Instance.HasItem(requiredItemId))
        {
            Debug.Log(
                "ACCESO DENEGADO: introduce una tarjeta de seguridad."
            );

            return;
        }

        
        isActivated =
            GameManager.Instance.ActivateTerminal(terminalId);

        if (!isActivated)
            return;

        base.Interact();

        Debug.Log("Recepcion: acceso autorizado.");

        
        UIManager ui = FindFirstObjectByType<UIManager>();

        if (ui != null)
        {
            ui.SetObjective(
                "Dirigete hacia la puerta de acceso."
            );
        }
    }
}
