using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject hudPanel;

    [Header("Botones")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;

    private void Start()
    {
    
        bool isMenu =
            SceneManager.GetActiveScene().name == "Menu";

        if (menuPanel != null)
            menuPanel.SetActive(isMenu);

        if (hudPanel != null)
            hudPanel.SetActive(!isMenu);

      
        if (isMenu)
            RefreshContinueButton();
    }

    public void RefreshContinueButton()
    {
        if (continueButton == null)
            return;

        bool hasSave =
            GameManager.Instance != null &&
            GameManager.Instance.Saves != null &&
            GameManager.Instance.Saves.HasSaveFile();

        continueButton.interactable = hasSave;
    }

    public void StartNewGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe GameManager.");
            return;
        }

        GameManager.Instance.StartNewGame();
    }

    public void ContinueGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe GameManager.");
            return;
        }

        SaveManager saves = GameManager.Instance.Saves;

        if (saves == null || !saves.LoadGame())
        {
            Debug.LogWarning("No se pudo cargar la partida.");
            RefreshContinueButton();
        }
    }
}
