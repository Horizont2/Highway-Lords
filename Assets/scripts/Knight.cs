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

    [Header("Компоненти")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    // === ЗМІНА 1: currentHealth тепер public для збереження ===
    public int currentHealth; 
    
    private int myDamage;
    private float nextAttackTime = 0f;
    private Vector3 originalScale;

    // Цілі
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

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic; 
        rb.gravityScale = 0; 
        rb.freezeRotation = true; 

        if(spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        startPoint = transform.position;
        originalScale = transform.localScale;

        // === ЗМІНА 2: Логіка ініціалізації здоров'я ===
        // Якщо це новий юніт (currentHealth == 0), даємо макс.
        // Якщо завантажений (currentHealth > 0), не чіпаємо.
        if (currentHealth <= 0) currentHealth = maxHealth;

        SetNewPatrolTarget();

        if (healthBar != null)
        {
            healthBar.targetTransform = transform;
            healthBar.SetHealth(currentHealth, maxHealth);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateUI();
            myDamage = GameManager.Instance.GetKnightDamage();

            if (GameManager.Instance.knightLevel > 1 && spriteRenderer != null) 
                spriteRenderer.color = new Color(1f, 0.9f, 0.9f);
        }
        else
        {
            myDamage = 10;
        }
    }

    // === ЗМІНА 3: Метод завантаження ===
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

        if (targetBoss != null && targetBoss.CompareTag("Untagged")) targetBoss = null;
        if (targetGuard != null && targetGuard.CompareTag("Untagged")) targetGuard = null;
        if (targetArcher != null && targetArcher.CompareTag("Untagged")) targetArcher = null;
        if (targetCart != null && targetCart.CompareTag("Untagged")) targetCart = null;

        FindNearestTarget();

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
        bool alignedY = true;
        
        if (isFightingCart)
        {
            if (Mathf.Abs(transform.position.y - moveDestination.y) > 0.2f) alignedY = false;
        }

        if (distance <= effectiveAttackRange && alignedY)
        {
            rb.linearVelocity = Vector2.zero; // velocity краще для сумісності з Save System при старті
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

    void MoveTo(Vector3 targetPosition)
    {
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
            if (GameManager.Instance != null && GameManager.Instance.engagementLine != null)
                if (go.transform.position.x > GameManager.Instance.engagementLine.position.x) continue;
            if (go.transform.position.x > maxX || go.transform.position.x < minX) continue;

            float dist = Vector2.Distance(transform.position, go.transform.position);

            if (go.GetComponent<Boss>()) { if (dist < shortestDist) { shortestDist = dist; targetBoss = go.GetComponent<Boss>(); targetGuard = null; targetArcher = null; targetCart = null; } continue; }
            if (go.GetComponent<Guard>() && !targetBoss) { if (dist < shortestDist) { shortestDist = dist; targetGuard = go.GetComponent<Guard>(); targetArcher = null; targetCart = null; } continue; }
            if (go.GetComponent<EnemyArcher>() && !targetBoss && !targetGuard) { if (dist < shortestDist) { shortestDist = dist; targetArcher = go.GetComponent<EnemyArcher>(); targetCart = null; } continue; }
            if (go.GetComponent<Cart>() && !targetBoss && !targetGuard && !targetArcher && dist < shortestDist) { shortestDist = dist; targetCart = go.GetComponent<Cart>(); }
        }
    }

    void Attack()
    {
        if (animator) animator.SetTrigger("Attack");
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.swordHit);
        
        if (targetBoss != null) targetBoss.TakeDamage(myDamage);
        else if (targetGuard != null) targetGuard.TakeDamage(myDamage);
        else if (targetArcher != null) targetArcher.TakeDamage(myDamage);
        else if (targetCart != null) targetCart.TakeDamage(myDamage);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SoundManager.Instance.knightHit);
        if (GameManager.Instance != null) GameManager.Instance.ShowDamage(damage, transform.position);
        
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentUnits--;
            GameManager.Instance.UpdateUI();
        }

        if (healthBar != null) healthBar.gameObject.SetActive(false);
        Transform shadow = transform.Find("Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);

        if (animator) animator.enabled = false;
        
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