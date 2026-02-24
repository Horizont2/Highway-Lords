using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Spearman : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 2.2f;          // Повільніший за лицаря
    public float attackRange = 2.2f;    // Велика дальність (б'є з другого ряду)
    public float attackRate = 0.8f;     // Середня швидкість атаки
    public int maxHealth = 90;          // Трохи менше здоров'я ніж у лицаря

    [Header("Навігація")]
    public LayerMask obstacleLayer; 
    public float avoidanceForce = 2.0f; 
    public float checkDistance = 1.5f;

    [Header("Компоненти")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    public int currentHealth; 
    
    private int myDamage;
    private float nextAttackTime = 0f;
    private Vector3 originalScale;

    // Цілі
    private Cart targetCart;
    private Guard targetGuard;
    private EnemyArcher targetArcher; 
    private EnemySpearman targetEnemySpearman;
    private Boss targetBoss; 
    private EnemyHorse targetHorse; // + НОВЕ: Ціль для Списоносця

    // Патруль
    private Vector3 startPoint;
    public float patrolRadius = 2.5f;
    private Rigidbody2D rb; 
    private bool isDead = false;

    private Vector3 currentPatrolTarget;
    private float patrolWaitTimer = 0f;
    private bool isWaiting = false;

    // Кешування статів
    private UnitStats myStats;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic; 
        rb.gravityScale = 0; 
        rb.freezeRotation = true; 

        if(spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Отримуємо свої стати (Spearman)
        myStats = GetComponent<UnitStats>();

        startPoint = transform.position;
        originalScale = transform.localScale;

        if (currentHealth <= 0) currentHealth = maxHealth;

        SetNewPatrolTarget();

        if (healthBar != null)
        {
            healthBar.targetTransform = transform;
            healthBar.SetHealth(currentHealth, maxHealth);
        }

        // === БАЛАНС: ОКРЕМА ПРОКАЧКА ===
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateUI();
            
            // Беремо урон з нової функції для Списоносця
            myDamage = GameManager.Instance.GetSpearmanDamage();

            // Візуалізація рівня (синій відтінок)
            if (GameManager.Instance.spearmanLevel > 1 && spriteRenderer != null) 
                spriteRenderer.color = new Color(0.8f, 0.9f, 1f);
        }
        else
        {
            myDamage = 12;
        }
    }

    public void LoadState(int savedHealth)
    {
        currentHealth = savedHealth;
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null && !isDead && !GameManager.Instance.isResettingUnits)
        {
            GameManager.Instance.currentUnits--;
            GameManager.Instance.UpdateUI();
        }
    }

    void Update()
    {
        if (isDead) return;

        // Оновлюємо урон (для Рогу)
        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetSpearmanDamage();
        }

        if (targetBoss != null && (targetBoss.CompareTag("Untagged") || !targetBoss.gameObject.activeInHierarchy)) targetBoss = null;
        if (targetGuard != null && (targetGuard.CompareTag("Untagged") || !targetGuard.gameObject.activeInHierarchy)) targetGuard = null;
        if (targetArcher != null && (targetArcher.CompareTag("Untagged") || !targetArcher.gameObject.activeInHierarchy)) targetArcher = null;
        if (targetEnemySpearman != null && (targetEnemySpearman.CompareTag("Untagged") || !targetEnemySpearman.gameObject.activeInHierarchy)) targetEnemySpearman = null;
        if (targetCart != null && (targetCart.CompareTag("Untagged") || !targetCart.gameObject.activeInHierarchy)) targetCart = null;
        if (targetHorse != null && (targetHorse.CompareTag("Untagged") || !targetHorse.gameObject.activeInHierarchy)) targetHorse = null;

        FindNearestTarget();

        Transform currentTarget = null;
        // Пріоритет Списоносця: Бос -> Кінь -> Гвардієць -> Інші
        if (targetBoss != null) currentTarget = targetBoss.transform; 
        else if (targetHorse != null) currentTarget = targetHorse.transform;
        else if (targetGuard != null) currentTarget = targetGuard.transform;
        else if (targetEnemySpearman != null) currentTarget = targetEnemySpearman.transform;
        else if (targetArcher != null) currentTarget = targetArcher.transform;
        else if (targetCart != null) currentTarget = targetCart.transform;

        if (currentTarget != null)
        {
            EngageEnemy(currentTarget);
            isWaiting = false; 
        }
        else
        {
            Patrol();
        }
    }

    void EngageEnemy(Transform target)
    {
        // Списоносець тримає дистанцію, йому не треба обходити так сильно
        float distance = Vector2.Distance(transform.position, target.position);
        FlipSprite(target.position.x);

        if (distance <= attackRange)
        {
            rb.linearVelocity = Vector2.zero; 
            if (animator) animator.SetBool("IsMoving", false);
            
            if (Time.time >= nextAttackTime)
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
        else
        {
            MoveTo(target.position);
        }
    }

    void Patrol()
    {
        if (isWaiting)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator) animator.SetBool("IsMoving", false);

            patrolWaitTimer -= Time.deltaTime;
            if (patrolWaitTimer <= 0)
            {
                isWaiting = false;
                SetNewPatrolTarget();
            }
            return;
        }

        float dist = Vector2.Distance(transform.position, currentPatrolTarget);
        if (dist < 0.2f)
        {
            isWaiting = true;
            patrolWaitTimer = Random.Range(1.0f, 3.0f);
        }
        else
        {
            MoveTo(currentPatrolTarget);
        }
    }

    void SetNewPatrolTarget()
    {
        Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
        currentPatrolTarget = startPoint + (Vector3)randomPoint;
    }

    void MoveTo(Vector3 targetPosition)
    {
        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator) animator.SetBool("IsMoving", false);
            return;
        }

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.rightBoundary != null && targetPosition.x > GameManager.Instance.rightBoundary.position.x)
                targetPosition.x = GameManager.Instance.rightBoundary.position.x;
            if (GameManager.Instance.leftBoundary != null && targetPosition.x < GameManager.Instance.leftBoundary.position.x)
                targetPosition.x = GameManager.Instance.leftBoundary.position.x;
        }
        
        if (animator) animator.SetBool("IsMoving", true);
        FlipSprite(targetPosition.x);
        
        Vector2 direction = (targetPosition - transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, checkDistance, obstacleLayer);
        if (hit.collider != null)
        {
            direction += hit.normal * avoidanceForce;
            direction.Normalize(); 
        }

        rb.linearVelocity = direction * speed;
    }

    void FlipSprite(float targetX)
    {
        float absX = Mathf.Abs(originalScale.x);
        if (targetX < transform.position.x) 
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
        else 
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
    }

    void FindNearestTarget()
    {
        if (targetBoss != null || targetGuard != null || targetArcher != null || targetEnemySpearman != null || targetCart != null || targetHorse != null) return;

        float minX = -1000f; float maxX = 1000f;
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.leftBoundary) minX = GameManager.Instance.leftBoundary.position.x - 2f;
            if (GameManager.Instance.rightBoundary) maxX = GameManager.Instance.rightBoundary.position.x + 2f;
        }

        float shortestDist = Mathf.Infinity;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject go in enemies)
        {
            if (go == gameObject) continue;
            if (go.CompareTag("Untagged")) continue;
            
            if (GameManager.Instance != null && GameManager.Instance.engagementLine != null)
                if (go.transform.position.x > GameManager.Instance.engagementLine.position.x) continue;
            
            if (go.transform.position.x > maxX || go.transform.position.x < minX) continue;

            float dist = Vector2.Distance(transform.position, go.transform.position);

            // ПРІОРИТЕТИ (Додано Horse)
            if (go.GetComponent<Boss>()) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetBoss = go.GetComponent<Boss>(); ResetTargets(); } 
                continue; 
            }
            // Списоносець фокусить коней!
            if (go.GetComponent<EnemyHorse>()) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetHorse = go.GetComponent<EnemyHorse>(); ResetTargets(); } 
                continue; 
            }
            if (go.GetComponent<Guard>() && !targetBoss && !targetHorse) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetGuard = go.GetComponent<Guard>(); ResetTargets(); } 
                continue; 
            }
            if (go.GetComponent<EnemySpearman>() && !targetBoss && !targetHorse && !targetGuard)
            {
                if (dist < shortestDist) { shortestDist = dist; targetEnemySpearman = go.GetComponent<EnemySpearman>(); ResetTargets(); }
                continue;
            }
            if (go.GetComponent<EnemyArcher>() && !targetBoss && !targetHorse && !targetGuard && !targetEnemySpearman) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetArcher = go.GetComponent<EnemyArcher>(); ResetTargets(); } 
                continue; 
            }
            if (go.GetComponent<Cart>() && !targetBoss && !targetHorse && !targetGuard && !targetArcher && dist < shortestDist) 
            { 
                shortestDist = dist; targetCart = go.GetComponent<Cart>(); 
            }
        }
    }

    void ResetTargets()
    {
        // Допоміжний метод для скидання інших цілей, коли знайдено пріоритетнішу
        // (Логіка вже є всередині if-ів, але це для чистоти, якщо захочете розширити)
    }

    void Attack()
    {
        if (animator) animator.SetTrigger("Attack");
    }

    // === ВИКЛИКАЄТЬСЯ З АНІМАЦІЇ (Event: Hit) ===
    public void Hit()
    {
        if (isDead) return;
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit); // Або spearHit

        // === НОВА ЛОГІКА УРОНУ ===
        int finalDamage = myDamage;
        UnitStats targetStats = null;
        GameObject targetObj = null;

        // Визначаємо ціль
        if (targetBoss != null) { targetObj = targetBoss.gameObject; targetStats = targetBoss.GetComponent<UnitStats>(); }
        else if (targetHorse != null) { targetObj = targetHorse.gameObject; targetStats = targetHorse.GetComponent<UnitStats>(); }
        else if (targetGuard != null) { targetObj = targetGuard.gameObject; targetStats = targetGuard.GetComponent<UnitStats>(); }
        else if (targetEnemySpearman != null) { targetObj = targetEnemySpearman.gameObject; targetStats = targetEnemySpearman.GetComponent<UnitStats>(); }
        else if (targetArcher != null) { targetObj = targetArcher.gameObject; targetStats = targetArcher.GetComponent<UnitStats>(); }
        else if (targetCart != null) { targetObj = targetCart.gameObject; }

        // Розраховуємо множник (наприклад x2 по конях)
        if (myStats != null && targetStats != null)
        {
            float multiplier = GameManager.GetDamageMultiplier(myStats.category, targetStats.category);
            finalDamage = Mathf.RoundToInt(myDamage * multiplier);
        }
        // =========================
        
        // Наносимо урон
        if (targetBoss != null) targetBoss.TakeDamage(finalDamage);
        else if (targetHorse != null) targetHorse.TakeDamage(finalDamage);
        else if (targetGuard != null) targetGuard.TakeDamage(finalDamage);
        else if (targetEnemySpearman != null) targetEnemySpearman.TakeDamage(finalDamage);
        else if (targetArcher != null) targetArcher.TakeDamage(finalDamage);
        else if (targetCart != null) targetCart.TakeDamage(finalDamage);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
        
        // === POPUP ===
        GameManager.CreateDamagePopup(transform.position, damage);
        
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        gameObject.tag = "Untagged";
        
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Static; }
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        if (GameManager.Instance != null) { GameManager.Instance.currentUnits--; GameManager.Instance.UpdateUI(); }
        if (healthBar != null) healthBar.gameObject.SetActive(false);

        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = false;
        }

        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null) { spriteRenderer.color = Color.gray; spriteRenderer.sortingOrder = 0; }
        
        Destroy(this);
        Destroy(gameObject, 10f);
    }
}