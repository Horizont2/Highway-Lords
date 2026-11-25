using UnityEngine;

public class RainArrow : MonoBehaviour, IPoolable
{
    public int damage = 50; 
    public float speed = 15f;
    
    /// <summary>
    /// Час життя рахуємо тут, а не в Start: Start виконується лише при
    /// першому створенні, а пул віддає екземпляр багато разів.
    /// </summary>
    public void OnSpawnFromPool()
    {
        PoolRegistry.DespawnAfter(gameObject, 3.0f);
    }

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            if (other.TryGetComponent<Guard>(out Guard g)) g.TakeDamage(damage);
            else if (other.TryGetComponent<EnemyArcher>(out EnemyArcher a)) a.TakeDamage(damage);
            else if (other.TryGetComponent<EnemyHorse>(out EnemyHorse h)) h.TakeDamage(damage);
            else if (other.TryGetComponent<EnemySpearman>(out EnemySpearman s)) s.TakeDamage(damage);

            PoolRegistry.Despawn(gameObject);
        }
        else if (other.CompareTag("Ground"))
        {
            PoolRegistry.Despawn(gameObject);
        }
    }
}