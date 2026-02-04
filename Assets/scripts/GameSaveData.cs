using System;
using System.Collections.Generic;

[Serializable]
public class UnitSaveData
{
    public string unitType; // "Knight" або "Archer"
    public float posX;
    public float posY;
    public int currentHealth;
}

[Serializable]
public class GameSaveData
{
    public List<UnitSaveData> units = new List<UnitSaveData>();
}