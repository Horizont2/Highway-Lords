using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Характеристики")]
    public float speed = 0.8f;      // Бос повільний
    public float attackRange = 1.8f; // Б'є далі, ніж звичайні юніти
    public int damage = 40;         // Сильний удар
    public int maxHealth = 500;     // Багато HP
    public int goldReward = 300;

    [Header("Компоненти")]
    public HealthBar healthBar;     // Перетягни сюди Canvas з хелсбаром
    
    private Animator animator;
    private Transform target;
    private int currentHealth;
    private float nextAttackTime = 0f;
    private float attackCooldown = 2.5f; // Б'є рідко
    private Vector3 originalScale; // Для правильного повороту

    void Start()
    {
        animator = GetComponent<Animator>();
        originalScale = transform.localScale; // Запам'ятовуємо розмір

        // Скалювання сили боса від хвилі
        if (GameManager.Instance != null)
        {
            int boost = GameManager.Instance.currentWave / 10; // Кожні 10 хвиль стає сильнішим
            maxHealth += boost * 250;
            damage += boost * 10;
        }
        
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.targetTransform = transform;
            healthBar.SetHealth(maxHealth, maxHealth);
        }
    }

    void Update()
    {
        FindTarget();

        if (target != null)
        {
            FaceTarget(target.position);
            float distance = Vector2.Distance(transform.position, target.position);

            if (distance <= attackRange)
            {
                // Атака
                if (animator) animator.SetBool("IsRunning", false);

                if (Time.time >= nextAttackTime)
                {
                    Attack();
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
            else
            {
                // Рух
                MoveTowards(target.position);
            }
        }
        else
        {
            // Якщо немає героїв — йдемо ламати замок
            Vector3 castlePos = transform.position + Vector3.left * 10f; 
            if (GameManager.Instance != null && GameManager.Instance.castle != null)
                castlePos = GameManager.Instance.castle.transform.position;

            FaceTarget(castlePos);
            MoveTowards(castlePos);
        }
    }

    void FindTarget()
    {
        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;

        // Шукаємо найближчого героя (Лицар або Лучник)
        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (var k in knights) CheckDist(k.transform, ref minDistance, ref closestTarget);

        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (var a in archers) CheckDist(a.transform, ref minDistance, ref closestTarget);

        // Якщо героїв немає - йдемо на Замок
        if (closestTarget == null && GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            closestTarget = GameManager.Instance.castle.transform;
        }
        target = closestTarget;
    }

    void CheckDist(Transform t, ref float minDesc, ref Transform closest)
    {
        float dist = Vector2.Distance(transform.position, t.position);
        if (dist < minDesc) { minDesc = dist; closest = t; }
    }

    void MoveTowards(Vector3 destination)
    {
        if (animator) animator.SetBool("IsRunning", true);
        transform.position = Vector2.MoveTowards(transform.position, destination, speed * Time.deltaTime);
    }

    void FaceTarget(Vector3 targetPos)
    {
        float absX = Mathf.Abs(originalScale.x);
        // Якщо ціль зліва -> дивимося вліво (-absX)
        if (targetPos.x < transform.position.x)
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z);
        else
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z);
    }

    void Attack()
    {
        if (animator) animator.SetTrigger("Attack");

        // Бос б'є по площі (всіх, хто в радіусі атаки)
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, attackRange);
        
        foreach (Collider2D hit in hitEnemies)
        {
            Knight k = hit.GetComponent<Knight>();
            if (k) k.TakeDamage(damage);

            Archer a = hit.GetComponent<Archer>();
            if (a) a.TakeDamage(damage);

            Castle c = hit.GetComponent<Castle>();
            if (c) c.TakeDamage(damage);
        }
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
        if (GameManager.Instance != null) GameManager.Instance.ShowDamage(damage, transform.position);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddResource(ResourceType.Gold, goldReward);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldReward, transform.position);
        }
        Destroy(gameObject);
    }
}