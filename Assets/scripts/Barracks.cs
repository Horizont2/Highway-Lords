using UnityEngine;

public class Barracks : MonoBehaviour
{
    // Цей скрипт тепер просто "мітка" та калькулятор ціни
    public int baseUpgradeCost = 100;
    public int costIncreasePerLevel = 50;
    
    // Скільки разів ми вже купили розширення (можна зберігати в GameManager, 
    // але для розрахунку ціни хай буде тут або будемо брати з ліміту)
    
    public int GetUpgradeCost(int currentMaxUnits)
    {
        // Формула: 100 + ((ПоточнийЛіміт - 5) * 50)
        // Якщо починали з 5, то перше покращення коштує 100.
        int upgradesCount = currentMaxUnits - 5; 
        if (upgradesCount < 0) upgradesCount = 0;
        
        return baseUpgradeCost + (upgradesCount * costIncreasePerLevel);
    }
}