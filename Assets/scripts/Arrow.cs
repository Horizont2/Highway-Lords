using UnityEngine;

public class Arrow : MonoBehaviour
{
    [Header("Налаштування")]
    public float speed = 15f; // Трохи збільшив швидкість для точності
    public int damage = 10;
    public float lifeTime = 5f; // Збільшив час життя

    private Transform target; // Тепер зберігаємо посилання на ворога
    private bool isInitialized = false;

    // ЗМІНА: Приймаємо Transform замість Vector3
    public void Initialize(Transform enemyTarget, int dmg)
    {
        damage = dmg;
        target = enemyTarget;
        isInitialized = true;

        // Гарантоване знищення через час життя (якщо ціль буде бігати колами)
        Destroy(gameObject, lifeTime); 
    }

    void Update()
    {
        if (!isInitialized) return;

        // Якщо ціль зникла, померла або стала неактивною — знищуємо стрілу
        if (target == null || !target.gameObject.activeInHierarchy || target.CompareTag("Untagged"))
        {
            Destroy(gameObject);
            return;
        }

        // 1. Постійно оновлюємо напрямок до рухомої цілі
        Vector3 direction = (target.position - transform.position).normalized;

        // 2. Повертаємо стрілу до ворога
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 3. Рухаємось до поточної позиції ворога (MoveTowards точніше для самонаведення)
        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Ігноруємо колізії зі своїми та іншими стрілами
        if (hitInfo.CompareTag("Projectile")) return;
        if (hitInfo.CompareTag("Player") || hitInfo.CompareTag("PlayerUnit")) return;

        // Знищення об границі екрану
        if (hitInfo.CompareTag("Boundary"))
        {
            Destroy(gameObject);
            return;
        }

        // Влучання у ворога
        if (hitInfo.CompareTag("Enemy"))
        {
            bool hit = false; 

            if (hitInfo.TryGetComponent<Guard>(out Guard g)) 
            { 
                g.TakeDamage(damage); hit = true; 
            }
            else if (hitInfo.TryGetComponent<Cart>(out Cart c)) 
            { 
                c.TakeDamage(damage); hit = true; 
            }
            else if (hitInfo.TryGetComponent<EnemyArcher>(out EnemyArcher ea)) 
            { 
                ea.TakeDamage(damage); hit = true; 
            }
            else if (hitInfo.TryGetComponent<Boss>(out Boss b)) 
            { 
                b.TakeDamage(damage); hit = true; 
            }

            // Якщо влучили - знищуємо стрілу
            if (hit) Destroy(gameObject);
        }
        
        // Влучання в землю (якщо промахнулись або зачепили декорації)
        if (hitInfo.gameObject.layer == LayerMask.NameToLayer("Ground") || hitInfo.CompareTag("Ground")) 
        {
            Destroy(gameObject);
        }
    }
}