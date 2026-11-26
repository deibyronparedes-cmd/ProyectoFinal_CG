using System.Collections;
using UnityEngine;

public class DoorAccess : Interactable
{
    [Header("Acceso")]
    [SerializeField] private string requiredItemId = "KEYCARD_RECEPCION";
    [SerializeField] private string requiredTerminalId = "TERMINAL_RECEPCION";

    [Header("Animacion de apertura")]
    [SerializeField] private float openingAngle = 90f;
    [SerializeField] private float openingDuration = 1.2f;

    [Header("Sonido de acceso denegado")]
    [SerializeField] private AudioClip deniedSound;
    [Range(0f, 1f)]
    [SerializeField] private float deniedSoundVolume = 1f;

    private bool isOpen;
    private bool isOpening;

    public override bool CanBeFocused => !isOpen && !isOpening;
    public override bool CanInteract => !isOpen && !isOpening;

    public override string GetInteractionPrompt()
    {
        if (GameManager.Instance == null)
            return "PUERTA SIN SISTEMA DE SEGURIDAD";

        if (!GameManager.Instance.HasItem(requiredItemId))
            return "PUERTA BLOQUEADA — NECESITAS LA TARJETA DE ACCESO";

        if (!GameManager.Instance.IsTerminalActivated(requiredTerminalId))
            return "PUERTA BLOQUEADA — ACTIVA EL TERMINAL DE SEGURIDAD";

        return "[E] ABRIR PUERTA";
    }

    public override void Interact()
    {
        if (isOpen || isOpening)
        {
            Debug.Log("La puerta ya esta abierta.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe un GameManager activo.");
            return;
        }

        if (!GameManager.Instance.HasItem(requiredItemId))
        {
            DenyAccess("Necesitas la tarjeta de seguridad.");
            return;
        }

        if (!GameManager.Instance.IsTerminalActivated(requiredTerminalId))
        {
            DenyAccess("Primero debes activar el terminal.");
            return;
        }

        Debug.Log("ACCESO AUTORIZADO.");
        isOpening = true;
        base.Interact();

        UIManager ui = FindFirstObjectByType<UIManager>();
        if (ui != null)
            ui.SetObjective("Avanza hacia el laboratorio principal.");

        StartCoroutine(OpenDoor());
    }

    private void DenyAccess(string message)
    {
        Debug.Log("ACCESO DENEGADO: " + message);

        if (deniedSound != null)
        {
            AudioSource.PlayClipAtPoint(
                deniedSound,
                transform.position,
                deniedSoundVolume
            );
        }
    }

    private IEnumerator OpenDoor()
    {
        Quaternion initialRotation = transform.localRotation;
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0f, openingAngle, 0f);
        float elapsed = 0f;

        while (elapsed < openingDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / openingDuration);
            transform.localRotation = Quaternion.Slerp(initialRotation, targetRotation, progress);
            yield return null;
        }

        transform.localRotation = targetRotation;
        isOpen = true;
        isOpening = false;
        Debug.Log("Puerta abierta correctamente.");
    }
}
