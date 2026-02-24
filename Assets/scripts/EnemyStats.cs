using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Економіка")]
    public int baseGoldReward = 15; // Базова ціна (для 1-ї хвилі)
    
    // Скільки реально дадуть за ворога (приховано в інспекторі, бо рахується автоматично)
    [HideInInspector] public int currentGoldReward; 

    void Start()
    {
        if (GameManager.Instance != null)
        {
            // === ФОРМУЛА ЗРОСТАННЯ ЦІНИ ===
            // Ціна = База * (Множник ^ НомерХвилі)
            // Наприклад: 15 * (1.08 ^ 5) = ~22 монети на 5-й хвилі
            
            float growthFactor = GameManager.Instance.goldRewardGrowth; // Наприклад, 1.08
            int wave = GameManager.Instance.currentWave;

            // Якщо це перша хвиля, множник 1. Для наступних - зростає.
            float multiplier = Mathf.Pow(growthFactor, wave - 1);
            
            currentGoldReward = Mathf.RoundToInt(baseGoldReward * multiplier);
        }
        else
        {
            // Якщо менеджера немає (тест сцени), беремо базу
            currentGoldReward = baseGoldReward;
        }
    }

    public void GiveGold()
    {
        if (GameManager.Instance != null)
        {
            // Нараховуємо вже перераховану (збільшену) суму
            GameManager.Instance.AddResource(ResourceType.Gold, currentGoldReward);
            
            // Показуємо +Gold над головою
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, currentGoldReward, transform.position);
            
            // Знімаємо з обліку хвилі
            GameManager.Instance.UnregisterEnemy();
        }
    }
}