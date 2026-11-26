
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Inventario")]
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

    [Header("Terminales")]
    private Dictionary<string, bool> terminalStates =
        new Dictionary<string, bool>();

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
    }

    // =====================================
    // INVENTARIO - LIST
    // =====================================

    public bool AddItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        if (inventory.Contains(itemId))
        {
            Debug.Log("Este objeto ya esta en el inventario.");
            return false;
        }

        inventory.Add(itemId);

        Debug.Log("Objeto agregado: " + itemId);

        InventoryChanged?.Invoke();

        return true;
    }

    public bool HasItem(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && inventory.Contains(itemId);
    }

    // Registra un objeto recogido y el identificador unico de su punto en el mundo.
    // El pickupId permite que un consumible no reaparezca al cargar una partida
    // despues de haber sido utilizado.
    public bool AddItem(string itemId, string pickupId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        if (!string.IsNullOrWhiteSpace(pickupId) &&
            collectedPickupIds.Contains(pickupId))
        {
            Debug.Log("Este punto de recogida ya fue utilizado: " + pickupId);
            return false;
        }

        if (inventory.Contains(itemId))
        {
            Debug.Log("Este objeto ya esta en el inventario: " + itemId);
            return false;
        }

        inventory.Add(itemId);

        if (!string.IsNullOrWhiteSpace(pickupId))
            collectedPickupIds.Add(pickupId);

        Debug.Log("Objeto agregado: " + itemId);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool HasCollectedPickup(string pickupId)
    {
        return !string.IsNullOrWhiteSpace(pickupId) &&
               collectedPickupIds.Contains(pickupId);
    }

    // Consume un botiquin y restaura salud en una sola operacion.
    // No consume el objeto cuando la salud ya esta al maximo.
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

        Debug.Log("Objeto consumido: " + itemId + ". Salud: " +
                  previousHealth + " -> " + currentHealth);
        return true;
    }

    // =====================================
    // SISTEMA DE VIDA
    // =====================================

    public void ApplyDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - amount);

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

        currentHealth = Mathf.Min(
            maxHealth,
            currentHealth + amount
        );

        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // =====================================
    // TERMINALES - DICTIONARY
    // =====================================

    public void RegisterTerminal(string terminalId)
    {
        if (string.IsNullOrWhiteSpace(terminalId))
            return;

        if (!terminalStates.ContainsKey(terminalId))
        {
            terminalStates.Add(terminalId, false);
        }
    }

    public bool ActivateTerminal(string terminalId)
    {
        if (string.IsNullOrWhiteSpace(terminalId))
            return false;

        RegisterTerminal(terminalId);

        terminalStates[terminalId] = true;

        Debug.Log("Terminal activado: " + terminalId);

        return true;
    }

    public bool IsTerminalActivated(string terminalId)
    {
        return terminalStates.TryGetValue(
            terminalId,
            out bool activated
        ) && activated;
    }

    // =====================================
    // PREPARAR DATOS PARA JSON
    // =====================================

    public List<TerminalSaveData> GetTerminalSaveData()
    {
        List<TerminalSaveData> result =
            new List<TerminalSaveData>();

        foreach (var terminal in terminalStates)
        {
            result.Add(new TerminalSaveData
            {
                terminalId = terminal.Key,
                activated = terminal.Value
            });
        }

        return result;
    }

    // =====================================
    // RESTAURAR PARTIDA
    // =====================================

    public void RestoreFromSave(SaveData data)
    {
        if (data == null)
            return;

        // Restaurar vida
        currentHealth = Mathf.Clamp(
            data.currentHealth,
            0,
            maxHealth
        );

        // Reconstruir inventario
        inventory.Clear();

        if (data.inventory != null)
        {
            foreach (string item in data.inventory)
            {
                if (!string.IsNullOrWhiteSpace(item) &&
                    !inventory.Contains(item))
                {
                    inventory.Add(item);
                }
            }
        }

        // Reconstruir puntos de recogida ya utilizados.
        // El null-check mantiene compatibilidad con archivos JSON anteriores.
        collectedPickupIds.Clear();
        if (data.collectedPickupIds != null)
        {
            foreach (string pickupId in data.collectedPickupIds)
            {
                if (!string.IsNullOrWhiteSpace(pickupId) &&
                    !collectedPickupIds.Contains(pickupId))
                {
                    collectedPickupIds.Add(pickupId);
                }
            }
        }

        // Reconstruir Dictionary
        terminalStates.Clear();

        if (data.terminals != null)
        {
            foreach (TerminalSaveData terminal in data.terminals)
            {
                if (terminal != null &&
                    !string.IsNullOrWhiteSpace(terminal.terminalId))
                {
                    terminalStates[terminal.terminalId] =
                        terminal.activated;
                }
            }
        }

        // Actualizar interfaz
        InventoryChanged?.Invoke();

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        Debug.Log("Partida restaurada correctamente.");
    }

    // =====================================
    // NUEVA PARTIDA
    // =====================================

    public void StartNewGame()
    {
        inventory.Clear();
        collectedPickupIds.Clear();

        terminalStates.Clear();

        currentHealth = maxHealth;

        InventoryChanged?.Invoke();

        HealthChanged?.Invoke(
            currentHealth,
            maxHealth
        );

        if (saveManager != null)
        {
            saveManager.DeleteSave();
        }

        Debug.Log("Nueva partida iniciada.");

        ChangeScene("Recepcion");
    }

    // =====================================
    // CAMBIO DE ESCENAS
    // =====================================

    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                "Escena no disponible en Build Profiles: " +
                sceneName
            );

            return;
        }

        Debug.Log("Cargando escena: " + sceneName);

        SceneManager.LoadScene(sceneName);
    }
}
