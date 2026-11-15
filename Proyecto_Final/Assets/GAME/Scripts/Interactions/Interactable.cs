using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    [Header("Informacion del objeto")]
    [SerializeField] private string objectName = "Objeto interactivo";

    [TextArea(2, 4)]
    [SerializeField]
    private string interactionMessage =
        "Has interactuado con este objeto.";

    [Header("Sonido")]
    [SerializeField] private AudioClip interactionSound;

    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 1f;

    public virtual void Interact()
    {
        Debug.Log(
            objectName + ": " + interactionMessage,
            gameObject
        );

        if (interactionSound != null)
        {
            AudioSource.PlayClipAtPoint(
                interactionSound,
                transform.position,
                soundVolume
            );
        }
    }
}
