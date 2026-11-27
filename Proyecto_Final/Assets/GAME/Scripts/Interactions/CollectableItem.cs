using UnityEngine;
public class CollectableItem : Interactable
{
    [Header("Identificación")]
    [SerializeField] private string itemId = "KEYCARD_RECEPCION";
    [SerializeField] private string objectiveAfterCollection = "";
    [SerializeField] private string pickupId = "PICKUP_KEYCARD_RECEPCION_01";

    private bool collected;

    private void Start()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
            return;
        if (manager.HasItem(itemId) || manager.HasCollectedPickup(pickupId))
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
            Debug.LogError("No existe un GameManager activo.", this);
            return;
        }

        if (!manager.AddItem(itemId, pickupId))
            return;

        collected = true;
        base.Interact();

        UIManager ui = FindFirstObjectByType<UIManager>();

        if (ui != null &&
            !string.IsNullOrWhiteSpace(objectiveAfterCollection))
        {
            ui.SetObjective(objectiveAfterCollection);
        }

        gameObject.SetActive(false);
    }
}
