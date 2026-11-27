using UnityEngine;

/// <summary>
/// Estación interactiva de SalaControl. La última estación completada puede deshacerse.
/// </summary>
public class ControlStationInteractable : Interactable
{
    [Header("Sala de Control")]
    [SerializeField] private string terminalId = "SALA_CAMARAS";
    [SerializeField] private int stationIndex = 0;
    [SerializeField] private string stationLabel = "Restablecer vigilancia";
    [SerializeField] private string prompt = "[E] ACTIVAR ESTACIÓN";

    [Header("Feedback de secuencia incorrecta")]
    [SerializeField] private AudioClip deniedSound;
    [Range(0f, 1f)]
    [SerializeField] private float deniedSoundVolume = 0.8f;

    [Header("Feedback al deshacer")]
    [SerializeField] private AudioClip undoSound;
    [Range(0f, 1f)]
    [SerializeField] private float undoSoundVolume = 0.8f;

    private SalaControlController sceneController;

    public string TerminalId => terminalId;
    public int StationIndex => stationIndex;
    public string StationLabel => stationLabel;

    public override bool CanBeFocused
    {
        get
        {
            if (GameManager.Instance == null)
                return true;

            if (!GameManager.Instance.IsTerminalActivated(terminalId))
                return true;

            return sceneController != null && sceneController.CanUndoStation(this);
        }
    }

    public override bool CanInteract => CanBeFocused;

    public override string GetInteractionPrompt()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsTerminalActivated(terminalId))
            return "[E] DESHACER ÚLTIMO PASO";

        return prompt;
    }

    public void Configure(SalaControlController controller, string id, int index, string label)
    {
        sceneController = controller;
        terminalId = id;
        stationIndex = index;
        stationLabel = label;
        prompt = "[E] " + label.ToUpperInvariant();
    }

    public override void Interact()
    {
        if (!CanInteract)
            return;

        if (sceneController == null)
        {
            Debug.LogError("ControlStationInteractable: falta asignar SalaControlController.", this);
            PlayDeniedFeedback();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.IsTerminalActivated(terminalId))
        {
            if (sceneController.TryUndoLastStation(this))
            {
                Debug.Log("Último paso deshecho: " + stationLabel, this);
                PlayUndoFeedback();
            }
            return;
        }

        if (sceneController.TryActivateStation(this))
            base.Interact();
    }

    public void PlayDeniedFeedback()
    {
        if (deniedSound != null)
            AudioSource.PlayClipAtPoint(deniedSound, transform.position, deniedSoundVolume);
    }

    private void PlayUndoFeedback()
    {
        if (undoSound != null)
            AudioSource.PlayClipAtPoint(undoSound, transform.position, undoSoundVolume);
    }
}
