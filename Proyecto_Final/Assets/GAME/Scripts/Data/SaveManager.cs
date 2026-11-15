
using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    private const string FileName = "savegame.json";

    public string SavePath
    {
        get
        {
            return Path.Combine(
                Application.streamingAssetsPath,
                FileName
            );
        }
    }

    // =====================================
    // COMPROBAR SI EXISTE UNA PARTIDA
    // =====================================

    public bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    // =====================================
    // GUARDAR PARTIDA
    // =====================================

    public bool SaveGame(string sceneName = null)
    {
        GameManager manager = GameManager.Instance;

        if (manager == null)
        {
            Debug.LogError("No existe GameManager.");
            return false;
        }

        try
        {
            SaveData data = new SaveData
            {
                sceneName = string.IsNullOrWhiteSpace(sceneName)
                    ? SceneManager.GetActiveScene().name
                    : sceneName,

                currentHealth = manager.CurrentHealth,

                inventory = new System.Collections.Generic.List<string>(
                    manager.Inventory
                ),

                terminals = manager.GetTerminalSaveData()
            };

            // Crear StreamingAssets si es necesario
            Directory.CreateDirectory(
                Application.streamingAssetsPath
            );

            // Convertir los datos a JSON
            string json = JsonUtility.ToJson(data, true);

            // Escribir el archivo
            File.WriteAllText(SavePath, json);

            Debug.Log("PARTIDA GUARDADA CORRECTAMENTE.");

            Debug.Log("Ruta: " + SavePath);

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Error al guardar: " + exception.Message
            );

            return false;
        }
    }

    // =====================================
    // CARGAR PARTIDA
    // =====================================

    public bool LoadGame()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("No existe una partida guardada.");
            return false;
        }

        try
        {
            string json = File.ReadAllText(SavePath);

            SaveData data = JsonUtility.FromJson<SaveData>(json);

            if (data == null ||
                string.IsNullOrWhiteSpace(data.sceneName))
            {
                Debug.LogError("El archivo JSON no es valido.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(data.sceneName))
            {
                Debug.LogError(
                    "La escena guardada no esta disponible: " +
                    data.sceneName
                );

                return false;
            }

            GameManager manager = GameManager.Instance;

            if (manager == null)
            {
                Debug.LogError("No existe GameManager para cargar.");
                return false;
            }

            // Recuperar los datos
            manager.RestoreFromSave(data);

            Debug.Log("PARTIDA CARGADA CORRECTAMENTE.");

            // Abrir la escena guardada
            manager.ChangeScene(data.sceneName);

            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "Error al cargar la partida: " + exception.Message
            );

            return false;
        }
    }

    // =====================================
    // ELIMINAR PARTIDA
    // =====================================

    public void DeleteSave()
    {
        try
        {
            if (HasSaveFile())
            {
                File.Delete(SavePath);

                Debug.Log("Partida guardada eliminada.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "No se pudo eliminar la partida: " +
                exception.Message
            );
        }
    }
}
