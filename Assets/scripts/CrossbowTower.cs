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
        if (target == null || Vector2.Distance(transform.position, target.position) > range)
        {
            UpdateTarget();
        }

        if (target != null)
        {
            if (fireCountdown <= 0f)
            {
                StartShooting(); // Тільки запускаємо анімацію
                fireCountdown = 1f / fireRate;
            }
            fireCountdown -= Time.deltaTime;
        }
    }

    void StartShooting()
    {
        // Якщо є аніматор - кажемо йому "Стріляй!"
        // А стрілу створимо пізніше, коли анімація дійде до потрібного кадру
        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
        else
        {
            // Якщо анімації немає - створюємо стрілу миттєво
            SpawnArrow();
        }
    }

    // Цю функцію викличе Анімація через посередника
    public void SpawnArrow()
    {
        if (projectilePrefab == null || firePoint == null) return;

        GameObject bulletGO = Instantiate(projectilePrefab, firePoint.position, firePoint.rotation);
        Projectile projectile = bulletGO.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Seek(target);
            if (GameManager.Instance != null)
            {
                projectile.damage = GameManager.Instance.globalArrowDamage;
            }
        }
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot);
    }

    // (Функція UpdateTarget залишається без змін...)
    void UpdateTarget()
    {
        Cart[] carts = FindObjectsByType<Cart>(FindObjectsSortMode.None);
        Guard[] guards = FindObjectsByType<Guard>(FindObjectsSortMode.None);
        float shortestDistance = Mathf.Infinity;
        Transform nearestEnemy = null;

        foreach (Cart enemy in carts)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < shortestDistance) { shortestDistance = dist; nearestEnemy = enemy.transform; }
        }
        foreach (Guard enemy in guards)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < shortestDistance) { shortestDistance = dist; nearestEnemy = enemy.transform; }
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