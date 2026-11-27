using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Estado global persistente del juego.
/// List: inventario y objetivos; Dictionary: terminales;
/// Queue: secuencia FIFO de SalaControl; Stack: historial LIFO para deshacer.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Inventario - List")]
    private List<string> inventory = new List<string>();
    private List<string> collectedPickupIds = new List<string>();

    public IReadOnlyList<string> Inventory => inventory;
    public IReadOnlyList<string> CollectedPickupIds => collectedPickupIds;
    public event Action InventoryChanged;

    [Header("Sistema de vida")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public event Action<int, int> HealthChanged;
    public event Action PlayerDefeated;

    [Header("Terminales - Dictionary")]
    private Dictionary<string, bool> terminalStates = new Dictionary<string, bool>();

    [Header("Secuencia de estaciones - Queue")]
    // La Queue se usa como FIFO real durante la partida.
    private Queue<string> stationSequenceQueue = new Queue<string>();

    [Header("Historial de estaciones - Stack")]
    // El último paso realizado queda en la cima del Stack.
    private Stack<string> stationHistoryStack = new Stack<string>();

    [Header("Progreso y estadísticas")]
    private List<string> completedObjectives = new List<string>();
    private string currentCheckpointId = "RECEPCION_INICIO";
    private int totalItemsCollected;
    private int totalDamageTaken;
    private int totalTerminalsActivated;

    public IReadOnlyList<string> CompletedObjectives => completedObjectives;
    public string CurrentCheckpointId => currentCheckpointId;
    public int TotalItemsCollected => totalItemsCollected;
    public int TotalDamageTaken => totalDamageTaken;
    public int TotalTerminalsActivated => totalTerminalsActivated;

    private SaveManager saveManager;
    public SaveManager Saves => saveManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentHealth = maxHealth;
        saveManager = GetComponent<SaveManager>();

        if (saveManager == null)
            Debug.LogWarning("GameManager: no se encontró SaveManager en el mismo objeto.", this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // =====================================================
    // INVENTARIO - LIST
    // =====================================================

    public bool AddItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || inventory.Contains(itemId))
            return false;

        inventory.Add(itemId);
        totalItemsCollected++;

        Debug.Log("Objeto agregado: " + itemId);
        InventoryChanged?.Invoke();
        SaveProgressCheckpoint("PICKUP_" + itemId);
        return true;
    }

    public bool AddItem(string itemId, string pickupId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        if (!string.IsNullOrWhiteSpace(pickupId) && collectedPickupIds.Contains(pickupId))
        {
            Debug.Log("Este punto de recogida ya fue utilizado: " + pickupId);
            return false;
        }

        if (inventory.Contains(itemId))
        {
            Debug.Log("Este objeto ya está en el inventario: " + itemId);
            return false;
        }

        inventory.Add(itemId);
        if (!string.IsNullOrWhiteSpace(pickupId))
            collectedPickupIds.Add(pickupId);

        totalItemsCollected++;
        Debug.Log("Objeto agregado: " + itemId);
        InventoryChanged?.Invoke();
        SaveProgressCheckpoint("PICKUP_" + (string.IsNullOrWhiteSpace(pickupId) ? itemId : pickupId));
        return true;
    }

    public bool HasItem(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && inventory.Contains(itemId);
    }

    public bool HasCollectedPickup(string pickupId)
    {
        return !string.IsNullOrWhiteSpace(pickupId) && collectedPickupIds.Contains(pickupId);
    }

    public bool TryConsumeHealingItem(string itemId, int healingAmount)
    {
        if (string.IsNullOrWhiteSpace(itemId) || healingAmount <= 0)
            return false;

        if (currentHealth <= 0 || currentHealth >= maxHealth)
            return false;

        int itemIndex = inventory.IndexOf(itemId);
        if (itemIndex < 0)
            return false;

        inventory.RemoveAt(itemIndex);
        int previousHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + healingAmount);

        InventoryChanged?.Invoke();
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log("Objeto consumido: " + itemId + ". Salud: " + previousHealth + " -> " + currentHealth);
        SaveProgressCheckpoint("ITEM_USADO_" + itemId);
        return true;
    }

    // =====================================================
    // SISTEMA DE VIDA
    // =====================================================

    public void ApplyDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        int previousHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        totalDamageTaken += previousHealth - currentHealth;

        Debug.Log("Vida actual: " + currentHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("VIDA AGOTADA: jugador derrotado.");
            PlayerDefeated?.Invoke();
        }
    }

    public void RestoreHealth(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // =====================================================
    // TERMINALES - DICTIONARY
    // =====================================================

    public void RegisterTerminal(string terminalId)
    {
        if (string.IsNullOrWhiteSpace(terminalId))
            return;

        if (!terminalStates.ContainsKey(terminalId))
            terminalStates.Add(terminalId, false);
    }

    public bool ActivateTerminal(string terminalId)
    {
        if (string.IsNullOrWhiteSpace(terminalId))
            return false;

        RegisterTerminal(terminalId);

        // Contabiliza y guarda solo el primer cambio de inactivo a activo.
        if (!terminalStates[terminalId])
        {
            terminalStates[terminalId] = true;
            totalTerminalsActivated++;
            Debug.Log("Terminal activado: " + terminalId);
            SaveProgressCheckpoint("TERMINAL_" + terminalId);
        }

        return true;
    }

    public bool DeactivateTerminal(string terminalId)
    {
        if (string.IsNullOrWhiteSpace(terminalId) ||
            !terminalStates.ContainsKey(terminalId) ||
            !terminalStates[terminalId])
            return false;

        terminalStates[terminalId] = false;
        Debug.Log("Terminal desactivado: " + terminalId);
        return true;
    }

    public bool IsTerminalActivated(string terminalId)
    {
        return !string.IsNullOrWhiteSpace(terminalId) &&
               terminalStates.TryGetValue(terminalId, out bool activated) &&
               activated;
    }

    // =====================================================
    // SECUENCIA DE SALA DE CONTROL - QUEUE
    // =====================================================

    /// <summary>
    /// Inicializa la cola FIFO en el primer acceso o reconstruye partidas antiguas.
    /// </summary>
    public void InitializeStationSequence(params string[] orderedTerminalIds)
    {
        if (orderedTerminalIds == null || orderedTerminalIds.Length == 0)
            return;

        List<string> validIds = new List<string>();
        foreach (string id in orderedTerminalIds)
        {
            if (string.IsNullOrWhiteSpace(id) || validIds.Contains(id))
                continue;

            validIds.Add(id);
            RegisterTerminal(id);
        }

        // Una cola vacía es válida cuando todos los pasos ya están completados.
        // Si quedan terminales sin activar, se reconstruyen en el orden definido.
        if (stationSequenceQueue.Count == 0)
        {
            foreach (string id in validIds)
            {
                if (!IsTerminalActivated(id))
                    stationSequenceQueue.Enqueue(id);
            }
        }

        // Compatibilidad con partidas antiguas que guardaban terminales, pero no Stack.
        if (stationHistoryStack.Count == 0)
        {
            foreach (string id in validIds)
            {
                if (IsTerminalActivated(id))
                    stationHistoryStack.Push(id);
            }
        }

        Debug.Log("Secuencia inicializada. Siguiente estación: " + (PeekNextStationId() ?? "ninguna"));
    }

    public string PeekNextStationId()
    {
        return stationSequenceQueue.Count > 0 ? stationSequenceQueue.Peek() : null;
    }

    /// <summary>
    /// Solo activa el elemento que está al frente de la Queue. Dequeue lo retira
    /// y Push registra la activación en el Stack.
    /// </summary>
    public bool TryActivateNextStation(string terminalId)
    {
        if (string.IsNullOrWhiteSpace(terminalId) || stationSequenceQueue.Count == 0)
            return false;

        string expectedId = stationSequenceQueue.Peek();
        if (!string.Equals(expectedId, terminalId, StringComparison.Ordinal))
            return false;

        if (IsTerminalActivated(terminalId))
            return false;

        // ActivateTerminal registra el estado en Dictionary y guarda el progreso.
        if (!ActivateTerminal(terminalId))
            return false;

        string removedId = stationSequenceQueue.Dequeue();
        stationHistoryStack.Push(removedId);

        Debug.Log("QUEUE Dequeue: " + removedId + ". Siguiente: " + (PeekNextStationId() ?? "ninguna"));
        Debug.Log("STACK Push: " + removedId + ". Historial: " + stationHistoryStack.Count + " estación(es).");
        return true;
    }

    public bool IsLastActivatedStation(string terminalId)
    {
        return !string.IsNullOrWhiteSpace(terminalId) &&
               stationHistoryStack.Count > 0 &&
               string.Equals(stationHistoryStack.Peek(), terminalId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Pop deshace la última estación y la devuelve al frente de la Queue.
    /// </summary>
    public bool UndoLastStation(out string undoneTerminalId)
    {
        undoneTerminalId = null;
        if (stationHistoryStack.Count == 0)
            return false;

        string lastId = stationHistoryStack.Peek();
        if (!DeactivateTerminal(lastId))
            return false;

        undoneTerminalId = stationHistoryStack.Pop();
        EnqueueAtFront(undoneTerminalId);

        Debug.Log("STACK Pop: " + undoneTerminalId + ". La estación volvió al frente de la Queue.");
        return true;
    }

    private void EnqueueAtFront(string terminalId)
    {
        Queue<string> reorderedQueue = new Queue<string>();
        reorderedQueue.Enqueue(terminalId);

        // Se conserva FIFO: los pendientes se transfieren mediante Dequeue/Enqueue.
        while (stationSequenceQueue.Count > 0)
            reorderedQueue.Enqueue(stationSequenceQueue.Dequeue());

        stationSequenceQueue = reorderedQueue;
    }

    // =====================================================
    // OBJETIVOS, CHECKPOINT Y ESTADÍSTICAS
    // =====================================================

    public bool MarkObjectiveCompleted(string objectiveId)
    {
        if (string.IsNullOrWhiteSpace(objectiveId) || completedObjectives.Contains(objectiveId))
            return false;

        completedObjectives.Add(objectiveId);
        Debug.Log("Objetivo completado: " + objectiveId);
        return true;
    }

    public bool RemoveCompletedObjective(string objectiveId)
    {
        return !string.IsNullOrWhiteSpace(objectiveId) && completedObjectives.Remove(objectiveId);
    }

    public bool HasCompletedObjective(string objectiveId)
    {
        return !string.IsNullOrWhiteSpace(objectiveId) && completedObjectives.Contains(objectiveId);
    }

    public void SetCheckpoint(string checkpointId)
    {
        if (!string.IsNullOrWhiteSpace(checkpointId))
            currentCheckpointId = checkpointId;
    }

    private void SaveProgressCheckpoint(string checkpointId)
    {
        if (saveManager == null)
            return;

        SetCheckpoint(checkpointId);
        string activeScene = SceneManager.GetActiveScene().name;
        saveManager.SaveGame(activeScene, currentCheckpointId);
    }

    public GameStatisticsSaveData GetStatisticsSaveData()
    {
        return new GameStatisticsSaveData
        {
            totalItemsCollected = totalItemsCollected,
            totalDamageTaken = totalDamageTaken,
            totalTerminalsActivated = totalTerminalsActivated
        };
    }

    // =====================================================
    // PREPARAR DATOS PARA JSON
    // =====================================================

    public List<TerminalSaveData> GetTerminalSaveData()
    {
        List<TerminalSaveData> result = new List<TerminalSaveData>();
        foreach (KeyValuePair<string, bool> terminal in terminalStates)
        {
            result.Add(new TerminalSaveData
            {
                terminalId = terminal.Key,
                activated = terminal.Value
            });
        }
        return result;
    }

    // Estas listas son únicamente DTO para serializar el CONTENIDO en JSON.
    // Durante el juego las operaciones se realizan sobre Queue y Stack reales.
    public List<string> GetStationQueueSaveData()
    {
        return new List<string>(stationSequenceQueue);
    }

    public List<string> GetStationHistorySaveData()
    {
        // Enumerar Stack produce el orden cima -> fondo; el restaurador invierte al hacer Push.
        return new List<string>(stationHistoryStack);
    }

    public List<string> GetCompletedObjectivesSaveData()
    {
        return new List<string>(completedObjectives);
    }

    // =====================================================
    // RESTAURAR PARTIDA
    // =====================================================

    public void RestoreFromSave(SaveData data)
    {
        if (data == null)
            return;

        currentHealth = Mathf.Clamp(data.currentHealth, 0, maxHealth);

        inventory.Clear();
        if (data.inventory != null)
        {
            foreach (string item in data.inventory)
            {
                if (!string.IsNullOrWhiteSpace(item) && !inventory.Contains(item))
                    inventory.Add(item);
            }
        }

        collectedPickupIds.Clear();
        if (data.collectedPickupIds != null)
        {
            foreach (string pickupId in data.collectedPickupIds)
            {
                if (!string.IsNullOrWhiteSpace(pickupId) && !collectedPickupIds.Contains(pickupId))
                    collectedPickupIds.Add(pickupId);
            }
        }

        terminalStates.Clear();
        if (data.terminals != null)
        {
            foreach (TerminalSaveData terminal in data.terminals)
            {
                if (terminal != null && !string.IsNullOrWhiteSpace(terminal.terminalId))
                    terminalStates[terminal.terminalId] = terminal.activated;
            }
        }

        completedObjectives.Clear();
        if (data.completedObjectives != null)
        {
            foreach (string objectiveId in data.completedObjectives)
            {
                if (!string.IsNullOrWhiteSpace(objectiveId) && !completedObjectives.Contains(objectiveId))
                    completedObjectives.Add(objectiveId);
            }
        }

        // Reconstruir Queue conservando el orden FIFO del JSON.
        stationSequenceQueue.Clear();
        if (data.stationQueue != null)
        {
            foreach (string terminalId in data.stationQueue)
            {
                if (!string.IsNullOrWhiteSpace(terminalId) && !stationSequenceQueue.Contains(terminalId))
                    stationSequenceQueue.Enqueue(terminalId);
            }
        }

        // El JSON guarda Stack de cima a fondo. Push en orden inverso restaura la misma cima.
        stationHistoryStack.Clear();
        if (data.stationHistoryStack != null)
        {
            for (int i = data.stationHistoryStack.Count - 1; i >= 0; i--)
            {
                string terminalId = data.stationHistoryStack[i];
                if (!string.IsNullOrWhiteSpace(terminalId))
                    stationHistoryStack.Push(terminalId);
            }
        }

        currentCheckpointId = string.IsNullOrWhiteSpace(data.checkpointId)
            ? (string.IsNullOrWhiteSpace(data.sceneName) ? "RECEPCION_INICIO" : data.sceneName)
            : data.checkpointId;

        if (data.statistics != null)
        {
            totalItemsCollected = Mathf.Max(0, data.statistics.totalItemsCollected);
            totalDamageTaken = Mathf.Max(0, data.statistics.totalDamageTaken);
            totalTerminalsActivated = Mathf.Max(0, data.statistics.totalTerminalsActivated);
        }
        else
        {
            // Compatibilidad con JSON anterior sin bloque de estadísticas.
            totalItemsCollected = inventory.Count;
            totalDamageTaken = 0;
            totalTerminalsActivated = 0;
            foreach (KeyValuePair<string, bool> terminal in terminalStates)
            {
                if (terminal.Value)
                    totalTerminalsActivated++;
            }
        }

        InventoryChanged?.Invoke();
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log("Partida restaurada. Queue: " + stationSequenceQueue.Count + ", Stack: " + stationHistoryStack.Count + ".");
    }

    // =====================================================
    // NUEVA PARTIDA
    // =====================================================

    public void StartNewGame()
    {
        inventory.Clear();
        collectedPickupIds.Clear();
        terminalStates.Clear();
        stationSequenceQueue.Clear();
        stationHistoryStack.Clear();
        completedObjectives.Clear();

        currentHealth = maxHealth;
        currentCheckpointId = "RECEPCION_INICIO";
        totalItemsCollected = 0;
        totalDamageTaken = 0;
        totalTerminalsActivated = 0;

        InventoryChanged?.Invoke();
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (saveManager != null)
        {
            saveManager.DeleteSave();
            // Crea un primer checkpoint válido para que Reintentar siempre tenga
            // un estado al que volver, incluso si el jugador muere al inicio.
            saveManager.SaveGame("Recepcion", currentCheckpointId);
        }

        Debug.Log("Nueva partida iniciada.");
        ChangeScene("Recepcion");
    }

    // =====================================================
    // CAMBIO DE ESCENAS
    // =====================================================

    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Escena no disponible en Build Profiles: " + sceneName);
            return;
        }

        Debug.Log("Cargando escena: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
