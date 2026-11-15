
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Textos del HUD")]
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text inventoryText;
    [SerializeField] private TMP_Text healthText;

    [Header("Barra de vida")]
    [SerializeField] private Slider healthSlider;

    private GameManager gameManager;

    private void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("No existe un GameManager activo.");
            return;
        }

        // Escuchar cambios del inventario
        gameManager.InventoryChanged += RefreshInventory;

        // Escuchar cambios de vida
        gameManager.HealthChanged += RefreshHealth;

        RefreshInventory();

        RefreshHealth(
            gameManager.CurrentHealth,
            gameManager.MaxHealth
        );

        SetObjective("Encuentra una tarjeta de acceso.");
    }

    private void OnDestroy()
    {
        if (gameManager != null)
        {
            gameManager.InventoryChanged -= RefreshInventory;
            gameManager.HealthChanged -= RefreshHealth;
        }
    }

    public void SetObjective(string message)
    {
        if (objectiveText != null)
        {
            objectiveText.text = "OBJETIVO: " + message;
        }
    }

    private void RefreshInventory()
    {
        if (inventoryText == null || gameManager == null)
            return;

        inventoryText.text =
            "Objetos recogidos: " + gameManager.Inventory.Count;
    }

    private void RefreshHealth(int currentHealth, int maxHealth)
    {
        if (healthText != null)
        {
            healthText.text =
                "Vida: " + currentHealth + " / " + maxHealth;
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }
}
