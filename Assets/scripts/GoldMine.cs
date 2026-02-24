using UnityEngine;
using System.Collections;

public class GoldMine : MonoBehaviour
{
    [Header("Налаштування")]
    public float productionInterval = 5f; // Як часто дає золото (секунди)
    public int baseGoldAmount = 10;       // Базовий дохід на 1 рівні
    public int increasePerLevel = 5;      // +5 золота за кожен наступний рівень

    [Header("Візуал")]
    public GameObject miningEffect; // (Опціонально) Партикли при видобутку

    private void Start()
    {
        StartCoroutine(ProductionRoutine());
    }

    IEnumerator ProductionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(productionInterval);

            if (GameManager.Instance != null)
            {
                // Отримуємо поточний рівень шахти з менеджера
                int currentLevel = GameManager.Instance.mineLevel;
                
                // Якщо раптом рівень 0 (помилка), вважаємо як 1
                if (currentLevel < 1) currentLevel = 1;

                // Формула: База + (Рівень - 1) * Приріст
                int amount = baseGoldAmount + ((currentLevel - 1) * increasePerLevel);

                // Додаємо золото
                GameManager.Instance.AddResource(ResourceType.Gold, amount);
                GameManager.Instance.ShowResourcePopup(ResourceType.Gold, amount, transform.position);

                // Ефект (якщо є)
                if (miningEffect != null) Instantiate(miningEffect, transform.position, Quaternion.identity);
            }
        }
    }
}