using UnityEngine;

public class Spikes : MonoBehaviour
{
    [Header("Характеристики")]
    public int maxHealth = 300;
    public int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        
        // Реєструємося в менеджері, щоб всі знали, що колючки існують
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentSpikes = this;
            GameManager.Instance.UpdateUI(); // Оновлюємо кнопки (блокуємо кнопку будівництва)
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ShowDamage(damage, transform.position);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Повідомляємо менеджеру, що колючок більше немає
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentSpikes = null;
            GameManager.Instance.UpdateUI(); // Розблокуємо кнопку будівництва
        }

        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.woodBreak); 

        Destroy(gameObject);
    }
}