using UnityEngine;

public class CollectableItem : Interactable
{
    [Header("Identificacion")]
    [SerializeField] private string itemId = "KEYCARD_RECEPCION";

    private bool collected = false;

    private void Start()
    {
        // Si el objeto ya estaba guardado en el inventario,
        // no debe volver a aparecer al cargar la escena.
        if (GameManager.Instance != null && GameManager.Instance.HasItem(itemId))
        {
            collected = true;
            gameObject.SetActive(false);
        }
    }

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
                ui.SetObjective("Activa el terminal de seguridad.");
            }

            gameObject.SetActive(false);
        }
    }
}
