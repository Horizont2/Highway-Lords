using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Knight : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 3.0f;
    public float attackRange = 0.8f;
    public float attackRate = 1f;
    public int maxHealth = 120;

    [Header("Навігація (Обхід)")]
    public LayerMask obstacleLayer; // ОБОВ'ЯЗКОВО вибери шар Building
    public float avoidanceForce = 2.0f; // Сила відштовхування від стін
    public float checkDistance = 1.5f;

    [Header("Компоненти")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    // Public для збереження
    public int currentHealth; 
    
    private int myDamage;
    private float nextAttackTime = 0f;
    private Vector3 originalScale;

    // Цілі (Пріоритетна система)
    private Cart targetCart;
    private Guard targetGuard;
    private EnemyArcher targetArcher; 
    private Boss targetBoss; 

    // Патруль
    private Vector3 startPoint;
    public float patrolRadius = 3f;
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
        
        // Отримуємо свої стати (Standard)
        myStats = GetComponent<UnitStats>();

        startPoint = transform.position;
        originalScale = transform.localScale;

        // Ініціалізація здоров'я
        if (currentHealth <= 0) currentHealth = maxHealth;

        SetNewPatrolTarget();

        if (healthBar != null)
        {
            healthBar.targetTransform = transform;
            healthBar.SetHealth(currentHealth, maxHealth);
        }

        // === ОТРИМАННЯ НОВОГО УРОНУ (Лінійний ріст) ===
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateUI();
            
            // Беремо урон з нової формули: 10 + (lvl * 5)
            myDamage = GameManager.Instance.GetKnightDamage();

            // Візуалізація рівня (червоніший відтінок)
            if (GameManager.Instance.knightLevel > 1 && spriteRenderer != null) 
                spriteRenderer.color = new Color(1f, 0.9f, 0.9f);
        }
        else
        {
            myDamage = 10;
        }
    }

    public void LoadState(int savedHealth)
    {
        currentHealth = savedHealth;
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null && !isDead)
        {
            GameManager.Instance.currentUnits--;
            GameManager.Instance.UpdateUI();
        }
    }

    void Update()
    {
        if (isDead) return;

        // Оновлюємо урон щокадру, щоб врахувати активацію Рогу
        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetKnightDamage();
        }

        // Перевірка на зникнення цілей (якщо померли або зникли)
        if (targetBoss != null && (targetBoss.CompareTag("Untagged") || !targetBoss.gameObject.activeInHierarchy)) targetBoss = null;
        if (targetGuard != null && (targetGuard.CompareTag("Untagged") || !targetGuard.gameObject.activeInHierarchy)) targetGuard = null;
        if (targetArcher != null && (targetArcher.CompareTag("Untagged") || !targetArcher.gameObject.activeInHierarchy)) targetArcher = null;
        if (targetCart != null && (targetCart.CompareTag("Untagged") || !targetCart.gameObject.activeInHierarchy)) targetCart = null;

        FindNearestTarget();

        // Визначаємо поточну активну ціль за пріоритетом
        Transform currentTarget = null;
        if (targetBoss != null) currentTarget = targetBoss.transform; 
        else if (targetGuard != null) currentTarget = targetGuard.transform;
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
        Vector3 moveDestination = target.position;
        float effectiveAttackRange = attackRange;
        bool isFightingCart = (targetCart != null && target == targetCart.transform);

        // Логіка обходу воза (флангування), щоб не товпитися в одній точці
        if (isFightingCart)
        {
            effectiveAttackRange = attackRange + 1.5f; 
            float flankOffset = 1.5f; 

            if (transform.position.y > target.position.y) moveDestination.y += flankOffset; 
            else moveDestination.y -= flankOffset; 
            
            if (transform.position.x < target.position.x) moveDestination.x -= 0.5f;
            else moveDestination.x += 0.5f;
        }

        FlipSprite(target.position.x);
        
        float distance = Vector2.Distance(transform.position, target.position);
        
        // Перевірка вирівнювання по Y для воза
        bool alignedY = true;
        if (isFightingCart)
        {
            if (Mathf.Abs(transform.position.y - moveDestination.y) > 0.2f) alignedY = false;
        }

        if (distance <= effectiveAttackRange && alignedY)
        {
            // Зупиняємось і атакуємо
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
            MoveTo(moveDestination);
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
        float randomX = startPoint.x + randomPoint.x;
        float randomY = startPoint.y + (randomPoint.y * 0.4f);
        currentPatrolTarget = new Vector3(randomX, randomY, 0);
    }

    // === ОНОВЛЕНИЙ МЕТОД РУХУ ===
    void MoveTo(Vector3 targetPosition)
    {
        // 1. Dead Zone: Якщо ми вже на місці, не рухаємось (фікс смикання)
        if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator) animator.SetBool("IsMoving", false);
            return;
        }

        // Обмеження руху кордонами екрану (якщо є в GameManager)
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

        // === ОБХІД ПЕРЕШКОД (Raycast) ===
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
        // Якщо у нас вже є ціль, не шукаємо нову (щоб не перемикатись постійно)
        if (targetBoss != null || targetGuard != null || targetArcher != null || targetCart != null) return;

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
            
            // Не біжимо за ворогами, які ще далеко за лінією фронту
            if (GameManager.Instance != null && GameManager.Instance.engagementLine != null)
                if (go.transform.position.x > GameManager.Instance.engagementLine.position.x) continue;
            
            if (go.transform.position.x > maxX || go.transform.position.x < minX) continue;

            float dist = Vector2.Distance(transform.position, go.transform.position);

            // ПРІОРИТЕТИ: Бос -> Гвардієць -> Лучник -> Віз
            if (go.GetComponent<Boss>()) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetBoss = go.GetComponent<Boss>(); targetGuard = null; targetArcher = null; targetCart = null; } 
                continue; 
            }
            if (go.GetComponent<Guard>() && !targetBoss) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetGuard = go.GetComponent<Guard>(); targetArcher = null; targetCart = null; } 
                continue; 
            }
            if (go.GetComponent<EnemyArcher>() && !targetBoss && !targetGuard) 
            { 
                if (dist < shortestDist) { shortestDist = dist; targetArcher = go.GetComponent<EnemyArcher>(); targetCart = null; } 
                continue; 
            }
            if (go.GetComponent<Cart>() && !targetBoss && !targetGuard && !targetArcher && dist < shortestDist) 
            { 
                shortestDist = dist; targetCart = go.GetComponent<Cart>(); 
            }
        }
    }

    void Attack()
    {
        if (animator) animator.SetTrigger("Attack");
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);
        
        // === НОВА ЛОГІКА: Розрахунок урону з урахуванням типів (Камінь-Ножиці-Папір) ===
        int finalDamage = myDamage;
        UnitStats targetStats = null;

        // Визначаємо, кого саме ми б'ємо, щоб отримати його стати
        if (targetBoss != null) targetStats = targetBoss.GetComponent<UnitStats>();
        else if (targetGuard != null) targetStats = targetGuard.GetComponent<UnitStats>();
        else if (targetArcher != null) targetArcher.GetComponent<UnitStats>();
        // Для воза статів може не бути, або вони "Building"

        if (myStats != null && targetStats != null)
        {
            float multiplier = GameManager.GetDamageMultiplier(myStats.category, targetStats.category);
            finalDamage = Mathf.RoundToInt(myDamage * multiplier);
        }
        // ==============================================================================

        // Наносимо фінальний урон конкретній цілі
        if (targetBoss != null) targetBoss.TakeDamage(finalDamage);
        else if (targetGuard != null) targetGuard.TakeDamage(finalDamage);
        else if (targetArcher != null) targetArcher.TakeDamage(finalDamage);
        else if (targetCart != null) targetCart.TakeDamage(finalDamage);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.knightHit);
        
        // === POPUP замість ShowDamage ===
        GameManager.CreateDamagePopup(transform.position, damage);
        
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        gameObject.tag = "Untagged";

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false; 

        // Оновлюємо лічильник живих юнітів
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentUnits--;
            GameManager.Instance.UpdateUI();
        }

        if (healthBar != null) healthBar.gameObject.SetActive(false);
        Transform shadow = transform.Find("Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);

        // === СКИНУТИ АНІМАЦІЮ ПЕРЕД СМЕРТЮ ===
        if (animator)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = false;
        }
        
        transform.Rotate(0, 0, -90);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.6f, 0.6f, 0.6f);
            spriteRenderer.sortingOrder = 0; 
        }

        Destroy(this);
        Destroy(gameObject, 10f);
    }
}