using UnityEngine;

public class RainArrow : MonoBehaviour
{
    public int damage = 50; 
    public float speed = 15f;
    
    void Start()
    {
        // Знищити стрілу через 3 секунди, якщо вона нікуди не влучила (на всяк випадок)
        Destroy(gameObject, 3.0f);
    }

    void Update()
    {
        // Рух стріли вперед (куди вона дивиться)
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Влучили у ВОРОГА
        if (other.CompareTag("Enemy"))
        {
            // Наносимо урон (тут твій код нанесення урону)
            if (other.TryGetComponent<Guard>(out Guard g)) g.TakeDamage(damage);
            else if (other.TryGetComponent<EnemyArcher>(out EnemyArcher a)) a.TakeDamage(damage);
            else if (other.TryGetComponent<EnemyHorse>(out EnemyHorse h)) h.TakeDamage(damage);
            else if (other.TryGetComponent<EnemySpearman>(out EnemySpearman s)) s.TakeDamage(damage);
            else if (other.TryGetComponent<Cart>(out Cart c)) c.TakeDamage(damage);
            
            // Ефект/Звук можна додати тут

            Destroy(gameObject); // Знищуємо стрілу
        }
        // 2. === НОВЕ: Влучили в ЗЕМЛЮ ===
        else if (other.CompareTag("Ground"))
        {
            // Тут можна додати ефект пилу або звук втикання в землю
            Destroy(gameObject);
        }
    }
}