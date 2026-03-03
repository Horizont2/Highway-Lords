using UnityEngine;
using System.Collections.Generic;

public class WallArcherManager : MonoBehaviour
{
    [Header("Налаштування Лучників")]
    public GameObject wallArcherPrefab; // Префаб лучника для стіни
    public Transform[] archerSlots;     // 5 ПУСТИХ ОБ'ЄКТІВ НА СТІНІ

    [Header("Поточний стан")]
    public int activeSlotsCount = 2; // Базово 2 лучники на старті
    private List<GameObject> spawnedArchers = new List<GameObject>();
    private int lastKnownLevel = -1;

    void Update()
    {
        // Перевіряємо, чи змінився рівень у GameManager
        if (GameManager.Instance != null && lastKnownLevel != GameManager.Instance.wallArcherLevel)
        {
            lastKnownLevel = GameManager.Instance.wallArcherLevel;
            CheckAndSpawnArchers();
            
            // Про всяк випадок кажемо всім живим лучникам оновити свій урон та аніматор
            foreach (var archer in spawnedArchers)
            {
                if (archer != null)
                {
                    WallArcher logic = archer.GetComponent<WallArcher>();
                    if (logic != null) logic.UpdateStats();
                }
            }
        }
    }

    void CalculateActiveSlots()
    {
        // Базово 2 слоти. 3-й відкривається на 5 рівні, 4-й на 10-му, 5-й на 15-му.
        if (lastKnownLevel >= 5)
        {
            activeSlotsCount = 2 + (lastKnownLevel / 5);
        }
        else
        {
            activeSlotsCount = 2;
        }

        // Обмежуємо максимум кількістю точок спавну
        int maxAllowed = Mathf.Min(5, archerSlots.Length);
        activeSlotsCount = Mathf.Clamp(activeSlotsCount, 2, maxAllowed);
    }

    public void CheckAndSpawnArchers()
    {
        CalculateActiveSlots();

        // Спавнимо ТІЛЬКИ тих лучників, яких не вистачає
        while (spawnedArchers.Count < activeSlotsCount)
        {
            int i = spawnedArchers.Count; // Беремо наступний вільний слот
            
            if (i < archerSlots.Length && archerSlots[i] != null)
            {
                GameObject newArcher = Instantiate(wallArcherPrefab, archerSlots[i].position, Quaternion.identity);
                newArcher.transform.SetParent(archerSlots[i]); 
                
                // Кажемо лучнику взяти свої стати з GameManager
                WallArcher logic = newArcher.GetComponent<WallArcher>();
                if (logic != null)
                {
                    logic.UpdateStats();
                }
                
                spawnedArchers.Add(newArcher);
            }
            else
            {
                break; // Якщо слоти закінчилися
            }
        }
    }
}