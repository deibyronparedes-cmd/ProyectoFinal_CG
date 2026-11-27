using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    private const string FileName = "savegame.json";

    public string SavePath => Path.Combine(Application.streamingAssetsPath, FileName);

    public bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    /// <summary>
    /// Guarda el contenido de la partida en StreamingAssets.
    /// Si se indica una escena destino, el checkpoint por defecto será esa escena.
    /// Si se guarda la escena actual sin indicar destino, conserva el checkpoint actual.
    /// </summary>
    public bool SaveGame(string sceneName = null, string checkpointId = null)
    {
        GameManager manager = GameManager.Instance;
        if (manager == null)
        {
            Debug.LogError("No existe GameManager.");
            return false;
        }

        try
        {
            bool explicitScene = !string.IsNullOrWhiteSpace(sceneName);
            string targetScene = explicitScene ? sceneName : SceneManager.GetActiveScene().name;

            if (string.IsNullOrWhiteSpace(checkpointId))
            {
                checkpointId = explicitScene
                    ? targetScene
                    : manager.CurrentCheckpointId;
            }

            if (string.IsNullOrWhiteSpace(checkpointId))
                checkpointId = targetScene;

            manager.SetCheckpoint(checkpointId);

            SaveData data = new SaveData
            {
                saveVersion = 2,
                sceneName = targetScene,
                checkpointId = manager.CurrentCheckpointId,
                currentHealth = manager.CurrentHealth,
                inventory = new System.Collections.Generic.List<string>(manager.Inventory),
                collectedPickupIds = new System.Collections.Generic.List<string>(manager.CollectedPickupIds),
                terminals = manager.GetTerminalSaveData(),
                stationQueue = manager.GetStationQueueSaveData(),
                stationHistoryStack = manager.GetStationHistorySaveData(),
                completedObjectives = manager.GetCompletedObjectivesSaveData(),
                statistics = manager.GetStatisticsSaveData()
            };

            Directory.CreateDirectory(Application.streamingAssetsPath);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);

            Debug.Log("PARTIDA GUARDADA CORRECTAMENTE.");
            Debug.Log("Ruta: " + SavePath);
            Debug.Log("Queue guardada: " + data.stationQueue.Count +
                      " | Stack guardado: " + data.stationHistoryStack.Count +
                      " | Objetivos: " + data.completedObjectives.Count);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("Error al guardar: " + exception.Message);
            return false;
        }
    }

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

            if (data == null || string.IsNullOrWhiteSpace(data.sceneName))
            {
                Debug.LogError("El archivo JSON no es válido.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(data.sceneName))
            {
                Debug.LogError("La escena guardada no está disponible: " + data.sceneName);
                return false;
            }

            GameManager manager = GameManager.Instance;
            if (manager == null)
            {
                Debug.LogError("No existe GameManager para cargar.");
                return false;
            }

            // Reconstruye inventario, Dictionary, Queue, Stack y estadísticas antes de cargar escena.
            manager.RestoreFromSave(data);
            Debug.Log("PARTIDA CARGADA CORRECTAMENTE. Checkpoint: " + data.checkpointId);
            manager.ChangeScene(data.sceneName);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError("Error al cargar: " + exception.Message);
            return false;
        }
    }

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
            Debug.LogError("No se pudo eliminar la partida: " + exception.Message);
        }
    }
}
