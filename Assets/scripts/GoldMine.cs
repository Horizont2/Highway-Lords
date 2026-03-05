using UnityEngine;
using System.Collections;

public class GoldMine : MonoBehaviour
{
    [Header("Налаштування")]
    public float productionInterval = 5f; // Як часто дає золото (секунди)
    public int baseGoldAmount = 10;       // Базовий дохід

    [Header("Візуал")]
    public GameObject miningEffect; // Партикли при видобутку

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
                int currentLevel = GameManager.Instance.mineLevel;
                if (currentLevel < 1) currentLevel = 1;

                // === НОВА ЕКОНОМІКА: Квадратичне зростання доходу ===
                // База + (Рівень * 10) + (Рівень^2 * 0.2)
                int amount = Mathf.RoundToInt(baseGoldAmount + (currentLevel * 10f) + (Mathf.Pow(currentLevel, 2) * 0.2f));

                // Місце для майбутнього бонусу за кристали (Mine Income)
                /*
                if (BonusManager.Instance != null) 
                {
                    amount = Mathf.RoundToInt(amount * BonusManager.Instance.GetMineIncomeMultiplier());
                }
                */

                // Додаємо золото
                GameManager.Instance.AddResource(ResourceType.Gold, amount);
                GameManager.Instance.ShowResourcePopup(ResourceType.Gold, amount, transform.position);

                // Ефект
                if (miningEffect != null) Instantiate(miningEffect, transform.position, Quaternion.identity);
            }
        }
    }
}