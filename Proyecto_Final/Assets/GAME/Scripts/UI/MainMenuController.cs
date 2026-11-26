using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private const string MusicPreferenceKey = "MenuMusicEnabled";

    [Header("Paneles")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject instructionsPanel;

    [Header("Botones")]
    [SerializeField] private Button continueButton;

    [Header("Musica del menu")]
    [SerializeField] private AudioSource menuMusicSource;
    [SerializeField] private TMP_Text musicButtonText;

    [Header("Sonido de interfaz")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.75f;

    private AudioSource uiSfxSource;
    private bool musicEnabled = true;
    private bool isMenuScene;

    private void Start()
    {
        isMenuScene = SceneManager.GetActiveScene().name == "Menu";

        if (menuPanel != null)
            menuPanel.SetActive(isMenuScene);

        if (hudPanel != null)
            hudPanel.SetActive(!isMenuScene);

        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);

        CacheUiSfxSource();

        if (isMenuScene)
        {
            RefreshContinueButton();
            InitializeMusic();
        }
        else if (menuMusicSource != null)
        {
            menuMusicSource.Stop();
        }
    }

    private void CacheUiSfxSource()
    {
     
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("MainMenuController: no se encontro GameManager para el sonido de interfaz.");
            return;
        }

        uiSfxSource = GameManager.Instance.GetComponent<AudioSource>();

        if (uiSfxSource == null)
            uiSfxSource = GameManager.Instance.gameObject.AddComponent<AudioSource>();

        uiSfxSource.playOnAwake = false;
        uiSfxSource.loop = false;
        uiSfxSource.spatialBlend = 0f;
    }

    private void PlayClickSound()
    {
        if (clickSound == null)
            return;

        if (uiSfxSource == null)
            CacheUiSfxSource();

        if (uiSfxSource != null)
            uiSfxSource.PlayOneShot(clickSound, clickVolume);
    }

    private void InitializeMusic()
    {
        musicEnabled = PlayerPrefs.GetInt(MusicPreferenceKey, 1) == 1;

        if (menuMusicSource == null)
        {
            GameObject menuMusicObject = GameObject.Find("MenuMusic");
            if (menuMusicObject != null)
                menuMusicSource = menuMusicObject.GetComponent<AudioSource>();
        }

        if (menuMusicSource != null)
        {
            menuMusicSource.loop = true;
            menuMusicSource.spatialBlend = 0f;
            menuMusicSource.volume = 0.3f;

            if (musicEnabled)
            {
                if (!menuMusicSource.isPlaying)
                    menuMusicSource.Play();
            }
            else
            {
                menuMusicSource.Stop();
            }
        }
        else
        {
            Debug.LogWarning("MainMenuController: asigna el AudioSource de MenuMusic en el Inspector.");
        }

        UpdateMusicButtonLabel();
    }

    private void UpdateMusicButtonLabel()
    {
        if (musicButtonText != null)
        {
            musicButtonText.text = musicEnabled
                ? "♫ MUSICA: ACTIVADA"
                : "♫ MUSICA: DESACTIVADA";
        }
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
        PlayClickSound();

        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe GameManager.");
            return;
        }

        GameManager.Instance.StartNewGame();
    }

    public void ContinueGame()
    {
        PlayClickSound();

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

    public void OpenInstructions()
    {
        PlayClickSound();

        if (instructionsPanel != null)
            instructionsPanel.SetActive(true);
        else
            Debug.LogWarning("No se asigno InstructionsPanel en MainMenuController.");
    }

    public void CloseInstructions()
    {
        PlayClickSound();

        if (instructionsPanel != null)
            instructionsPanel.SetActive(false);
    }

    public void ToggleMusic()
    {
        PlayClickSound();

        musicEnabled = !musicEnabled;
        PlayerPrefs.SetInt(MusicPreferenceKey, musicEnabled ? 1 : 0);
        PlayerPrefs.Save();

        if (menuMusicSource != null)
        {
            if (musicEnabled)
            {
                if (!menuMusicSource.isPlaying)
                    menuMusicSource.Play();
            }
            else
            {
                menuMusicSource.Stop();
            }
        }

        UpdateMusicButtonLabel();
    }

    public void QuitGame()
    {
        PlayClickSound();
        StartCoroutine(QuitAfterClickSound());
    }

    private IEnumerator QuitAfterClickSound()
    {
        float waitTime = clickSound != null ? Mathf.Min(clickSound.length, 0.5f) : 0f;
        if (waitTime > 0f)
            yield return new WaitForSecondsRealtime(waitTime);

        Debug.Log("Saliendo del juego...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // En una compilacion independiente, cierra la aplicacion.
        Application.Quit();
#endif
    }
}
