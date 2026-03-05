using UnityEngine;

public class RainArrow : MonoBehaviour
{
    public int damage = 50; 
    public float speed = 15f;
    
    void Start()
    {
        // === НОВЕ: Місце для бонусу урону від кристалів ===
        /*
        if (BonusManager.Instance != null)
        {
            damage += BonusManager.Instance.GetExtraVolleyDamage();
        }
        */
        Destroy(gameObject, 3.0f);
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
            
            Destroy(gameObject); 
        }
        else if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}