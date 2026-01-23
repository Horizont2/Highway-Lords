using UnityEngine;

public class Archer : MonoBehaviour
{
    [Header("Характеристики")]
    public float speed = 3f;
    public float attackRange = 5f;
    public float timeBetweenShots = 1.5f;
    
    [Header("Здоров'я")]
    public int maxHealth = 40; 
    private int currentHealth;

    [Header("Налаштування")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    private Animator animator;
    private Transform target;
    private float nextShotTime;

    void Start()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        
        // ВАЖЛИВО: Ми ПРИБРАЛИ звідси додавання currentUnits++,
        // бо це вже робить GameManager при натисканні кнопки!
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentUnits--;
            GameManager.Instance.UpdateUI();
        }
    }

    void Update()
    {
        FindClosestEnemy();

        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance > attackRange)
            {
                MoveTowardsEnemy();
                animator.SetBool("IsRunning", true);
                // Скидаємо поворот точки стрільби, коли біжимо
                firePoint.localRotation = Quaternion.identity;
            }
            else
            {
                StopMoving();
                animator.SetBool("IsRunning", false);
                
                // === ПРИЦІЛЮВАННЯ ===
                AimAtTarget();

                if (Time.time > nextShotTime)
                {
                    animator.SetTrigger("Attack");
                    ShootArrow();
                    nextShotTime = Time.time + timeBetweenShots;
                }
            }
        }
        else
        {
            animator.SetBool("IsRunning", false);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        if (GameManager.Instance != null)
            GameManager.Instance.ShowDamage(damage, transform.position);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(gameObject);
    }

    void FindClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closestDistance = Mathf.Infinity;
        GameObject closestEnemy = null;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }
        if (closestEnemy != null) target = closestEnemy.transform;
    }

    void MoveTowardsEnemy()
    {
        transform.position = Vector2.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        if (target.position.x > transform.position.x) transform.localScale = new Vector3(1, 1, 1);
        else transform.localScale = new Vector3(-1, 1, 1);
    }

    void StopMoving()
    {
        if (target != null)
        {
             if (target.position.x > transform.position.x) transform.localScale = new Vector3(1, 1, 1);
            else transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    // Новий метод для повороту "лука" (точки FirePoint)
    void AimAtTarget()
    {
        if (firePoint != null && target != null)
        {
            Vector3 direction = target.position - firePoint.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    public void ShootArrow()
    {
        if (arrowPrefab != null && firePoint != null)
        {
            Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
        }
    }
}