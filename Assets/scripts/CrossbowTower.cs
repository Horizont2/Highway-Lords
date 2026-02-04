using UnityEngine;

public class CrossbowTower : MonoBehaviour
{
    [Header("Параметри стрільби")]
    public float range = 10f;
    public float fireRate = 1f;
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
            
            // Перевіряємо, чи ціль підходить (жива, активна, ворог, в радіусі)
            if (manual != null && 
                manual.gameObject.activeInHierarchy && 
                !manual.CompareTag("Untagged") && 
                Vector2.Distance(transform.position, manual.position) <= range)
            {
                target = manual;
            }
            else
            {
                // Якщо ручна ціль не підходить — шукаємо найближчу автоматично
                UpdateTarget();
            }
        }
        else
        {
            // Якщо ручної цілі немає — працюємо в автоматичному режимі
            
            // Перевіряємо поточну ціль: якщо вона зникла, померла або вийшла за радіус
            if (target == null || 
                !target.gameObject.activeInHierarchy || 
                target.CompareTag("Untagged") || 
                Vector2.Distance(transform.position, target.position) > range)
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
                fireCountdown = 1f / fireRate;
            }
            fireCountdown -= Time.deltaTime;
        }
    }

    void Shoot()
    {
        if (projectilePrefab != null && firePoint != null && target != null)
        {
            // Створюємо стрілу (без повороту, вона сама повернеться)
            GameObject bulletGO = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            
            // Отримуємо скрипт снаряда
            Projectile projectile = bulletGO.GetComponent<Projectile>();

            if (projectile != null)
            {
                int damage = 30; 
                
                if (GameManager.Instance != null)
                {
                    damage = GameManager.Instance.GetTowerDamage();
                }

                // ВАЖЛИВО: Передаємо Transform (target) для самонаведення
                projectile.Initialize(target, damage);
            }
        }

        if (animator != null) animator.SetTrigger("Shoot");
        
        if (SoundManager.Instance != null) 
            SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot); 
    }

    void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            // Ігноруємо мертвих або тих, що без тегу
            if (enemy.CompareTag("Untagged")) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            
            if (dist < shortestDistance) 
            { 
                shortestDistance = dist; 
                nearestEnemy = enemy.transform; 
            }
        }

        // Якщо знайшли ворога і він у радіусі дії
        if (nearestEnemy != null && shortestDistance <= range) 
        {
            target = nearestEnemy;
        }
        else 
        {
            target = null;
        }
    }

    // Малює радіус атаки в редакторі Unity
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}