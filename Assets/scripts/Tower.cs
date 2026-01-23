using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Налаштування")]
    public float range = 5f;       
    public float fireRate = 1f;    
    private float fireCountdown = 0f;

    [Header("Об'єкти")]
    public GameObject bulletPrefab; 
    public Transform firePoint;     
    public Animator animator;       

    private Transform target;

    void Start()
    {
        InvokeRepeating("UpdateTarget", 0f, 0.5f);
    }

    void UpdateTarget()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float shortestDistance = Mathf.Infinity;
        GameObject nearestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distanceToEnemy = Vector2.Distance(transform.position, enemy.transform.position);
            if (distanceToEnemy < shortestDistance)
            {
                shortestDistance = distanceToEnemy;
                nearestEnemy = enemy;
            }
        }

        if (nearestEnemy != null && shortestDistance <= range)
        {
            target = nearestEnemy.transform;
        }
        else
        {
            target = null;
        }
    }

    void Update()
    {
        if (target == null) return;

        // Я ПРИБРАВ ЗВІДСИ КОД ПОВОРОТУ (Mathf.Atan2)
        // Тепер вежа просто чекає таймера

        if (fireCountdown <= 0f)
        {
            StartShooting(); 
            fireCountdown = 1f / fireRate;
        }

        fireCountdown -= Time.deltaTime;
    }

    void StartShooting()
    {
        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
    }

    // Цю функцію викликає Animation Event
    public void SpawnArrow()
    {
        if (target == null) return; 

        // Створюємо стрілу (вона сама повернеться до ворога завдяки своєму скрипту Projectile)
        GameObject bulletGO = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Projectile bullet = bulletGO.GetComponent<Projectile>();

        if (bullet != null)
        {
            if (GameManager.Instance != null)
            {
                bullet.damage = GameManager.Instance.globalArrowDamage;
            }
            bullet.Seek(target);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}