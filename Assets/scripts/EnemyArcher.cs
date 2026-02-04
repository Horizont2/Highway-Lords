using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyArcher : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 2.5f;
    public float attackRange = 6.5f;
    public float timeBetweenShots = 2.0f;
    public int damage = 10;
    public int maxHealth = 40;
    public int goldReward = 15;
    
    // Дистанція за спиною Гвардійця
    private float safeDistanceBehindTank = 2.0f;

    [Header("Стрільба")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    private int currentHealth;
    private Animator animator;
    private Transform target;
    private float nextShotTime;
    
    private Vector3 originalScale;
    private int _maxHealth;
    private Rigidbody2D rb;
    private bool isDead = false;
    private SpriteRenderer spriteRenderer;

    private Transform myCart;

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        if (GameManager.Instance != null)
        {
            maxHealth += (GameManager.Instance.currentWave - 1) * 5;
            damage += (GameManager.Instance.currentWave - 1) * 2;
            GameManager.Instance.RegisterEnemy();
        }
        
        currentHealth = maxHealth;
        _maxHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.targetTransform = transform;
            healthBar.SetHealth(currentHealth, _maxHealth);
        }

        Cart cartScript = FindFirstObjectByType<Cart>();
        if (cartScript != null) myCart = cartScript.transform;
    }

    void Update()
    {
        if (isDead) return;

        if (target != null && target.CompareTag("Untagged")) target = null;

        FindTarget();

        // За замовчуванням стоїмо
        Vector2 finalVelocity = Vector2.zero;
        bool shouldMove = false;

        if (target != null)
        {
            float distance = Vector2.Distance(transform.position, target.position);
            FaceTarget(target.position);

            if (distance > attackRange)
            {
                // Ціль далеко - треба йти
                // 1. ПЕРЕВІРКА: Чи не обганяємо ми Гвардійця?
                if (ShouldWaitForTank())
                {
                    // Стоїмо, чекаємо танка
                    finalVelocity = Vector2.zero;
                    shouldMove = false;
                    
                    // Але якщо ВІЗ їде на нас - ухиляємось навіть стоячи!
                    if (IsBlockingCart())
                    {
                         Vector2 evadeDir = (transform.position.y > myCart.position.y) ? Vector2.up : Vector2.down;
                         finalVelocity = evadeDir * speed;
                         shouldMove = true;
                    }
                }
                else
                {
                    // Ідемо до цілі
                    Vector2 direction = (target.position - transform.position).normalized;
                    
                    // Ухилення від воза
                    if (IsBlockingCart())
                    {
                        float yDiff = transform.position.y - myCart.position.y;
                        float avoidY = yDiff > 0 ? 1f : -1f;
                        direction.y += avoidY * 2.5f;
                        direction.Normalize();
                    }

                    finalVelocity = direction * speed;
                    shouldMove = true;
                    if (firePoint) firePoint.localRotation = Quaternion.identity;
                }
            }
            else
            {
                // В радіусі атаки. Стоїмо і стріляємо.
                if (IsBlockingCart())
                {
                    Vector2 evadeDir = (transform.position.y > myCart.position.y) ? Vector2.up : Vector2.down;
                    finalVelocity = evadeDir * speed;
                    shouldMove = true;
                }
                else
                {
                    AimAtTarget();
                    if (Time.time > nextShotTime)
                    {
                        animator.SetTrigger("Attack"); 
                        nextShotTime = Time.time + timeBetweenShots;
                    }
                }
            }
        }
        else
        {
            // Рух до замку (немає цілей)
            if (ShouldWaitForTank())
            {
                 // Навіть якщо немає цілей, не обганяємо танка
                 finalVelocity = Vector2.zero;
                 shouldMove = false;
            }
            else
            {
                Vector3 destination = transform.position + Vector3.left;
                FaceTarget(destination);
                Vector2 direction = (destination - transform.position).normalized;
                
                if (IsBlockingCart())
                {
                    float yDiff = transform.position.y - myCart.position.y;
                    float avoidY = yDiff > 0 ? 1f : -1f;
                    direction.y += avoidY * 2.5f;
                    direction.Normalize();
                }

                finalVelocity = direction * speed;
                shouldMove = true;
            }
        }

        rb.linearVelocity = finalVelocity;
        if (animator) animator.SetBool("IsRunning", shouldMove);
    }

    // === НОВА ЛОГІКА ПОЗИЦІОНУВАННЯ ДЛЯ ВОРОГА ===
    bool ShouldWaitForTank()
    {
        // Шукаємо Гвардійців
        Guard[] guards = FindObjectsByType<Guard>(FindObjectsSortMode.None);
        
        // Вороги йдуть вліво, тому "передній" край - це найменший X
        float forwardMostX = 9999f; 
        bool hasTank = false;

        foreach (Guard g in guards)
        {
            if (g.CompareTag("Untagged")) continue;

            if (g.transform.position.x < forwardMostX)
            {
                forwardMostX = g.transform.position.x;
                hasTank = true;
            }
        }

        if (hasTank)
        {
            // Якщо ми стоїмо лівіше (попереду) точки "Спина Гвардійця"
            // Точка спини = Позиція Гвардійця + Безпечна дистанція (бо рух вліво)
            if (transform.position.x < (forwardMostX + safeDistanceBehindTank))
            {
                return true; // Треба чекати
            }
        }

        return false; 
    }

    bool IsBlockingCart()
    {
        if (myCart == null) return false;
        if (transform.position.x < myCart.position.x && Vector2.Distance(transform.position, myCart.position) < 3.5f)
        {
            if (Mathf.Abs(transform.position.y - myCart.position.y) < 1.2f) return true;
        }
        return false;
    }

    void FindTarget()
    {
        if (target != null && !target.CompareTag("Untagged")) return;

        float minDistance = Mathf.Infinity;
        Transform closestTarget = null;

        Knight[] knights = FindObjectsByType<Knight>(FindObjectsSortMode.None);
        foreach (Knight k in knights) {
            float dist = Vector2.Distance(transform.position, k.transform.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = k.transform; }
        }

        Archer[] archers = FindObjectsByType<Archer>(FindObjectsSortMode.None);
        foreach (Archer a in archers) {
            float dist = Vector2.Distance(transform.position, a.transform.position);
            if (dist < minDistance) { minDistance = dist; closestTarget = a.transform; }
        }

        if (closestTarget == null && GameManager.Instance != null && GameManager.Instance.castle != null)
        {
            closestTarget = GameManager.Instance.castle.transform;
        }
        target = closestTarget;
    }

    void FaceTarget(Vector3 targetPos)
    {
        float absX = Mathf.Abs(originalScale.x);
        if (targetPos.x > transform.position.x)
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
        else
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
    }

    void AimAtTarget()
    {
        if (firePoint != null && target != null)
        {
            Vector3 direction = target.position - firePoint.position;
            if (transform.localScale.x < 0) { direction.x = -direction.x; direction.y = -direction.y; }
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            firePoint.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    public void ShootArrow()
    {
        if (target == null) return;
        if (arrowPrefab != null && firePoint != null)
        {
            AimAtTarget(); 
            GameObject arrowObj = Instantiate(arrowPrefab, firePoint.position, firePoint.rotation);
            EnemyProjectile p = arrowObj.GetComponent<EnemyProjectile>();
            if (p != null) p.Initialize(target.position, damage);
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot);
        }
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;
        currentHealth -= damageAmount;
        if (healthBar != null) healthBar.SetHealth(currentHealth, _maxHealth);
        if (GameManager.Instance != null) GameManager.Instance.ShowDamage(damageAmount, transform.position);
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        gameObject.tag = "Untagged";
        
        if (healthBar != null) healthBar.gameObject.SetActive(false);
        Transform shadow = transform.Find("Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterEnemy();
            GameManager.Instance.AddResource(ResourceType.Gold, goldReward);
            GameManager.Instance.ShowResourcePopup(ResourceType.Gold, goldReward, transform.position);
        }
        
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.enemyDeath);

        if (animator) animator.enabled = false;
        if (rb) rb.simulated = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col) col.enabled = false;

        transform.Rotate(0, 0, -90);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f);
            spriteRenderer.sortingOrder = 0; 
        }

        this.enabled = false;
        Destroy(gameObject, 10f);
    }
}