using UnityEngine;

public class CrossbowTower : MonoBehaviour
{
    [Header("Параметри стрільби (Базові)")]
    public float range = 10f;
    [Tooltip("Використовується тільки якщо GameManager відсутній")]
    public float fallbackReloadTime = 2.5f; 
    private float fireCountdown = 0f;

    [Header("Налаштування Unity")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public Animator animator;

    private Transform target;

    void Update()
    {
        // === 1. ВИБІР ЦІЛІ ===
        
        // Спочатку перевіряємо, чи є ручна ціль від гравця
        if (GameManager.Instance != null && GameManager.Instance.manualTarget != null)
        {
            Transform manual = GameManager.Instance.manualTarget;
            
            // Перевіряємо, чи ручна ціль валідна
            if (IsValidTarget(manual))
            {
                target = manual;
            }
            else
            {
                // Ручна ціль померла або вийшла з радіусу -> шукаємо найближчу
                UpdateTarget();
            }
        }
        else
        {
            // Автоматичний режим
            // Якщо поточної цілі немає АБО вона стала невалідною (померла/вийшла)
            if (!IsValidTarget(target))
            {
                UpdateTarget();
            }
        }

        // === 2. СТРІЛЬБА ===
        if (target != null)
        {
            if (fireCountdown <= 0f)
            {
                Shoot(); 
                
                // === НОВЕ: Беремо час перезарядки з GameManager ===
                if (GameManager.Instance != null)
                {
                    fireCountdown = GameManager.Instance.GetCrossbowReloadTime();
                }
                else
                {
                    fireCountdown = fallbackReloadTime;
                }
            }
            fireCountdown -= Time.deltaTime;
        }
    }

    // Допоміжна функція для перевірки цілі (щоб не дублювати код)
    bool IsValidTarget(Transform t)
    {
        if (t == null) return false;
        if (!t.gameObject.activeInHierarchy) return false;
        if (t.CompareTag("Untagged")) return false; // Якщо вже труп
        if (Vector2.Distance(transform.position, t.position) > range) return false;
        
        return true;
    }

    void Shoot()
    {
        // Контрольна перевірка перед пострілом.
        // Якщо за час перезарядки ціль померла — скасовуємо постріл.
        if (!IsValidTarget(target))
        {
            target = null;
            return; // Не стріляємо!
        }

        if (projectilePrefab != null && firePoint != null)
        {
            GameObject bulletGO = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile projectile = bulletGO.GetComponent<Projectile>();

            if (projectile != null)
            {
                int damage = 25; 
                // === НОВЕ: Беремо урон для Арбалетної башні ===
                if (GameManager.Instance != null)
                {
                    damage = GameManager.Instance.GetCrossbowDamage();
                }

                projectile.Initialize(target, damage);
            }
        }

        if (animator != null) animator.SetTrigger("Shoot");
        
        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot); 
    }

    void UpdateTarget()
    {
        // Оптимізація: шукаємо тільки ворогів
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            // Додаткова перевірка, про всяк випадок
            if (enemy.CompareTag("Untagged")) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            
            if (dist < shortestDistance) 
            { 
                shortestDistance = dist; 
                nearestEnemy = enemy.transform; 
            }
        }

        if (nearestEnemy != null && shortestDistance <= range) 
        {
            target = nearestEnemy;
        }
        else 
        {
            target = null;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}