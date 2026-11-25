using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    [Header("Економіка")]
    public int baseGoldReward = 15; // Базова ціна
    
    [HideInInspector] public int currentGoldReward; 

    [Header("Здоров'я")]
    public int maxHealth = 50;
    public int currentHealth;

    void Start()
    {
        // === НОВА ЕКОНОМІКА ТА БАЛАНС ===
        if (GameManager.Instance != null)
        {
            int wave = GameManager.Instance.currentWave;
            currentGoldReward = EconomyConfig.GetEnemyGoldDrop(baseGoldReward, wave);
            
            // ВАЖЛИВО: Тепер передаємо БАЗОВЕ здоров'я конкретного ворога,
            // щоб GameManager міг правильно його зменшити для 1-ї хвилі
            maxHealth = GameManager.Instance.GetScaledEnemyHealth(maxHealth);
        }
        else
        {
            currentGoldReward = baseGoldReward;
        }

        currentHealth = maxHealth; // Встановлюємо поточне здоров'я
    }

    public void GiveGold()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(ResourceType.Gold, currentGoldReward);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, currentGoldReward, transform.position);
            GameManager.Instance.UnregisterEnemy();
        }
    }
}