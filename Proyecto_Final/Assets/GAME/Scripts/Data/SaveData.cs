
using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string sceneName;

    public int currentHealth;

    public List<string> inventory = new List<string>();

    // Identificadores unicos de puntos de recogida ya utilizados.
    public List<string> collectedPickupIds = new List<string>();

    public List<TerminalSaveData> terminals =
        new List<TerminalSaveData>();
}

[Serializable]
public class TerminalSaveData
{
    public string terminalId;
    public bool activated;
}
