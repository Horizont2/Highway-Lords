using UnityEngine;

public class Barracks : MonoBehaviour
{
    public int baseUpgradeCost = 100;
    
    public int GetUpgradeCost(int currentMaxUnits)
    {
        int upgradesCount = currentMaxUnits - 5; 
        if (upgradesCount < 0) upgradesCount = 0;
        
        // === НОВА ЕКОНОМІКА ===
        return EconomyConfig.GetUpgradeCost(baseUpgradeCost, upgradesCount + 1);
    }
}