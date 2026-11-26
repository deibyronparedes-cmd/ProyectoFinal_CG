using UnityEngine;

public class SecurityTerminal : Interactable
{
    [Header("Identificacion")]
    [SerializeField] private string terminalId = "TERMINAL_RECEPCION";

    [Header("Requisitos")]
    [SerializeField] private string requiredItemId = "KEYCARD_RECEPCION";

    [Header("Sonido de acceso denegado")]
    [SerializeField] private AudioClip deniedSound;
    [Range(0f, 1f)]
    [SerializeField] private float deniedSoundVolume = 1f;

    private bool isActivated;

    public override bool CanBeFocused
    {
        get
        {
            if (isActivated)
                return false;

            return GameManager.Instance == null ||
                   !GameManager.Instance.IsTerminalActivated(terminalId);
        }
    }

    public override bool CanInteract => CanBeFocused;

    public override string GetInteractionPrompt()
    {
        if (GameManager.Instance == null)
            return "TERMINAL SIN CONEXION";

        if (isActivated || GameManager.Instance.IsTerminalActivated(terminalId))
            return "TERMINAL — SISTEMA YA AUTORIZADO";

        if (!GameManager.Instance.HasItem(requiredItemId))
            return "TERMINAL BLOQUEADO — INTRODUCE LA TARJETA DE ACCESO";

        return "[E] ACTIVAR TERMINAL DE SEGURIDAD";
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterTerminal(terminalId);
            isActivated = GameManager.Instance.IsTerminalActivated(terminalId);
        }
    }

    public override void Interact()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe un GameManager.");
            return;
        }

        if (isActivated || GameManager.Instance.IsTerminalActivated(terminalId))
        {
            Debug.Log("Terminal: sistema ya autorizado.");
            return;
        }

        if (!GameManager.Instance.HasItem(requiredItemId))
        {
            Debug.Log("ACCESO DENEGADO: introduce una tarjeta de seguridad.");
            PlayDeniedSound();
            return;
        }

        isActivated = GameManager.Instance.ActivateTerminal(terminalId);
        if (!isActivated)
            return;

        base.Interact();
        Debug.Log("Recepcion: acceso autorizado.");

        UIManager ui = FindFirstObjectByType<UIManager>();
        if (ui != null)
            ui.SetObjective("Dirigete hacia la puerta de acceso.");
    }

    private void PlayDeniedSound()
    {
        if (deniedSound != null)
        {
            AudioSource.PlayClipAtPoint(
                deniedSound,
                transform.position,
                deniedSoundVolume
            );
        }
    }
}
