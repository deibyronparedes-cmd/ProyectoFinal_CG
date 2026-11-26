using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class InventoryItemDefinition
{
    public string itemId;
    public string displayName;
    [TextArea(2, 5)] public string description;
    public Sprite icon;
}

public class UIManager : MonoBehaviour
{
    [Header("Textos del HUD")]
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text inventoryText;
    [SerializeField] private TMP_Text healthText;

    [Header("Barra de vida")]
    [SerializeField] private Slider healthSlider;

    [Header("Panel de inventario")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button closeInventoryButton;
    [SerializeField] private Button[] inventorySlotButtons = new Button[8];
    [SerializeField] private TMP_Text inventoryCountLabel;

    [Header("Detalles del objeto seleccionado")]
    [SerializeField] private Image selectedItemIcon;
    [SerializeField] private TMP_Text selectedItemName;
    [SerializeField] private TMP_Text selectedItemDescription;

    [Header("Catalogo de objetos")]
    [SerializeField] private InventoryItemDefinition[] itemDefinitions;

    private GameManager gameManager;
    private int selectedItemIndex = -1;

    public bool IsInventoryOpen => inventoryPanel != null && inventoryPanel.activeSelf;

    private void Start()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (closeInventoryButton != null)
            closeInventoryButton.onClick.AddListener(CloseInventory);

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

        RefreshInventory();
        RefreshHealth(gameManager.CurrentHealth, gameManager.MaxHealth);
        SetObjective("Encuentra una tarjeta de acceso.");
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.InventoryChanged -= RefreshInventory;
            gameManager.HealthChanged -= RefreshHealth;
        }

        if (closeInventoryButton != null)
            closeInventoryButton.onClick.RemoveListener(CloseInventory);
    }

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
        SetInventoryOpen(!IsInventoryOpen);
    }

    public void OpenInventory()
    {
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
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshInventory();

            if (gameManager != null && gameManager.Inventory.Count > 0 && selectedItemIndex < 0)
                SelectInventoryItem(0);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SetObjective(string message)
    {
        if (objectiveText != null)
            objectiveText.text = "OBJETIVO: " + message;
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
    }
}
