using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Налаштування")]
    public int baseHealth = 30; // Базове здоров'я
    
    // === ВАЖЛИВО: Це змінна, яку шукає EnemySpawner для розрахунку ===
    public int goldReward = 15; 
    
    private int currentHealth;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            // 1. Отримуємо здоров'я залежно від хвилі
            currentHealth = GameManager.Instance.GetDifficultyHealth();

            // === НОВЕ: Оновлюємо нагороду відповідно до прогресії гри ===
            // (Щоб на 10-й хвилі давало більше золота, ніж на 1-й)
            goldReward = GameManager.Instance.GetGoldReward();

            // 2. Реєструємо ворога
            GameManager.Instance.RegisterEnemy();
        }
        else
        {
            currentHealth = baseHealth;
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // 3. Показуємо цифру урону
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowDamage(damage, transform.position);
        }
        
        // 4. Звук отримання удару
        if (SoundManager.Instance != null) 
             SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // 5. Повідомляємо менеджеру, що ворог помер
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy();

            // 6. Даємо гравцю золото (використовуємо нашу змінну)
            GameManager.Instance.AddResource(ResourceType.Gold, goldReward);
            
            // Показуємо іконку +монетки
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldReward, transform.position);
        }

        // 7. Звук смерті
        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);

        Destroy(gameObject); 
    }
}