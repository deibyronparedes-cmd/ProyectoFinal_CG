using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[Serializable]
public class InventoryItemDefinition
{
    public string itemId;
    public string displayName;
    [TextArea(2, 5)] public string description;
    public Sprite icon;

    [Header("Uso del objeto")]
    [Min(0)] public int healingAmount;
    public string useButtonLabel = "USAR";
}

public class UIManager : MonoBehaviour
{
    [Header("Textos del HUD")]
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text inventoryText;
    [SerializeField] private TMP_Text healthText;

    [Header("Barra de vida")]
    [SerializeField] private Slider healthSlider;

    [Header("Indicador de interaccion") ]
    [SerializeField] private GameObject interactionPromptPanel;
    [SerializeField] private TMP_Text interactionPromptText;

    [Header("Panel de inventario")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button closeInventoryButton;
    [SerializeField] private Button[] inventorySlotButtons = new Button[8];
    [SerializeField] private TMP_Text inventoryCountLabel;

    [Header("Detalles del objeto seleccionado")]
    [SerializeField] private Image selectedItemIcon;
    [SerializeField] private TMP_Text selectedItemName;
    [SerializeField] private TMP_Text selectedItemDescription;

    [Header("Accion del objeto seleccionado")]
    [SerializeField] private Button selectedItemUseButton;
    [SerializeField] private TMP_Text selectedItemUseButtonText;

    [Header("Catalogo de objetos")]
    [SerializeField] private InventoryItemDefinition[] itemDefinitions;

    [Header("Paneles de estado")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private GameObject defeatPanel;

    [Header("Botones de pausa")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button pauseMainMenuButton;

    [Header("Botones de victoria")]
    [SerializeField] private Button victoryRestartButton;
    [SerializeField] private Button victoryMenuButton;

    [Header("Botones de derrota")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button defeatMenuButton;

    [Header("Escenas")]
    [SerializeField] private string menuSceneName = "Menu";

    private GameManager gameManager;
    private int selectedItemIndex = -1;

    public bool IsInventoryOpen => inventoryPanel != null && inventoryPanel.activeSelf;
    public bool IsPauseOpen => pausePanel != null && pausePanel.activeSelf;
    public bool IsEndPanelOpen =>
        (victoryPanel != null && victoryPanel.activeSelf) ||
        (defeatPanel != null && defeatPanel.activeSelf);

    private void Start()
    {
        // Evita heredar un Time.timeScale de 0 al cargar otra escena.
        Time.timeScale = 1f;

        SetPanelActive(inventoryPanel, false);
        SetPanelActive(pausePanel, false);
        SetPanelActive(victoryPanel, false);
        SetPanelActive(defeatPanel, false);
        HideInteractionPrompt();

        BindButton(closeInventoryButton, CloseInventory);
        BindButton(selectedItemUseButton, UseSelectedItem);
        BindButton(resumeButton, ResumeGame);
        BindButton(restartButton, RestartGame);
        BindButton(pauseMainMenuButton, ReturnToMenuFromPause);
        BindButton(victoryRestartButton, RestartGame);
        BindButton(victoryMenuButton, ReturnToMenu);
        BindButton(retryButton, RestartGame);
        BindButton(defeatMenuButton, ReturnToMenu);

        InitializeInventorySlots();
        ClearSelectedItem();

        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("No existe un GameManager activo.");
            return;
        }

        gameManager.InventoryChanged += RefreshInventory;
        gameManager.HealthChanged += RefreshHealth;
        gameManager.PlayerDefeated += HandlePlayerDefeated;

        RefreshInventory();
        RefreshHealth(gameManager.CurrentHealth, gameManager.MaxHealth);

        // No sobrescribe el objetivo si otro sistema ya lo actualizó después.
        if (objectiveText != null && string.IsNullOrWhiteSpace(objectiveText.text))
            SetObjective("Encuentra una tarjeta de acceso.");
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.InventoryChanged -= RefreshInventory;
            gameManager.HealthChanged -= RefreshHealth;
            gameManager.PlayerDefeated -= HandlePlayerDefeated;
        }

        UnbindButton(closeInventoryButton, CloseInventory);
        UnbindButton(selectedItemUseButton, UseSelectedItem);
        UnbindButton(resumeButton, ResumeGame);
        UnbindButton(restartButton, RestartGame);
        UnbindButton(pauseMainMenuButton, ReturnToMenuFromPause);
        UnbindButton(victoryRestartButton, RestartGame);
        UnbindButton(victoryMenuButton, ReturnToMenu);
        UnbindButton(retryButton, RestartGame);
        UnbindButton(defeatMenuButton, ReturnToMenu);
    }

    private void BindButton(Button button, UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private void UnbindButton(Button button, UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    // =====================================================
    // INVENTARIO
    // =====================================================

    private void InitializeInventorySlots()
    {
        if (inventorySlotButtons == null)
            return;

        for (int i = 0; i < inventorySlotButtons.Length; i++)
        {
            Button slotButton = inventorySlotButtons[i];
            if (slotButton == null)
                continue;

            int slotIndex = i;
            slotButton.onClick.AddListener(() => SelectInventoryItem(slotIndex));
        }
    }

    public void ToggleInventory()
    {
        if (IsPauseOpen || IsEndPanelOpen)
            return;

        SetInventoryOpen(!IsInventoryOpen);
    }

    public void OpenInventory()
    {
        if (IsPauseOpen || IsEndPanelOpen)
            return;

        SetInventoryOpen(true);
    }

    public void CloseInventory()
    {
        SetInventoryOpen(false);
    }

    private void SetInventoryOpen(bool open)
    {
        if (inventoryPanel == null)
        {
            Debug.LogWarning("Asigna InventoryPanel en el Inspector de UIManager.");
            return;
        }

        inventoryPanel.SetActive(open);

        if (open)
        {
            HideInteractionPrompt();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshInventory();

            if (gameManager != null && gameManager.Inventory.Count > 0 && selectedItemIndex < 0)
                SelectInventoryItem(0);
        }
        else if (!IsPauseOpen && !IsEndPanelOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void RefreshInventory()
    {
        if (gameManager == null)
            return;

        int itemCount = gameManager.Inventory.Count;

        if (inventoryText != null)
            inventoryText.text = "Objetos recogidos: " + itemCount;

        if (inventoryCountLabel != null)
            inventoryCountLabel.text = "OBJETOS RECOGIDOS: " + itemCount;

        if (inventorySlotButtons != null)
        {
            for (int i = 0; i < inventorySlotButtons.Length; i++)
            {
                Button slotButton = inventorySlotButtons[i];
                if (slotButton == null)
                    continue;

                bool hasItem = i < itemCount;
                slotButton.interactable = hasItem;

                TMP_Text slotLabel = slotButton.GetComponentInChildren<TMP_Text>(true);
                if (slotLabel == null)
                    continue;

                if (hasItem)
                {
                    string itemId = gameManager.Inventory[i];
                    InventoryItemDefinition definition = FindItemDefinition(itemId);
                    slotLabel.text = definition != null && !string.IsNullOrWhiteSpace(definition.displayName)
                        ? definition.displayName
                        : itemId;
                }
                else
                {
                    slotLabel.text = "VACIO";
                }
            }
        }

        if (selectedItemIndex >= itemCount)
        {
            selectedItemIndex = -1;
            ClearSelectedItem();
        }
    }

    private void SelectInventoryItem(int slotIndex)
    {
        if (gameManager == null || slotIndex < 0 || slotIndex >= gameManager.Inventory.Count)
            return;

        selectedItemIndex = slotIndex;
        string itemId = gameManager.Inventory[slotIndex];
        InventoryItemDefinition definition = FindItemDefinition(itemId);

        if (selectedItemName != null)
        {
            selectedItemName.text = definition != null && !string.IsNullOrWhiteSpace(definition.displayName)
                ? definition.displayName
                : itemId;
        }

        if (selectedItemDescription != null)
        {
            selectedItemDescription.text = definition != null && !string.IsNullOrWhiteSpace(definition.description)
                ? definition.description
                : "No hay una descripcion registrada para este objeto.";
        }

        if (selectedItemIcon != null)
        {
            Sprite icon = definition != null ? definition.icon : null;
            selectedItemIcon.sprite = icon;
            selectedItemIcon.enabled = icon != null;
        }

        UpdateSelectedItemAction(definition);
    }

    private void UpdateSelectedItemAction(InventoryItemDefinition definition)
    {
        if (selectedItemUseButton == null)
            return;

        bool isHealingItem = definition != null && definition.healingAmount > 0;
        selectedItemUseButton.gameObject.SetActive(isHealingItem);

        if (!isHealingItem)
            return;

        bool canUse = gameManager != null &&
                      gameManager.CurrentHealth > 0 &&
                      gameManager.CurrentHealth < gameManager.MaxHealth;

        selectedItemUseButton.interactable = canUse;

        if (selectedItemUseButtonText != null)
        {
            if (gameManager == null || gameManager.CurrentHealth <= 0)
                selectedItemUseButtonText.text = "NO DISPONIBLE";
            else if (gameManager.CurrentHealth >= gameManager.MaxHealth)
                selectedItemUseButtonText.text = "VIDA AL MAXIMO";
            else
                selectedItemUseButtonText.text = string.IsNullOrWhiteSpace(definition.useButtonLabel)
                    ? "USAR"
                    : definition.useButtonLabel;
        }
    }

    public void UseSelectedItem()
    {
        if (gameManager == null || selectedItemIndex < 0 ||
            selectedItemIndex >= gameManager.Inventory.Count)
        {
            return;
        }

        string itemId = gameManager.Inventory[selectedItemIndex];
        InventoryItemDefinition definition = FindItemDefinition(itemId);

        if (definition == null || definition.healingAmount <= 0)
            return;

        if (gameManager.CurrentHealth >= gameManager.MaxHealth)
        {
            UpdateSelectedItemAction(definition);
            Debug.Log("La salud ya esta al maximo. El botiquin no se ha consumido.");
            return;
        }

        int previousSelection = selectedItemIndex;
        selectedItemIndex = -1;

        if (!gameManager.TryConsumeHealingItem(itemId, definition.healingAmount))
        {
            selectedItemIndex = previousSelection;
            SelectInventoryItem(previousSelection);
            return;
        }

        ClearSelectedItem();
        RefreshInventory();
        Debug.Log("Botiquin utilizado correctamente.");
    }

    private InventoryItemDefinition FindItemDefinition(string itemId)
    {
        if (itemDefinitions == null)
            return null;

        for (int i = 0; i < itemDefinitions.Length; i++)
        {
            InventoryItemDefinition definition = itemDefinitions[i];
            if (definition != null && definition.itemId == itemId)
                return definition;
        }

        return null;
    }

    private void ClearSelectedItem()
    {
        if (selectedItemIcon != null)
        {
            selectedItemIcon.sprite = null;
            selectedItemIcon.enabled = false;
        }

        if (selectedItemName != null)
            selectedItemName.text = "SIN OBJETO SELECCIONADO";

        if (selectedItemDescription != null)
            selectedItemDescription.text = "Selecciona un objeto de tu inventario para consultar su informacion y utilidad.";

        if (selectedItemUseButton != null)
        {
            selectedItemUseButton.interactable = false;
            selectedItemUseButton.gameObject.SetActive(false);
        }
    }

    // =====================================================
    // INDICADOR DE INTERACCION
    // =====================================================

    public void ShowInteractionPrompt(string message)
    {
        if (string.IsNullOrWhiteSpace(message) ||
            interactionPromptPanel == null ||
            interactionPromptText == null)
        {
            HideInteractionPrompt();
            return;
        }

        if (IsPauseOpen || IsInventoryOpen || IsEndPanelOpen || Time.timeScale <= 0f)
        {
            HideInteractionPrompt();
            return;
        }

        if (interactionPromptText.text != message)
            interactionPromptText.text = message;

        if (!interactionPromptPanel.activeSelf)
            interactionPromptPanel.SetActive(true);
    }

    public void HideInteractionPrompt()
    {
        if (interactionPromptPanel != null && interactionPromptPanel.activeSelf)
            interactionPromptPanel.SetActive(false);
    }

    // =====================================================
    // PAUSA
    // =====================================================

    public void TogglePause()
    {
        if (IsEndPanelOpen || pausePanel == null)
            return;

        if (IsPauseOpen)
            ResumeGame();
        else
            OpenPause();
    }

    public void OpenPause()
    {
        if (pausePanel == null || IsEndPanelOpen)
            return;

        SetPanelActive(inventoryPanel, false);
        HideInteractionPrompt();
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        SetPanelActive(pausePanel, false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // =====================================================
    // VICTORIA Y DERROTA
    // =====================================================

    public void ShowVictory()
    {
        HideInteractionPrompt();
        SetPanelActive(inventoryPanel, false);
        SetPanelActive(pausePanel, false);
        SetPanelActive(defeatPanel, false);
        SetPanelActive(victoryPanel, true);
        FreezeForEndPanel();
    }

    public void ShowDefeat()
    {
        HideInteractionPrompt();
        SetPanelActive(inventoryPanel, false);
        SetPanelActive(pausePanel, false);
        SetPanelActive(victoryPanel, false);
        SetPanelActive(defeatPanel, true);
        FreezeForEndPanel();
    }

    private void HandlePlayerDefeated()
    {
        ShowDefeat();
    }

    private void FreezeForEndPanel()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // =====================================================
    // ACCIONES DE LOS BOTONES
    // =====================================================

    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe GameManager para reiniciar la partida.");
            return;
        }

        GameManager.Instance.StartNewGame();
    }

    public void ReturnToMenuFromPause()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe GameManager para guardar y volver al menu.");
            SceneManager.LoadScene(menuSceneName);
            return;
        }

        if (GameManager.Instance.Saves != null)
            GameManager.Instance.Saves.SaveGame();

        GameManager.Instance.ChangeScene(menuSceneName);
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.ChangeScene(menuSceneName);
        else
            SceneManager.LoadScene(menuSceneName);
    }

    public void SetObjective(string message)
    {
        if (objectiveText != null)
            objectiveText.text = "OBJETIVO: " + message;
    }

    private void RefreshHealth(int currentHealth, int maxHealth)
    {
        if (healthText != null)
            healthText.text = "Vida: " + currentHealth + " / " + maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (gameManager != null && selectedItemIndex >= 0 &&
            selectedItemIndex < gameManager.Inventory.Count)
        {
            InventoryItemDefinition definition =
                FindItemDefinition(gameManager.Inventory[selectedItemIndex]);
            UpdateSelectedItemAction(definition);
        }
    }
}
