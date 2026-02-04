using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Налаштування")]
    public int baseHealth = 30; // Базове здоров'я (якщо не використовуємо складність)
    private int currentHealth;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            // 1. Отримуємо здоров'я залежно від хвилі (прогресія складності)
            // Якщо хочеш фіксоване - просто напиши: currentHealth = baseHealth;
            currentHealth = GameManager.Instance.GetDifficultyHealth();

            // 2. ВАЖЛИВО: Реєструємо ворога, щоб заблокувати кнопку "Наступна хвиля"
            GameManager.Instance.RegisterEnemy();
        }
        else
        {
            currentHealth = baseHealth;
        }
    }

    // Цей метод викликає Стріла або Лицар
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // 3. Показуємо цифру урону над головою
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowDamage(damage, transform.position);
        }
        
        // 4. Звук отримання удару (опціонально)
        if (SoundManager.Instance != null) 
             SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit); // Або інший звук

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // 5. ВАЖЛИВО: Повідомляємо менеджеру, що ворог помер
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy();

            // 6. Даємо гравцю золото за вбивство
            int goldReward = GameManager.Instance.GetGoldReward();
            GameManager.Instance.AddResource(ResourceType.Gold, goldReward);
            
            // Показуємо іконку +монетки
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldReward, transform.position);
        }

        // 7. Звук смерті
        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);

        // Тут можна додати анімацію смерті перед Destroy, якщо є аніматор
        // GetComponent<Animator>().SetTrigger("Death");
        
        Destroy(gameObject); 
    }
}