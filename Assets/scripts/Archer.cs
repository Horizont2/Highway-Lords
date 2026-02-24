using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Archer : MonoBehaviour
{
    [Header("UI")]
    public HealthBar healthBar;

    [Header("Характеристики")]
    public float speed = 2.5f;
    public float attackRange = 5f; 
    public float attackRate = 0.8f;
    public int maxHealth = 80;

    [Header("Навігація")]
    public LayerMask obstacleLayer; 
    public float checkDistance = 1.5f; 
    public float avoidanceForce = 2.0f; 

    [Header("Стрільба")]
    public GameObject arrowPrefab;
    public Transform firePoint;

    [Header("Компоненти")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    public int currentHealth;
    private int myDamage;
    private float nextAttackTime = 0f;
    private Vector3 originalScale;
    private Transform target; 
    private Rigidbody2D rb; 
    private bool isDead = false;

    // Патруль
    private Vector3 startPoint;
    public float patrolRadius = 2.5f;
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
        if(animator == null) animator = GetComponent<Animator>(); 
        
        // Отримуємо свої стати (категорію Ranged)
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

        // Отримуємо базовий урон (враховуючи рівень і Бойовий Ріг)
        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetArcherDamage();
            if (GameManager.Instance.archerLevel > 1 && spriteRenderer != null) 
                spriteRenderer.color = new Color(0.9f, 1f, 0.9f);
        }
        else myDamage = 8;
    }

    public void LoadState(int savedHealth)
    {
        currentHealth = savedHealth;
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
    }

    void Update()
    {
        if (isDead) return;
        
        // Оновлюємо урон щокадру, щоб врахувати активацію Рогу (GlobalDamageMultiplier)
        if (GameManager.Instance != null)
        {
            myDamage = GameManager.Instance.GetArcherDamage();
        }
        
        if (target != null && (target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy)) 
            target = null;

        FindNearestTarget();

        if (target != null)
        {
            EngageEnemy(target);
            isWaiting = false;
        }
        else
        {
            Patrol();
        }
    }

    void EngageEnemy(Transform targetTransform)
    {
        float distance = Vector2.Distance(transform.position, targetTransform.position);
        FlipSprite(targetTransform.position.x);

        if (distance <= attackRange)
        {
            rb.linearVelocity = Vector2.zero; 
            if (animator) animator.SetBool("IsMoving", false);

            if (Time.time >= nextAttackTime)
            {
                StartAttack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
        else
        {
            MoveTo(targetTransform.position);
        }
    }

    void StartAttack()
    {
        if (animator != null) animator.SetTrigger("Attack");
    }

    // === ВИКЛИКАЄТЬСЯ З АНІМАЦІЇ ===
    public void ShootArrow()
    {
        if (isDead) return;

        if (target == null || target.CompareTag("Untagged") || !target.gameObject.activeInHierarchy) 
            return;

        float dist = Vector2.Distance(transform.position, target.position);
        
        if (dist > attackRange + 1.5f) 
        {
            return; 
        }

        if (arrowPrefab != null && firePoint != null)
        {
            if (SoundManager.Instance != null) 
                SoundManager.Instance.PlaySFX(SoundManager.Instance.arrowShoot);

            GameObject arrowGO = Instantiate(arrowPrefab, firePoint.position, Quaternion.identity);
            Arrow arrowScript = arrowGO.GetComponent<Arrow>();
            
            if (arrowScript != null)
            {
                // === НОВА ЛОГІКА РОЗРАХУНКУ УРОНУ ===
                int finalDamage = myDamage;

                // Перевіряємо бонуси категорій (наприклад, по кавалерії)
                if (myStats != null)
                {
                    UnitStats targetStats = target.GetComponent<UnitStats>();
                    if (targetStats != null)
                    {
                        float multiplier = GameManager.GetDamageMultiplier(myStats.category, targetStats.category);
                        finalDamage = Mathf.RoundToInt(myDamage * multiplier);
                    }
                }
                // =====================================

                // Передаємо стрілі вже готовий, порахований урон
                arrowScript.Initialize(target, finalDamage);
            }
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

        if (Vector2.Distance(transform.position, currentPatrolTarget) < 0.2f)
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
        if (targetX > transform.position.x) 
            transform.localScale = new Vector3(absX, originalScale.y, originalScale.z); 
        else 
            transform.localScale = new Vector3(-absX, originalScale.y, originalScale.z); 
    }

    void FindNearestTarget()
    {
        if (target != null 
            && !target.CompareTag("Untagged") 
            && target.gameObject.activeInHierarchy
            && Vector2.Distance(transform.position, target.position) <= attackRange) 
        {
            return;
        }

        float shortestDist = Mathf.Infinity;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;

        foreach (GameObject go in enemies)
        {
            if (go.CompareTag("Untagged")) continue;
            float dist = Vector2.Distance(transform.position, go.transform.position);
            
            if (dist < shortestDist && dist <= attackRange * 2.0f) 
            {
                shortestDist = dist;
                nearest = go.transform;
            }
        }
        target = nearest;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        
        if (healthBar != null) healthBar.SetHealth(currentHealth, maxHealth);
        
        // === НОВИЙ ВІЗУАЛ (POPUP) ===
        // Викликаємо новий метод для тексту
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