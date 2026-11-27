using UnityEngine;

/// <summary>
/// Controlador local de SalaControl. Consulta y notifica el progreso al GameManager.
/// </summary>
public class SalaControlController : MonoBehaviour
{
    [Header("Referencias de escena")]
    [SerializeField] private UIManager uiManager;
    [SerializeField] private ControlStationInteractable camerasStation;
    [SerializeField] private ControlStationInteractable containmentStation;
    [SerializeField] private ControlStationInteractable evacuationStation;

    [Header("Identificadores persistentes")]
    [SerializeField] private string camerasId = "SALA_CAMARAS";
    [SerializeField] private string containmentId = "SALA_CONTENCION";
    [SerializeField] private string evacuationId = "SALA_EVACUACION";

    private const string ProtocolObjectiveId = "SALA_CONTROL_PROTOCOLO_COMPLETADO";
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

        gameManager.InitializeStationSequence(camerasId, containmentId, evacuationId);
        camerasStation.Configure(this, camerasId, 0, "Restablecer vigilancia");
        containmentStation.Configure(this, containmentId, 1, "Aislar el sector");
        evacuationStation.Configure(this, evacuationId, 2, "Autorizar evacuación");

        if (IsSequenceComplete())
        {
            gameManager.MarkObjectiveCompleted(ProtocolObjectiveId);
            if (gameManager.Saves != null)
                gameManager.Saves.SaveGame("SalaControl", "SALA_CONTROL_COMPLETADO");
            if (uiManager != null)
                uiManager.ShowVictory();
        }
        else
        {
            RefreshObjective();
        }
    }

    public bool TryActivateStation(ControlStationInteractable station)
    {
        if (station == null || gameManager == null || gameManager.IsTerminalActivated(station.TerminalId))
            return false;

        string expectedId = gameManager.PeekNextStationId();
        if (string.IsNullOrWhiteSpace(expectedId))
        {
            Debug.LogWarning("SalaControl: no hay estaciones pendientes.", station);
            return false;
        }

        if (expectedId != station.TerminalId)
        {
            string message = "SECUENCIA INCORRECTA. SIGUIENTE PASO: " + GetExpectedStationName(expectedId) + ".";
            if (uiManager != null)
                uiManager.SetObjective(message);
            Debug.Log(message, station);
            station.PlayDeniedFeedback();
            return false;
        }

        // Peek valida; Dequeue avanza la Queue y Push registra el historial Stack.
        if (!gameManager.TryActivateNextStation(station.TerminalId))
            return false;

        gameManager.MarkObjectiveCompleted(GetObjectiveId(station.TerminalId));
        gameManager.SetCheckpoint("SALA_CONTROL_DESPUES_" + station.TerminalId);

        Debug.Log("Sala de Control: estación activada - " + station.StationLabel, station);

        if (IsSequenceComplete())
        {
            gameManager.MarkObjectiveCompleted(ProtocolObjectiveId);
            gameManager.SetCheckpoint("SALA_CONTROL_COMPLETADO");
            if (gameManager.Saves != null)
                gameManager.Saves.SaveGame("SalaControl", gameManager.CurrentCheckpointId);
            if (uiManager != null)
                uiManager.ShowVictory();
            Debug.Log("PROTOCOLO COMPLETADO: evacuación autorizada.");
            return true;
        }

        if (gameManager.Saves != null)
            gameManager.Saves.SaveGame("SalaControl", gameManager.CurrentCheckpointId);

        RefreshObjective();
        return true;
    }

    public bool CanUndoStation(ControlStationInteractable station)
    {
        return station != null && gameManager != null &&
               !IsSequenceComplete() &&
               gameManager.IsLastActivatedStation(station.TerminalId);
    }

    public bool TryUndoLastStation(ControlStationInteractable station)
    {
        if (!CanUndoStation(station))
            return false;

        if (!gameManager.UndoLastStation(out string undoneTerminalId))
            return false;

        gameManager.RemoveCompletedObjective(GetObjectiveId(undoneTerminalId));
        gameManager.RemoveCompletedObjective(ProtocolObjectiveId);
        gameManager.SetCheckpoint("SALA_CONTROL_REVERTIDO_" + undoneTerminalId);

        Debug.Log("Se deshizo la última estación: " + undoneTerminalId +
                  ". La Queue vuelve a solicitar ese paso.", station);

        if (gameManager.Saves != null)
            gameManager.Saves.SaveGame("SalaControl", gameManager.CurrentCheckpointId);

        RefreshObjective();
        return true;
    }

    private bool IsSequenceComplete()
    {
        return gameManager != null &&
               gameManager.IsTerminalActivated(camerasId) &&
               gameManager.IsTerminalActivated(containmentId) &&
               gameManager.IsTerminalActivated(evacuationId) &&
               gameManager.PeekNextStationId() == null;
    }

    private string GetExpectedStationName(string terminalId)
    {
        if (terminalId == camerasId) return "RESTABLECER VIGILANCIA";
        if (terminalId == containmentId) return "AISLAR EL SECTOR";
        if (terminalId == evacuationId) return "AUTORIZAR EVACUACIÓN";
        return "PROTOCOLO COMPLETADO";
    }

    private string GetObjectiveId(string terminalId)
    {
        if (terminalId == camerasId) return "SALA_CONTROL_VIGILANCIA";
        if (terminalId == containmentId) return "SALA_CONTROL_CONTENCION";
        if (terminalId == evacuationId) return "SALA_CONTROL_EVACUACION";
        return string.Empty;
    }

    private void RefreshObjective()
    {
        if (uiManager == null || gameManager == null)
            return;

        string nextId = gameManager.PeekNextStationId();
        if (nextId == camerasId)
            uiManager.SetObjective("Restablece el sistema de vigilancia.");
        else if (nextId == containmentId)
            uiManager.SetObjective("Vigilancia restablecida. Ahora aísla el sector.");
        else if (nextId == evacuationId)
            uiManager.SetObjective("Sector aislado. Autoriza la evacuación.");
        else
            uiManager.SetObjective("Protocolo completado. Evacuación autorizada.");
    }
}
