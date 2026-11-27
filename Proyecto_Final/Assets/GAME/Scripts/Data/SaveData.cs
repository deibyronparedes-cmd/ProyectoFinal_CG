using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int saveVersion = 2;

    // Escena que debe abrir Continuar e identificador del último hito.
    public string sceneName;
    public string checkpointId;
    public int currentHealth;

    // Contenido del inventario y puntos de recogida ya utilizados.
    public List<string> inventory = new List<string>();
    public List<string> collectedPickupIds = new List<string>();

    // El Dictionary se serializa como pares clave/valor.
    public List<TerminalSaveData> terminals = new List<TerminalSaveData>();

    // Solo se serializa el contenido. Al cargar se reconstruyen Queue y Stack reales.
    public List<string> stationQueue = new List<string>();
    public List<string> stationHistoryStack = new List<string>();

    // Objetivos completados y estadísticas acumuladas.
    public List<string> completedObjectives = new List<string>();
    public GameStatisticsSaveData statistics = new GameStatisticsSaveData();
}

[Serializable]
public class TerminalSaveData
{
    public string terminalId;
    public bool activated;
}

[Serializable]
public class GameStatisticsSaveData
{
    public int totalItemsCollected;
    public int totalDamageTaken;
    public int totalTerminalsActivated;
}
