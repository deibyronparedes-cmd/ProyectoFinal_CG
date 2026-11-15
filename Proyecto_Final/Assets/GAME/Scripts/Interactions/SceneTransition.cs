using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    [Header("Escena de destino")]
    [SerializeField] private string sceneToLoad = "Laboratorio";

    private bool transitionStarted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (transitionStarted)
            return;

        PlayerHealth player =
            other.GetComponentInParent<PlayerHealth>();

        if (player == null)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe un GameManager activo.");
            return;
        }

        transitionStarted = true;

        Debug.Log("El jugador ha salido de Recepcion.");

        
        if (GameManager.Instance.Saves != null)
        {
            GameManager.Instance.Saves.SaveGame(sceneToLoad);
        }

       
        GameManager.Instance.ChangeScene(sceneToLoad);
    }
}
