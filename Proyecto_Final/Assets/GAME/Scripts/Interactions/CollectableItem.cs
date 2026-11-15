using UnityEngine;

public class CollectableItem : Interactable
{
    [Header("Identificacion")]
    [SerializeField] private string itemId = "KEYCARD_RECEPCION";

    private bool collected = false;

    public override void Interact()
    {
        if (collected)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe un GameManager activo.");
            return;
        }

        if (GameManager.Instance.AddItem(itemId))
        {
            base.Interact();

            collected = true;

            Debug.Log("Tarjeta de acceso recogida.");

            UIManager ui = FindFirstObjectByType<UIManager>();

            if (ui != null)
            {
                ui.SetObjective(
                    "Activa el terminal de seguridad."
                );
            }

            gameObject.SetActive(false);
        }
    }
}
