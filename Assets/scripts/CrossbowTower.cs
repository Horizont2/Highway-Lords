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
        // 1. ВИБІР ЦІЛІ
        if (GameManager.Instance != null && GameManager.Instance.manualTarget != null)
        {
            Transform manual = GameManager.Instance.manualTarget;
            if (IsValidTarget(manual)) target = manual;
            else UpdateTarget();
        }
        else
        {
            if (!IsValidTarget(target)) UpdateTarget();
        }

        // 2. СТРІЛЬБА ТА ПЕРЕЗАРЯДКА
        if (target != null)
        {
            if (fireCountdown <= 0f)
            {
                Shoot();
                if (GameManager.Instance != null)
                {
                    fireCountdown = GameManager.Instance.GetCrossbowReloadTime();
                }
                else 
                {
                    fireCountdown = fallbackReloadTime;
                }
            }
        }

        if (fireCountdown > 0f)
        {
            fireCountdown -= Time.deltaTime;
        }
    }

    bool IsValidTarget(Transform t)
    {
        if (t == null) return false;
        if (!t.gameObject.activeInHierarchy) return false;
        if (t.CompareTag("Untagged")) return false;
        
        float dist = Vector2.Distance(transform.position, t.position);
        return dist <= range;
    }

    void Shoot()
    {
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject projGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
            
            int damage = 25; 
            if (GameManager.Instance != null)
            {
                damage = GameManager.Instance.GetCrossbowDamage();
            }

            // ФІКС: Використовуємо правильні класи снарядів
            if (projGO.TryGetComponent<Projectile>(out Projectile proj))
            {
                proj.Initialize(target.position, damage);
            }
            else if (projGO.TryGetComponent<Arrow>(out Arrow arrowScript))
            {
                arrowScript.Initialize(target, damage);
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
            if (enemy.CompareTag("Untagged")) continue;

            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            
            if (dist < shortestDistance) 
            { 
                shortestDistance = dist; 
                nearestEnemy = enemy.transform; 
            }
        }

        if (nearestEnemy != null && shortestDistance <= range) target = nearestEnemy;
        else target = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}