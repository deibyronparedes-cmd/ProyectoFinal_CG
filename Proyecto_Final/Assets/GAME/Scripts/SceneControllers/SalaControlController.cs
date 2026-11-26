using UnityEngine;

/// <summary>
/// Controls the three-stage security sequence in SalaControl.
/// Global progress is stored in GameManager's terminal Dictionary,
/// so the sequence survives scene changes and JSON saves.
/// </summary>
public class SalaControlController : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private ControlStationInteractable camerasStation;
    [SerializeField] private ControlStationInteractable containmentStation;
    [SerializeField] private ControlStationInteractable evacuationStation;

    [Header("Persistent terminal IDs")]
    [SerializeField] private string camerasId = "SALA_CAMARAS";
    [SerializeField] private string containmentId = "SALA_CONTENCION";
    [SerializeField] private string evacuationId = "SALA_EVACUACION";

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("SalaControlController: no existe GameManager activo.", this);
            enabled = false;
            return;
        }

        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        if (camerasStation == null || containmentStation == null || evacuationStation == null)
        {
            Debug.LogError("SalaControlController: asigna las tres estaciones en el Inspector.", this);
            enabled = false;
            return;
        }

        gameManager.RegisterTerminal(camerasId);
        gameManager.RegisterTerminal(containmentId);
        gameManager.RegisterTerminal(evacuationId);

        camerasStation.Configure(this, camerasId, 0, "Restablecer vigilancia");
        containmentStation.Configure(this, containmentId, 1, "Aislar el sector");
        evacuationStation.Configure(this, evacuationId, 2, "Autorizar evacuación");

        RefreshObjective();

        if (IsSequenceComplete())
        {
            if (gameManager.Saves != null)
                gameManager.Saves.SaveGame("SalaControl");

            if (uiManager != null)
                uiManager.ShowVictory();
        }
    }

    public bool TryActivateStation(ControlStationInteractable station)
    {
        if (station == null || gameManager == null)
            return false;

        if (gameManager.IsTerminalActivated(station.TerminalId))
            return false;

        int expectedIndex = GetNextRequiredIndex();
        if (station.StationIndex != expectedIndex)
        {
            string expectedName = GetExpectedStationName(expectedIndex);
            string message = "SECUENCIA INCORRECTA. SIGUIENTE PASO: " + expectedName + ".";

            if (uiManager != null)
                uiManager.SetObjective(message);

            Debug.Log(message, station);
            station.PlayDeniedFeedback();
            return false;
        }

        if (!gameManager.ActivateTerminal(station.TerminalId))
            return false;

        Debug.Log("Sala de Control: estación activada - " + station.StationLabel, station);

        if (gameManager.Saves != null)
            gameManager.Saves.SaveGame("SalaControl");

        if (IsSequenceComplete())
        {
            if (uiManager != null)
                uiManager.ShowVictory();

            Debug.Log("PROTOCOLO COMPLETADO: evacuación autorizada.");
            return true;
        }

        RefreshObjective();
        return true;
    }

    private int GetNextRequiredIndex()
    {
        if (!gameManager.IsTerminalActivated(camerasId))
            return 0;
        if (!gameManager.IsTerminalActivated(containmentId))
            return 1;
        if (!gameManager.IsTerminalActivated(evacuationId))
            return 2;

        return 3;
    }

    private bool IsSequenceComplete()
    {
        return gameManager.IsTerminalActivated(camerasId)
            && gameManager.IsTerminalActivated(containmentId)
            && gameManager.IsTerminalActivated(evacuationId);
    }

    private string GetExpectedStationName(int index)
    {
        switch (index)
        {
            case 0: return "RESTABLECER VIGILANCIA";
            case 1: return "AISLAR EL SECTOR";
            case 2: return "AUTORIZAR EVACUACIÓN";
            default: return "PROTOCOLO COMPLETADO";
        }
    }

    private void RefreshObjective()
    {
        if (uiManager == null)
            return;

        int nextIndex = GetNextRequiredIndex();
        switch (nextIndex)
        {
            case 0:
                uiManager.SetObjective("Restablece el sistema de vigilancia.");
                break;
            case 1:
                uiManager.SetObjective("Vigilancia restablecida. Ahora aísla el sector.");
                break;
            case 2:
                uiManager.SetObjective("Sector aislado. Autoriza la evacuación.");
                break;
            default:
                uiManager.SetObjective("Protocolo completado. Evacuación autorizada.");
                break;
        }
    }
}
