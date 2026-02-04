using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("Налаштування")]
    public float speed = 9f;
    public int damage = 10;
    public float lifeTime = 4f;

    private Vector3 moveDirection;
    private bool isInitialized = false;

    public void Initialize(Vector3 targetPos, int dmg)
    {
        damage = dmg;
        moveDirection = (targetPos - transform.position).normalized;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        isInitialized = true;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (!isInitialized) return;
        transform.Translate(moveDirection * speed * Time.deltaTime, Space.World);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Ігноруємо інші ворожі стріли та самих ворогів
        if (hitInfo.CompareTag("Projectile") || hitInfo.CompareTag("Enemy")) return;

        // === ВАЖЛИВО: Знищення об границі ===
        if (hitInfo.CompareTag("Boundary"))
        {
            Destroy(gameObject);
            return;
        }

        // 1. Лицар
        if (hitInfo.TryGetComponent<Knight>(out Knight knight)) 
        { 
            knight.TakeDamage(damage); 
            Destroy(gameObject); 
            return; 
        }

        // 2. Лучник
        if (hitInfo.TryGetComponent<Archer>(out Archer archer)) 
        { 
            archer.TakeDamage(damage); 
            Destroy(gameObject); 
            return; 
        }

        // 3. Замок
        if (hitInfo.TryGetComponent<Castle>(out Castle castle)) 
        { 
            castle.TakeDamage(damage); 
            Destroy(gameObject); 
            return; 
        }

        // 4. Земля
        if (hitInfo.CompareTag("Ground")) { Destroy(gameObject); }
    }
}