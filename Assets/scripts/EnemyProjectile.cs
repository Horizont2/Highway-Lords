using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    private Vector3 startPosition;
    private int damage;
    private float maxRange = 15f; 
    private float speed = 10f; 

    public void Initialize(Vector3 targetPos, int dmg)
    {
        damage = dmg;
        startPosition = transform.position;

        Vector3 direction = (targetPos - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * speed; 
        }

        // === ВАЖЛИВА ЗМІНА: Знищити через 1.5 секунди ===
        Destroy(gameObject, 1.0f);
    }

    void Update()
    {
        // Додаткова перевірка дальності (якщо раптом 1.5 сек не вистачить)
        if (Vector3.Distance(startPosition, transform.position) > maxRange)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // Ігноруємо ворогів (своїх) та інші стріли
        if (collision.CompareTag("Enemy") || collision.CompareTag("Projectile")) return;

        bool hit = false;

        // 1. Влучили в Барикаду (Spikes)
        if (collision.TryGetComponent<Spikes>(out Spikes spikes))
        {
            spikes.TakeDamage(damage);
            hit = true; 
        }
        // 2. Влучили в Замок
        else if (collision.TryGetComponent<Castle>(out Castle c))
        {
            c.TakeDamage(damage);
            hit = true;
        }
        // 3. Влучили в Юнітів гравця
        else if (collision.TryGetComponent<Knight>(out Knight k))
        {
            k.TakeDamage(damage);
            hit = true;
        }
        else if (collision.TryGetComponent<Archer>(out Archer a))
        {
            a.TakeDamage(damage);
            hit = true;
        }
        else if (collision.TryGetComponent<Spearman>(out Spearman s))
        {
            s.TakeDamage(damage);
            hit = true;
        }
        // 4. Влучили в землю або межі карти
        else if (collision.CompareTag("Ground") || collision.CompareTag("Boundary"))
        {
            hit = true;
        }

        // Якщо в щось влучили - знищуємо
        if (hit) Destroy(gameObject);
    }
}