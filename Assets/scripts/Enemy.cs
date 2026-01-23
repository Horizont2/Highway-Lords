using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int maxHealth = 30;
    int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    // Цей метод викликає Стріла, коли влучає
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        // Анімація отримання шкоди (опціонально)
        // GetComponent<Animator>().SetTrigger("Hurt");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Ворог помер!");
        
        // Тут можна додати анімацію смерті або випадіння монет
        // GetComponent<Animator>().SetTrigger("Death");
        
        // Знищуємо об'єкт ворога
        Destroy(gameObject); 
    }
}