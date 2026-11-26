using UnityEngine;

public class CollectableItem : Interactable
{
    [Header("Identificacion")]
    [SerializeField] private string itemId = "KEYCARD_RECEPCION";
    [SerializeField] private string pickupId = "PICKUP_KEYCARD_RECEPCION_01";

    [Header("Objetivo posterior (opcional)")]
    [TextArea(1, 3)]
    [SerializeField] private string objectiveAfterCollection =
        "Activa el terminal de seguridad.";

    private bool collected;

    private void Start()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;

        // Compatible con guardados antiguos: si el objeto ya esta en el
        // inventario, tampoco se vuelve a mostrar en el escenario.
        if (manager.HasCollectedPickup(pickupId) || manager.HasItem(itemId))
        {
            collected = true;
            gameObject.SetActive(false);
        }
    }

    public override void Interact()
    {
        if (collected)
            return;

        GameManager manager = GameManager.Instance;
        if (manager == null)
        {
            Debug.LogError("No existe un GameManager activo.");
            return;
        }

        if (!manager.AddItem(itemId, pickupId))
            return;

        base.Interact();
        collected = true;

        Debug.Log("Objeto recogido: " + itemId);

        if (!string.IsNullOrWhiteSpace(objectiveAfterCollection))
        {
            UIManager ui = FindFirstObjectByType<UIManager>();
            if (ui != null)
                ui.SetObjective(objectiveAfterCollection);
        }

        gameObject.SetActive(false);
    }
}
