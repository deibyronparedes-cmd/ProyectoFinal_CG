using UnityEngine;

/// <summary>
/// Controlador propio de la escena Laboratorio.
/// Coordina el fusible, el terminal electrico, la salida y el GameManager global.
/// No almacena copias de vida ni inventario.
/// </summary>
public class LaboratorioController : MonoBehaviour
{
    [Header("Referencias de escena")]
    [SerializeField] private UIManager uiManager;

    [Header("Progresion del laboratorio")]
    [SerializeField] private string fuseItemId = "FUSIBLE_LABORATORIO";
    [SerializeField] private string powerTerminalId = "TERMINAL_LABORATORIO";
    [SerializeField] private string nextSceneName = "SalaControl";

    private GameManager gameManager;
    private bool transitionStarted;

    public string FuseItemId => fuseItemId;
    public string PowerTerminalId => powerTerminalId;
    public bool HasFuse => gameManager != null && gameManager.HasItem(fuseItemId);
    public bool IsPowerRestored => gameManager != null && gameManager.IsTerminalActivated(powerTerminalId);

    private void Awake()
    {
        gameManager = GameManager.Instance;

        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();
    }

    private void Start()
    {
        if (gameManager == null)
        {
            Debug.LogError("LaboratorioController: no se encontro GameManager. Entra a Laboratorio desde Menu/Recepcion para conservar el GameManager persistente.", this);
            return;
        }

        gameManager.RegisterTerminal(powerTerminalId);
        RefreshObjective();
    }

    public void NotifyFuseCollected()
    {
        if (uiManager != null)
            uiManager.SetObjective("Fusible recuperado. Llevatelo al panel electrico.");
    }

    public bool TryRestorePower()
    {
        if (gameManager == null)
        {
            Debug.LogError("No existe GameManager.", this);
            return false;
        }

        if (!gameManager.HasItem(fuseItemId))
        {
            if (uiManager != null)
                uiManager.SetObjective("Sistema sin energia. Encuentra el fusible industrial.");

            Debug.Log("Laboratorio: falta el fusible industrial.", this);
            return false;
        }

        bool activated = gameManager.ActivateTerminal(powerTerminalId);

        if (activated && uiManager != null)
            uiManager.SetObjective("Energia restaurada. Dirigete a la salida del laboratorio.");

        return activated;
    }

    public void NotifyExitBlocked()
    {
        if (uiManager != null)
            uiManager.SetObjective("Salida bloqueada. Primero restaura la energia del laboratorio.");
    }

    public void CompleteLaboratory()
    {
        if (transitionStarted)
            return;

        if (!IsPowerRestored)
        {
            NotifyExitBlocked();
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError("La siguiente escena no esta incluida en Build Profiles: " + nextSceneName, this);
            if (uiManager != null)
                uiManager.SetObjective("La salida esta lista, pero SalaControl aun no esta disponible en Build Profiles.");
            return;
        }

        transitionStarted = true;

        // Guardar el destino para que Continuar reanude desde la siguiente escena.
        if (gameManager != null && gameManager.Saves != null)
            gameManager.Saves.SaveGame(nextSceneName);

        gameManager.ChangeScene(nextSceneName);
    }

    private void RefreshObjective()
    {
        if (uiManager == null || gameManager == null)
            return;

        if (IsPowerRestored)
            uiManager.SetObjective("Energia restaurada. Dirigete a la salida del laboratorio.");
        else if (HasFuse)
            uiManager.SetObjective("Llevate el fusible al panel electrico.");
        else
            uiManager.SetObjective("Encuentra el fusible industrial y restaura la energia.");
    }
}
