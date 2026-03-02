using UnityEngine;
using System.Collections.Generic;

public class WallArcherManager : MonoBehaviour
{
    [Header("Налаштування Лучників")]
    public GameObject wallArcherPrefab; // Префаб лучника для стіни
    public Transform[] archerSlots;     // ТЕПЕР ТУТ МАЄ БУТИ 5 ПУСТИХ ОБ'ЄКТІВ

    [Header("Економіка (Лучники)")]
    public int currentLevel = 1;
    public int baseUpgradeCost = 100;
    public float costMultiplier = 1.3f;

    [Header("Поточний стан")]
    public int activeSlotsCount = 2; // Базово 2 лучники на старті
    private List<GameObject> spawnedArchers = new List<GameObject>();

    void Start()
    {
        // Завантаження рівня з PlayerPrefs
        currentLevel = PlayerPrefs.GetInt("WallArcherLevel", 1);
        CalculateActiveSlots();
        SpawnArchers();
    }

    public int GetUpgradeCost()
    {
        return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(costMultiplier, currentLevel - 1));
    }

    public void UpgradeWallArchers()
    {
        currentLevel++;
        PlayerPrefs.SetInt("WallArcherLevel", currentLevel);
        
        CalculateActiveSlots();
        SpawnArchers(); // Оновлюємо кількість і силу
        
        // Тут можна додати звук або ефект апгрейду
    }

    void CalculateActiveSlots()
    {
        // Базово 2 слоти. 
        // 3-й слот відкривається на 5 рівні, 4-й на 10-му, 5-й на 15-му.
        if (currentLevel >= 5)
        {
            // Формула: 2 базових + 1 за кожні повні 5 рівнів
            activeSlotsCount = 2 + (currentLevel / 5);
        }
        else
        {
            activeSlotsCount = 2;
        }

        // Жорстко обмежуємо максимум до 5 (або до розміру масиву, якщо ви додали менше точок)
        int maxAllowed = Mathf.Min(5, archerSlots.Length);
        activeSlotsCount = Mathf.Clamp(activeSlotsCount, 2, maxAllowed);
    }

    public void SpawnArchers()
    {
        // Видаляємо старих
        foreach (var archer in spawnedArchers)
        {
            if (archer != null) Destroy(archer);
        }
        spawnedArchers.Clear();

        // Спавнимо нових відповідно до кількості активних слотів
        for (int i = 0; i < activeSlotsCount; i++)
        {
            if (i < archerSlots.Length && archerSlots[i] != null)
            {
                GameObject newArcher = Instantiate(wallArcherPrefab, archerSlots[i].position, Quaternion.identity);
                newArcher.transform.SetParent(archerSlots[i]); // Робимо дочірнім
                
                // Передаємо їм поточний рівень (для розрахунку урону)
                WallArcher logic = newArcher.GetComponent<WallArcher>();
                if (logic != null)
                {
                    logic.Initialize(currentLevel);
                }
                
                spawnedArchers.Add(newArcher);
            }
        }
    }
}