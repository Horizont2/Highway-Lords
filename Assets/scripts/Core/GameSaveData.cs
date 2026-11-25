using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public List<UnitSaveData> units = new List<UnitSaveData>();
}

[System.Serializable]
public class UnitSaveData
{
    public string unitType;
    public float posX;
    public float posY;
    public int currentHealth;
}