using UnityEngine;

/// <summary>
/// Interactable station used by SalaControlController.
/// The station advances only when it is the next one in the required sequence.
/// </summary>
public class ControlStationInteractable : Interactable
{
    [Header("Sala de Control")]
    [SerializeField] private string terminalId = "SALA_CAMARAS";
    [SerializeField] private int stationIndex = 0;
    [SerializeField] private string stationLabel = "Restablecer vigilancia";
    [SerializeField] private string prompt = "[E] ACTIVAR ESTACIÓN";

    [Header("Feedback when used out of order")]
    [SerializeField] private AudioClip deniedSound;
    [Range(0f, 1f)]
    [SerializeField] private float deniedSoundVolume = 0.8f;

    private SalaControlController sceneController;

    public string TerminalId => terminalId;
    public int StationIndex => stationIndex;
    public string StationLabel => stationLabel;

    public override bool CanBeFocused
    {
        get
        {
            return GameManager.Instance == null
                || !GameManager.Instance.IsTerminalActivated(terminalId);
        }
    }

    public override bool CanInteract => CanBeFocused;

    public override string GetInteractionPrompt()
    {
        if (!CanBeFocused)
            return "SISTEMA ACTIVO";

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

        bool activated = sceneController.TryActivateStation(this);
        if (activated)
            base.Interact();
    }

    public void PlayDeniedFeedback()
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
