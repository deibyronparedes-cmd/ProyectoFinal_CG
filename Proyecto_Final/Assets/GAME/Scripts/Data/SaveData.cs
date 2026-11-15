
using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public string sceneName;

    public int currentHealth;

    public List<string> inventory = new List<string>();

    public List<TerminalSaveData> terminals =
        new List<TerminalSaveData>();
}

[Serializable]
public class TerminalSaveData
{
    public string terminalId;
    public bool activated;
}
